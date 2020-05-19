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
                Uri uri = new Uri(slide.fileName);
                var bitmap = new BitmapImage(uri);
                thumb.Source = bitmap;
                thumb.Margin = new Thickness(5, 5, 5, 5);
                slidePanel.Children.Add(thumb);
                Rectangle gap = new Rectangle();
                gap.Height = 10;
                gap.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                slidePanel.Children.Add(gap);
            });



            List<KF> keys = slides[0].keys;
            keys.ForEach(key =>
            {
                StackPanel keyPanel = new StackPanel();
                keyPanel.Orientation = Orientation.Vertical;
                keyPanel.Margin = new Thickness(5, 5, 5, 5);
                keyPanel.Children.Add(new TextBox { Text = "X: " + key.x.ToString() });
                keyPanel.Children.Add(new TextBox { Text = "Y: " + key.y.ToString() });
                keyPanel.Children.Add(new TextBox { Text = "Zoom: " + key.zoomFactor.ToString() });
                keyPanel.Children.Add(new TextBox { Text = "Duration: " + key.duration.ToString() });
                Border border = new Border { Background = Brushes.Transparent, BorderThickness = new Thickness(5,5,5,5) };
                keyPanel.Children.Add(border);
                keyframePanel.Children.Add(keyPanel);
                Rectangle gap = new Rectangle();
                gap.Width= 10;
                gap.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                keyframePanel.Children.Add(gap);
            });

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
