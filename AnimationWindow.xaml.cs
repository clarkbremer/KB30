using System;
using System.CodeDom;
using System.Collections.Generic;
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
    /// Interaction logic for AnimationWindow.xaml
    /// </summary>
    public partial class AnimationWindow : Window
    {
        public AnimationWindow()
        {
            InitializeComponent();
        }

 

        private void KeyHandler(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    this.ToggleWindow();
                    break;

                case Key.Escape:
                    if (this.WindowState == WindowState.Maximized)
                    {
                        ToggleWindow();
                    }
                    else
                    {
                        this.Close();
                    }
                    break;
            }
        }
        private void ToggleWindow()
        {
            switch (this.WindowState)
            {
                case (WindowState.Maximized):
                    {
                        this.WindowState = WindowState.Normal;
                        this.WindowStyle = WindowStyle.SingleBorderWindow;
                    }
                    break;

                default:
                    {
                        this.WindowState = WindowState.Maximized;
                        this.WindowStyle = WindowStyle.None;
                    }
                    break;
            }
        }

        private void WindowStateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                // hide the window before changing window style to cover taskbar
                this.Visibility = Visibility.Collapsed;
                this.Topmost = true;
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.NoResize;
                // re-show the window after changing style
                this.Visibility = Visibility.Visible;
            }
            else
            {
                this.Topmost = false;
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                this.ResizeMode = ResizeMode.CanResize;
            }
        }
    }
}
