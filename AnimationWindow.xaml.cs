using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Linq;
using System.Windows.Threading;

namespace KB30
{
    /// <summary>
    /// Interaction logic for AnimationWindow.xaml
    /// </summary>
    public partial class AnimationWindow : Window
    {
        public Slides slides = new Slides();
        int audioSlide = -1;
        int backgroundAudioSlide = -1;

        Image currentImage;
        Image otherImage;
        int currentSlideIndex;
        int nextSlideIndex;
        double speedFactor = 1;

        const int NONE = 0;
        const int PAN_ZOOM = 1;
        const int FADE_OUT_IN = 2;

        private int animationState = NONE;

        private static readonly CubicEase easeOut = new CubicEase() { EasingMode = EasingMode.EaseOut };
        private static readonly CubicEase easeIn = new CubicEase() { EasingMode = EasingMode.EaseIn };
        private static readonly CubicEase easeInOut = new CubicEase() { EasingMode = EasingMode.EaseInOut };

        private DispatcherTimer playTimer = new DispatcherTimer();

        private List<AnimationClock> currentClocks = new List<AnimationClock>();

        private Boolean paused = false;
        private Boolean skip_fade = false;
        public Boolean exitOnClose { get; set; }

        public AnimationWindow()
        {
            InitializeComponent();
            exitOnClose = false;
            Loaded += animationWindowLoaded;
            Closed += animationWindowClosed;
            playTimer.Interval = new TimeSpan(0, 0, 0, 1);
            playTimer.Tick += playTimerTick;
        }

        private void animationWindowClosed(object sender, EventArgs e)
        {
            stopAllClocks();
            playTimer.Stop();
        }

        public void animate(Slides _slides, int _start = 0, String _soundtrack = "")
        {
            slides.Clear();
            foreach (Slide slide in _slides)
            {
                if (slide.keys.Count == 1 && slide.keys[0].duration == 0)
                {
                    slide.keys[0].duration = 0.1;
                }
                slides.Add(slide);

            }
            if (slides.Count == 0)
            {
                MessageBox.Show("At least one slide must have non-zero duration.");
                exitOnClose = true;
                this.Close();
                return;
            }

            // for back-compatibility 
            if (String.IsNullOrEmpty(slides[0].backgroundAudio))
            {
                slides[0].backgroundAudio = _soundtrack;
            }

            startAnimation(_start);
        }

        public void startAnimation(int _start = 0)
        {
            currentImage = image1;
            otherImage = image2;
            currentSlideIndex = _start;
            nextSlideIndex = currentSlideIndex + 1;
            if (nextSlideIndex >= slides.Count) { nextSlideIndex = 0; }

            currentImage.Source = Util.BitmapFromUri(slides[currentSlideIndex].uri);
            otherImage.Source = Util.BitmapFromUri(slides[nextSlideIndex].uri);

            this.UpdateLayout();

            transformImage(currentImage, slides[currentSlideIndex].keys[0]);

            syncBackgroundAudioPosition(currentSlideIndex);
            paused = false;
            frame1.Opacity = 1;
            frame2.Opacity = 0;
            beginPanZoom(currentImage, slides[currentSlideIndex].keys);
            playTimer.Start();
        }


        private void transformImage(Image image, Keyframe kf)
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
            if (skip_fade)
            {
                skip_fade = false;
                fillAllClocks();
            }
        }

        private void beginFadeOutIn()
        {
            animationState = FADE_OUT_IN;
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
            currentSlideIndex = nextSlideIndex;
            nextSlideIndex++;
            if (nextSlideIndex >= slides.Count)
            {
                nextSlideIndex = 0;
            }
        }

        private void endFadeOutIn(object sender, EventArgs e)
        {
            if (skip_fade) { skip_fade = false; }


            beginPanZoom(currentImage, slides[currentSlideIndex].keys);

            // Load next image *after* we start pan/zoom, for smoother transititions.
            otherImage.Source = Util.BitmapFromUri(slides[nextSlideIndex].uri);
        }

        private void beginPanZoom(Image image, List<Keyframe> keys)
        {
            if (!String.IsNullOrEmpty(slides[nextSlideIndex].backgroundAudio))
            {
                startBackgroundAudioFade(currentSlideIndex);
            }

            if (!String.IsNullOrEmpty(slides[currentSlideIndex].audio))
            {
                transitionAudio(slides[currentSlideIndex]);
            }
            if (!String.IsNullOrEmpty(slides[currentSlideIndex].backgroundAudio))
            {
                transitionBackgroundAudio(slides[currentSlideIndex]);
            }

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

            status.Visibility = Visibility.Visible;
            time_remaining.Visibility = Visibility.Visible;
            animationState = PAN_ZOOM;
        }

        void togglePauseAnimation()
        {
            if (paused)
            {
                currentClocks.ForEach(c =>
                {
                    c.Controller.Resume();
                });
                backgroundAudio.Play();
                playTimer.Start();
                paused = false;
            }
            else
            {
                currentClocks.ForEach(c =>
                {
                    c.Controller.Pause();
                });
                backgroundAudio.Pause();
                playTimer.Stop();
                paused = true;
            }
        }

        void skipAhead()
        {
            skip_fade = true;
            fillAllClocks();
            syncBackgroundAudioPosition(nextSlideIndex);
        }

        void skipBack()
        {
            if (currentSlideIndex < 0)
            {
                return;
            }
            if (animationState == PAN_ZOOM)
            {
                if (currentClocks.First().CurrentTime > TimeSpan.FromSeconds(1))
                {
                    currentClocks.ForEach(c =>
                    {
                        c.Controller.Begin();
                    });
                    syncBackgroundAudioPosition(currentSlideIndex);
                }
                else
                {
                    currentSlideIndex = currentSlideIndex - 2;
                    if (currentSlideIndex < 0)
                    {
                        currentSlideIndex = currentSlideIndex + slides.Count;
                    }
                    nextSlideIndex = currentSlideIndex + 1;
                    if (nextSlideIndex >= slides.Count) { nextSlideIndex = 0; }
                    otherImage.Source = Util.BitmapFromUri(slides[nextSlideIndex].uri);
                    skip_fade = true;
                    fillAllClocks();
                    syncBackgroundAudioPosition(nextSlideIndex);
                }
            }
            else  // fade inout
            {
                if (slides[currentSlideIndex].Duration() < 1)
                {
                    currentSlideIndex = currentSlideIndex - 2;
                }
                else
                {
                    currentSlideIndex = currentSlideIndex - 1;
                }
                if (currentSlideIndex < 0)
                {
                    currentSlideIndex = currentSlideIndex + slides.Count;
                }
                nextSlideIndex = currentSlideIndex + 1;
                if (nextSlideIndex >= slides.Count) { nextSlideIndex = 0; }

                currentImage.Source = Util.BitmapFromUri(slides[nextSlideIndex].uri);
                fillAllClocks();
                syncBackgroundAudioPosition(nextSlideIndex);
            }
        }
        void escape()
        {
            if (this.WindowState == WindowState.Maximized)
            {
                ToggleWindow();
            }
            else
            {
                stopAllClocks();
                exitOnClose = true;
                this.Close();
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
            // wait for rendering to complete so new window size is set
            Dispatcher.Invoke(new Action(() => { }), DispatcherPriority.Render, null);
            if (slides.Count > 0)
            {
                if (!paused)
                {
                    stopAllClocks();
                }
                transformImage(currentImage, slides[currentSlideIndex].keys[0]);
                if (!paused)
                {
                    beginPanZoom(currentImage, slides[currentSlideIndex].keys);
                }
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
        void stopAllClocks()
        {
            currentClocks.ForEach(c =>
            {
                c.Controller.Stop();
            });
        }
        void fillAllClocks()
        {
            currentClocks.ForEach(c =>
            {
                c.Controller.SkipToFill();
            });
        }

        double timeBetweenSlides(int start_slide_index, int end_slide_index)
        {
            double totalDuration = 0;
            for (int s = start_slide_index; s < end_slide_index; s++)
            {
                for (int k = 0; k < slides[s].keys.Count; k++)
                {
                    totalDuration += slides[s].keys[k].duration;
                }
                totalDuration += 1.5; // for fade in out
            }
            totalDuration = totalDuration / speedFactor;
            return totalDuration;
        }

        string timeElapsed()
        {
            double currentSlideDuration;
            double totalDuration;

            if (currentSlideIndex < 0)
            {
                // don't know why
                return ("0:00");
            }

            if (animationState == PAN_ZOOM)
            {
                currentSlideDuration = slides[currentSlideIndex].keys.Sum(k => k.duration);
                totalDuration = currentClocks[0].CurrentProgress.Value * currentSlideDuration;
            }
            else
            {
                totalDuration = slides[currentSlideIndex].keys.Sum(k => k.duration);
                totalDuration += currentClocks[0].CurrentProgress.Value * 1.5;
            }

            for (int s = 0; s < currentSlideIndex; s++)
            {
                totalDuration += slides[s].Duration();
                totalDuration += 1.5; // for fade in out
            }
            totalDuration = totalDuration / speedFactor;
            int durationMins = (int)(totalDuration / 60);
            int durationSecs = (int)(totalDuration % 60);
            return durationMins.ToString("D2") + ":" + durationSecs.ToString("D2");
        }


        string timeRemaining()
        {
            double currentSlideDuration;
            double totalDuration;

            if (currentSlideIndex < 0)
            {
                // don't know why
                return ("0:00");
            }
            if (animationState == PAN_ZOOM)
            {
                currentSlideDuration = slides[currentSlideIndex].keys.Sum(k => k.duration);
                totalDuration = ((1 - currentClocks[0].CurrentProgress.Value) * currentSlideDuration) + 1.5;
            }
            else
            {
                currentSlideDuration = 1.5;
                totalDuration = (1 - currentClocks[0].CurrentProgress.Value) * currentSlideDuration;
            }

            for (int s = currentSlideIndex + 1; s < slides.Count; s++)
            {
                totalDuration += slides[s].Duration();
                totalDuration += 1.5; // for fade in out
            }
            totalDuration = totalDuration / speedFactor;
            int durationMins = (int)(totalDuration / 60);
            int durationSecs = (int)(totalDuration % 60);
            return durationMins.ToString("D2") + ":" + durationSecs.ToString("D2");
        }

        private void playTimerTick(object sender, EventArgs e)
        {
            time_remaining.Text = " " + timeRemaining();
            status.Text = " " + DateTime.Now.ToString("HH:mm") + "  ♦  " + (currentSlideIndex + 1).ToString() + " of " + slides.Count + "  ♦  " + timeElapsed() + " ";
        }


        /*
         * Audio
         */
        private void transitionAudio(Slide audio_slide)
        {
            if (!String.IsNullOrEmpty(audio_slide.audio))
            {
                audio.Stop();
                audio.Source = new Uri(audio_slide.audio, UriKind.RelativeOrAbsolute);
                if (audio_slide.audioVolume != 0)
                {
                    audio.Volume = audio_slide.audioVolume;
                } else
                {
                    audio.Volume = 1.0;
                }
                audio.Play();
                audioSlide = slides.IndexOf(audio_slide);
            }
        }


        /*
         * Background Audio 
         */

        private void backgroundAudioOpened(object sender, RoutedEventArgs e)
        {
            if (slides[backgroundAudioSlide].backgroundVolume != 0)
            {
                backgroundAudio.Volume = slides[backgroundAudioSlide].backgroundVolume;
            }
            else
            {
                backgroundAudio.Volume = 0.5;
            }
            double song_duration = backgroundAudio.NaturalDuration.TimeSpan.TotalSeconds;
            double offset = timeBetweenSlides(backgroundAudioSlide, currentSlideIndex);
            double audioStartTime = offset % song_duration;
            backgroundAudio.Position = TimeSpan.FromSeconds(audioStartTime);
            backgroundAudio.MediaEnded += restartBackgroundAudio;
        }

        void restartBackgroundAudio(object sender, RoutedEventArgs e)
        {
            backgroundAudio.Position = TimeSpan.FromSeconds(0);
            backgroundAudio.Play();
        }

        private void syncBackgroundAudioPosition(int slideIndex)
        {
            int audio_slide_index = -1;
            for (int i = slideIndex; i >= 0; i--)
            {
                if (!String.IsNullOrEmpty(slides[i].backgroundAudio))
                {
                    audio_slide_index = i;
                    break;
                }
            }

            if (audio_slide_index >= 0)
            {
                if (audio_slide_index != backgroundAudioSlide)
                {
                    backgroundAudio.Stop();
                    backgroundAudioSlide = audio_slide_index;
                    backgroundAudio.Source = new Uri(slides[audio_slide_index].backgroundAudio, UriKind.RelativeOrAbsolute);
                    backgroundAudio.Play();
                }
                else
                {
                    double song_duration = backgroundAudio.NaturalDuration.TimeSpan.TotalSeconds;
                    double offset = timeBetweenSlides(backgroundAudioSlide, slideIndex);
                    double audioStartTime = offset % song_duration;
                    backgroundAudio.Position = TimeSpan.FromSeconds(audioStartTime);
                }
            }
        }

        private void startBackgroundAudioFade(int s)
        {
            Slide slide = slides[s];
            double slideDuration = slide.keys.Sum(k => k.duration);
            var volumeFadeOut = new DoubleAnimation(backgroundAudio.Volume, 0, TimeSpan.FromSeconds(slideDuration + 1.5));
            volumeFadeOut.Completed += endBackgroundAudioFadeOut;
            AnimationClock volumeFadeOutClock = volumeFadeOut.CreateClock();
            backgroundAudio.ApplyAnimationClock(MediaElement.VolumeProperty, volumeFadeOutClock);
        }

        private void transitionBackgroundAudio(Slide audio_slide)
        {
            if (!String.IsNullOrEmpty(audio_slide.backgroundAudio))
            {
                backgroundAudio.Stop();
                backgroundAudio.Source = new Uri(audio_slide.backgroundAudio, UriKind.RelativeOrAbsolute);
                backgroundAudio.Play();
                backgroundAudioSlide = slides.IndexOf(audio_slide);
            }
        }

        private void endBackgroundAudioFadeOut(object sender, EventArgs e)
        {
            backgroundAudio.Stop();
            var volumeFadeIn = new DoubleAnimation(0, 0.5, TimeSpan.FromSeconds(0.1));
            AnimationClock volumeFadeClock = volumeFadeIn.CreateClock();
            backgroundAudio.ApplyAnimationClock(MediaElement.VolumeProperty, volumeFadeClock);
        }

        private void KeyHandler(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    if (this.WindowState == WindowState.Maximized)
                    {
                        ToggleWindow();
                    }
                    MessageBox.Show("Breakpoint!");
                    break;

                case Key.Enter:
                    this.ToggleWindow();
                    break;

                case Key.Escape:
                    escape();
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
        private void animationWindow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            escape();
        }

        private void animationWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            skipBack();
        }

        private void animationWindow_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            skipAhead();
        }
    }
}
