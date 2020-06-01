using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Microsoft.Win32;
using System.ComponentModel;
using Newtonsoft.Json;
using Microsoft.VisualBasic.CompilerServices;
using System.Configuration;


/*
 * To DO:  
 *  Drag and Drop slides and keys to re-order
 *  Progress bar while loading images
 *  
 *  
 *  Bugs:
 *  - Resize window -> resize preview image -> cropper control doesn't resize.  (touch current keyframe control fixes it)
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
        public Slide clipboardSlide = null;
        private String initialConfig;

        public MainWindow()
        {
            InitializeComponent();
        }

        public class Config
        {
            public string version { get; set; }
            public List<Slide> slides { get; set; }
        }
        
        /*********
         *  Slides 
         */
        public void initializeSlidesUI()
        {
            Cursor = Cursors.Wait;
            slidePanel.Children.Clear();

            for (int i = 0; i < slides.Count; i++)
            {
                Slide slide = slides[i];
                addSlideControl(slide);
            }
            selectSlide(0);
            Cursor = Cursors.Arrow;
        }

        public void addSlideControl(Slide slide)
        {
            SlideControl slideControl = new SlideControl();
            BitmapImage bmp = new BitmapImage(new Uri(slide.fileName));
            slideControl.image.Source = bmp;
            slideControl.caption.Text = System.IO.Path.GetFileName(slide.fileName) + " (" + bmp.PixelWidth + " x " + bmp.PixelHeight + ")";
            slideControl.button.Click += delegate (object sender, RoutedEventArgs e) { slideClick(sender, e, slide); };
            slideControl.CMCut.Click += delegate (object sender, RoutedEventArgs e) { cutSlideClick(sender, e, slide); };
            slideControl.CMPaste.Click += delegate (object sender, RoutedEventArgs e) { pasteSlideClick(sender, e, slide); };
            slideControl.SlideContextMenu.Opened += delegate (object sender, RoutedEventArgs e) { slideContextMenuOpened(sender, e, slide); };
            slide.slideControl = slideControl;
            slidePanel.Children.Add(slideControl);
            slideControl.DeSelect();
        }

        public void selectSlide(int slideIndex)
        {
            (slidePanel.Children[currentSlideIndex] as SlideControl).DeSelect();
            KF oldKey = slides[currentSlideIndex].keys[currentKeyframeIndex];
            KeyframeControl oldKFControl = oldKey.kfControl;
            unBindKFC(oldKFControl, oldKey);

            currentSlideIndex = slideIndex;
            Uri uri = new Uri(slides[slideIndex].fileName);
            var bitmap = new BitmapImage(uri);
            imageCropper.image.Source = bitmap;
            (slidePanel.Children[currentSlideIndex] as SlideControl).Select();
            currentKeyframeIndex = 0;
            initializeKeysUI(slides[currentSlideIndex]);
        }

        private void slideClick(object sender, RoutedEventArgs e, Slide slide)
        {
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
                    newSlide.keys.Add(new KF(4.0, 0.5, 0.5, 0));
                    slides.Add(newSlide);
                    addSlideControl(newSlide);
                    selectSlide(slides.Count - 1);
                }
            }
        }

        private void cutSlideClick(object sender, RoutedEventArgs e, Slide slide)
        {
            if (slides.Count == 1)
            {
                MessageBox.Show("At least one slide is required");
                return;
            }
            var victimIndex = slides.IndexOf(slide);

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
            clipboardSlide = slide;
            slides.Remove(slide);

            if (currentSlideIndex > victimIndex) { currentSlideIndex--; }
        }

        private void pasteSlideClick(object sender, RoutedEventArgs e, Slide slide)
        {
            var insertIndex = slides.IndexOf(slide);
            slides.Insert(insertIndex, clipboardSlide);
            slidePanel.Children.Insert(insertIndex, clipboardSlide.slideControl);
            if (currentSlideIndex >= insertIndex)
            {
                currentSlideIndex++;
            }
            clipboardSlide = null;
        }

        private void slideContextMenuOpened(object sender, RoutedEventArgs e, Slide slide)
        {
            if(clipboardSlide == null)
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

            selectKeyframe(keys[0], 0);
        }

        public void addKeyframeControl(KF key)
        {
            KeyframeControl kfControl = new KeyframeControl();
            kfControl.DeSelect();
            key.kfControl = kfControl;
            keyframePanel.Children.Add(kfControl);

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

            kfControl.CMDelete.Click += delegate (object sender, RoutedEventArgs e) { deleteKeyframeClick(sender, e, key); };
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

        public void selectKeyframe(KF key, int keyFrameIndex)
        {
            KF oldKey = slides[currentSlideIndex].keys[currentKeyframeIndex];
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
            List<KF> keys = slides[currentSlideIndex].keys;
            int index = keys.IndexOf(key, 0);
            selectKeyframe(key, index);
        }

        private void addKeyframeClick(object sender, RoutedEventArgs e)
        {
            if (slides.Count == 0) { return; }
            Slide currentSlide = slides[currentSlideIndex];
            KF currentKey = currentSlide.keys[currentKeyframeIndex];
            KF newKey = new KF(currentKey.zoomFactor, currentKey.x, currentKey.y, DEFAULT_DURATION);
            slides[currentSlideIndex].keys.Add(newKey);
            addKeyframeControl(newKey);
            selectKeyframe(newKey, currentSlide.keys.Count - 1);
        }

        private void deleteKeyframeClick(object sender, RoutedEventArgs e, KF key)
        {
            List<KF> keys = slides[currentSlideIndex].keys;
            if (keys.Count > 1)
            {
                KeyframeControl kfc = key.kfControl;
                keyframePanel.Children.Remove(kfc);
                keys.Remove(key);
                if (currentKeyframeIndex >= keys.Count) { currentKeyframeIndex = 0; }
            }
            else
            {
                MessageBox.Show("A minimum of one keyframe is required.");
            }
        }


        /************
         *  Animate!
         */

        private void playIt()
        {
            AnimationWindow animationWindow = new AnimationWindow();
            animationWindow.Show();
            animationWindow.animate(slides);
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

        private void mainWindowLoaded(object sender, RoutedEventArgs e)
        {
            initialConfig = serializeCurrentConfig();
            var allArgs = Environment.GetCommandLineArgs();
            if (allArgs.Length > 1)
            {
                var immediateFileName = allArgs[1];
                loadIt(immediateFileName);
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
                initialConfig = serializeCurrentConfig();
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
                MessageBox.Show("Congif File version is newer than this version of the program.");
                return;
            }
            initialConfig = jsonString;
            currentFileName = filename;
            this.Title = "KB30 - " + currentFileName;
            slides = config.slides;
            initializeSlidesUI();
        }

        private void fileOpenClick(object sender, RoutedEventArgs e)
        {
            if (saveIfDirty())
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "KB30 files (*.kb30)|*.kb30|All files (*.*)|*.*";
                if (openFileDialog.ShowDialog() == true)
                {
                    loadIt(openFileDialog.FileName);
                }
            }
        }

        private Boolean saveIfDirty()
        {
            if (serializeCurrentConfig() != initialConfig)
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
                if (slide.keys.Count <= 1)
                {
                    MessageBox.Show("Slide " + s.ToString() + " must have more than 1 keyframe.");
                    return false;
                }
            }
            return true;
        }

        private String serializeCurrentConfig()
        {
            string jsonString;
            Config config = new Config();

            config.version = CONFIG_VERSION.ToString();
            config.slides = slides;

            return JsonConvert.SerializeObject(config, Formatting.Indented);
        }

        private Boolean saveIt(String filename)
        {
            if (configValid())
            {
                File.WriteAllText(filename, serializeCurrentConfig());
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
    }
}

