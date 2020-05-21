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
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;
using System.ComponentModel;

namespace KB30
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int currentSlideIndex = 0;
        public int currentKeyframeIndex = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        public class KF
        {
            public double zoomFactor { get; set; }
            public double x { get; set; }
            public double y { get; set; }
            public double duration { get; set; }

            public KF(double z, double x, double y, double d)
            {
                this.zoomFactor = z;
                this.x = x;
                this.y = y;
                this.duration = d;
            }

            public KF() { }

            [JsonIgnore]
            public Button keyButton { get; set; }

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

 
        public void selectKeyframe(int keyFrameIndex)
        {
            (keyframePanel.Children[currentKeyframeIndex] as Border).BorderBrush = Brushes.LightBlue;
            (keyframePanel.Children[keyFrameIndex] as Border).BorderBrush = Brushes.Blue;
            currentKeyframeIndex = keyFrameIndex;
        }

        private void keyFrameClick(object sender, RoutedEventArgs e)
        {
            List<KF> keys = slides[currentSlideIndex].keys;
            Button btn = e.Source as Button;
            KF key = keys.Find(k => k.keyButton == btn);
            int index = keys.IndexOf(key, 0);
            selectKeyframe(index);
            // position crop conttol here
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
                kfControl.Margin = new Thickness(2, 2, 2, 2);

                kfControl.DataContext = key;
                Binding durBinding = new Binding();
                durBinding.Source = key;
                durBinding.Path = new PropertyPath("duration");
                durBinding.Mode = BindingMode.TwoWay;
                BindingOperations.SetBinding(kfControl.durTb, TextBox.TextProperty, durBinding);
                kfControl.button.Click += keyFrameClick;
                key.keyButton = kfControl.button;

                kfControl.xTxt.Text = key.x.ToString();
                kfControl.yTxt.Text = key.y.ToString();
                kfControl.zoomTxt.Text = key.zoomFactor.ToString();

                border.Child = kfControl;
            }

            selectKeyframe(0);
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
                jsonString = File.ReadAllText(openFileDialog.FileName);
                config = JsonSerializer.Deserialize<Config>(jsonString);
                slides = config.slides;
                initializeUI();
            }
        }

        private void fileSaveClick(object sender, RoutedEventArgs e) {
            string jsonString;
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                Config config = new Config();

                config.version = "0.1";
                config.slides = slides;

                jsonString = JsonSerializer.Serialize(config, options);
                File.WriteAllText(saveFileDialog.FileName, jsonString);
            }
        }


        private void fileSaveAsClick(object sender, RoutedEventArgs e) { MessageBox.Show("File SaveAs"); }
        private void addSlideClick(object sender, RoutedEventArgs e) { MessageBox.Show("Add Slide"); }
        private void addKeyframeClick(object sender, RoutedEventArgs e) { MessageBox.Show("Add Keyframe"); }
        private void playClick(object sender, RoutedEventArgs e) { MessageBox.Show("Play it again, Sam"); }

    }
}
