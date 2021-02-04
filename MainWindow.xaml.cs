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
    public partial class MainWindow : Window
    {
        const double CONFIG_VERSION = 1.0;
        const double DEFAULT_DURATION = 9.0;

        const int ABOVE = 1;
        const int BELOW = 2;
        const int LEFT = 3;
        const int RIGHT = 4;

        public int currentSlideIndex = 0;
        public int currentKeyframeIndex = 0;
        private String currentFileName = "untitled";
        public Slides slides = new Slides();
        public Slides clipboardSlides = new Slides();
        public Keyframe clipboardKey = null;
        private String lastSavedAlbum;
        private String soundtrack = "";
        private Boolean playWithArgumentFile = false;
        private Boolean uiLoaded = false;
        private Slide startDragSlide = null;
        private Point initialSlideMousePosition;
        private Keyframe startDragKeyframe = null;
        private Point initialKeyframeMousePosition;
        public History history; 

        public MainWindow()
        {
            InitializeComponent();
            this.Title = "KB30 - " + currentFileName;
            history = new History(this);
        }

        public class Album
        {
            public string version { get; set; }
            public string soundtrack { get; set; }
            public Slides slides { get; set; }
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
                currentFileName = "untitled";
                soundtrack = "";
                lastSavedAlbum = serializeCurrentAlbum();
                this.Title = "KB30 - " + currentFileName;
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
                    slide.basePath = Path.GetDirectoryName(currentFileName);
                }
                lastSavedAlbum = serializeCurrentAlbum();
                File.WriteAllText(currentFileName, lastSavedAlbum);
                this.Title = "KB30 - " + currentFileName;
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
                currentFileName = saveFileDialog.FileName;
                saveIt(currentFileName);
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
