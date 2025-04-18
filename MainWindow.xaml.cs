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
using System.Diagnostics;


/*
 * Bugs:
 * To DO:
 *  - Export 
 *  - Portrait Mode
 *  - Undo/Redo needs work
 *  Config options both global and local to this album:
 *   - default fadein/out duration
 *   - default key duration
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
        public History history;
        private Boolean playWithArgumentFile = false;
        private Boolean uiLoaded = false;
        public Slides clipboardSlides = new Slides();
        public Keyframe clipboardKey = null;
        private Slide startDragSlide = null;
        private Point initialSlideMousePosition;
        public ImageExplorerWindow imageExplorerWindow = null;

        public MainWindow()
        {
            InitializeComponent();
            album = new Album(slides, "untitled");
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
            animationWindow.Closing += animationWindow_Closing;
            animationWindow.Show();
            animationWindow.Animate(slides, start);
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
                animationWindow.Animate(oneSlide);
            }
        }
        private void playFromHereClick(object sender, RoutedEventArgs e)
        {
            if(currentSlideIndex >= 0)
            {
                playIt(currentSlideIndex);
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

        private void animationWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!playWithArgumentFile)
            {
                AnimationWindow animationWindow = (AnimationWindow)sender;
                selectSlide(animationWindow.currentSlideIndex);
                currentSlide.BringIntoView();
            }
        }


        private void mainWindowLoaded(object sender, RoutedEventArgs e)
        {
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
            /* debug 
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
        private void imageExplorerClick(object sender, RoutedEventArgs e)
        {
            showImageExplorer();
        }
        private void fileNewClick(object sender, RoutedEventArgs e)
        {
            if (album.SaveIfDirty())
            {
                blankUI();
                album = new Album(slides, "untitled");
                this.Title = "KB30 - " + album.Filename;
            }
        }

        private bool loadIt(string filename)
        {
            Album old_album = album;  // just in case

            try { 
                album = new Album(filename);
            } catch (Exception e)
            {
                if (e.Message != "Quiet")
                {
                    MessageBox.Show(e.Message);
                }
                return false;
            }
            slides = album.slides;
            this.Title = "KB30 - " + album.Filename;
            return true;
        }

        private void fileOpenClick(object sender, RoutedEventArgs e)
        {
            if (album.SaveIfDirty())
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "KB30 files (*.kb30)|*.kb30|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == true)
                {
                    caption.Text = "Loading Thumbnails...";
                    if (loadIt(openFileDialog.FileName)){ 
                        initializeSlidesUI();
                    }
                }
            }
        }

        private void fileSaveAsClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = Path.GetFileName(album.Filename);
            saveFileDialog.Filter = "KB30 files (*.kb30)|*.kb30|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                album.Filename = saveFileDialog.FileName;
                this.Title = "KB30 - " + album.Filename;
                album.SaveToFile();
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
                album.SaveToFile();
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
                            "Total duration " + durationMins + ":" + durationSecs.ToString("D2"), "File Info");
        }

        public void showImageExplorer()
        {
            if (imageExplorerWindow == null)
            {
                imageExplorerWindow = new ImageExplorerWindow();
                imageExplorerWindow.mainWindow = this;
                if (slides.Count > 0 && currentSlide.fileName != "black" && currentSlide.fileName != "white")
                {
                    imageExplorerWindow.starting_path = Path.GetDirectoryName(currentSlide.fileName);
                }
                imageExplorerWindow.Closed += ImageExplorerWindow_Closed;
                if (this.Top > 100)
                {
                    imageExplorerWindow.Top = this.Top - 100;
                } else
                {
                    imageExplorerWindow.Top = 0;
                }

                imageExplorerWindow.Left = this.Left + 200;
                imageExplorerWindow.Show();
            }
            else
            {
                imageExplorerWindow.Activate();
            }
        }

        private void ImageExplorerWindow_Closed(object sender, EventArgs e)
        {
            imageExplorerWindow = null;
        }

        private void mainWindowClosing(object sender, CancelEventArgs e)
        {
            if (album.SaveIfDirty() == false)
            {
                e.Cancel = true;
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

        }

        public void nextSlide()
        {
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
        }

        public void prevSlide()
        {
            if (currentSlideIndex > 0) {
                if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                {
                    slides.UncheckAll();
                }
                selectSlide(currentSlideIndex - 1);
                currentSlide.Check();
                currentSlide.BringIntoView();
            }
        }

        public void nexKeyframe()
        {
            if (currentKeyframeIndex == currentSlide.keys.Count - 1)
            {
                nextSlide();
            }
            else
            {
                selectKeyframe(currentKeyframeIndex + 1);
            }
        }

        public void prevKeyframe()
        {
            if (currentKeyframeIndex == 0)
            {
                prevSlide();
                selectKeyframe(currentSlide.keys.Last());
            }
            else
            {
                selectKeyframe(currentKeyframeIndex - 1);
            }
        }

        private void mainWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                switch (e.Key)
                {
                    case Key.S:
                        fileSaveClick(sender, e);
                        e.Handled = true;
                        break;
                    case Key.O:
                        fileOpenClick(sender, e);
                        e.Handled = true;
                        break;
                    case Key.N:
                        fileNewClick(sender, e);
                        e.Handled = true;
                        break;
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
                        history.Undo();
                        e.Handled = true;
                        break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Up:
                        prevSlide();
                        e.Handled = true;
                        break;
                    case Key.Down:
                        nextSlide();
                        e.Handled = true;
                        break;
                    case Key.Left:
                        prevKeyframe();
                        e.Handled = true;
                        break;
                    case Key.Right:
                        nexKeyframe();
                        e.Handled = true;
                        break;
                    case Key.Enter:
                        playFromHereClick(sender, e);
                        e.Handled = true;
                        break;
                }
            }
        }
    }
}
