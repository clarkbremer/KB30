using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace KB30
{
    /// <summary>
    /// Interaction logic for CropControl.xaml
    /// </summary>
    public partial class ImageCropper : UserControl, INotifyPropertyChanged
    {
        private MainWindow mainWindow;
        public ImageCropper()
        {
            InitializeComponent();
            Loaded += cropperLoaded;
        }

        private void cropperLoaded(object sender, RoutedEventArgs e)
        {
            mainWindow = (MainWindow)Window.GetWindow(this);
            mainWindow.PreviewKeyDown += cropperKeyDown;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }
        public double cropX
        {
            get
            {
                double ix = (grid.ActualWidth - image.ActualWidth) / 2;
                double rx = Canvas.GetLeft(cropper);
                double rw = cropper.Width;
                return Math.Round(((rx - ix + (rw / 2)) / image.ActualWidth), 3);
            }

            set
            {
                double ix = (grid.ActualWidth - image.ActualWidth) / 2;
                double cc = value * image.ActualWidth;
                double rw = cropper.Width;
                Canvas.SetLeft(cropper, ix + cc - (rw / 2));
            }
        }

        public double cropY
        {
            get
            {
                double iy = (grid.ActualHeight - image.ActualHeight) / 2;
                double ry = Canvas.GetTop(cropper);
                double rh = cropper.Height;
                return Math.Round(((ry - iy + (rh / 2)) / image.ActualHeight), 3);
            }

            set
            {
                double iy = (grid.ActualHeight - image.ActualHeight) / 2;
                double cc = value * image.ActualHeight;
                double rh = cropper.Height;
                Canvas.SetTop(cropper, iy + cc - (rh / 2));
            }
        }


        public double cropZoom
        {
            get
            {
                double iw = image.ActualWidth;
                double ih = image.ActualHeight;
                double z;

                if ((iw * 9) < (ih * 16))  // portrait
                {
                    z = Math.Round((image.ActualHeight / cropper.ActualHeight), 3);
                }
                else // landscape
                {
                    z = Math.Round((image.ActualWidth / cropper.ActualWidth), 3);
                }
                return z;
            }

            set
            {
                double iw = image.ActualWidth;
                double ih = image.ActualHeight;

                if ((iw * 9) < (ih * 16))
                {
                    cropper.Height = image.ActualHeight / value;
                    cropper.Width = cropper.Height * 16 / 9;
                }
                else
                {
                    cropper.Width = image.ActualWidth / value;
                    cropper.Height = cropper.Width * 9 / 16;

                }
                cropper.Visibility = Visibility.Visible;
            }
        }


        // The part of the rectangle the mouse is over.
        private enum HitType
        {
            None, Body, UL, UR, LR, LL, L, R, T, B
        };

        // True if a drag is in progress.
        private bool DragInProgress = false;

        // The drag's last point.
        private Point LastPoint;

        // The part of the rectangle under the mouse.
        HitType MouseHitType = HitType.None;

        // Return a HitType value to indicate what is at the point.
        private HitType SetHitType(Rectangle rect, Point point)
        {
            double left = Canvas.GetLeft(cropper);
            double top = Canvas.GetTop(cropper);
            double right = left + cropper.Width;
            double bottom = top + cropper.Height;
            if (point.X < left) return HitType.None;
            if (point.X > right) return HitType.None;
            if (point.Y < top) return HitType.None;
            if (point.Y > bottom) return HitType.None;

            const double GAP = 10;
            if (point.X - left < GAP)
            {
                // Left edge.
                if (point.Y - top < GAP) return HitType.UL;
                if (bottom - point.Y < GAP) return HitType.LL;
                return HitType.L;
            }
            if (right - point.X < GAP)
            {
                // Right edge.
                if (point.Y - top < GAP) return HitType.UR;
                if (bottom - point.Y < GAP) return HitType.LR;
                return HitType.R;
            }
            if (point.Y - top < GAP) return HitType.T;
            if (bottom - point.Y < GAP) return HitType.B;
            return HitType.Body;
        }

        // Set a mouse cursor appropriate for the current hit type.
        private void SetMouseCursor()
        {
            // See what cursor we should display.
            Cursor desired_cursor = Cursors.Arrow;
            switch (MouseHitType)
            {
                case HitType.None:
                    desired_cursor = Cursors.Arrow;
                    break;
                case HitType.Body:
                    desired_cursor = Cursors.ScrollAll;
                    break;
                case HitType.UL:
                case HitType.LR:
                    desired_cursor = Cursors.SizeNWSE;
                    break;
                case HitType.LL:
                case HitType.UR:
                    desired_cursor = Cursors.SizeNESW;
                    break;
                case HitType.T:
                case HitType.B:
                    desired_cursor = Cursors.SizeNS;
                    break;
                case HitType.L:
                case HitType.R:
                    desired_cursor = Cursors.SizeWE;
                    break;
            }

            // Display the desired cursor.
            if (Cursor != desired_cursor) Cursor = desired_cursor;
        }

        // Start dragging.
        private void cropperMouseDown(object sender, MouseButtonEventArgs e)
        {
            MouseHitType = SetHitType(cropper, Mouse.GetPosition(cropperCanvas));
            SetMouseCursor();
            if (MouseHitType == HitType.None) return;

            mainWindow.kfModifiedHistory();
            LastPoint = Mouse.GetPosition(cropperCanvas);
            DragInProgress = true;
        }

        // If a drag is in progress, continue the drag.
        // Otherwise display the correct cursor.
        private void cropperMouseMove(object sender, MouseEventArgs e)
        {
            if (!DragInProgress)
            {
                MouseHitType = SetHitType(cropper, Mouse.GetPosition(cropperCanvas));
                SetMouseCursor();
            }
            else
            {
                // See how much the mouse has moved.
                Point point = Mouse.GetPosition(cropperCanvas);
                double offset_x = point.X - LastPoint.X;
                double offset_y = point.Y - LastPoint.Y;

                // Get the rectangle's current position.
                double new_x = Canvas.GetLeft(cropper);
                double new_y = Canvas.GetTop(cropper);
                double new_width = cropper.Width;
                double new_height = cropper.Height;

                // Update the rectangle.
                switch (MouseHitType)
                {
                    case HitType.Body:
                        new_x += offset_x;
                        new_y += offset_y;
                        break;
                    case HitType.UL:
                        new_x += offset_x;
                        new_width -= offset_x;
                        new_height = new_width * 9 / 16;
                        new_y += offset_x * 9 / 16;
                        break;
                    case HitType.UR:
                        new_y -= offset_x * 9 / 16;
                        new_width += offset_x;
                        new_height = new_width * 9 / 16;
                        break;
                    case HitType.LR:
                        new_width += offset_x;
                        new_height = new_width * 9 / 16;
                        break;
                    case HitType.LL:
                        new_x -= offset_y * 16 / 9;
                        new_height += offset_y;
                        new_width = new_height * 16 / 9;
                        break;
                    case HitType.L:
                        new_x += offset_x;
                        new_width -= offset_x;
                        new_height = new_width * 9 / 16;
                        new_y += (offset_x * 9 / 32);
                        break;
                    case HitType.R:
                        new_width += offset_x;
                        new_height = new_width * 9 / 16;
                        new_y -= (offset_x * 9 / 32);
                        break;
                    case HitType.B:
                        new_height += offset_y;
                        new_width = new_height * 16 / 9;
                        new_x -= (offset_y * 16 / 18);
                        break;
                    case HitType.T:
                        new_y += offset_y;
                        new_height -= offset_y;
                        new_width = new_height * 16 / 9;
                        new_x += (offset_y * 16 / 18);
                        break;
                }

                // Save the mouse's new location.
                LastPoint = point;
                updateCropper(new_x, new_y, new_width, new_height);
            }
        }

        void cropperMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
            {
                mainWindow.prevSlide();
            }
            else if (e.Delta < 0)
            {
                mainWindow.nextSlide();
            }
        }

        void updateCropper(double new_x, double new_y, double new_width, double new_height)
        {

            // Don't allow tiny rectangle
            if ((new_width > 25) && (new_height > 25))
            {
                // Update the rectangle.
                Canvas.SetLeft(cropper, new_x);
                Canvas.SetTop(cropper, new_y);
                cropper.Width = new_width;
                cropper.Height = new_height;

                updateLayout();

                // Tell the world
                OnPropertyChanged("cropX");
                OnPropertyChanged("cropY");
                OnPropertyChanged("cropZoom");
            }
        }

        public void updateLayout()
        {
            BitmapSource bms = image.Source as BitmapSource;
            double scale = image.ActualWidth / bms.PixelWidth;
            double cwp = cropper.Width / scale;
            if (cwp < 1080)
            {
                // resolution has dropped below 2K screen res
                cropper.Stroke = Brushes.OrangeRed;
            }
            else if (cwp < 2048)
            {
                cropper.Stroke = Brushes.Yellow;
            }
            else
            {
                cropper.Stroke = Brushes.Lime;
            }

            this.UpdateLayout();
        }

        // Stop dragging.
        private void cropperMouseUp(object sender, MouseButtonEventArgs e)
        {
            DragInProgress = false;
        }

        private void cropperKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                switch (e.Key)
                {
                    case Key.Up:
                        // Set rectangle to full width of the image
                        double old_height = cropper.Height;
                        double new_y = Canvas.GetTop(cropper);
                        double new_x = (cropperCanvas.ActualWidth - image.ActualWidth) / 2;
                        double new_width = image.ActualWidth;
                        double new_height = new_width * 9 / 16;
                        new_y += (old_height - new_height) / 2;
                        updateCropper(new_x, new_y, new_width, new_height);
                        e.Handled = true;
                        break;

                    case Key.Down:
                        // Set rectangle to 2k pixels wide
                        BitmapSource bms = image.Source as BitmapSource;
                        double scale = image.ActualWidth / bms.PixelWidth;
                        old_height = cropper.Height;
                        double old_width = cropper.Width;
                        new_width = 2048 * scale;
                        new_height = new_width * 9 / 16;
                        new_y = Canvas.GetTop(cropper);
                        new_y += (old_height - new_height) / 2;
                        new_x = Canvas.GetLeft(cropper);
                        new_x += (old_width - new_width) / 2;
                        updateCropper(new_x, new_y, new_width, new_height);
                        e.Handled = true;
                        break;
                }
            }
        }
    }
}
