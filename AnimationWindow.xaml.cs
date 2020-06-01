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
using System.Windows.Media.Animation;
using System.Linq;

namespace KB30
{
    /// <summary>
    /// Interaction logic for AnimationWindow.xaml
    /// </summary>
    public partial class AnimationWindow : Window
    {
        public List<Slide> slides = new List<Slide>();

        Image currentImage;
        Image otherImage;
        int currentSlideIndex;
        int nextSlideIndex;

        private static readonly CubicEase easeOut = new CubicEase() { EasingMode = EasingMode.EaseOut };
        private static readonly CubicEase easeIn = new CubicEase() { EasingMode = EasingMode.EaseIn };
        private static readonly CubicEase easeInOut = new CubicEase() { EasingMode = EasingMode.EaseInOut };

        public AnimationWindow()
        {
            InitializeComponent();
            Loaded += animationWindowLoaded;
        }

        public void animate(List<Slide> _slides)
        {

            slides = _slides;
            currentImage = image1;
            otherImage = image2;
            currentSlideIndex = 0;
            nextSlideIndex = 1;

            Uri uri = new Uri(slides[0].fileName);
            var bitmap = new BitmapImage(uri);
            currentImage.Source = bitmap;

            uri = new Uri(slides[1].fileName);
            bitmap = new BitmapImage(uri);
            otherImage.Source = bitmap;

            this.UpdateLayout();

            transformImage(currentImage, slides[currentSlideIndex].keys[0]);
            startPanZoom(currentImage, slides[currentSlideIndex].keys);
        }

        private void transformImage(Image image, KF kf)
        {
            Double w = image.ActualWidth;
            Double h = image.ActualHeight;

            var st = new ScaleTransform();
            st.CenterX = w / 2;
            st.CenterY = h / 2;
            st.ScaleX = kf.zoomFactor;
            st.ScaleY = kf.zoomFactor;
            var tt = new TranslateTransform();
            tt.X = w * (0.5 - kf.x);
            tt.Y = h * (0.5 - kf.y);

            var tg = (TransformGroup)image.RenderTransform;
            tg.Children[0] = tt;
            tg.Children[1] = st;
        }


        private void endPanZoom(object sender, EventArgs e)
        {
            beginFadeOutIn();
        }

        private void beginFadeOutIn()
        {
            var duration = new Duration(TimeSpan.FromSeconds(1));
            var animFadeOut = new DoubleAnimation(1, 0, duration);
            var animFadeIn = new DoubleAnimation(0, 1, duration);
            animFadeIn.Completed += new EventHandler(endFadeOutIn);

            transformImage(otherImage, slides[nextSlideIndex].keys[0]);

            if (currentImage == image1)
            {
                currentImage = image2;
                otherImage = image1;
                frame1.BeginAnimation(OpacityProperty, animFadeOut);
                frame2.BeginAnimation(OpacityProperty, animFadeIn);
            }
            else
            {
                currentImage = image1;
                otherImage = image2;
                frame2.BeginAnimation(OpacityProperty, animFadeOut);
                frame1.BeginAnimation(OpacityProperty, animFadeIn);
            }
        }

        private void endFadeOutIn(object sender, EventArgs e)
        {
            currentSlideIndex = nextSlideIndex;
            nextSlideIndex++;
            if (nextSlideIndex >= slides.Count)
            {
                nextSlideIndex = 0;
            }

            Uri uri = new Uri(slides[nextSlideIndex].fileName);
            var bitmap = new BitmapImage(uri);
            otherImage.Source = bitmap;

            startPanZoom(currentImage, slides[currentSlideIndex].keys);
        }


        private void startPanZoom(Image image, List<KF> keys)
        {
            var iw = image.ActualWidth;
            var ih = image.ActualHeight;

            TimeSpan duration = TimeSpan.FromSeconds(keys.Sum(k => k.duration));
            TimeSpan partialDuration = TimeSpan.FromSeconds(0);
            var animZoom = new DoubleAnimationUsingKeyFrames();
            animZoom.Duration = duration;
            var animCtrX = new DoubleAnimationUsingKeyFrames();
            animCtrX.Duration = duration;
            var animCtrY = new DoubleAnimationUsingKeyFrames();
            animCtrY.Duration = duration;
            var animPanX = new DoubleAnimationUsingKeyFrames();
            animPanX.Duration = duration;
            var animPanY = new DoubleAnimationUsingKeyFrames();
            animPanY.Duration = duration;

            animPanY.Completed += new EventHandler(endPanZoom);

            keys.ForEach(key =>
            {
                partialDuration += TimeSpan.FromSeconds(key.duration);
                KeyTime kt = KeyTime.FromTimeSpan(partialDuration);
                animZoom.KeyFrames.Add(new EasingDoubleKeyFrame(key.zoomFactor, kt, easeInOut));
                animCtrX.KeyFrames.Add(new EasingDoubleKeyFrame(iw / 2, kt, easeInOut));
                animCtrY.KeyFrames.Add(new EasingDoubleKeyFrame(ih / 2, kt, easeInOut));
                animPanX.KeyFrames.Add(new EasingDoubleKeyFrame((0.5 - key.x) * iw, kt, easeInOut));
                animPanY.KeyFrames.Add(new EasingDoubleKeyFrame((0.5 - key.y) * ih, kt, easeInOut));
            });

            var tg = (TransformGroup)image.RenderTransform;
            tg.Children[0].BeginAnimation(TranslateTransform.XProperty, animPanX);
            tg.Children[0].BeginAnimation(TranslateTransform.YProperty, animPanY);
            tg.Children[1].BeginAnimation(ScaleTransform.ScaleXProperty, animZoom);
            tg.Children[1].BeginAnimation(ScaleTransform.ScaleYProperty, animZoom);
            tg.Children[1].BeginAnimation(ScaleTransform.CenterXProperty, animCtrX);
            tg.Children[1].BeginAnimation(ScaleTransform.CenterYProperty, animCtrY);
        }

        void animationWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.ToggleWindow();
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
