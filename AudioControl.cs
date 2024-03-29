﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace KB30
{
    public class AudioControl
    {
        MediaElement mediaPlayer;
        Slides slides;
        string filename_property;
        string volume_property;
        string loop_property;
        AnimationClock volumeFadeOutClock;
        Slide mediaSlide;
        int currentSlideIndex;

        public AudioControl(
            MediaElement _player,
            Slides _slides,
            string _filename_property,
            string _volume_property,
            string _loop_property
            )
        {
            mediaPlayer = _player;
            mediaPlayer.MediaOpened += mediaOpened;
            mediaPlayer.MediaEnded += mediaEnded;
            slides = _slides;
            filename_property = _filename_property;
            volume_property = _volume_property;
            loop_property = _loop_property;
        }

        public void start(Slide audio_slide)
        {
            if (File.Exists(filename(audio_slide)))
            {
                mediaPlayer.Stop();
                mediaPlayer.Source = new Uri(filename(audio_slide), UriKind.RelativeOrAbsolute);
                mediaPlayer.Play();  // volume will get set in "opened()"
                mediaSlide = audio_slide;
                currentSlideIndex = slides.IndexOf(audio_slide);
            }
        }

        public void play()
        {
            // olny if we had been playing
            var state = GetMediaState(mediaPlayer);
            if (state == MediaState.Pause)
            {
                mediaPlayer.Play();
            }
        }
        public void pause()
        {
            var state = GetMediaState(mediaPlayer);
            if (state == MediaState.Play)
            {
                mediaPlayer.Pause();
            }
        }


        public void syncToSlide(int slideIndex)
        {
            currentSlideIndex = slideIndex;
            Slide prev_media_slide = null;
            for (int i = slideIndex; i >= 0; i--)
            {
                if (hasAudio(slides[i]))
                {
                    prev_media_slide = slides[i];
                    break;
                }
            }

            if (prev_media_slide == null)  // nothing to play
            {
                return;
            }

            if (volumeFadeOutClock != null && volumeFadeOutClock.CurrentState == ClockState.Active)
            {
                volumeFadeOutClock.Controller.Stop();
                mediaPlayer.ApplyAnimationClock(MediaElement.VolumeProperty, null);
            }

            if (prev_media_slide == mediaSlide)   // same audio, just need to set new position
            {
                double audio_duration = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                double time_between = timeBetweenSlides(mediaSlide, slideIndex);
                if ((audio_duration > time_between) || loop(mediaSlide))  // still playing first time through or looping
                {
                    double audioStartTime = time_between % audio_duration;
                    mediaPlayer.Position = TimeSpan.FromSeconds(audioStartTime);
                    mediaPlayer.Play();
                }
                else
                {
                    mediaPlayer.Stop();
                }
            }
            else // not the same audio
            {
                if (File.Exists(filename(prev_media_slide)))
                {
                    mediaPlayer.Stop();
                    mediaSlide = prev_media_slide;
                    mediaPlayer.Source = new Uri(filename(prev_media_slide), UriKind.RelativeOrAbsolute);
                    mediaPlayer.Play();  // the position will be adjusted in mediaOpened().  We don't know duration, etc, until then.
                }
            }
        }

        public void beginFadeOut(int s)
        {
            Slide slide = slides[s];
            double slideDuration = slide.keys.Sum(k => k.duration);
            var volumeFadeOut = new DoubleAnimation(mediaPlayer.Volume, 0, TimeSpan.FromSeconds(slideDuration + 1.5));
            volumeFadeOut.Completed += endFadeOut;
            volumeFadeOut.FillBehavior = FillBehavior.Stop;
            volumeFadeOutClock = volumeFadeOut.CreateClock();
            mediaPlayer.ApplyAnimationClock(MediaElement.VolumeProperty, volumeFadeOutClock);
        }

        private void endFadeOut(object sender, EventArgs e)
        {
            mediaPlayer.Stop();
        }

        private void mediaOpened(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Volume = volume(mediaSlide);

            double audio_duration = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            double time_between = timeBetweenSlides(mediaSlide, currentSlideIndex);
            if ((audio_duration > time_between) || loop(mediaSlide))  // still playing first time through or looping
            {
                double audioStartTime = time_between % audio_duration;
                mediaPlayer.Position = TimeSpan.FromSeconds(audioStartTime);
                mediaPlayer.MediaEnded += mediaEnded;
            }
            else
            {
                mediaPlayer.Stop();
            }
        }
        private void mediaEnded(object sender, RoutedEventArgs e)
        {
            if (mediaSlide != null && loop(mediaSlide))
            {
                mediaPlayer.Position = TimeSpan.FromSeconds(0);
                mediaPlayer.Play();
            }
            else
            {
                mediaSlide = null;
                mediaPlayer.Close();
            }
        }

        private string filename(Slide slide)
        {
            string f = (string) typeof(Slide).GetProperty(filename_property, typeof(string)).GetValue(slide, null);
            return f;
        }

        private double volume(Slide slide)
        {
            double v = (double) typeof(Slide).GetProperty(volume_property, typeof(double)).GetValue(slide, null);
            return v;
        }

        private Boolean loop(Slide slide)
        {
            Boolean l = (Boolean)typeof(Slide).GetProperty(loop_property, typeof(Boolean)).GetValue(slide, null);
            return l;
        }

        private Boolean hasAudio(Slide slide)
        {
            return (!String.IsNullOrEmpty(filename(slide)));
        }
        private MediaState GetMediaState(MediaElement myMedia)  // I didn't write this - found it on the net
        {
            FieldInfo hlp = typeof(MediaElement).GetField("_helper", BindingFlags.NonPublic | BindingFlags.Instance);
            object helperObject = hlp.GetValue(myMedia);
            FieldInfo stateField = helperObject.GetType().GetField("_currentState", BindingFlags.NonPublic | BindingFlags.Instance);
            MediaState state = (MediaState)stateField.GetValue(helperObject);
            return state;
        }

        private double timeBetweenSlides(Slide start_slide, int end_slide_index)
        {
            return timeBetweenSlides(slides.IndexOf(start_slide), end_slide_index);
        }
        private double timeBetweenSlides(int start_slide_index, int end_slide_index)
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
            return totalDuration;
        }
    }
}
