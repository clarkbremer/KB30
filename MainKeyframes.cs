
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using Microsoft.Win32;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Windows.Documents;

namespace KB30
{
    public partial class MainWindow : Window
    {
        /*********
         *  Keyframe UI functions
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
            KeyframeControl kfControl = new KeyframeControl();
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

            kfControl.PreviewMouseDown += delegate (object sender, MouseButtonEventArgs e) { keyframePreviewMouseDown(sender, e, key); };
            kfControl.MouseMove += delegate (object sender, MouseEventArgs e) { keyframeMouseMove(sender, e, key); };
            kfControl.DragOver += delegate (object sender, DragEventArgs e) { keyframeDragOver(sender, e, key); };
            kfControl.Drop += delegate (object sender, DragEventArgs e) { keyframeDrop(sender, e, key); };
            kfControl.GiveFeedback += delegate (object sender, GiveFeedbackEventArgs e) { keyframeGiveFeedback(sender, e, key); };

            kfControl.xTb.Text = key.x.ToString();
            kfControl.yTb.Text = key.y.ToString();
            kfControl.zoomTb.Text = key.zoomFactor.ToString();
            kfControl.durTb.Text = key.duration.ToString();

            kfControl.xTb.TextChanged += delegate (object sender, TextChangedEventArgs e) { kfControlChangeEvent(sender, e, key); };
            kfControl.yTb.TextChanged += delegate (object sender, TextChangedEventArgs e) { kfControlChangeEvent(sender, e, key); };
            kfControl.zoomTb.TextChanged += delegate (object sender, TextChangedEventArgs e) { kfControlChangeEvent(sender, e, key); };
            kfControl.durTb.TextChanged += delegate (object sender, TextChangedEventArgs e) { kfControlChangeEvent(sender, e, key); };

            kfControl.CMCut.Click += delegate (object sender, RoutedEventArgs e) { cutKeyframeClick(sender, e, key); };
            kfControl.CMPaste.Click += delegate (object sender, RoutedEventArgs e) { pasteKeyframeClick(sender, e, key); };
            kfControl.CMInsert.Click += delegate (object sender, RoutedEventArgs e) { insertKeyframeClick(sender, e, key); };
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

        public void selectKeyframe(Keyframe key)
        {
            Keyframes keys = currentSlide.keys;
            int keyFrameIndex = keys.IndexOf(key);
            Keyframe oldKey = keys[currentKeyframeIndex];
            KeyframeControl oldKFControl = oldKey.keyframeControl;

            unBindKFC(oldKFControl, oldKey);

            oldKFControl.DeSelect();

            currentKeyframeIndex = keyFrameIndex;

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
                            kfModifiedHistory();
                            key.duration = maybe;
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
            Keyframe newKey = new Keyframe(currentKey.zoomFactor, currentKey.x, currentKey.y, DEFAULT_DURATION);
            currentSlide.keys.Add(newKey);
            addKeyframeControl(newKey);
            newKey.keyframeControl.durTb.Focus();
            selectKeyframe(newKey);
            kfAddedHistory(currentSlide.keys.IndexOf(newKey));
        }

        private void insertKeyframeClick(object sender, RoutedEventArgs e, Keyframe key)
        {
            Keyframes keys = currentSlide.keys;
            var insertIndex = keys.IndexOf(key);
            Keyframe newKey = new Keyframe(key.zoomFactor, key.x, key.y, DEFAULT_DURATION);
            keys.Insert(insertIndex, newKey);
            addKeyframeControl(newKey, insertIndex);
            if (currentKeyframeIndex >= insertIndex)
            {
                currentKeyframeIndex++;
            }
            selectKeyframe(newKey);
            kfAddedHistory(insertIndex);
        }

        private void pasteKeyframeClick(object sender, RoutedEventArgs e, Keyframe key)
        {
            pasteKeyframe(key, LEFT);
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

        private void placeKeyframe(Keyframe key, int insertIndex) { 
            Keyframes keys = currentSlide.keys;

            keys.Insert(insertIndex, key);
            keyframePanel.Children.Insert(insertIndex, key.keyframeControl);
            if (currentKeyframeIndex >= insertIndex)
            {
                currentKeyframeIndex++;
            }
            selectKeyframe(key);
        }

        private void cutKeyframeClick(object sender, RoutedEventArgs e, Keyframe key)
        {
            cutKeyframe(key);
        }
        public void cutKeyframe(Keyframe key)
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
        public void deleteKeyframe(Keyframe key)
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
        }

        private void keyframeContextMenuOpened(object sender, RoutedEventArgs e, Keyframe key)
        {
            if (clipboardKey == null)
            {
                key.keyframeControl.CMPaste.IsEnabled = false;
            }
            else
            {
                key.keyframeControl.CMPaste.IsEnabled = true;
            }
        }

        private void imageCropper_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (slides.Count > 0)
            {
                selectKeyframe(currentSlide.keys[currentKeyframeIndex]);
            }
        }


        /*****
         * 
         * drag and drop 
         * 
         *****/

        private void keyframePreviewMouseDown(object sender, MouseButtonEventArgs e, Keyframe key)
        {
            initialKeyframeMousePosition = e.GetPosition(key.keyframeControl);
            startDragKeyframe = key;
        }

        private KeyframeAdorner keyAdorner;
        private void keyframeMouseMove(object sender, System.Windows.Input.MouseEventArgs e, Keyframe key)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var movedDistance = Point.Subtract(initialKeyframeMousePosition, e.GetPosition(key.keyframeControl)).Length;
                if (movedDistance > 15)
                {
                    // What happened here is that the drag started on one slide, but we detected it on another. 
                    if (key != startDragKeyframe)
                    {
                        return;
                    }

                    // package the data
                    DataObject data = new DataObject();
                    data.SetData("Keyframe", key);

                    // dim the source key
                    selectKeyframe(key);
                    key.Dim();

                    // set up the adorner
                    var adLayer = AdornerLayer.GetAdornerLayer(keyScrollViewer);
                    Rect renderRect = new Rect(key.keyframeControl.RenderSize);
                    renderRect.Height = renderRect.Height / 2;
                    keyAdorner = new KeyframeAdorner(keyScrollViewer, renderRect);
                    adLayer.Add(keyAdorner);

                    // do it!
                    DragDropEffects result = DragDrop.DoDragDrop(key.keyframeControl, key, DragDropEffects.Copy | DragDropEffects.Move);
                    if (result.HasFlag(DragDropEffects.Move))
                    {
                        Console.Beep(2000, 50);
                    }
                    else
                    {
                        Console.Beep(600, 50);
                        Console.Beep(600, 50);
                    }
                    // clean up
                    adLayer.Remove(keyAdorner);
                    currentSlide.keys.UnDimAll();
                    currentSlide.keys.ClearHighlightAll();
                }
            }
        }

        private int dropDirection(DragEventArgs e, Keyframe target_key)
        {
            KeyframeControl kfc = target_key.keyframeControl;
            Point p = e.GetPosition(kfc);
            if (p.X < (kfc.ActualWidth / 2))
            {
                return LEFT;
            }
            else
            {
                return RIGHT;
            }
        }

        private void keyframeDragOver(object sender, System.Windows.DragEventArgs e, Keyframe key)
        {
            e.Effects = DragDropEffects.None;

            if (sender is KeyframeControl)
            {
                Point p = e.GetPosition(keyScrollViewer);
                if (p.X < (keyScrollViewer.ActualWidth * 0.1))
                {
                    keyScrollViewer.ScrollToHorizontalOffset(keyScrollViewer.HorizontalOffset - 20);
                }
                if (p.X > (keyScrollViewer.ActualWidth * 0.9))
                {
                    keyScrollViewer.ScrollToHorizontalOffset(keyScrollViewer.HorizontalOffset + 20);
                }

                if (e.Data.GetDataPresent(typeof(Keyframe)))
                {
                    keyAdorner.Arrange(new Rect(p.X, p.Y, keyAdorner.DesiredSize.Width, keyAdorner.DesiredSize.Height));
                    // don't allow drop on self.
                    if (key.IsSelected())
                    {
                        e.Handled = true;
                        return;
                    }
                }

                // clear all other highlights
                currentSlide.keys.ClearHightlightsExcept(key);

                if (dropDirection(e, key) == LEFT)
                {
                    key.highlightLeft();
                }
                else
                {
                    key.highlightRight();
                }

                e.Effects = DragDropEffects.Move;
            }
        }
        private void keyframeGiveFeedback(object sender, GiveFeedbackEventArgs e, Keyframe key)
        {
            // These Effects values are set in the drop target's DragOver event handler.
            if (e.Effects.HasFlag(DragDropEffects.Move) || e.Effects.HasFlag(DragDropEffects.Copy))
            {
                Mouse.SetCursor(Cursors.Hand);
            }
            else
            {
                Mouse.SetCursor(Cursors.No);
            }
            e.Handled = true;
        }

        private void keyframeDrop(object sender, DragEventArgs e, Keyframe target_key = null)
        {
            if (e.Data.GetDataPresent(typeof(Keyframe)))
            {
                if (target_key == null) { return; }
                Keyframe source_key = e.Data.GetData(typeof(Keyframe)) as Keyframe;
                if (target_key.IsSelected())
                {
                    Console.Beep(600, 200);
                    return;  // don't drop on self
                }
   //             kfMovedHistory(currentSlide.keys.IndexOf(source_key), currentSlide.keys.IndexOf(target_key));
                cutKeyframe(source_key);
                pasteKeyframe(target_key, dropDirection(e, target_key));
                target_key.highlightClear();
            }
        }

        private void keyPanelDrop(object sender, DragEventArgs e)
        {
            if (e.OriginalSource is ScrollViewer)
            {
                keyframeDrop(sender, e, currentSlide.keys.Last());
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
            selectKeyframe(key);
        }
 
        public class KeyframeMoved : History.UndoItem
        {
            int from_index;
            int to_index;
            int slide_index;

            public KeyframeMoved(int fromIndex, int toIndex, int slideIndex)
            {
                from_index = fromIndex;
                to_index = toIndex;
                slide_index = slideIndex;
            }
            public override void Undo(MainWindow mainWindow)
            {
                mainWindow.selectSlide(slide_index);
                Keyframes keys = mainWindow.currentSlide.keys;
                Keyframe old_key = keys[to_index];
                mainWindow.deleteKeyframe(old_key);
                mainWindow.placeKeyframe(old_key, from_index);
            }
        }
        public void kfMovedHistory(int from_index, int to_index)
        {
            history.Add(new KeyframeMoved(from_index, to_index, currentSlideIndex));
        }
    }
}