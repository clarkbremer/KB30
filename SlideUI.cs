using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.ComponentModel;
using System.Windows.Documents;


namespace KB30
{
    public partial class MainWindow : Window
    {
        /*********
         *  Slides 
         */
        public void initializeSlidesUI()
        {
            currentSlideIndex = 0;
            currentKeyframeIndex = 0;
            slidePanel.Children.Clear();
            if (slides.Count == 0)
            {
                blankUI();
            }
            else
            {
                addSlideControlInBackground(slides[0]);
            }
            imageCropper.SizeChanged += imageCropper_SizeChanged;
            uiLoaded = true;
        }

        private Slide currentSlide
        {
            get
            {
                if ((currentSlideIndex < slides.Count) && (slides.Count > 0))
                {
                    return slides[currentSlideIndex];
                }
                else
                {
                    return null;
                }
            }
        }

        class WorkerResult
        {
            public Slide slide { get; set; }
            public BitmapImage bmp { get; set; }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Slide slide = (Slide)e.Argument;
            WorkerResult workerResult = new WorkerResult();

            workerResult.slide = slide;
            workerResult.bmp = Util.BitmapFromUri(slide.uri, 200, true);
            e.Result = workerResult;
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            WorkerResult wr = (WorkerResult)e.Result;
            addSlideControl(wr.slide, wr.bmp);
            int i = slides.IndexOf(wr.slide);
            if (i == 0)
            {
                selectSlide(0, false);
                slides[0].UnCheck();
            }
            i++;
            if (i < slides.Count)
            {
                caption.Text = "Loading thumbnail " + (i + 1).ToString() + " of " + (slides.Count).ToString();
                addSlideControlInBackground(slides[i]);
            }
            else
            {
                caption.Text = "Done Loading";
                selectSlide(0, true);
                slides[0].UnCheck();
            }
        }


        void addSlideControlInBackground(Slide slide)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync(slide);
        }

        public Boolean addSlideControl(Slide slide, BitmapImage bmp = null, int insertPosition = -1)
        {
            SlideControl slideControl = new SlideControl();

            if (bmp == null)
            {
                bmp = Util.BitmapFromUri(slide.uri, 200, true);
            }
            slideControl.image.Source = bmp;
            slideControl.caption.Text = System.IO.Path.GetFileName(slide.fileName);

            slideControl.MouseMove += delegate (object sender, MouseEventArgs e) { slideMouseMove(sender, e, slide); };
            slideControl.MouseLeftButtonDown += delegate (object sender, MouseButtonEventArgs e) { slideMouseLeftButtonDown(sender, e, slide); };
            slideControl.MouseRightButtonDown += delegate (object sender, MouseButtonEventArgs e) { slideMouseRightButtonDown(sender, e, slide); };
            slideControl.MouseLeftButtonUp += delegate (object sender, MouseButtonEventArgs e) { slideMouseLeftButtonUp(sender, e, slide); };
            slideControl.PreviewMouseDown += delegate (object sender, MouseButtonEventArgs e) { slidePreviewMouseDown(sender, e, slide); };
            slideControl.Drop += delegate (object sender, DragEventArgs e) { slideDrop(sender, e, slide); };
            slideControl.DragOver += delegate (object sender, DragEventArgs e) { slideDragOver(sender, e, slide); };
            slideControl.GiveFeedback += delegate (object sender, GiveFeedbackEventArgs e) { slideGiveFeedback(sender, e, slide); };
            slideControl.CMCut.Click += delegate (object sender, RoutedEventArgs e) { cutSlideClick(sender, e, slide); };
            slideControl.CMCopy.Click += delegate (object sender, RoutedEventArgs e) { copySlideClick(sender, e, slide); };
            slideControl.CMPasteAbove.Click += delegate (object sender, RoutedEventArgs e) { pasteSlideClick(sender, e, slide, ABOVE); };
            slideControl.CMPasteBelow.Click += delegate (object sender, RoutedEventArgs e) { pasteSlideClick(sender, e, slide, BELOW); };
            slideControl.CMInsertAbove.Click += delegate (object sender, RoutedEventArgs e) { insertSlideClick(sender, e, slide, ABOVE); };
            slideControl.CMInsertBelow.Click += delegate (object sender, RoutedEventArgs e) { insertSlideClick(sender, e, slide, BELOW); };
            slideControl.CMAudio.Click += delegate (object sender, RoutedEventArgs e) { audioClick(sender, e, slide); };
            slideControl.CMBackground.Click += delegate (object sender, RoutedEventArgs e) { backgroundClick(sender, e, slide); };

            slideControl.CMPlayFromHere.Click += delegate (object sender, RoutedEventArgs e) { playFromHereClick(sender, e, slide); };
            slideControl.SlideContextMenu.Opened += delegate (object sender, RoutedEventArgs e) { slideContextMenuOpened(sender, e, slide); };

            slide.slideControl = slideControl;
            if (insertPosition == -1)
            {
                slidePanel.Children.Add(slideControl);
            }
            else
            {
                slidePanel.Children.Insert(insertPosition, slideControl);
            }
            slideControl.DeSelect();
            slide.UpdateAudioNotations();
            slides.Renumber();
            updateCaptionCounts();
            return true;
        }

        public void selectSlide(Slide slide, Boolean unbindOld = true) { selectSlide(slides.IndexOf(slide), unbindOld); }
        public void selectSlide(int slideIndex, Boolean unbindOld = true)
        {
            if (slideIndex < 0) { return; }
            if (slideIndex > slides.Count - 1) { return; }

            currentSlide.slideControl.DeSelect();
            if (unbindOld)
            {
                Keyframe oldKey = currentSlide.keys[currentKeyframeIndex];
                KeyframeControl oldKFControl = oldKey.keyframeControl;
                unBindKFC(oldKFControl, oldKey);
            }
            currentSlideIndex = slideIndex;
            currentSlide.slideControl.Select();
            currentKeyframeIndex = 0;
            var bitmap = Util.BitmapFromUri(slides[slideIndex].uri);
            imageCropper.image.Source = bitmap;

            initializeKeysUI(currentSlide);
            caption.Text = currentSlide.fileName + " (" + bitmap.PixelWidth + " x " + bitmap.PixelHeight + ")  ";
            caption.ScrollToHorizontalOffset(caption.ExtentWidth);
            updateCaptionCounts();
        }

        private void updateCaptionCounts()
        {
            number.Text = (currentSlideIndex + 1) + " of " + slides.Count;
            time.Text = slides.SlideStartTimeString(currentSlideIndex) + " of " + slides.TotalTimeString() + " ("+ slides.TimeRemainingString(currentSlideIndex) + ")";
        }

        private void caption_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            caption.ScrollToHorizontalOffset(caption.ExtentWidth);
        }
        internal Slide addSlides(string[] fileNames)
        {
            Slide newSlide = null;
            foreach (String fname in fileNames)
            {
                newSlide = new Slide(fname);
                if (addSlideControl(newSlide))
                {
                    newSlide.SetupDefaultKeyframes();
                    slides.Add(newSlide);
                    if (fname == fileNames.First())
                    {
                        selectSlide(slides.Count - 1);
                    }
                }
                Console.Beep(1000, 100);
                Console.Beep(2000, 100);
            }
            slides.Renumber();
            updateCaptionCounts();
            return newSlide;
        }
        private void slideMouseLeftButtonDown(object sender, RoutedEventArgs e, Slide slide)
        {
            if (e.OriginalSource is CheckBox) { return; }

            int slideIndex = slides.IndexOf(slide);
            if (!slide.IsChecked() && !Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                slides.UncheckAll();
                slide.Check();
            }

            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
                foreach (SlideControl sc in slidePanel.Children) { sc.UnCheck(); }

                if (currentSlideIndex < slideIndex)
                {
                    for (int i = currentSlideIndex; i <= slideIndex; i++)
                    {
                        slides[i].Check();
                    }
                }
                else
                {
                    for (int i = slideIndex; i <= currentSlideIndex; i++)
                    {
                        slides[i].Check();
                    }
                }
            }
            selectSlide(slide);
        }
        private void slideMouseRightButtonDown(object sender, RoutedEventArgs e, Slide slide)
        {
            if (e.OriginalSource is CheckBox) { return; }
            selectSlide(slide);
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                slide.ToggleCheck();
            }
            else
            {
                if (!slide.IsChecked())
                {
                    slides.UncheckAll();
                    slide.Check();
                }
            }
        }
        private void slideMouseLeftButtonUp(object sender, RoutedEventArgs e, Slide slide)
        {
            if (e.OriginalSource is CheckBox) { return; }
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                slide.ToggleCheck();
            }
            else if (slide.IsChecked() && !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                slides.UncheckAll();
                slide.Check();
            }
        }

        private void addBlackClick(object sender, RoutedEventArgs e)
        {
            Slide s = insertSlide("black");
        }
        private void addWhiteClick(object sender, RoutedEventArgs e)
        {
            Slide s = insertSlide("white");
        }


        private void addSlideClick(object sender, RoutedEventArgs e)
        {
            showFinder();
        }

        internal Slide insertSlide(string fileName, int direction = BELOW)
        {
            if (slides.Count == 0)
            {
                return addSlides(new string[] { fileName });
            }
            else
            {
                return insertSlides(new string[] { fileName }, currentSlideIndex, direction);
            }
        }


        internal Slide insertSlides(string[] fileNames, Slide slide, int direction)
        {
            var insertIndex = slides.IndexOf(slide);
            return insertSlides(fileNames, insertIndex, direction);
        }

        internal Slide insertSlides(string[] fileNames, int insertIndex, int direction)
        {
            int numInserted = 0;
            Slide newSlide = null;
            if (direction == BELOW)
            {
                insertIndex += 1;
            }
            foreach (String fname in fileNames)
            {
                newSlide = new Slide(fname);
                if (addSlideControl(newSlide, null, insertIndex))
                {
                    newSlide.SetupDefaultKeyframes();
                    slides.Insert(insertIndex, newSlide);
                    if (currentSlideIndex >= insertIndex)
                    {
                        currentSlideIndex++;
                    }
                    if (fname == fileNames.First())
                    {
                        selectSlide(insertIndex);
                    }
                    slideAddedHistory(insertIndex);
                    insertIndex++;
                    numInserted++;
                    newSlide.BringIntoView();
                    Console.Beep(1000, 100);
                    Console.Beep(2000, 100);
                }
            }
            history.Add(new History.CompoundUndo(numInserted));
            slides.Renumber();
            updateCaptionCounts();
            return (newSlide);
        }

        private void insertSlideClick(object sender, RoutedEventArgs e, Slide slide, int position)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select image file(s)";
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Images (*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                insertSlides(openFileDialog.FileNames, slide, position);
            }
        }

        private void pasteSlideClick(object sender, RoutedEventArgs e, Slide insertSlide, int direction) { pasteClipboardSlides(insertSlide, direction); }

        private void pasteClipboardSlides(Slide targetSlide, int direction = ABOVE)
        {
            var insertIndex = slides.IndexOf(targetSlide);
            if (direction == BELOW)
            {
                insertIndex += 1;
            }

            foreach (Slide slide in clipboardSlides)
            {
                placeSlide(slide, insertIndex);
                if (slide == clipboardSlides.First())
                {
                    selectSlide(insertIndex);
                }
                slideAddedHistory(insertIndex);
                insertIndex++;
            }
            history.Add(new History.CompoundUndo(clipboardSlides.Count));
            clipboardSlides.Clear();
            slides.UncheckAll();
        }


        private void placeSlide(Slide slide, int insertIndex)
        {
            slides.Insert(insertIndex, slide);
            if (slide.slideControl == null)
            {
                addSlideControl(slide, null, insertIndex);
            }
            else
            {
                slidePanel.Children.Insert(insertIndex, slide.slideControl);
            }
            if (currentSlideIndex >= insertIndex)
            {
                currentSlideIndex++;
            }
            slides.Renumber();
            updateCaptionCounts();
        }

        private void cutSlideClick(object sender, RoutedEventArgs e, Slide s) { cutSlidesToClipboard(s); }
        private void cutSlidesToClipboard(Slide s)
        {
            clipboardSlides.Clear();
            foreach (Slide slide in slides)
            {
                if (slide.IsChecked())
                {
                    clipboardSlides.Add(slide);
                }
            }
            if (clipboardSlides.Count == 0)
            {
                clipboardSlides.Add(s);
            }

            foreach (Slide slide in clipboardSlides)
            {
                SlideDeletedHistory(slide, slides.IndexOf(slide));
                deleteSlide(slide);
            }
            history.Add(new History.CompoundUndo(clipboardSlides.Count));
        }

        private void deleteSlide(Slide slide)
        {
            int victimIndex = slides.IndexOf(slide);

            if (currentSlideIndex == victimIndex)
            {
                if (currentSlideIndex == (slides.Count - 1))
                {
                    selectSlide(currentSlideIndex - 1);
                }
                else
                {
                    selectSlide(currentSlideIndex + 1);
                }
            }

            slidePanel.Children.Remove(slide.slideControl);
            slides.Remove(slide);

            if (currentSlideIndex > victimIndex) { currentSlideIndex--; }
            slides.Renumber();
            updateCaptionCounts();
            if (slides.Count == 0)
            {
                blankUI();
                return;
            }
            currentSlide.BringIntoView();
        }

        private void copySlideClick(object sender, RoutedEventArgs e, Slide s) { copySlidesToClipboard(s); }

        private void copySlidesToClipboard(Slide targetSlide, int direction = ABOVE)
        {
            clipboardSlides.Clear();
            foreach (Slide s in slides)
            {
                if (s.IsChecked())
                {
                    clipboardSlides.Add(s.Clone());
                }
            }
            if (clipboardSlides.Count == 0)
            {
                clipboardSlides.Add(targetSlide.Clone());
            }
        }

        private void audioClick(object sender, RoutedEventArgs e, Slide slide)
        {
            AudioDialog audioDialog = new AudioDialog();
            audioDialog.Title = "Select Audio File";
            if (String.IsNullOrEmpty(slide.audio))
            {
                audioDialog.filenameTextBlock.Text = "";
                audioDialog.volumeText.Text = "8";
                audioDialog.volumeSlider.Value = 8;
                audioDialog.loopCheckBox.IsChecked = false;
            }
            else
            {
                audioDialog.filenameTextBlock.Text = slide.audio;
                audioDialog.volumeText.Text = (slide.audioVolume * 10.0).ToString();
                audioDialog.volumeSlider.Value = (slide.audioVolume * 10.0);
                audioDialog.loopCheckBox.IsChecked = slide.loopAudio;
            }
            if (audioDialog.ShowDialog() == true)
            {
                slide.audio = audioDialog.filenameTextBlock.Text;
                slide.audioVolume = Convert.ToDouble(audioDialog.volumeText.Text) / 10.0;
                slide.loopAudio = audioDialog.loopCheckBox.IsChecked ?? false;
                slide.UpdateAudioNotations();
            }
        }

        private void backgroundClick(object sender, RoutedEventArgs e, Slide slide)
        {
            AudioDialog audioDialog = new AudioDialog();
            audioDialog.Title = "Select Background File";
            if (String.IsNullOrEmpty(slide.backgroundAudio))
            {
                audioDialog.filenameTextBlock.Text = "";
                audioDialog.volumeText.Text = "5";
                audioDialog.volumeSlider.Value = 5;
                audioDialog.loopCheckBox.IsChecked = true;
            }
            else
            {
                audioDialog.filenameTextBlock.Text = slide.backgroundAudio;
                audioDialog.volumeText.Text = (slide.backgroundVolume * 10.0).ToString();
                audioDialog.volumeSlider.Value = (slide.backgroundVolume * 10.0);
                audioDialog.loopCheckBox.IsChecked = slide.loopBackground;
            }
            if (audioDialog.ShowDialog() == true)
            {
                slide.backgroundAudio = audioDialog.filenameTextBlock.Text;
                slide.backgroundVolume = Convert.ToDouble(audioDialog.volumeText.Text) / 10.0;
                slide.loopBackground = audioDialog.loopCheckBox.IsChecked ?? false;
                slide.UpdateAudioNotations();
            }
        }


        private void playFromHereClick(object sender, RoutedEventArgs e, Slide slide)
        {
            playIt(slides.IndexOf(slide));
        }

        private void slideContextMenuOpened(object sender, RoutedEventArgs e, Slide slide)
        {
            int numSelected = slides.Count(s => s.IsChecked());
            slide.slideControl.CMCut.Header = "Cut " + numSelected + " Slide(s)";
            slide.slideControl.CMCopy.Header = "Copy " + numSelected + " Slide(s)";
            if (clipboardSlides.Count == 0)
            {
                slide.slideControl.CMPasteAbove.Header = "Paste Slide(s) Above";
                slide.slideControl.CMPasteBelow.Header = "Paste Slide(s) Below";
                slide.slideControl.CMPasteAbove.IsEnabled = false;
                slide.slideControl.CMPasteBelow.IsEnabled = false;
            }
            else
            {
                slide.slideControl.CMPasteAbove.Header = "Paste " + clipboardSlides.Count + " Slides(s) Above";
                slide.slideControl.CMPasteBelow.Header = "Paste " + clipboardSlides.Count + " Slides(s) Below";
                slide.slideControl.CMPasteAbove.IsEnabled = true;
                slide.slideControl.CMPasteBelow.IsEnabled = true;
            }
            slide.slideControl.CMAudio.Header = "Audio: " + System.IO.Path.GetFileName(slide.audio);
            slide.slideControl.CMBackground.Header = "Background: " + System.IO.Path.GetFileName(slide.backgroundAudio);
        }

        private void slideScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            currentSlide?.BringIntoView();
        }

        /*************
        * Drag and Drop
        */

        private void slidePreviewMouseDown(object sender, MouseButtonEventArgs e, Slide slide)
        {
            initialSlideMousePosition = e.GetPosition(slide.slideControl);
            startDragSlide = slide;
        }

        private SlideAdorner slideAdorner;
        private void slideMouseMove(object sender, System.Windows.Input.MouseEventArgs e, Slide slide)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var movedDistance = Point.Subtract(initialSlideMousePosition, e.GetPosition(slide.slideControl)).Length;
                if (movedDistance > 15 && initialSlideMousePosition != new Point(0, 0))  // We moved enough with the button down to be considered a drag
                {
                    // What happened here is that the drag started on one slide, but we detected it on another.
                    // Because the mouse down happend outside the slideControl, so slidePreviewMouseDown() was not called.  
                    // If the mouse then wanders onto the slideControl, this method gets triggered.
                    if (slide != startDragSlide)
                    {
                        return;
                    }
                    // package the data
                    DataObject data = new DataObject();
                    data.SetData("SlideControl", slide);

                    // If this slide is already checked, then we move all checked slides.  
                    // If its not checked, then we move only this one.
                    // Unless ctrl is pressed, then we check this one and move them all.
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        slide.Check();
                    }
                    else
                    {
                        if (!slide.IsChecked())
                        {
                            slides.UncheckAll();
                        }
                    }
                    // dim and count all the slides that will be dragged
                    int numChecked = 0;
                    foreach (Slide s in slides)
                    {
                        if (s.IsChecked())
                        {
                            s.Dim();
                            numChecked++;
                        }
                    }

                    // set up the adorner
                    var adLayer = AdornerLayer.GetAdornerLayer(slideScrollViewer);
                    Rect renderRect = new Rect(slide.slideControl.RenderSize);
                    renderRect.Height = renderRect.Height / 2;
                    renderRect.Width = renderRect.Width / 2;
                    slideAdorner = new SlideAdorner(slideScrollViewer, numChecked, slide.slideControl.image.Source, renderRect);
                    adLayer.Add(slideAdorner);

                    // do it!
                    DragDropEffects result = DragDrop.DoDragDrop(slide.slideControl, slide, DragDropEffects.Copy | DragDropEffects.Move);

                    // Techically, this is where we would delete the dragged items if result.HasFlag(DragDropEffects.Move).
                    // clean up
                    adLayer.Remove(slideAdorner);
                    slides.UnDimAll();
                    slides.ClearHighlightAll();
                }
            }
        }
        private void slidePanelDrop(object sender, DragEventArgs e)
        {
            if (e.OriginalSource is ScrollViewer)
            {
                slideDrop(sender, e);
            }
        }

        private int dropDirection(DragEventArgs e, Slide target_slide)
        {
            SlideControl sc = target_slide.slideControl;
            Point p = e.GetPosition(sc);
            if (p.Y < (sc.ActualHeight / 2))
            {
                return (ABOVE);
            }
            else
            {
                return (BELOW);
            }
        }
        private void slideDrop(object sender, DragEventArgs e, Slide target_slide = null)
        {
            String[] allFormats = e.Data.GetFormats();  // debug
            if (e.Data.GetDataPresent(DataFormats.FileDrop))  // this was from a winodws explorer window
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (target_slide == null)
                {
                    addSlides(files);
                }
                else
                {
                    if (sender is SlideControl) // dropped onto a slide
                    {
                        insertSlides(files, target_slide, dropDirection(e, target_slide));
                        target_slide.highlightClear();
                    }
                    else  // dropped onto empty space
                    {
                        insertSlides(files, target_slide, ABOVE);
                    }
                }
            }
            else if (e.Data.GetDataPresent(typeof(Slide)))
            {
                if (target_slide == null) { return; }
                Slide source_slide = e.Data.GetData(typeof(Slide)) as Slide;
                if (source_slide != startDragSlide)
                {
                    Console.Beep(600, 400);
                }
                if (target_slide.IsChecked() || (target_slide == source_slide))
                {
                    Console.Beep(600, 200);
                    e.Effects = DragDropEffects.None;
                    return;  // don't drop on self
                }
                if (!source_slide.IsChecked())
                {
                    source_slide.slideControl.Check();
                }
                if (e.KeyStates.HasFlag(DragDropKeyStates.ControlKey))
                {
                    e.Effects = DragDropEffects.Copy;
                    copySlidesToClipboard(target_slide, dropDirection(e, target_slide));
                    int copy_count = clipboardSlides.Count;
                    pasteClipboardSlides(target_slide, dropDirection(e, target_slide));
                }
                else
                {
                    e.Effects = DragDropEffects.Move;
                    cutSlidesToClipboard(source_slide);
                    int move_count = clipboardSlides.Count;
                    pasteClipboardSlides(target_slide, dropDirection(e, target_slide));
                    history.Add(new History.CompoundUndo(2));
                }
                target_slide.highlightClear();
            }
        }
        private void slideDragOver(object sender, System.Windows.DragEventArgs e, Slide slide)
        {
            e.Effects = DragDropEffects.None;

            if (sender is SlideControl)
            {
                Point p = e.GetPosition(slideScrollViewer);
                if (p.Y < (slideScrollViewer.ActualHeight * 0.1))
                {
                    slideScrollViewer.ScrollToVerticalOffset(slideScrollViewer.VerticalOffset - 20);
                }
                if (p.Y > (slideScrollViewer.ActualHeight * 0.9))
                {
                    slideScrollViewer.ScrollToVerticalOffset(slideScrollViewer.VerticalOffset + 20);
                }

                if (e.Data.GetDataPresent(typeof(Slide)))
                {
                    if (slideAdorner != null)  // becuase it came from a different instance of the app?
                    {
                        slideAdorner.Arrange(new Rect(p.X, p.Y, slideAdorner.DesiredSize.Width, slideAdorner.DesiredSize.Height));
                    }
                    // don't allow drop on self.
                    if (slide.IsChecked())
                    {
                        e.Handled = true;
                        return;
                    }
                }

                // clear all other highlights
                slides.ClearHightlightsExcept(slide);

                if (dropDirection(e, slide) == ABOVE)
                {
                    slide.highlightAbove();
                }
                else
                {
                    slide.highlightBelow();
                }

                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.Move;
                }
            }
        }

        private void slideGiveFeedback(object sender, GiveFeedbackEventArgs e, Slide s)
        {
            // These Effects values are set in the drop target's DragOver event handler.
            if (e.Effects.HasFlag(DragDropEffects.Move))
            {
                slideAdorner.SetHint("-> Move");
                Mouse.SetCursor(Cursors.Hand);
            }
            else if (e.Effects.HasFlag(DragDropEffects.Copy))
            {
                slideAdorner.SetHint("+ Copy");
                Mouse.SetCursor(Cursors.Hand);
            }
            else
            {
                slideAdorner.SetHint("");
                Mouse.SetCursor(Cursors.No);
            }
            e.Handled = true;
        }
        /****
        * Undo
        *****/

        public class SlideAdded : History.UndoItem
        {
            int slide_index;
            public SlideAdded(int slideIndex)
            {
                slide_index = slideIndex;
            }
            public override void Undo(MainWindow mainWindow)
            {
                mainWindow.selectSlide(slide_index);
                Slide slide = mainWindow.slides[slide_index];
                mainWindow.deleteSlide(slide);
            }
        }
        public void slideAddedHistory(int index)
        {
            history.Add(new SlideAdded(index));
        }

        public class SlideDeleted : History.UndoItem
        {
            int slide_index;
            Slide slide;
            public SlideDeleted(Slide _slide, int slideIndex)
            {
                slide = _slide;
                slide_index = slideIndex;
            }
            public override void Undo(MainWindow mainWindow)
            {
                mainWindow.placeSlide(slide, slide_index);
            }
        }
        public void SlideDeletedHistory(Slide slide, int index)
        {
            history.Add(new SlideDeleted(slide, index));
        }
    }
}