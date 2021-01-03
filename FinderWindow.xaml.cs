using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Path = System.IO.Path;

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
        List<Tile> current_image_tiles = new List<Tile>();
        int current_file_index = 0;
        string current_folder;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadDrives();
        }

        private void loadDrives() {
            current_folder = "";
            filePanel.Children.Clear();
            folderNavPanel.Children.Clear();

            foreach (var drive in DriveInfo.GetDrives())
            {
                Tile driveTile = new Tile();
                driveTile.tileType = Tile.Type.Drive;
                driveTile.fullPath = drive.Name;
                driveTile.Caption.Text = drive.Name + Environment.NewLine + drive.VolumeLabel;
                driveTile.Thumbnail.Source = ThumbnailFromUri(new Uri(@"pack://application:,,,/Resources/drive.png", UriKind.Absolute));
                driveTile.Thumbnail.Height = 75;
                driveTile.MouseLeftButtonUp += folderButtonClick;
                filePanel.Children.Add(driveTile);
            }
        }

        private void folderButtonClick(object sender, RoutedEventArgs e)
        {
            loadFolder((sender as Tile).fullPath);
        }

        private void loadFolder(string folder) {
            current_folder = folder;
            // Draw folder nav buttons
            folderNavPanel.Children.Clear();
            string root = Path.GetPathRoot(folder);
            Button rbtn = new Button();
            rbtn.Tag = root;
            rbtn.Content = root;
            rbtn.Click += folderNavClick;
            folderNavPanel.Children.Add(rbtn);
            if (root != folder)
            {
                string partial_path = root;
                string[] path_parts = current_folder.Remove(0, root.Length).Split("\\");
                foreach (string f in path_parts)
                {
                    partial_path += (f);
                    Button btn = new Button();
                    btn.Padding = new Thickness(5);
                    btn.Tag = partial_path;
                    btn.Content = f + " \\";
                    btn.Click += folderNavClick;
                    folderNavPanel.Children.Add(btn);
                    partial_path += "\\";
                }
            }

            // load left panel images
            filePanel.Children.Clear();
            try
            {
                foreach (string folderName in Directory.GetDirectories(current_folder)){
                    DirectoryInfo info = new DirectoryInfo(folderName);
                    if (info.Attributes.HasFlag(FileAttributes.Hidden)) {
                        continue;
                    }
                    Tile folderTile = new Tile();
                    folderTile.tileType = Tile.Type.Folder;
                    folderTile.fullPath = folderName;
                    folderTile.Caption.Text = Path.GetFileName(folderName);
                    folderTile.Thumbnail.Source = ThumbnailFromUri(new Uri(@"pack://application:,,,/Resources/folder.jpg", UriKind.Absolute));
                    folderTile.Thumbnail.Height = 75;
                    folderTile.MouseLeftButtonUp += folderButtonClick;
                    filePanel.Children.Add(folderTile);
                }
                current_image_tiles.Clear();
                foreach (String fname in Directory.GetFiles(current_folder))  {
                    if (fname.EndsWith(".gif") || fname.EndsWith(".jpg")) {
                        Tile imageTile = new Tile();
                        imageTile.tileType = Tile.Type.Image;
                        imageTile.fullPath = fname;
                        imageTile.Caption.Text = Path.GetFileName(fname);
                        imageTile.MouseLeftButtonUp += thumbnailButtonClick;
                        filePanel.Children.Add(imageTile);
                        current_image_tiles.Add(imageTile);
                    }
                }
                loadThumbsInBackground(current_image_tiles[0]);
            }
            catch (Exception) { }
        }
        
        void loadThumbsInBackground(Tile tile)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync(tile);
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Tile tile = (Tile)e.Argument;
            WorkerResult workerResult = new WorkerResult();
            BitmapImage bmp = ThumbnailFromUri(new Uri(tile.fullPath));
            workerResult.tile = tile;
            workerResult.bmp = bmp;
            e.Result = workerResult;
        }
        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            WorkerResult workerResult = (WorkerResult)e.Result;
            Tile tile = workerResult.tile;
            tile.Thumbnail.Source = workerResult.bmp;
            int i = current_image_tiles.IndexOf(tile);
            i++;
            if (i < current_image_tiles.Count)
            {
                loadThumbsInBackground(current_image_tiles[i]);
            }
        }

        class WorkerResult
        {
            public Tile tile { get; set; }
            public BitmapImage bmp { get; set; }
        }

        private void folderNavClick(object sender, RoutedEventArgs e)
        {
            loadFolder((string)(sender as Button).Tag);
        }

        private void thumbnailButtonClick(object sender, RoutedEventArgs e)
        {
            loadImage(sender as Tile);
        }

        private void loadImage(Tile tile) { 
            BitmapImage bmp = new BitmapImage();
            try
            {
                bmp.BeginInit();
                bmp.UriSource = new Uri(tile.fullPath); ;
                bmp.EndInit();
                bmp.Freeze();
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show("Error loading image file: " + tile.fullPath, "Call the doctor, I think I'm gonna crash!");
            }
            previewImage.Source = bmp;
            Caption.Text = Path.GetFileName(tile.fullPath);
            current_folder = Path.GetDirectoryName(tile.fullPath);
            current_file_index = current_image_tiles.IndexOf(tile);
        }

        public void addButton_Click(object sender, RoutedEventArgs e)
        {
            if (previewImage.Source != null)
            {
                String fname = previewImage.Source.ToString();
                ((MainWindow)this.Owner).insertSlide(fname);
            }
        }

        private void previewImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
                if (current_file_index < (current_image_tiles.Count - 1))
                {
                    loadImage(current_image_tiles[current_file_index + 1]);
                }
            }

            else if (e.Delta > 0)
            {
                if (current_file_index > 0)
                {
                    loadImage(current_image_tiles[current_file_index - 1]);
                }
            }
        }

        private BitmapImage ThumbnailFromUri(Uri uri)
        {
            BitmapImage bmp = new BitmapImage();
            try
            {
                bmp.BeginInit();
                bmp.UriSource = uri;
                bmp.DecodePixelHeight = 150;
                bmp.EndInit();
                bmp.Freeze();
            }
            catch (NotSupportedException ex)
            {
                MessageBox.Show("Error loading image file: " + uri.ToString(), "Call the doctor, I think I'm gonna crash!");
                return null;
            }
            return bmp;
        }

        private void upDirButtonClick(object sender, RoutedEventArgs e)
        {
            if (current_folder == "")
            {
                return;
            }
            DirectoryInfo info = new DirectoryInfo(current_folder);
            if (info.Parent != null)
            {
                loadFolder(info.Parent.FullName);
            }
            else
            {
                loadDrives();
            }
        }

        private void nextDirButtonClick(object sender, RoutedEventArgs e)
        {
            if (current_folder == "" ) {return;}

            DirectoryInfo info = new DirectoryInfo(current_folder);
            if (info.Parent != null)
            {
                string parent = info.Parent.FullName;
                List<string> siblings = new List<string> (Directory.GetDirectories(parent));
                int index = siblings.IndexOf(current_folder);
                if (index < siblings.Count - 1)
                {
                    loadFolder(siblings[index + 1]);
                }
            }
        }

        private void prevDirButtonClick(object sender, RoutedEventArgs e)
        {
            if (current_folder == "") { return; }

            DirectoryInfo info = new DirectoryInfo(current_folder);
            if (info.Parent != null)
            {
                string parent = info.Parent.FullName;
                List<string> siblings = new List<string>(Directory.GetDirectories(parent));
                int index = siblings.IndexOf(current_folder);
                if (index > 0)
                {
                    loadFolder(siblings[index -1 ]);
                }
            }

        }
    }
}
