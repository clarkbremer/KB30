﻿using System;
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

        AudioControl background_audio;
        AudioControl foreground_audio;

        Image currentImage;
        Image otherImage;
        public int currentSlideIndex;
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
        public Boolean exitOnClose { get; set; }

        public AnimationWindow()
        {
            InitializeComponent();
            exitOnClose = false;
            Loaded += animationWindowLoaded;
            Closed += animationWindowClosed;
            playTimer.Interval = new TimeSpan(0, 0, 0, 1);
            playTimer.Tick += playTimerTick;
            background_audio = new AudioControl(
                backgroundMedia,
                slides,
                "backgroundAudio",
                "backgroundVolume",
                "loopBackground"
                );
            foreground_audio = new AudioControl(
                 foregroundMedia,
                 slides,
                 "audio",
                 "audioVolume",
                 "loopAudio"
                 );
        }

        private void animationWindowClosed(object sender, EventArgs e)
        {
            stopAllClocks();
            playTimer.Stop();
        }

        public void Animate(Slides _slides, int _start = 0)
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

            currentImage = image1;
            otherImage = image2;
            currentSlideIndex = _start;
            nextSlideIndex = currentSlideIndex + 1;
            if (nextSlideIndex >= slides.Count) { nextSlideIndex = 0; }

            currentImage.Source = Util.BitmapFromUri(slides[currentSlideIndex].uri);
            otherImage.Source = Util.BitmapFromUri(slides[nextSlideIndex].uri);

            this.UpdateLayout();

            transformImage(currentImage, slides[currentSlideIndex].keys[0]);

            background_audio.syncToSlide(currentSlideIndex);
            foreground_audio.syncToSlide(currentSlideIndex);
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
            beginPanZoom(currentImage, slides[currentSlideIndex].keys);

            // Load next image *after* we start pan/zoom, for smoother transititions.
            otherImage.Source = Util.BitmapFromUri(slides[nextSlideIndex].uri);
        }

        private void beginPanZoom(Image image, List<Keyframe> keys)
        {
            if (slides[nextSlideIndex].hasBackgroundAudio())
            {
                background_audio.beginFadeOut(currentSlideIndex);
            }

            if (slides[currentSlideIndex].hasAudio())
            {
                foreground_audio.start(slides[currentSlideIndex]);
            }
            if (slides[currentSlideIndex].hasBackgroundAudio())
            {
                background_audio.start(slides[currentSlideIndex]);
            }

            if (currentImage == image1)  // this should already be set by the fade in/out animations, but in case we get called from other places...
            {
                frame1.Opacity = 1;
                frame2.Opacity = 0;
            }
            else
            {
                frame2.Opacity = 1;
                frame1.Opacity = 0;
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
            if (paused) { 
                unPauseAnimation(); 
            }
            else { 
                pauseAnimation(); 
            }
        }

        void unPauseAnimation() { 
            currentClocks.ForEach(c =>
            {
                c.Controller.Resume();
            });
            background_audio.play();
            foreground_audio.play();
            playTimer.Start();
            paused = false;
        }
        void pauseAnimation() { 
            currentClocks.ForEach(c =>
            {
                c.Controller.Pause();
            });
            background_audio.pause();
            foreground_audio.pause();
            playTimer.Stop();
            paused = true;
        }

        void skipAhead()
        {
            int target = currentSlideIndex + 1;
            if (target >= slides.Count)
            {
                target = 0;
            }
            skipTo(target);
        }

        void skipBack()
        {
            if (currentClocks.First().CurrentTime > TimeSpan.FromSeconds(1))  // if were already more than 1 second in, then just restart current slide
            {
                skipTo(currentSlideIndex);
            }
            else
            {
                int target = currentSlideIndex - 1;
                if (target < 0)
                {
                    target = slides.Count - 1;
                }
                skipTo(target);
            }
        }

        void skipTo(int target_index)
        {
            stopAllClocks();
            currentSlideIndex = target_index;
            nextSlideIndex = currentSlideIndex + 1;
            if (nextSlideIndex >= slides.Count) { nextSlideIndex = 0; }
           
            currentImage.Source = Util.BitmapFromUri(slides[currentSlideIndex].uri);
            otherImage.Source = Util.BitmapFromUri(slides[nextSlideIndex].uri);
            
            this.UpdateLayout();

            transformImage(currentImage, slides[currentSlideIndex].keys[0]);

            background_audio.syncToSlide(currentSlideIndex);
            foreground_audio.syncToSlide(currentSlideIndex);
            paused = false;
          
            beginPanZoom(currentImage, slides[currentSlideIndex].keys);
            playTimer.Start();
        }

        void skipAheadChapter()
        {
            int target_index = currentSlideIndex;
            Boolean found = false;
            while (!found)
            {
                target_index++;
                if (target_index >= slides.Count)
                {
                    target_index = 0;
                }
                if (target_index == currentSlideIndex)
                {
                    break;
                }
                if (slides[target_index].fileName == "black" || slides[target_index].fileName == "white")
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                skipTo(target_index);
            }
        }
        void skipBackChapter()
        {
            int target_index = currentSlideIndex;
            Boolean found = false;
            while (!found)
            {
                target_index--;
                if (target_index < 0)
                {
                    target_index = slides.Count-1;
                }
                if (target_index == currentSlideIndex)
                {
                    break;
                }
                if (slides[target_index].fileName == "black" || slides[target_index].fileName == "white")
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                skipTo(target_index);
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

        double timeBetweenSlides(Slide start_slide, int end_slide_index)
        {
            return timeBetweenSlides(slides.IndexOf(start_slide), end_slide_index);
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
            int finalSlideIndex;

            if (currentSlideIndex < 0)
            {
                // don't know why
                return ("0:00");
            }


            if (animationState == PAN_ZOOM)
            {
                currentSlideDuration = slides[currentSlideIndex].keys.Sum(k => k.duration);
                totalDuration = currentClocks[0].CurrentProgress.Value * currentSlideDuration;
                finalSlideIndex = currentSlideIndex;
            }
            else
            {
                if (currentSlideIndex == 0)  // we are fading from last slide to first.
                {
                    finalSlideIndex = slides.Count - 1;
                }
                else
                {
                    finalSlideIndex = currentSlideIndex - 1;
                }
                totalDuration = slides[finalSlideIndex].keys.Sum(k => k.duration);
                totalDuration += currentClocks[0].CurrentProgress.Value * 1.5;
            }

            for (int s = 0; s < finalSlideIndex; s++)
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
                for (int s = currentSlideIndex + 1; s < slides.Count; s++)
                {
                    totalDuration += slides[s].Duration();
                    totalDuration += 1.5; // for fade in out
                }
            }
            else
            {
                currentSlideDuration = 1.5;
                totalDuration = (1 - currentClocks[0].CurrentProgress.Value) * currentSlideDuration;
                for (int s = currentSlideIndex; s < slides.Count; s++)
                {
                    totalDuration += slides[s].Duration();
                    totalDuration += 1.5; // for fade in out
                }
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
         * User input 
         */
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

                case Key.PageUp:
                    skipBackChapter();
                    break;

                case Key.PageDown:
                    skipAheadChapter();
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
            if (this.WindowState == WindowState.Maximized)
            {
                skipAhead();
            }
            else
            {
                ToggleWindow();
            }
        }

        private void animationWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.XButton1 == MouseButtonState.Pressed)
            {
                skipAheadChapter();
            }
            else if (e.XButton2 == MouseButtonState.Pressed)
            {
                skipBackChapter();
            }
            else if (e.MiddleButton == MouseButtonState.Pressed)
            {
                togglePauseAnimation();
            }
        }
    }
}
