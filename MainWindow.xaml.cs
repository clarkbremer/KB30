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
 *  New Slide
 *  New Keyframe
 *  Animate Window
 *  Delete Slide
 *  Delete Key
 *  Drag and Drop slides and keys to re-order
 */
namespace KB30
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
            public Button thumb { get; set; }
        }

        public List<Slide> slides = new List<Slide>();

        public class Config
        {
            public string version { get; set; }
            public List<Slide> slides { get; set; }
        }

 
        public void selectKeyframe(KF key, int keyFrameIndex)
        {
            (keyframePanel.Children[currentKeyframeIndex] as Border).BorderBrush = Brushes.LightBlue;
            KF oldKey = slides[currentSlideIndex].keys[currentKeyframeIndex];
            KeyframeControl oldKFControl = oldKey.kfControl;

            BindingOperations.ClearBinding(oldKFControl.xTb, TextBox.TextProperty);
            BindingOperations.ClearBinding(oldKFControl.yTb, TextBox.TextProperty);
            BindingOperations.ClearBinding(oldKFControl.zoomTb, TextBox.TextProperty);

            // because clearing the binding clears the targets
            oldKFControl.xTb.Text = oldKey.x.ToString();
            oldKFControl.yTb.Text = oldKey.y.ToString();
            oldKFControl.zoomTb.Text = oldKey.zoomFactor.ToString();

            (keyframePanel.Children[keyFrameIndex] as Border).BorderBrush = Brushes.Blue;
           

            currentKeyframeIndex = keyFrameIndex;

            imageCropper.UpdateLayout();
            imageCropper.cropZoom = key.zoomFactor;
            imageCropper.cropX = key.x;
            imageCropper.cropY = key.y;

            imageCropper.UpdateLayout();

            KeyframeControl kfControl = key.kfControl;
            
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
    
        private void keyFrameClick(object sender, RoutedEventArgs e)
        {
            List<KF> keys = slides[currentSlideIndex].keys;
            Button btn = e.Source as Button;
            KF key = keys.Find(k => k.kfControl == btn.Parent);
            int index = keys.IndexOf(key, 0);
            selectKeyframe(key, index);
        }

        public void initializeKeysUI(Slide slide)
        {
            keyframePanel.Children.Clear();
            List<KF> keys = slide.keys;
            for (int i = 0; i < keys.Count; i++)
            {
                KF key = keys[i];

                Border border = new Border
                {
                    Name = "keyBorder" + i.ToString(),
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.LightBlue,
                    Margin = new Thickness(2, 2, 2, 2),
                    BorderThickness = new Thickness(5, 5, 5, 5),
                    VerticalAlignment = System.Windows.VerticalAlignment.Center
                };
                keyframePanel.Children.Add(border);

                KeyframeControl kfControl = new KeyframeControl();
                key.kfControl = kfControl;

                kfControl.Margin = new Thickness(2, 2, 2, 2);
                kfControl.button.Click += keyFrameClick;

                kfControl.xTb.Text = key.x.ToString();
                kfControl.yTb.Text = key.y.ToString();
                kfControl.zoomTb.Text = key.zoomFactor.ToString();
                kfControl.durTb.Text = key.duration.ToString();

                kfControl.xTb.TextChanged += delegate (object sender, TextChangedEventArgs e) { kfControlChangeEvent(sender, e, key); };
                kfControl.yTb.TextChanged += delegate (object sender, TextChangedEventArgs e) { kfControlChangeEvent(sender, e, key); };
                kfControl.zoomTb.TextChanged += delegate (object sender, TextChangedEventArgs e) { kfControlChangeEvent(sender, e, key); };
                kfControl.durTb.TextChanged += delegate (object sender, TextChangedEventArgs e) { kfControlChangeEvent(sender, e, key); };

                border.Child = kfControl;
            }

            selectKeyframe(keys[0], 0);
        }


        public void selectSlide(int slideIndex)
        {
            Uri uri = new Uri(slides[slideIndex].fileName);
            var bitmap = new BitmapImage(uri);
            imageCropper.image.Source = bitmap;
            (slidePanel.Children[currentSlideIndex] as Border).BorderBrush = Brushes.LightBlue;
            (slidePanel.Children[slideIndex] as Border).BorderBrush = Brushes.Blue;

            initializeKeysUI(slides[slideIndex]);
            currentSlideIndex = slideIndex;
        }


        private void slideClick(object sender, RoutedEventArgs e)
        {
            Button btn = e.Source as Button;
            Slide slide = slides.Find(s => s.thumb == btn);
            int index = slides.IndexOf(slide, 0);

            selectSlide(index);
        }

        public void initializeUI()
        {
            slidePanel.Children.Clear();

            for (int i = 0; i < slides.Count; i++)
            {
                Slide slide = slides[i];

                ThumbButtonControl thumbButton = new ThumbButtonControl();
                thumbButton.image.Source = new BitmapImage(new Uri(slide.fileName));
                thumbButton.button.Click += slideClick;
                slide.thumb = thumbButton.button;

                Border border = new Border
                {
                    BorderBrush = Brushes.LightBlue,
                    Background = Brushes.Transparent,
                    Margin = new Thickness(2, 2, 2, 2),
                    BorderThickness = new Thickness(5, 5, 5, 5)
                };
                border.Child = thumbButton;
                slidePanel.Children.Add(border);
            }
            selectSlide(0);
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
        private void addSlideClick(object sender, RoutedEventArgs e) { MessageBox.Show("Add Slide"); }
        private void addKeyframeClick(object sender, RoutedEventArgs e) { MessageBox.Show("Add Keyframe"); }
        private void playClick(object sender, RoutedEventArgs e) { MessageBox.Show("Play it again, Sam"); }

    }
}
