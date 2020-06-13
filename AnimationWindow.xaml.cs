using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
        public MediaPlayer mediaPlayer = new MediaPlayer();

        Image currentImage;
        Image otherImage;
        int currentSlideIndex;
        int nextSlideIndex;
        double speedFactor = 1;
        String soundtrack = "";

        private static readonly CubicEase easeOut = new CubicEase() { EasingMode = EasingMode.EaseOut };
        private static readonly CubicEase easeIn = new CubicEase() { EasingMode = EasingMode.EaseIn };
        private static readonly CubicEase easeInOut = new CubicEase() { EasingMode = EasingMode.EaseInOut };

        private List<AnimationClock> currentClocks = new List<AnimationClock>();

        private Boolean paused = false;
        public Boolean exitOnClose { get; set; }

        public AnimationWindow()
        {
            InitializeComponent();
            exitOnClose = false ;
            Loaded += animationWindowLoaded;
            Closed += animationWindowClosed;
        }

        private void animationWindowClosed(object sender, EventArgs e)
        {
            mediaPlayer.Close();
        }

        public void animate(List<Slide> _slides, int _start = 0, String _soundtrack = "")
        {
            if (_slides != null)
            {
                slides.Clear();
                foreach (Slide slide in _slides)
                {
                    if (slide.keys.Count > 1 || slide.keys[0].duration > 0)
                    {
                        slides.Add(slide);
                    }
                }
            }

            soundtrack = _soundtrack;

            currentImage = image1;
            frame1.Opacity = 1;
            otherImage = image2;
            frame2.Opacity = 0;
            currentSlideIndex = _start;
            nextSlideIndex = currentSlideIndex + 1;
            if (nextSlideIndex >= slides.Count) { nextSlideIndex = 0; }

            var bitmap = new BitmapImage(slides[currentSlideIndex].uri);
            currentImage.Source = bitmap;
            
            bitmap = new BitmapImage(slides[nextSlideIndex].uri);
            otherImage.Source = bitmap;

            this.UpdateLayout();

            transformImage(currentImage, slides[currentSlideIndex].keys[0]);
            startPanZoom(currentImage, slides[currentSlideIndex].keys);
            if (soundtrack != "")
            {
                if (mediaPlayer.Position == TimeSpan.FromSeconds(0)) {
                    mediaPlayer.Open(new Uri(soundtrack));
                }
                mediaPlayer.Play();
            }
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
            var duration = new Duration(TimeSpan.FromSeconds(1.5));
            var animFadeOut = new DoubleAnimation(1, 0, duration);
            var animFadeIn = new DoubleAnimation(0, 1, duration);
            animFadeIn.Completed += new EventHandler(endFadeOutIn);

            AnimationClock fadeInClock = animFadeIn.CreateClock();
            AnimationClock fadeOutClock = animFadeOut.CreateClock();
            currentClocks.Clear();
            currentClocks.Add(fadeInClock);
            currentClocks.Add(fadeOutClock);

            transformImage(otherImage, slides[nextSlideIndex].keys[0]);

            if (currentImage == image1)
            {
                currentImage = image2;
                otherImage = image1;
                frame1.ApplyAnimationClock(OpacityProperty, fadeOutClock);
                frame2.ApplyAnimationClock(OpacityProperty, fadeInClock);
            }
            else
            {
                currentImage = image1;
                otherImage = image2;
                frame2.ApplyAnimationClock(OpacityProperty, fadeOutClock);
                frame1.ApplyAnimationClock(OpacityProperty, fadeInClock);
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
            startPanZoom(currentImage, slides[currentSlideIndex].keys);

            var bitmap = new BitmapImage(slides[nextSlideIndex].uri);
            otherImage.Source = bitmap;

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

            AnimationClock zClock = animZoom.CreateClock();
            AnimationClock cxClock = animCtrX.CreateClock();
            AnimationClock cyClock = animCtrY.CreateClock();
            AnimationClock pxClock = animPanX.CreateClock();
            AnimationClock pyClock = animPanY.CreateClock();

            currentClocks.Clear();
            currentClocks.Add(zClock);
            currentClocks.Add(cxClock);
            currentClocks.Add(cyClock);
            currentClocks.Add(pxClock);
            currentClocks.Add(pyClock);

            currentClocks.ForEach(c =>
            {
                c.Controller.SpeedRatio = speedFactor;
            });

            var tg = (TransformGroup)image.RenderTransform;
            tg.Children[0].ApplyAnimationClock(TranslateTransform.XProperty, pxClock);
            tg.Children[0].ApplyAnimationClock(TranslateTransform.YProperty, pyClock);
            tg.Children[1].ApplyAnimationClock(ScaleTransform.ScaleXProperty, zClock);
            tg.Children[1].ApplyAnimationClock(ScaleTransform.ScaleYProperty, zClock);
            tg.Children[1].ApplyAnimationClock(ScaleTransform.CenterXProperty, cxClock);
            tg.Children[1].ApplyAnimationClock(ScaleTransform.CenterYProperty, cyClock);
        }

        void togglePauseAnimation()
        {
            if (paused)
            {
                currentClocks.ForEach(c =>
                {
                    c.Controller.Resume();
                    mediaPlayer.Play();
                });
                paused = false;
            }
            else
            {
                currentClocks.ForEach(c =>
                {
                    c.Controller.Pause();
                    mediaPlayer.Pause();
                });
                paused = true;
            }
        }

        void skipAhead()
        {
            currentClocks.ForEach(c =>
            {
                c.Controller.Stop();
            });
            int newStart = currentSlideIndex + 1;
            if (newStart >= slides.Count) { newStart = 0; }
            animate(null, newStart, soundtrack);
        }

        void skipBack()
        {
            if (currentClocks.First().CurrentProgress > 0.2)
            {
                currentClocks.ForEach(c =>
                {
                    c.Controller.Begin();
                });
            }
            else
            {
                currentClocks.ForEach(c =>
                {
                    c.Controller.Stop();
                });
                int newStart = currentSlideIndex - 1;
                if (newStart < 0) { newStart = slides.Count - 1; }
                animate(null, newStart, soundtrack);
            }
        }
        void speedUp()
        {
            speedFactor = speedFactor * 2.0;
            currentClocks.ForEach(c =>
            {
                c.Controller.SpeedRatio = speedFactor;
            });
        }

        void speedDown()
        {
            speedFactor = speedFactor / 2.0;
            currentClocks.ForEach(c =>
            {
                c.Controller.SpeedRatio = speedFactor;
            });
        }


        void animationWindowLoaded(object sender, RoutedEventArgs e)
        {
            this.ToggleWindow();
        }

        private void KeyHandler(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    if(this.WindowState == WindowState.Maximized)
                    {
                        ToggleWindow();
                    }
                    MessageBox.Show("Breakpoint");
                    break;

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
                        exitOnClose = true;
                        this.Close();
                    }
                    break;

                case Key.Up:
                    speedUp();
                    break;

                case Key.Down:
                    speedDown();
                    break;

                case Key.Right:
                    skipAhead();
                    break;

                case Key.Left:
                    skipBack();
                    break;

                case Key.Space:
                    togglePauseAnimation();
                    break;

                case Key.Home:
                    exitOnClose = false;
                    this.Close();
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
            if (slides.Count > 0)
            {
                currentClocks.ForEach(c =>
                {
                    c.Controller.Stop();
                });
                transformImage(currentImage, slides[currentSlideIndex].keys[0]);
                startPanZoom(currentImage, slides[currentSlideIndex].keys);

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
