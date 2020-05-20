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
            public StackPanel keyPanel { get; set; }

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
            public Image thumb { get; set; }
        }

        public List<Slide> slides = new List<Slide>();

        public class Config
        {
            public string version { get; set; }
            public List<Slide> slides { get; set; }
        }

        public void initializeUI()
        {
            Uri uri = new Uri(slides[0].fileName);
            var bitmap = new BitmapImage(uri);
            previewImage.Source = bitmap;

            slides.ForEach(slide =>
            {
                Image thumb = new Image();
                slide.thumb = thumb;
                Uri uri = new Uri(slide.fileName);
                var bitmap = new BitmapImage(uri);
                thumb.Source = bitmap;
                thumb.Margin = new Thickness(5, 5, 5, 5);

                Border border = new Border
                {
                    Background = Brushes.LightBlue,
                    BorderThickness = new Thickness(5, 5, 5, 5)
                };
                border.Child = thumb;
                slidePanel.Children.Add(border);
                Rectangle gap = new Rectangle();
                gap.Height = 5;
                gap.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                slidePanel.Children.Add(gap);
            });



            List<KF> keys = slides[0].keys;
            for (int i = 0; i < keys.Count; i++) {
                KF key = keys[i];
                StackPanel keyPanel = new StackPanel {
                    Name = "keyPanel" + i.ToString(),
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(5, 5, 5, 5)
                };
                key.keyPanel = keyPanel;

                StackPanel durPanel = new StackPanel { Orientation = Orientation.Horizontal, DataContext = key };
                Binding durBinding = new Binding();
                durBinding.Source = key;
                durBinding.Path = new PropertyPath("duration");
                durBinding.Mode = BindingMode.TwoWay;

                TextBox durTB = new TextBox { Width = 25 };
                BindingOperations.SetBinding(durTB, TextBox.TextProperty, durBinding);

                durPanel.Children.Add(new Label { Content = "Duration: " });
                durPanel.Children.Add(durTB);

                keyPanel.Children.Add(durPanel);
                keyPanel.Children.Add(new TextBlock { Text = "X: " + key.x.ToString() });
                keyPanel.Children.Add(new TextBlock { Text = "Y: " + key.y.ToString() });
                keyPanel.Children.Add(new TextBlock { Text = "Zoom: " + key.zoomFactor.ToString() });
                Border border = new Border {
                    Name = "keyBorder" + i.ToString(),
                    Background = Brushes.LightBlue,
                    BorderThickness = new Thickness(5, 5, 5, 5),
                    VerticalAlignment = System.Windows.VerticalAlignment.Center
                };
                border.Child = keyPanel;
                keyframePanel.Children.Add(border);
                Rectangle gap = new Rectangle();
                gap.Width= 5;
                gap.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                keyframePanel.Children.Add(gap);
            }

            (keyframePanel.Children[0] as Border).Background = Brushes.Blue;
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
