using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KB30
{
    /// <summary>
    /// Interaction logic for FinderWindow.xaml
    /// </summary>
    public partial class FinderWindow : Window
    {
        public FinderWindow()
        {
            InitializeComponent();
        }

        Object emptyNode = new Object();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string s in Directory.GetLogicalDrives())
            {
                TreeViewItem item = new TreeViewItem();
                item.Header = s;
                item.Tag = s;
                item.FontWeight = FontWeights.Normal;
                item.Items.Add(emptyNode);
                item.Expanded += new RoutedEventHandler(folder_Expanded);
                folderTree.Items.Add(item);
            }

        }

        void folder_Expanded(object sender, RoutedEventArgs e)
        {

            TreeViewItem item = (TreeViewItem)sender;
            if (item.Items.Count == 1 && item.Items[0] == emptyNode)
            {
                item.Items.Clear();
                try
                {
                    foreach (string s in Directory.GetDirectories(item.Tag.ToString()))
                    {
                        TreeViewItem subitem = new TreeViewItem();
                        subitem.Header = s.Substring(s.LastIndexOf("\\") + 1);
                        subitem.Tag = s;
                        subitem.FontWeight = FontWeights.Normal;
                        subitem.Items.Add(emptyNode);
                        subitem.Expanded += new RoutedEventHandler(folder_Expanded);
                        item.Items.Add(subitem);
                    }
                    foreach (String fname in Directory.GetFiles(item.Tag.ToString()))
                    {
                        if (fname.EndsWith(".gif") || fname.EndsWith(".jpg")) {
                            TreeViewItem subitem = new TreeViewItem();
                            subitem.Header = fname.Substring(fname.LastIndexOf("\\") + 1);
                            subitem.Tag = fname;
                            subitem.FontWeight = FontWeights.Normal;
                            subitem.MouseDoubleClick += new MouseButtonEventHandler(fileClicked);
                            item.Items.Add(subitem);
                        }
                    }
                }
                catch (Exception) { }
            }
        }

        private void fileClicked(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem tvi = sender as TreeViewItem;
            String fname = tvi.Tag as String;
            BitmapImage bmp = new BitmapImage();
            try
            {
                bmp.BeginInit();
                bmp.UriSource = new Uri(fname); ;
                bmp.EndInit();
                bmp.Freeze();
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show("Error loading image file: " + fname, "Call the doctor, I think I'm gonna crash!");
            }
            previewImage.Source = bmp;
        }
    }
}
