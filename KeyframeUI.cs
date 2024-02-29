using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Documents;


namespace KB30
{
    public partial class MainWindow : Window
    {
        /*********
         *  Keyframes
         */
        public void initializeKeysUI(Slide slide)
        {
            keyframePanel.Children.Clear();
            Keyframes keys = slide.keys;
            for (int i = 0; i < keys.Count; i++)
            {
                Keyframe key = keys[i];
                addKeyframeControl(key);
            }

            selectKeyframe(keys[0]);
        }

        public void addKeyframeControl(Keyframe key, int insertIndex = -1)
        {
            KeyframeControl kfControl = new KeyframeControl(imageExplorerWindow);
            kfControl.DeSelect();
            key.keyframeControl = kfControl;
            if (insertIndex == -1)
            {
                keyframePanel.Children.Add(kfControl);
            }
            else
            {
                keyframePanel.Children.Insert(insertIndex, kfControl);
            }

            kfControl.PreviewMouseUp += delegate (object sender, MouseButtonEventArgs e) { keyFrameClick(sender, e, key); };

            kfControl.xTb.Text = key.x.ToString();
            kfControl.yTb.Text = key.y.ToString();
            kfControl.zoomTb.Text = key.zoomFactor.ToString();
            kfControl.durTb.Text = key.duration.ToString();

            kfControl.xTb.TextChanged += delegate (object sender, TextChangedEventArgs e) { kfControlChangeEvent(sender, e, key); };
            kfControl.yTb.TextChanged += delegate (object sender, TextChangedEventArgs e) { kfControlChangeEvent(sender, e, key); };
            kfControl.zoomTb.TextChanged += delegate (object sender, TextChangedEventArgs e) { kfControlChangeEvent(sender, e, key); };
            kfControl.durTb.TextChanged += delegate (object sender, TextChangedEventArgs e) { kfControlChangeEvent(sender, e, key); };

            kfControl.CMCut.Click += delegate (object sender, RoutedEventArgs e) { cutKeyframeClick(sender, e, key); };
            kfControl.CMPasteLeft.Click += delegate (object sender, RoutedEventArgs e) { pasteKeyframeClick(sender, e, key, LEFT); };
            kfControl.CMPasteRight.Click += delegate (object sender, RoutedEventArgs e) { pasteKeyframeClick(sender, e, key, RIGHT); };
            kfControl.CMDuplicate.Click += delegate (object sender, RoutedEventArgs e) { addKeyframeClick(sender, e); };
            kfControl.CMSwap.Click += delegate (object sender, RoutedEventArgs e) { swapKeyframeClick(sender, e, key); };
            kfControl.KeyframeContextMenu.Opened += delegate (object sender, RoutedEventArgs e) { keyframeContextMenuOpened(sender, e, key); };
        }


        private void unBindKFC(KeyframeControl kfc, Keyframe key)
        {
            if (kfc == null)
            {
                return;
            }
            BindingOperations.ClearBinding(kfc.xTb, TextBox.TextProperty);
            BindingOperations.ClearBinding(kfc.yTb, TextBox.TextProperty);
            BindingOperations.ClearBinding(kfc.zoomTb, TextBox.TextProperty);

            // because clearing the binding clears the targets
            kfc.xTb.Text = key.x.ToString();
            kfc.yTb.Text = key.y.ToString();
            kfc.zoomTb.Text = key.zoomFactor.ToString();
        }

        public void selectKeyframe(int index)
        {
            selectKeyframe(currentSlide.keys[index]);
        }

        public void selectKeyframe(Keyframe key)
        {
            Keyframes keys = currentSlide.keys;
            int keyFrameIndex = keys.IndexOf(key);
            if (currentKeyframeIndex >= 0)
            {
                Keyframe oldKey = keys[currentKeyframeIndex];
                KeyframeControl oldKFControl = oldKey.keyframeControl;
                unBindKFC(oldKFControl, oldKey);
                oldKFControl.DeSelect();
            }

            currentKeyframeIndex = keyFrameIndex;

            imageCropper.updateLayout();
            imageCropper.cropZoom = key.zoomFactor;
            imageCropper.cropX = key.x;
            imageCropper.cropY = key.y;

            imageCropper.updateLayout();

            KeyframeControl kfControl = key.keyframeControl;
            kfControl.Select();

            Binding xBinding = new Binding("cropX")
            {
                Source = imageCropper,
                Mode = BindingMode.OneWay
            };
            kfControl.xTb.SetBinding(TextBox.TextProperty, xBinding);

            Binding yBinding = new Binding("cropY")
            {
                Source = imageCropper,
                Mode = BindingMode.OneWay
            };
            kfControl.yTb.SetBinding(TextBox.TextProperty, yBinding);

            Binding zoomBinding = new Binding("cropZoom")
            {
                Source = imageCropper,
                Mode = BindingMode.OneWay
            };
            kfControl.zoomTb.SetBinding(TextBox.TextProperty, zoomBinding);
        }

        private void kfControlChangeEvent(object sender, TextChangedEventArgs e, Keyframe key)
        {
            Double maybe;
            TextBox tb = (e.Source as TextBox);
            if (Double.TryParse(tb.Text, out maybe))
            {
                if (!Double.IsNaN(maybe))
                {
                    switch (tb.Name)
                    {
                        case "xTb":
                            key.x = maybe;
                            break;
                        case "yTb":
                            key.y = maybe;
                            break;
                        case "durTb":
                            key.duration = maybe;
                            updateCaptionCounts();
                            break;
                        case "zoomTb":
                            key.zoomFactor = maybe;
                            break;
                    }
                }
            }
        }

        private void keyFrameClick(object sender, RoutedEventArgs e, Keyframe key)
        {
            selectKeyframe(key);
        }

        private void addKeyframeClick(object sender, RoutedEventArgs e)
        {
            if (slides.Count == 0) { return; }
            Keyframe currentKey = currentSlide.keys[currentKeyframeIndex];
            Keyframe newKey = currentKey.Clone();
            currentSlide.keys.Insert(currentKeyframeIndex + 1, newKey);
            addKeyframeControl(newKey, currentKeyframeIndex + 1);
            newKey.keyframeControl.durTb.Focus();
            selectKeyframe(newKey);
            updateCaptionCounts();
            kfAddedHistory(currentSlide.keys.IndexOf(newKey));
        }


        private void pasteKeyframeClick(object sender, RoutedEventArgs e, Keyframe key, int direction)
        {
            pasteKeyframe(key, direction);
        }

        private void swapKeyframeClick(object sender, RoutedEventArgs e, Keyframe key)
        {
            Keyframes keys = currentSlide.keys;
            if (keys.Count < 2) { return; }
            cutKeyframe(keys.Last());
            pasteKeyframe(keys.First(), LEFT);
            cutKeyframe(keys[1]);
            pasteKeyframe(keys.Last(), RIGHT);
            var temp = keys.First().keyframeControl.durTb.Text;
            keys.First().keyframeControl.durTb.Text = keys.Last().keyframeControl.durTb.Text;
            keys.Last().keyframeControl.durTb.Text = temp;
        }

        private void pasteKeyframe(Keyframe insertKey, int direction)
        {
            if (clipboardKey == null) { return; }

            var insertIndex = currentSlide.keys.IndexOf(insertKey);
            if (direction == RIGHT)
            {
                insertIndex += 1;
            }
            placeKeyframe(clipboardKey, insertIndex);
            clipboardKey = null;
            kfAddedHistory(insertIndex);
        }
        private void placeKeyframe(Keyframe key, int insertIndex)
        {
            Keyframes keys = currentSlide.keys;

            keys.Insert(insertIndex, key);
            keyframePanel.Children.Insert(insertIndex, key.keyframeControl);
            if (currentKeyframeIndex >= insertIndex)
            {
                currentKeyframeIndex++;
            }
            selectKeyframe(key);
            updateCaptionCounts();
        }

        private void cutKeyframeClick(object sender, RoutedEventArgs e, Keyframe key)
        {
            cutKeyframe(key);
        }
        private void cutKeyframe(Keyframe key)
        {
            Keyframes keys = currentSlide.keys;
            if (keys.Count == 1)
            {
                MessageBox.Show("At least one keyframe is required");
                return;
            }
            kfDeletedHistory(key, keys.IndexOf(key));
            deleteKeyframe(key);
            clipboardKey = key;
        }
        private void deleteKeyframe(Keyframe key)
        {
            Keyframes keys = currentSlide.keys;
            var victimIndex = keys.IndexOf(key);

            if (currentKeyframeIndex == victimIndex)
            {
                if (currentKeyframeIndex == (keys.Count - 1))
                {
                    selectKeyframe(keys[currentKeyframeIndex - 1]);
                }
                else
                {
                    selectKeyframe(keys[currentKeyframeIndex + 1]);
                }
            }

            KeyframeControl kfc = key.keyframeControl;
            keyframePanel.Children.Remove(kfc);
            keys.Remove(key);

            if (currentKeyframeIndex > victimIndex) { currentKeyframeIndex--; }
            updateCaptionCounts();
        }

        private void keyframeContextMenuOpened(object sender, RoutedEventArgs e, Keyframe key)
        {
            if (clipboardKey == null)
            {
                key.keyframeControl.CMPasteLeft.IsEnabled = false;
                key.keyframeControl.CMPasteRight.IsEnabled = false;
            }
            else
            {
                key.keyframeControl.CMPasteLeft.IsEnabled = true;
                key.keyframeControl.CMPasteRight.IsEnabled = true;
            }
            if (currentSlide.keys.Count < 2)
            {
                key.keyframeControl.CMSwap.IsEnabled = false;
            }
            else
            {
                key.keyframeControl.CMSwap.IsEnabled = true;
            }
        }

        private void imageCropper_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (slides.Count > 0)
            {
                selectKeyframe(currentSlide.keys[currentKeyframeIndex]);
            }
        }

        /****
        * Undo
        *****/

        class KeyframeModified : History.UndoItem
        {
            Keyframe old_key;
            int keyframe_index;
            int slide_index;
            public KeyframeModified(Keyframe keyframe, int keyframeIndex, int slideIndex)
            {
                old_key = keyframe.Clone();
                keyframe_index = keyframeIndex;
                slide_index = slideIndex;
            }
            public override void Undo(MainWindow mainWindow)
            {
                Keyframe target = mainWindow.slides[slide_index].keys[keyframe_index];
                target.zoomFactor = old_key.zoomFactor;
                target.duration = old_key.duration;
                target.x = old_key.x;
                target.y = old_key.y;
                mainWindow.selectSlide(slide_index);
                mainWindow.selectKeyframe(target);
            }
        }
        public void kfModifiedHistory()
        {
            history.Add(new KeyframeModified(currentSlide.keys[currentKeyframeIndex], currentKeyframeIndex, currentSlideIndex));
        }

        public class KeyframeAdded : History.UndoItem
        {
            int keyframe_index;
            int slide_index;
            public KeyframeAdded(int keyframeIndex, int slideIndex)
            {
                keyframe_index = keyframeIndex;
                slide_index = slideIndex;
            }
            public override void Undo(MainWindow mainWindow)
            {
                mainWindow.selectSlide(slide_index);
                Keyframe key = mainWindow.slides[slide_index].keys[keyframe_index];
                mainWindow.deleteKeyframe(key);
            }
        }
        public void kfAddedHistory(int index)
        {
            history.Add(new KeyframeAdded(index, currentSlideIndex));
        }
        public class KeyframeDeleted : History.UndoItem
        {
            Keyframe old_key;
            int keyframe_index;
            int slide_index;
            public KeyframeDeleted(Keyframe key, int keyframeIndex, int slideIndex)
            {
                old_key = key;
                keyframe_index = keyframeIndex;
                slide_index = slideIndex;
            }
            public override void Undo(MainWindow mainWindow)
            {
                mainWindow.selectSlide(slide_index);
                mainWindow.placeKeyframe(old_key, keyframe_index);
            }
        }
        public void kfDeletedHistory(Keyframe key, int index)
        {
            history.Add(new KeyframeDeleted(key, index, currentSlideIndex));
        }
    }
}