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

        private void fileNewClick(object sender, RoutedEventArgs e) { MessageBox.Show("File New"); }
        private void fileOpenClick(object sender, RoutedEventArgs e) { MessageBox.Show("File Open"); }
        private void fileSaveClick(object sender, RoutedEventArgs e) { MessageBox.Show("File Save"); }
        private void fileSaveAsClick(object sender, RoutedEventArgs e) { MessageBox.Show("File SaveAs"); }
        private void addSlideClick(object sender, RoutedEventArgs e) { MessageBox.Show("Add Slide"); }
        private void addKeyframeClick(object sender, RoutedEventArgs e) { MessageBox.Show("Add Keyframe"); }
        private void playClick(object sender, RoutedEventArgs e) { MessageBox.Show("Play it again, Sam"); }

    }
}
