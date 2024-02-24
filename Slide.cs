using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace KB30
{

    public class Slide
    {
        public Slide()
        {
            keys = new Keyframes();
        }
        public Slide(string s)
        {
            fileName = s;
            keys = new Keyframes();
        }
        public Keyframes keys { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string backgroundAudio;
        public bool ShouldSerializebackgroundAudio() { return !string.IsNullOrEmpty(backgroundAudio); }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double backgroundVolume = 0.5;
        public bool ShouldSerializebackgroundVolume() { return !string.IsNullOrEmpty(backgroundAudio); }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool loopBackground = true;
        public bool ShouldSerializeloopBackground() { return !string.IsNullOrEmpty(backgroundAudio); }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string audio;
        public bool ShouldSerializeaudio() { return !string.IsNullOrEmpty(audio); }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public double audioVolume { get; set; } = 0.9;
        public bool ShouldSerializeaudioVolume() { return !string.IsNullOrEmpty(audio); }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool loopAudio = false;
        public bool ShouldSerializeloopAudio() { return !string.IsNullOrEmpty(audio); }

        [JsonIgnore]
        public SlideControl slideControl { get; set; }

        private Uri _uri;
        [JsonIgnore]
        public Uri uri
        {
            get { 
                if (_uri == null)
                {
                    if (fileName == "black")
                    {
                        _uri = new Uri(@"pack://application:,,,/Resources/black.png", UriKind.Absolute);
                    }
                    else if (fileName == "white")
                    {
                        _uri = new Uri(@"pack://application:,,,/Resources/white.png", UriKind.Absolute);
                    }
                    else
                    {
                        _uri = new Uri(fileName);
                    }
                }
                return _uri; 
            }
            set { 
                _uri = value; 
            }
        }

        [JsonIgnore]
        public bool isResource = false;

        // these are vestigal
        public bool ShouldSerializerelativePath() { return false; }
        public string relativePath;
        
        public string fileName;

        public double Duration()
        {
            double totalDuration = 0;
            for (int k = 0; k < keys.Count; k++)
            {
                totalDuration += keys[k].duration;
            }
            return totalDuration;
        }

        public void SetupDefaultKeyframes()
        {
            var bmp = (System.Windows.Media.Imaging.BitmapImage)slideControl.image.Source;
            if (bmp.UriSource.Scheme == "file")
            {
                double w = bmp.Width;
                double h = bmp.Height;
                if (w > h)
                {
                    // Lansdcape
                    keys.Add(new Keyframe(1.3, 0.5, 0.5, 0));
                    keys.Add(new Keyframe(2.0, 0.5, 0.5, 9));
                }
                else
                {
                    // Portrait
                    keys.Add(new Keyframe(3.0, 0.5, 0.2, 0));
                    keys.Add(new Keyframe(3.0, 0.5, 0.8, 9));
                }
            }
            else
            {
                keys.Add(new Keyframe(2.0, 0.5, 0.5, 1));
            }
        }

        public Slide Clone()
        {
            Slide clone = new Slide(fileName);
            foreach (Keyframe k in keys)
            {
                clone.keys.Add(k.Clone());
            }
            return clone;
        }
        public void UpdateAudioNotations()
        {
            if (slideControl != null)
            {
                if (!String.IsNullOrEmpty(backgroundAudio))
                {
                    slideControl.slideNote.Text = "B♦";
                }
                else
                {
                    slideControl.slideNote.Text = "";
                }
                if (!String.IsNullOrEmpty(audio))
                {
                    slideControl.slideNote.Text = slideControl.slideNote.Text + "A♦";
                }
            }
        }

        public void Dim() { if (slideControl != null) { slideControl.image.Opacity = 0.4; } }
        public void UnDim() { if (slideControl != null) { slideControl.image.Opacity = 1.0; } }

        public bool IsChecked() { return (bool)(slideControl?.IsChecked() ?? false); }
        public void Check() { slideControl?.Check(); }
        public void UnCheck() { slideControl?.UnCheck(); }
        public void ToggleCheck() { slideControl?.ToggleCheck(); }
        public void highlightAbove() { slideControl?.highlightAbove(); }
        public void highlightBelow() { slideControl?.highlightBelow(); }
        public void highlightClear() { slideControl?.highlightClear(); }

        public void BringIntoView() { slideControl?.BringIntoView(); }

        public bool hasBackgroundAudio() { return !String.IsNullOrEmpty(backgroundAudio); }
        public bool hasAudio() { return !String.IsNullOrEmpty(audio); }

    }

    public class Slides : List<Slide>
    {
        public void UncheckAll()
        {
            foreach (Slide slide in this) { slide.UnCheck(); }
        }

        public void Renumber()
        {
            foreach (Slide slide in this)
            {
                if (slide.slideControl != null)
                {
                    slide.slideControl.slideNumber = this.IndexOf(slide) + 1;
                }
            }
        }

        String double_to_time_string(double t)
        {
            int durationMins = (int)(t / 60);
            int durationSecs = (int)(t % 60);
            return durationMins.ToString("D2") + ":" + durationSecs.ToString("D2");
        }

        public String SlideStartTimeString(int slide_index)
        {
            return (double_to_time_string(SlideStartTime(slide_index)));
        }
        public double SlideStartTime(int slide_index)
        {
            double start_time = 0.0;
            for (int s = 0; s < slide_index; s++)
            {
                start_time += this[s].Duration();
                start_time += 1.5; // for fade in out
            }
            return (start_time);
        }

        public String TotalTimeString()
        {
            return (double_to_time_string(TotalTime()));
        }
        public double TotalTime()
        {
            double totalDuration = 0;
            foreach (Slide slide in this)
            {
                for (int k = 0; k < slide.keys.Count; k++)
                {
                    totalDuration += slide.keys[k].duration;
                }
                totalDuration += 1.5; // for fade in out
            }
            return (totalDuration);
        }

        public double TimeRemaining(int slide_index)
        {
            double remaining = TotalTime() - SlideStartTime(slide_index);
            return (remaining);
        }

        public String TimeRemainingString(int slide_index)
        {
            return (double_to_time_string(TimeRemaining(slide_index)));
        }


        public void UnDimAll()
        {
            foreach (Slide slide in this) { slide.UnDim(); }
        }
        public void ClearHighlightAll()
        {
            foreach (Slide slide in this) { slide.highlightClear(); }
        }

        public void ClearHightlightsExcept(Slide notMe)
        {
            foreach (Slide s in this)
            {
                if (s != notMe)
                {
                    s.highlightClear();
                }
            }
        }
    }
}