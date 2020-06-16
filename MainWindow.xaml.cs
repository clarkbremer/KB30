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


/*
 * To DO:  
 *  Drag and Drop slides and keys to re-order
 *  Insert or append group of slide - load in background?
 *  Cut/Paste multiple slides
 *  
 *  Bugs:
 *  - skip ahead/back sometimes stops animation - opacity on wrong frame?
 *   
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
        public int currentSlideIndex = 0;
        public int currentKeyframeIndex = 0;
        private String currentFileName = "untitled";
        public List<Slide> slides = new List<Slide>();
        public List<Slide> clipboardSlides = new List<Slide>();
        public KF clipboardKey = null;
        private String lastSavedConfig;
        private String soundtrack = "";
        private Boolean playWithArgumentFile = false;
        private Boolean uiLoaded = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        public class Config
        {
            public string version { get; set; }
            public string soundtrack { get; set; }
            public List<Slide> slides { get; set; }
        }
        
        /*********
         *  Slides 
         */
        public void initializeSlidesUI()
        {
            currentSlideIndex = 0;
            currentKeyframeIndex = 0;
            slidePanel.Children.Clear();
            addSlideControlInBackground(slides[0]);
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
            if(i == 0)
            {
                selectSlide(0, false);
            }
            i++;
            if(i < slides.Count) {
                caption.Text = "Loading slide " + (i + 1).ToString() + " of " + (slides.Count).ToString();
                addSlideControlInBackground(slides[i]);
            }
            else
            {
                caption.Text = "Done Loading";
                selectSlide(0, false);
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
            slideControl.button.Click += delegate (object sender, RoutedEventArgs e) { slideClick(sender, e, slide); };
            slideControl.CMCut.Click += delegate (object sender, RoutedEventArgs e) { cutSlideClick(sender, e, slide); };
            slideControl.CMPaste.Click += delegate (object sender, RoutedEventArgs e) { pasteSlideClick(sender, e, slide); };
            slideControl.CMInsert.Click += delegate (object sender, RoutedEventArgs e) { insertSlideClick(sender, e, slide); };
            slideControl.CMPlayFromHere.Click += delegate (object sender, RoutedEventArgs e) { playFromHereClick(sender, e, slide); };
            slideControl.SlideContextMenu.Opened += delegate (object sender, RoutedEventArgs e) { slideContextMenuOpened(sender, e, slide); };
            slide.slideControl = slideControl;
            if(insertPosition == -1)
            {
                slidePanel.Children.Add(slideControl);
            }
            else
            {
                slidePanel.Children.Insert(insertPosition, slideControl);
            }
            slideControl.DeSelect();
            return true;
        }

        public void selectSlide(int slideIndex, Boolean unbindOld = true)
        {
            slides[currentSlideIndex].slideControl.DeSelect();
            if (unbindOld) { 
                KF oldKey = slides[currentSlideIndex].keys[currentKeyframeIndex];
                KeyframeControl oldKFControl = oldKey.kfControl;
                unBindKFC(oldKFControl, oldKey);
            }
            currentSlideIndex = slideIndex;
            var bitmap = new BitmapImage(slides[slideIndex].uri);
            imageCropper.image.Source = bitmap;

            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl))
            {
                foreach (SlideControl sc in slidePanel.Children) { sc.UnCheck(); }
            }

            slides[currentSlideIndex].slideControl.Select();

            currentKeyframeIndex = 0;
            initializeKeysUI(slides[currentSlideIndex]);
            caption.Text = slides[currentSlideIndex].fileName + " (" + bitmap.PixelWidth + " x " + bitmap.PixelHeight + ")";
        }

        private void slideClick(object sender, RoutedEventArgs e, Slide slide)
        {
            if (e.OriginalSource is CheckBox) { return; }

            int index = slides.IndexOf(slide, 0);
            selectSlide(index);
        }

        private void addSlideClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select image file(s)";
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Images (*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (String fname in openFileDialog.FileNames)
                {
                    Slide newSlide = new Slide(fname);
                    newSlide.keys.Add(new KF(4.0, 0.5, 0.5, 1));
                    if (addSlideControl(newSlide))
                    {
                        slides.Add(newSlide);
                        if (fname == openFileDialog.FileNames.First())
                        {
                            selectSlide(slides.Count - 1);
                        }
                    }
                }
            }
        }

        private void insertSlideClick(object sender, RoutedEventArgs e, Slide slide)
        {
            var insertIndex = slides.IndexOf(slide);

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select image file(s)";
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Images (*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (String fname in openFileDialog.FileNames)
                {
                    Slide newSlide = new Slide(fname);
                    newSlide.keys.Add(new KF(4.0, 0.5, 0.5, 1));
                    if (addSlideControl(newSlide, null, insertIndex))
                    {
                        slides.Insert(insertIndex, newSlide);
                        if (currentSlideIndex >= insertIndex)
                        {
                            currentSlideIndex++;
                        }
                        if (fname == openFileDialog.FileNames.First())
                        {
                            selectSlide(insertIndex, true);
                        }
                        insertIndex++;
                    }
                }
            }
        }

        private void pasteSlideClick(object sender, RoutedEventArgs e, Slide insertSlide)
        {
            var insertIndex = slides.IndexOf(insertSlide);
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
            foreach (Slide slide in slides) { slide.slideControl.UnCheck(); }
        }

        private void cutSlideClick(object sender, RoutedEventArgs e, Slide s)
        {
            clipboardSlides.Clear();
            foreach (Slide slide in slides)
            {
                if (slide.slideControl.IsChecked())
                {
                    clipboardSlides.Add(slide);
                }
            }
            if(clipboardSlides.Count == 0)
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
        }

        private void playFromHereClick(object sender, RoutedEventArgs e, Slide slide)
        {
            playIt(slides.IndexOf(slide));
        }

        private void slideContextMenuOpened(object sender, RoutedEventArgs e, Slide slide)
        {
            if(clipboardSlides.Count == 0)
            {
                slide.slideControl.CMPaste.IsEnabled = false;
            }
            else
            {
                slide.slideControl.CMPaste.IsEnabled = true;
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
            if(insertIndex == -1)
            {
                keyframePanel.Children.Add(kfControl);
            }
            else
            {
                keyframePanel.Children.Insert(insertIndex, kfControl);
            }

            kfControl.Margin = new Thickness(2, 2, 2, 2);
            kfControl.button.Click += delegate (object sender, RoutedEventArgs e) { keyFrameClick(sender, e, key); };

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

            imageCropper.UpdateLayout();
            imageCropper.cropZoom = key.zoomFactor;
            imageCropper.cropX = key.x;
            imageCropper.cropY = key.y;

            imageCropper.UpdateLayout();

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
            selectKeyframe(slides[currentSlideIndex].keys[currentKeyframeIndex]);
        }


        /************
         *  Animate!
         */

        private void playIt(int start=0)
        {
            AnimationWindow animationWindow = new AnimationWindow();
            animationWindow.Closed += animationWindow_Closed;
            animationWindow.Show();
            animationWindow.animate(slides, start, soundtrack);
        }
        private void playClick(object sender, RoutedEventArgs e)
        {
            if (configValid())
            {
                playIt();
            }
        }
        private void playSlideClick(object sender, RoutedEventArgs e)
        {
            if (configValid())
            {
                List<Slide> oneSlide = new List<Slide>();
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
            lastSavedConfig = serializeCurrentConfig();
            var allArgs = Environment.GetCommandLineArgs();
            if (allArgs.Length > 1)
            {
                var filenameArgument = allArgs[1];
                playWithArgumentFile = true;                
                loadIt(filenameArgument);
                playIt();
            }
        }


        /************
         *  File Menu
         */
        private void fileNewClick(object sender, RoutedEventArgs e)
        {
            if (saveIfDirty())
            {
                slides.Clear();
                keyframePanel.Children.Clear();
                slidePanel.Children.Clear();
                imageCropper.image.Source = null;
                imageCropper.cropper.Visibility = Visibility.Collapsed;
                currentSlideIndex = 0;
                currentKeyframeIndex = 0;
                currentFileName = "untitled";
                lastSavedConfig = serializeCurrentConfig();
                caption.Text = "Add a slide to get started...";
            }
        }

        private void loadIt(string filename)
        {
            Config config = new Config();
            string jsonString;

            jsonString = File.ReadAllText(filename);
            config = JsonConvert.DeserializeObject<Config>(jsonString);
            if (Convert.ToDouble(config.version) > CONFIG_VERSION)
            {
                MessageBox.Show("Config File version is newer than this version of the program.");
                return;
            }
            if (config.soundtrack != null)
            {
                soundtrack = config.soundtrack;
            }
            slides = config.slides;
            for (int i = slides.Count - 1; i >= 0; i--)
            {
                if (!File.Exists(slides[i].fileName))
                {
                    MessageBox.Show("File Not Found: " + slides[i].fileName, "File Not Found");
                    slides.RemoveAt(i);
                }
            }
            lastSavedConfig = jsonString;
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
            String snapshot = serializeCurrentConfig();
            if (snapshot != lastSavedConfig)
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

        private Boolean configValid()
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

        private String serializeCurrentConfig()
        {
            Config config = new Config();

            config.version = CONFIG_VERSION.ToString();
            config.soundtrack = soundtrack;
            config.slides = slides;

            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }

        private Boolean saveIt(String filename)
        {
            if (configValid())
            {
                lastSavedConfig = serializeCurrentConfig();
                File.WriteAllText(filename, lastSavedConfig);
                currentFileName = filename;
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
    }
}

