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


/*
 * To DO:  
 *  Animate Window
 *  Delete Slide (right click -> context menu -> delete?)
 *  Display current image file name
 *  Drag and Drop slides and keys to re-order
 *  Progress bar while loading images
 *  Remane "thumb" to "slide"
 *  
 *  Bugs:
 *  - Resize window -> resize preview image -> cropper control doesn't resize.
 *   
 */
namespace KB30
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const double DEFAULT_DURATION = 5.0;
        public int currentSlideIndex = 0;
        public int currentKeyframeIndex = 0;
        private String currentFileName = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        public class KF : FrameworkElement
        {
            [JsonProperty] 
            public double zoomFactor { get; set; }
            [JsonProperty] 
            public double x { get; set; }
            [JsonProperty] 
            public double y { get; set; }
            [JsonProperty] 
            public double duration { get; set; }

            public static readonly DependencyProperty zoomFactorProperty = DependencyProperty.Register("zoomFactor", typeof(double), typeof(KF));
            public static readonly DependencyProperty xProperty = DependencyProperty.Register("x", typeof(double), typeof(KF));
            public static readonly DependencyProperty yProperty = DependencyProperty.Register("y", typeof(double), typeof(KF));
            public static readonly DependencyProperty durationProperty = DependencyProperty.Register("duration", typeof(double), typeof(KF));
           

            public KF(double z, double x, double y, double d)
            {
                this.zoomFactor = z;
                this.x = x;
                this.y = y;
                this.duration = d;
            }

            public KF() { }

            public KeyframeControl kfControl { get; set; }
         }

        public class Slide
        {
            public string fileName { get; set; }
            public List<KF> keys { get; set; }
            public Slide()
            {
                keys = new List<KF>();
            }
            public Slide(string s)
            {
                fileName = s;
                keys = new List<KF>();
            }

            [JsonIgnore]
            public ThumbButtonControl thumb { get; set; }
        }

        public List<Slide> slides = new List<Slide>();

        public class Config
        {
            public string version { get; set; }
            public List<Slide> slides { get; set; }
        }

        private void unBindKFC(KeyframeControl kfc, KF key)
        {
            if(kfc == null)
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
            if (Double.TryParse(tb.Text, out maybe)){
                if(!Double.IsNaN(maybe))
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


        public void selectSlide(int slideIndex)
        {
            (slidePanel.Children[currentSlideIndex] as ThumbButtonControl).DeSelect();
            KF oldKey = slides[currentSlideIndex].keys[currentKeyframeIndex];
            KeyframeControl oldKFControl = oldKey.kfControl;
            unBindKFC(oldKFControl, oldKey);

            currentSlideIndex = slideIndex;
            Uri uri = new Uri(slides[slideIndex].fileName);
            var bitmap = new BitmapImage(uri);
            imageCropper.image.Source = bitmap;
            (slidePanel.Children[currentSlideIndex] as ThumbButtonControl).Select();
            currentKeyframeIndex = 0;
            initializeKeysUI(slides[currentSlideIndex]);
        }


        private void deleteSlideClick(object sender, RoutedEventArgs e, Slide slide)
        {
            if(slides.Count == 1)
            {
                MessageBox.Show("At least one slide is required");
                return;
            }

            if (slides.IndexOf(slide) == 0)
            {
                selectSlide(1);
                currentSlideIndex = 0;
            }
            else
            {
                selectSlide(0);
            }
            slidePanel.Children.Remove(slide.thumb);
            slides.Remove(slide);
        }
            
        private void slideClick(object sender, RoutedEventArgs e, Slide slide)
        {
            int index = slides.IndexOf(slide, 0);
            selectSlide(index);
        }

        public void addSlideControl(Slide slide)
        {
            ThumbButtonControl thumbButton = new ThumbButtonControl();
            thumbButton.image.Source = new BitmapImage(new Uri(slide.fileName));
            thumbButton.button.Click += delegate (object sender, RoutedEventArgs e) { slideClick(sender, e, slide); };
            thumbButton.CMDelete.Click += delegate (object sender, RoutedEventArgs e) { deleteSlideClick(sender, e, slide); };
            slide.thumb = thumbButton;
            slidePanel.Children.Add(thumbButton);
            thumbButton.DeSelect();
        }

        public void initializeUI()
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


        private void fileNewClick(object sender, RoutedEventArgs e) { MessageBox.Show("File New"); }

        private void fileOpenClick(object sender, RoutedEventArgs e) {
            Config config = new Config();
            string jsonString;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                currentFileName = openFileDialog.FileName;
                jsonString = File.ReadAllText(currentFileName);
                config = JsonConvert.DeserializeObject<Config>(jsonString);
                // config = JsonSerializer.Deserialize<Config>(jsonString);
                slides = config.slides;
                initializeUI();
            }
        }

        private void saveIt(String filename)
        {
            string jsonString;

            Config config = new Config();

            config.version = "0.1";
            config.slides = slides;

            jsonString = JsonConvert.SerializeObject(config, Formatting.Indented);

            File.WriteAllText(filename, jsonString);
        }

        private void fileSaveAsClick(object sender, RoutedEventArgs e) {
 

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                saveIt(saveFileDialog.FileName);
            }
        }


        private void fileSaveClick(object sender, RoutedEventArgs e) { 
            if(currentFileName == "")
            {
                fileSaveAsClick(sender, e);
            }
            else
            {
                saveIt(currentFileName);
            }
        }
        private void addSlideClick(object sender, RoutedEventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.jpg)|*.jpg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                Slide newSlide = new Slide(openFileDialog.FileName);
                newSlide.keys.Add(new KF(1, 0.5, 0.5, 0));
                newSlide.keys.Add(new KF(2, 0.5, 0.5, DEFAULT_DURATION));
                slides.Add(newSlide);
                addSlideControl(newSlide);
                selectSlide(slides.Count - 1);
            }
        }

        private void addKeyframeClick(object sender, RoutedEventArgs e) {
            Slide currentSlide = slides[currentSlideIndex];
            KF currentKey = currentSlide.keys[currentKeyframeIndex];
            KF newKey = new KF(currentKey.zoomFactor, currentKey.x, currentKey.y, DEFAULT_DURATION);
            slides[currentSlideIndex].keys.Add(newKey);
            addKeyframeControl(newKey);
            selectKeyframe(newKey, currentSlide.keys.Count-1);
        }

        private void deleteKeyframeClick(object sender, RoutedEventArgs e, KF key) {
            List<KF> keys = slides[currentSlideIndex].keys;
            if (keys.Count > 1)
            {
                KeyframeControl kfc = key.kfControl;
                keyframePanel.Children.Remove(kfc);
                keys.Remove(key);
                if (currentKeyframeIndex >= keys.Count)
                {
                    currentKeyframeIndex = 0;
                }
            }
            else
            {
                MessageBox.Show("A minimum of one keyframe is required.");
            }
        }

        private void playClick(object sender, RoutedEventArgs e) { MessageBox.Show("Play it again, Sam"); }

    }
}
