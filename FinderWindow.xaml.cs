﻿using System;
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
        List<string> image_extensions = new List<string>() {".gif", ".jpg", ".png", ".bmp"};

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            loadDrives();
        }

        private void loadDrives() {
            current_folder = "";
            folderNavPanel.Children.Clear();
            
            filePanel.Children.Clear();
            foreach (var drive in DriveInfo.GetDrives())
            {
                Tile driveTile = new Tile();
                driveTile.tileType = Tile.Type.Drive;
                driveTile.fullPath = drive.Name;
                driveTile.Caption.Text = drive.Name + Environment.NewLine + drive.VolumeLabel;
                driveTile.Thumbnail.Source = ThumbnailFromUri(new Uri(@"pack://application:,,,/Resources/drive.png", UriKind.Absolute));
                driveTile.Thumbnail.Height = 75;
                driveTile.MouseDoubleClick += folderClick;
                filePanel.Children.Add(driveTile);
            }
            updateNavButtons();
        }

        private void folderClick(object sender, RoutedEventArgs e)
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
            foreach (string folderName in Directory.GetDirectories(current_folder)){
                DirectoryInfo directory_info = new DirectoryInfo(folderName);
                if (directory_info.Attributes.HasFlag(FileAttributes.Hidden)) {
                    continue;
                }
                Tile folderTile = new Tile();
                folderTile.tileType = Tile.Type.Folder;
                folderTile.fullPath = folderName;
                folderTile.Caption.Text = Path.GetFileName(folderName);
                folderTile.Thumbnail.Source = ThumbnailFromUri(new Uri(@"pack://application:,,,/Resources/folder.jpg", UriKind.Absolute));
                folderTile.Thumbnail.Height = 75;
                folderTile.MouseDoubleClick += folderClick;
                filePanel.Children.Add(folderTile);
            }
            current_image_tiles.Clear();
            current_file_index = -1;
            foreach (String fname in Directory.GetFiles(current_folder))  {
                   
                if (image_extensions.IndexOf(Path.GetExtension(fname).ToLower()) > 0 ) {
                    Tile imageTile = new Tile();
                    imageTile.tileType = Tile.Type.Image;
                    imageTile.fullPath = fname;
                    imageTile.Caption.Text = Path.GetFileName(fname);
                    imageTile.MouseLeftButtonUp += thumbnailButtonClick;
                    imageTile.MouseDoubleClick += thumbnailDoubleClick;
                    filePanel.Children.Add(imageTile);
                    current_image_tiles.Add(imageTile);
 
                }
            }
            if (current_image_tiles.Count > 0)
            {
                loadThumbsInBackground(current_image_tiles[0]);
                selectTile(current_image_tiles[0]);
            }
            else
            {
                previewImage.Source = null;
            }

            updateNavButtons();
            filePanelScrollViewer.ScrollToVerticalOffset(0);
        }
        
        void updateNavButtons()
        {
            if (current_folder == "")
            {
                upDirButton.IsEnabled = false;
                prevDirButton.IsEnabled = false;
                nextDirButton.IsEnabled = false;
                return;
            }

            upDirButton.IsEnabled = true;
            prevDirButton.IsEnabled = true;
            nextDirButton.IsEnabled = true;

            DirectoryInfo directory_info = new DirectoryInfo(current_folder);
            if (directory_info.Parent == null)
            {
                prevDirButton.IsEnabled = false;
                nextDirButton.IsEnabled = false;
            }
            else
            {
                string parent = directory_info.Parent.FullName;
                List<string> siblings = new List<string>(Directory.GetDirectories(parent));
                siblings.RemoveAll(isHidden);

                int index = siblings.IndexOf(current_folder);
                if (index == 0)
                {
                    prevDirButton.IsEnabled = false;
                }
                if (index == siblings.Count - 1)
                {
                    nextDirButton.IsEnabled = false;
                }
            }
        }


        private static bool isHidden(string f)
        {
            return File.GetAttributes(f).HasFlag(FileAttributes.Hidden);
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
            Tile tile = sender as Tile;
            selectTile(tile);
        }
        private void thumbnailDoubleClick(object sender, RoutedEventArgs e)
        {
            Tile tile = sender as Tile;
            ((MainWindow)this.Owner).insertSlide(tile.fullPath);
        }

        private void selectTile(Tile tile) {
            if ((current_file_index >= 0) && (current_file_index < current_image_tiles.Count))
            {
                current_image_tiles[current_file_index].unhighlight();
            }
            BitmapImage bmp = Util.BitmapFromUri(new Uri(tile.fullPath));
            previewImage.Source = bmp;
            current_folder = Path.GetDirectoryName(tile.fullPath);
            current_file_index = current_image_tiles.IndexOf(tile);
            current_image_tiles[current_file_index].highlight();
            Caption.Text = Path.GetFileName(tile.fullPath) + " (" + bmp.PixelWidth + " x " + bmp.PixelHeight + ")  (" + (current_file_index + 1) + " of " + current_image_tiles.Count + ")";
        }

        public void addButton_Click(object sender, RoutedEventArgs e)
        {
            if (current_file_index >= 0)
            {
                String fname = current_image_tiles[current_file_index].fullPath;
                ((MainWindow)this.Owner).insertSlide(fname);
                if (current_file_index < (current_image_tiles.Count - 1))
                {
                    selectTile(current_image_tiles[current_file_index + 1]);
                }
            }
        }

        private void previewImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (current_image_tiles.Count == 0)
            {
                return;
            }
            if (e.Delta < 0)
            {
                if (current_file_index < (current_image_tiles.Count - 1))
                {
                    selectTile(current_image_tiles[current_file_index + 1]);
                }
            }

            else if (e.Delta > 0)
            {
                if (current_file_index > 0)
                {
                    selectTile(current_image_tiles[current_file_index - 1]);
                }
            }
        }

        private BitmapImage ThumbnailFromUri(Uri uri)
        {
            return Util.BitmapFromUri(uri, 150, true);
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
                siblings.RemoveAll(isHidden);
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
                siblings.RemoveAll(isHidden);
                int index = siblings.IndexOf(current_folder);
                if (index > 0)
                {
                    loadFolder(siblings[index -1 ]);
                }
            }

        }
    }
}
