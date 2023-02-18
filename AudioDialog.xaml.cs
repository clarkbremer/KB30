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
using System.Windows.Shapes;
using Microsoft.Win32;

namespace KB30
{
    /// <summary>
    /// Interaction logic for AudioDialog.xaml
    /// </summary>
    public partial class AudioDialog : Window
    {
        public AudioDialog()
        {
            InitializeComponent();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double val = Convert.ToInt32(e.NewValue);
            this.volumeText.Text = val.ToString();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select audio file";
            openFileDialog.Filter = "Sound files (*.mp3)|*.mp3|All files (*.*)|*.*";
            if (!String.IsNullOrEmpty(filenameTextBlock.Text))
            {
                openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(filenameTextBlock.Text);
                openFileDialog.FileName = System.IO.Path.GetFileName(filenameTextBlock.Text);
            }
            if (openFileDialog.ShowDialog() == true)
            {
                filenameTextBlock.Text = openFileDialog.FileName;
            }
        }

        private void okButton_Click(object sender, RoutedEventArgs e) =>
            DialogResult = true;

        private void cancelButton_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            filenameTextBlock.Text = "";
            volumeText.Text = "0";
            DialogResult = true;
        }
    }
}
