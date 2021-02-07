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
using System.Diagnostics;


/*
 * Bugs:
 * To DO:
 *  - Undo/Redo
 *  - Drag and Drop from finder left panel into main window left panel.
 *  - Edit Soundtrack (maybe multiple tracks)
 *  - Option to lock cropper within bounds of image
 *  Break up this file (drag and drop in own file?)
 *  Config options both global and local to this album:
 *   - Absolute/Relative paths 
 *   - default fadein/out duration
 *   - default key duration
 *  Duration textbox allow only numeric.
 *  Key to reverse keyframe order
 */
namespace KB30
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window
    {
        const double DEFAULT_DURATION = 9.0;

        const int ABOVE = 1;
        const int BELOW = 2;
        const int LEFT = 3;
        const int RIGHT = 4;

        public int currentSlideIndex = 0;
        public int currentKeyframeIndex = 0;
        public Slides slides = new Slides();
        public Album album;
        private String lastSavedAlbum;
        public History history;
        private Boolean playWithArgumentFile = false;
        private Boolean uiLoaded = false;
        public Slides clipboardSlides = new Slides();
        public Keyframe clipboardKey = null;
        private Slide startDragSlide = null;
        private Point initialSlideMousePosition;
        private Keyframe startDragKeyframe = null;
        private Point initialKeyframeMousePosition;

        public MainWindow()
        {
            InitializeComponent();
            album = new Album(slides, "", "untitled");
            this.Title = "KB30 - " + album.Filename;
            history = new History(this);
        }
 

        /************
         *  Animate!
         */

        private void playIt(int start = 0)
        {
            AnimationWindow animationWindow = new AnimationWindow();
            animationWindow.Closed += animationWindow_Closed;
            animationWindow.Show();
            animationWindow.animate(slides, start, album.Soundtrack);
        }
        private void playClick(object sender, RoutedEventArgs e)
        {
            if (album.Valid())
            {
                playIt();
            }
        }
        private void playSlideClick(object sender, RoutedEventArgs e)
        {
            if (album.Valid())
            {
                Slides oneSlide = new Slides();
                oneSlide.Add(currentSlide);
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
            lastSavedAlbum = album.ToJson();
            var allArgs = Environment.GetCommandLineArgs();
            if (allArgs.Length > 1)
            {
                var filenameArgument = allArgs[1];
                playWithArgumentFile = true;
                if (loadIt(filenameArgument))
                {
                    playIt();
                }
            }
            /* debug */
            else
            {
                loadIt("C:\\Users\\clark\\source\\repos\\pictures\\cardZERO.kb30");
                initializeSlidesUI();

            }
           /**/
        }


        /************
         *  File Menu
         */
        private void finderClick(object sender, RoutedEventArgs e)
        {
            FinderWindow finderWindow = new FinderWindow();
            finderWindow.Owner = this;
            finderWindow.Show();
        }
        private void fileNewClick(object sender, RoutedEventArgs e)
        {
            if (saveIfDirty())
            {
                blankUI();
                album = new Album(slides, "", "untitled");
                lastSavedAlbum = album.ToJson();
                this.Title = "KB30 - " + album.Filename;
            }
        }

        private bool loadIt(string filename)
        {
            Album old_album = album;  // just in case

            try { 
                album = Album.LoadFromFile(filename);
            } catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }
            slides = album.slides;

            lastSavedAlbum = album.ToJson();
            this.Title = "KB30 - " + album.Filename;
            return true;
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
                    if (loadIt(openFileDialog.FileName)){ 
                        initializeSlidesUI();
                    }
                }
            }
        }

        private Boolean saveIfDirty()
        {
            String snapshot = album.ToJson();
            if (snapshot != lastSavedAlbum)
            {
                MessageBoxResult result = MessageBox.Show("Save changes to " + album.Filename + "?", "KB30", MessageBoxButton.YesNoCancel);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        lastSavedAlbum = album.SaveToFile();
                        if (lastSavedAlbum == "")
                        {
                            return false;
                        } 
                        return true;
                    case MessageBoxResult.No:
                        return true;
                    case MessageBoxResult.Cancel:
                        return false;
                }
            }
            return true;
        }


        private void fileSaveAsClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "KB30 files (*.kb30)|*.kb30|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                album.Filename = saveFileDialog.FileName;
                this.Title = "KB30 - " + album.Filename;
                lastSavedAlbum = album.SaveToFile();
            }
        }

        private void fileSaveClick(object sender, RoutedEventArgs e)
        {
            if (album.Filename == "" || album.Filename == "untitled")
            {
                fileSaveAsClick(sender, e);
            }
            else
            {
                lastSavedAlbum = album.SaveToFile();
            }
        }
        private void fileDetailsClick(object sender, RoutedEventArgs e)
        {
            double totalDuration = 0;
            foreach (Slide s in slides)
            {
                foreach (Keyframe k in s.keys)
                {
                    totalDuration += k.duration;
                }
            }
            totalDuration += slides.Count * 1.5;  // fade in/out Transitions
            int durationMins = (int)(totalDuration / 60);
            int durationSecs = (int)(totalDuration % 60);
            MessageBox.Show(slides.Count + " slides." + Environment.NewLine +
                            "Total duration " + durationMins + ":" + durationSecs.ToString("D2") + Environment.NewLine +
                            "Soundtrack: " + album.Soundtrack, "File Info");
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
                album.Soundtrack = openFileDialog.FileName;
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
            caption.Text = "<---- Add a slide to get started...";
        }


        private void mainWindowActivated(object sender, EventArgs e)
        {
            //       Topmost = true;
        }

        private void mainWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                switch (e.Key)
                {
                    case Key.X:
                        cutSlidesToClipboard(currentSlide);
                        e.Handled = true;
                        break;
                    case Key.C:
                        copySlidesToClipboard(currentSlide);
                        e.Handled = true;
                        break;
                    case Key.V:
                        pasteClipboardSlides(currentSlide);
                        e.Handled = true;
                        break;
                    case Key.Z:
                        
                        e.Handled = true;
                        break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Up:
                        if (currentSlideIndex > 0)
                        {
                            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                            {
                                slides.UncheckAll();
                            }
                            selectSlide(currentSlideIndex - 1);
                            currentSlide.Check();
                            currentSlide.BringIntoView();
                        }
                        e.Handled = true;
                        break;
                    case Key.Down:
                        if (currentSlideIndex < slides.Count - 1)
                        {
                            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                            {
                                slides.UncheckAll();
                            }
                            selectSlide(currentSlideIndex + 1);
                            currentSlide.Check();
                            currentSlide.BringIntoView();
                        }
                        e.Handled = true;
                        break;
                }
            }
        }
    }
}
