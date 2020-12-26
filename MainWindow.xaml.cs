﻿using System;
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


/*
 * Bugs:
 *  Load non-image file -> trouble.
 * 
 * To DO:  
 *  Drag and Drop keys to re-order.
 *  Break up this file (drag and drop in own file?)
 *  Config options both global and local to this album:
 *   - Absolute/Relative paths 
 *   - default fadein/out duration
 *   - default key duration
 *  Duration textbox allow only numeric.
 *  Maybe combine slide and slidecontol classes.  Same with keys.  Figure out serialization.
 *  Key to reverse keyframe order
 */
namespace KB30
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const double CONFIG_VERSION = 1.0;
        const double DEFAULT_DURATION = 10.0;

        const int ABOVE = 1;
        const int BELOW = 2;

        public int currentSlideIndex = 0;
        public int currentKeyframeIndex = 0;
        private String currentFileName = "untitled";
        public Slides slides = new Slides();
        public Slides clipboardSlides = new Slides();
        public KF clipboardKey = null;
        private String lastSavedAlbum;
        private String soundtrack = "";
        private Boolean playWithArgumentFile = false;
        private Boolean uiLoaded = false;
        private Point initialMousePosition;

        public MainWindow()
        {
            InitializeComponent();
        }

        public class Album
        {
            public string version { get; set; }
            public string soundtrack { get; set; }
            public Slides slides { get; set; }
        }

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


        class WorkerResult
        {
            public Slide slide { get; set; }
            public BitmapImage bmp { get; set; }
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Slide slide = (Slide)e.Argument;
            WorkerResult workerResult = new WorkerResult();

            BitmapImage bmp = new BitmapImage();
            try
            {
                bmp.BeginInit();
                bmp.UriSource = slide.uri;
                bmp.DecodePixelWidth = 200;
                bmp.EndInit();
                bmp.Freeze();
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show("Error loading image file: " + slide.fileName, "Call the doctor, I think I'm gonna crash!");
            }

            workerResult.slide = slide;
            workerResult.bmp = bmp;
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
                caption.Text = "Loading slide " + (i + 1).ToString() + " of " + (slides.Count).ToString();
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
                bmp = new BitmapImage();
                try
                {
                    bmp.BeginInit();
                    bmp.UriSource = slide.uri;
                    bmp.DecodePixelWidth = 200;
                    bmp.EndInit();
                    bmp.Freeze();
                }
                catch (NotSupportedException ex)
                {
                    MessageBox.Show("Error loading image file: " + slide.fileName, "Call the doctor, I think I'm gonna crash!");
                    return false;
                }
            }
            slideControl.image.Source = bmp;
            slideControl.caption.Text = System.IO.Path.GetFileName(slide.fileName);
            slideControl.MouseMove += delegate (object sender, MouseEventArgs e) { slideMouseMove(sender, e, slide); };
            slideControl.MouseLeftButtonUp += delegate (object sender, MouseButtonEventArgs e) { slideClick(sender, e, slide); };
            slideControl.PreviewMouseDown += delegate (object sender, MouseButtonEventArgs e) { slidePreviewMouseDown(sender, e, slide); };
            slideControl.Drop += delegate (object sender, DragEventArgs e) { slideDrop(sender, e, slide); };
            slideControl.DragOver += delegate (object sender, DragEventArgs e) { slideDragOver(sender, e, slide); };
            slideControl.GiveFeedback += delegate (object sender, GiveFeedbackEventArgs e) { slideGiveFeedback(sender, e, slide); };
            slideControl.CMCut.Click += delegate (object sender, RoutedEventArgs e) { cutSlideClick(sender, e, slide); };
            slideControl.CMPasteAbove.Click += delegate (object sender, RoutedEventArgs e) { pasteSlideClick(sender, e, slide, ABOVE); };
            slideControl.CMPasteBelow.Click += delegate (object sender, RoutedEventArgs e) { pasteSlideClick(sender, e, slide, BELOW); };
            slideControl.CMInsertAbove.Click += delegate (object sender, RoutedEventArgs e) { insertSlideClick(sender, e, slide, ABOVE); };
            slideControl.CMInsertBelow.Click += delegate (object sender, RoutedEventArgs e) { insertSlideClick(sender, e, slide, BELOW); };
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
            slides.Renumber();
            return true;
        }

        public void selectSlide(Slide slide, Boolean unbindOld = true, Boolean unCheckAll = true) { selectSlide(slides.IndexOf(slide), unbindOld, unCheckAll); }
        public void selectSlide(int slideIndex, Boolean unbindOld = true, Boolean unCheckAll = true)
        {
            if (slideIndex < 0) { return; }

            slides[currentSlideIndex].slideControl.DeSelect(unCheckAll);
            if (unbindOld)
            {
                KF oldKey = slides[currentSlideIndex].keys[currentKeyframeIndex];
                KeyframeControl oldKFControl = oldKey.kfControl;
                unBindKFC(oldKFControl, oldKey);
            }
            currentSlideIndex = slideIndex;
            var bitmap = new BitmapImage(slides[slideIndex].uri);
            imageCropper.image.Source = bitmap;

            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl) && unCheckAll)
            {
                foreach (SlideControl sc in slidePanel.Children) { sc.UnCheck(); }
            }

            slides[currentSlideIndex].slideControl.Select();

            currentKeyframeIndex = 0;
            initializeKeysUI(slides[currentSlideIndex]);
            caption.Text = slides[currentSlideIndex].fileName + " (" + bitmap.PixelWidth + " x " + bitmap.PixelHeight + ")  " + (slideIndex + 1) + " of " + slides.Count;
        }
        private void addSlides(string[] fileNames)
        {
            foreach (String fname in fileNames)
            {
                Slide newSlide = new Slide(fname);
                newSlide.keys.Add(new KF(2.0, 0.5, 0.5, 1));
                if (addSlideControl(newSlide))
                {
                    slides.Add(newSlide);
                    if (fname == fileNames.First())
                    {
                        selectSlide(slides.Count - 1);
                    }
                }
            }
            slides.Renumber();
        }
        private void slideClick(object sender, RoutedEventArgs e, Slide slide)
        {
            if (e.OriginalSource is CheckBox) { return; }

            int index = slides.IndexOf(slide, 0);
            selectSlide(slide);
        }

        private void addSlideClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select image file(s)";
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Images (*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                addSlides(openFileDialog.FileNames);
            }
        }

        private void insertSlides(string[] fileNames, Slide slide, int position)
        {
            var insertIndex = slides.IndexOf(slide);
            if (position == BELOW)
            {
                insertIndex += 1;
            }
            foreach (String fname in fileNames)
            {
                Slide newSlide = new Slide(fname);
                newSlide.keys.Add(new KF(2.0, 0.5, 0.5, 1));
                if (addSlideControl(newSlide, null, insertIndex))
                {
                    slides.Insert(insertIndex, newSlide);
                    if (currentSlideIndex >= insertIndex)
                    {
                        currentSlideIndex++;
                    }
                    if (fname == fileNames.First())
                    {
                        selectSlide(insertIndex, true);
                    }
                    insertIndex++;
                }
            }
            slides.Renumber();
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

        private void pasteSlideClick(object sender, RoutedEventArgs e, Slide insertSlide, int direction) { pasteSlides(insertSlide, direction); }
        private void pasteSlides(Slide insertSlide, int direction)
        {
            var insertIndex = slides.IndexOf(insertSlide);
            if (direction == BELOW)
            {
                insertIndex += 1;
            }

            foreach (Slide slide in clipboardSlides)
            {
                slides.Insert(insertIndex, slide);
                slidePanel.Children.Insert(insertIndex, slide.slideControl);
                if (currentSlideIndex >= insertIndex)
                {
                    currentSlideIndex++;
                }
                if (slide == clipboardSlides.First())
                {
                    selectSlide(insertIndex, true);
                }
                insertIndex++;
            }
            clipboardSlides.Clear();
            slides.UncheckAll();
            slides.Renumber();
        }


        private void cutSlideClick(object sender, RoutedEventArgs e, Slide s) { cutSlides(s); }
        private void cutSlides(Slide s)
        {
            clipboardSlides.Clear();
            foreach (Slide slide in slides)
            {
                if (slide.slideControl.IsChecked())
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
            }
            slides.Renumber();
            if (slides.Count == 0)
            {
                blankUI();
            }
        }

        private void playFromHereClick(object sender, RoutedEventArgs e, Slide slide)
        {
            playIt(slides.IndexOf(slide));
        }

        private void slideContextMenuOpened(object sender, RoutedEventArgs e, Slide slide)
        {
            int numSelected = slides.Count(s => s.slideControl.IsChecked());
            slide.slideControl.CMCut.Header = "Cut " + numSelected + " Slide(s)";
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
        }

        /*********
         *  Keyframes
         */

        public void initializeKeysUI(Slide slide)
        {
            keyframePanel.Children.Clear();
            List<KF> keys = slide.keys;
            for (int i = 0; i < keys.Count; i++)
            {
                KF key = keys[i];
                addKeyframeControl(key);
            }

            selectKeyframe(keys[0]);
        }

        public void addKeyframeControl(KF key, int insertIndex = -1)
        {
            KeyframeControl kfControl = new KeyframeControl();
            kfControl.DeSelect();
            key.kfControl = kfControl;
            if (insertIndex == -1)
            {
                keyframePanel.Children.Add(kfControl);
            }
            else
            {
                keyframePanel.Children.Insert(insertIndex, kfControl);
            }

            kfControl.Margin = new Thickness(2, 2, 2, 2);
            kfControl.MouseLeftButtonUp += delegate (object sender, MouseButtonEventArgs e) { keyFrameClick(sender, e, key); };
            kfControl.xTb.PreviewMouseDown += delegate (object sender, MouseButtonEventArgs e) { keyFrameClick(sender, e, key); };
            kfControl.yTb.PreviewMouseDown += delegate (object sender, MouseButtonEventArgs e) { keyFrameClick(sender, e, key); };
            kfControl.zoomTb.PreviewMouseDown += delegate (object sender, MouseButtonEventArgs e) { keyFrameClick(sender, e, key); };
            kfControl.durTb.PreviewMouseDown += delegate (object sender, MouseButtonEventArgs e) { keyFrameClick(sender, e, key); };

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

 
        private void unBindKFC(KeyframeControl kfc, KF key)
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

        public void selectKeyframe(KF key)
        {
            List<KF> keys = slides[currentSlideIndex].keys;
            int keyFrameIndex = keys.IndexOf(key);
            KF oldKey = keys[currentKeyframeIndex];
            KeyframeControl oldKFControl = oldKey.kfControl;

            unBindKFC(oldKFControl, oldKey);

            oldKFControl.DeSelect();

            currentKeyframeIndex = keyFrameIndex;

            imageCropper.updateLayout();
            imageCropper.cropZoom = key.zoomFactor;
            imageCropper.cropX = key.x;
            imageCropper.cropY = key.y;

            imageCropper.updateLayout();

            KeyframeControl kfControl = key.kfControl;
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

        private void kfControlChangeEvent(object sender, TextChangedEventArgs e, KF key)
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
                            break;
                        case "zoomTb":
                            key.zoomFactor = maybe;
                            break;
                    }
                }
            }
        }

        private void keyFrameClick(object sender, RoutedEventArgs e, KF key)
        {
            selectKeyframe(key);
        }

        private void addKeyframeClick(object sender, RoutedEventArgs e)
        {
            if (slides.Count == 0) { return; }
            Slide currentSlide = slides[currentSlideIndex];
            KF currentKey = currentSlide.keys[currentKeyframeIndex];
            KF newKey = new KF(currentKey.zoomFactor, currentKey.x, currentKey.y, DEFAULT_DURATION);
            currentSlide.keys.Add(newKey);
            addKeyframeControl(newKey);
            newKey.kfControl.durTb.Focus();
            selectKeyframe(newKey);
        }

        private void insertKeyframeClick(object sender, RoutedEventArgs e, KF key)
        {
            Slide currentSlide = slides[currentSlideIndex];
            List<KF> keys = currentSlide.keys;
            var insertIndex = keys.IndexOf(key);
            KF newKey = new KF(key.zoomFactor, key.x, key.y, DEFAULT_DURATION);
            keys.Insert(insertIndex, newKey);
            addKeyframeControl(newKey, insertIndex);
            if (currentKeyframeIndex >= insertIndex)
            {
                currentKeyframeIndex++;
            }
            selectKeyframe(newKey);
        }

        private void pasteKeyframeClick(object sender, RoutedEventArgs e, KF key)
        {
            List<KF> keys = slides[currentSlideIndex].keys;

            var insertIndex = keys.IndexOf(key);
            keys.Insert(insertIndex, clipboardKey);
            keyframePanel.Children.Insert(insertIndex, clipboardKey.kfControl);
            if (currentKeyframeIndex >= insertIndex)
            {
                currentKeyframeIndex++;
            }
            clipboardKey = null;
        }

        private void cutKeyframeClick(object sender, RoutedEventArgs e, KF key)
        {
            List<KF> keys = slides[currentSlideIndex].keys;
            if (keys.Count == 1)
            {
                MessageBox.Show("At least one keyframe is required");
                return;
            }
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

            KeyframeControl kfc = key.kfControl;
            keyframePanel.Children.Remove(kfc);
            clipboardKey = key;
            keys.Remove(key);

            if (currentKeyframeIndex > victimIndex) { currentKeyframeIndex--; }
        }

        private void keyframeContextMenuOpened(object sender, RoutedEventArgs e, KF key)
        {
            if (clipboardKey == null)
            {
                key.kfControl.CMPaste.IsEnabled = false;
            }
            else
            {
                key.kfControl.CMPaste.IsEnabled = true;
            }
        }

        private void imageCropper_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (slides.Count > 0)
            {
                selectKeyframe(slides[currentSlideIndex].keys[currentKeyframeIndex]);
            }
        }


        /************
         *  Animate!
         */

        private void playIt(int start = 0)
        {
            AnimationWindow animationWindow = new AnimationWindow();
            animationWindow.Closed += animationWindow_Closed;
            animationWindow.Show();
            animationWindow.animate(slides, start, soundtrack);
        }
        private void playClick(object sender, RoutedEventArgs e)
        {
            if (albumValid())
            {
                playIt();
            }
        }
        private void playSlideClick(object sender, RoutedEventArgs e)
        {
            if (albumValid())
            {
                Slides oneSlide = new Slides();
                oneSlide.Add(slides[currentSlideIndex]);
                AnimationWindow animationWindow = new AnimationWindow();
                animationWindow.Show();
                animationWindow.animate(oneSlide);
            }
        }

        private void animationWindow_Closed(object sender, EventArgs e)
        {
            if (playWithArgumentFile)
            {
                if (((AnimationWindow)sender).exitOnClose)
                {
                    this.Close();
                }
                else
                {
                    if (uiLoaded == false)
                    {
                        initializeSlidesUI();
                    }
                    playWithArgumentFile = false;
                }
            }
        }

        private void mainWindowLoaded(object sender, RoutedEventArgs e)
        {
            lastSavedAlbum = serializeCurrentAlbum();
            var allArgs = Environment.GetCommandLineArgs();
            if (allArgs.Length > 1)
            {
                var filenameArgument = allArgs[1];
                playWithArgumentFile = true;
                loadIt(filenameArgument);
                playIt();
            }
            /* debug */
            else
            {
                loadIt("C:\\Users\\clark\\source\\repos\\pictures\\cards.kb30");
                initializeSlidesUI();

            }

        }


        /************
         *  File Menu
         */
        private void fileNewClick(object sender, RoutedEventArgs e)
        {
            if (saveIfDirty())
            {
                blankUI();
                currentFileName = "untitled";
                soundtrack = "";
                lastSavedAlbum = serializeCurrentAlbum();
            }
        }

        private void loadIt(string filename)
        {
            Album album = new Album();
            string jsonString;

            jsonString = File.ReadAllText(filename);
            album = JsonConvert.DeserializeObject<Album>(jsonString);
            if (Convert.ToDouble(album.version) > CONFIG_VERSION)
            {
                MessageBox.Show("Album File version is newer than this version of the program.");
                return;
            }
            if (album.soundtrack != null)
            {
                if (Path.IsPathFullyQualified(album.soundtrack))
                {
                    soundtrack = album.soundtrack;
                }
                else
                {
                    soundtrack = Path.GetFullPath(album.soundtrack, Path.GetDirectoryName(filename));
                }
            }
            slides = album.slides;
            for (int i = slides.Count - 1; i >= 0; i--)
            {
                slides[i].basePath = Path.GetDirectoryName(filename);
                if (!File.Exists(slides[i].fileName))
                {
                    MessageBox.Show("File Not Found: " + slides[i].fileName, "File Not Found");
                    slides.RemoveAt(i);
                }
            }
            lastSavedAlbum = jsonString;
            currentFileName = filename;
            this.Title = "KB30 - " + currentFileName;
        }

        private void fileOpenClick(object sender, RoutedEventArgs e)
        {
            if (saveIfDirty())
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "KB30 files (*.kb30)|*.kb30|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == true)
                {
                    caption.Text = "Loading Slides...";
                    loadIt(openFileDialog.FileName);
                    initializeSlidesUI();
                }
            }
        }

        private Boolean saveIfDirty()
        {
            String snapshot = serializeCurrentAlbum();
            if (snapshot != lastSavedAlbum)
            {
                MessageBoxResult result = MessageBox.Show("Save changes to " + currentFileName + "?", "KB30", MessageBoxButton.YesNoCancel);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        return saveIt(currentFileName);
                    case MessageBoxResult.No:
                        return true;
                    case MessageBoxResult.Cancel:
                        return false;
                }
            }
            return true;
        }

        private Boolean albumValid()
        {
            if (slides.Count <= 0)
            {
                MessageBox.Show("Must have at least 1 slide.");
                return false;
            }
            for (int s = 0; s < slides.Count; s++)
            {
                Slide slide = slides[s];
                if (slide.keys.Count <= 0)
                {
                    MessageBox.Show("Slide " + s.ToString() + " must have at least 1 keyframe.");
                    return false;
                }
            }
            return true;
        }

        private String serializeCurrentAlbum()
        {
            Album album = new Album();

            album.version = CONFIG_VERSION.ToString();
            if (soundtrack.Length > 0)
            {
                album.soundtrack = Path.GetRelativePath(Path.GetDirectoryName(currentFileName), soundtrack);
            }
            album.slides = slides;

            return JsonConvert.SerializeObject(album, Formatting.Indented);
        }

        private Boolean saveIt(String filename)
        {
            if (albumValid())
            {
                currentFileName = filename;
                foreach (Slide slide in slides)
                {
                    slide.basePath = Path.GetDirectoryName(filename);
                }
                lastSavedAlbum = serializeCurrentAlbum();
                File.WriteAllText(filename, lastSavedAlbum);
                return true;
            }
            return false;
        }

        private void fileSaveAsClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "KB30 files (*.kb30)|*.kb30|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                saveIt(saveFileDialog.FileName);
                currentFileName = saveFileDialog.FileName;
            }
        }

        private void fileSaveClick(object sender, RoutedEventArgs e)
        {
            if (currentFileName == "" || currentFileName == "untitled")
            {
                fileSaveAsClick(sender, e);
            }
            else
            {
                saveIt(currentFileName);
            }
        }
        private void fileDetailsClick(object sender, RoutedEventArgs e)
        {
            double totalDuration = 0;
            foreach (Slide s in slides)
            {
                foreach (KF k in s.keys)
                {
                    totalDuration += k.duration;
                }
            }
            totalDuration += slides.Count * 1.5;  // fade in/out Transitions
            int durationMins = (int)(totalDuration / 60);
            int durationSecs = (int)(totalDuration % 60);
            MessageBox.Show(slides.Count + " slides." + Environment.NewLine +
                            "Total duration " + durationMins + ":" + durationSecs.ToString("D2") + Environment.NewLine +
                            "Soundtrack: " + soundtrack, "File Info");
        }

        private void mainWindowClosing(object sender, CancelEventArgs e)
        {
            if (saveIfDirty() == false)
            {
                e.Cancel = true;
            }
        }

        private void soundtrackClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Sound files (*.mp3)|*.mp3|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                soundtrack = openFileDialog.FileName;
            }
        }

        private void blankUI()
        {
            slides.Clear();
            keyframePanel.Children.Clear();
            slidePanel.Children.Clear();
            imageCropper.image.Source = null;
            imageCropper.cropper.Visibility = Visibility.Collapsed;
            currentSlideIndex = 0;
            currentKeyframeIndex = 0;
            caption.Text = "Add a slide to get started...";
        }
        /*************
         * Drag and Drop
         */
        private void slidePreviewMouseDown(object sender, MouseButtonEventArgs e, Slide slide)
        {
            initialMousePosition = e.GetPosition(slide.slideControl);
        }

        private SlideAdorner slideAdorner;
        private void slideMouseMove(object sender, System.Windows.Input.MouseEventArgs e, Slide slide)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var movedDistance = Point.Subtract(initialMousePosition, e.GetPosition(slide.slideControl)).Length;
                if (movedDistance > 10)
                {
                    // package the data
                    DataObject data = new DataObject();
                    data.SetData("SlideControl", slide);

                    // If this slide is already checked, then we move all checked slides.  If its not checked, then we move only this one. 
                    if (!slide.IsChecked())
                    {
                        slides.UncheckAll();
                    }
                    // dim and count all the slides that will be dragged
                    selectSlide(slide, true, false);
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
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
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
                if (target_slide.IsChecked())
                {
                    Console.Beep(600, 200);
                    return;  // don't drop on self
                }
                if (!source_slide.slideControl.IsChecked())
                {
                    source_slide.slideControl.Check();
                }
                cutSlides(source_slide);
                pasteSlides(target_slide, dropDirection(e, target_slide));
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
                    slideAdorner.Arrange(new Rect(p.X, p.Y, slideAdorner.DesiredSize.Width, slideAdorner.DesiredSize.Height));
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

                e.Effects = DragDropEffects.Move;
            }
        }

        private void slideGiveFeedback(object sender, GiveFeedbackEventArgs e, Slide s)
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
    }
 
}

