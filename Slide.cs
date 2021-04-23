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

        [JsonIgnore]
        public SlideControl slideControl { get; set; }

        [JsonIgnore]
        public string basePath;

        [JsonIgnore]
        public Uri uri;

        public bool ShouldSerializefileName() { return false; }

        private string _fileName;
        public string fileName
        {
            get
            {
                if (!string.IsNullOrEmpty(basePath) && string.IsNullOrEmpty(_fileName))
                {
                    _fileName = Path.GetFullPath(_relativePath, basePath);
                    uri = new Uri(_fileName);
                }
                return (_fileName);
            }
            set
            {
                _fileName = value;
                uri = new Uri(_fileName);
                if (!string.IsNullOrEmpty(basePath) && string.IsNullOrEmpty(_relativePath))
                {
                    _relativePath = Path.GetRelativePath(basePath, _fileName);
                }
            }
        }

        private string _relativePath;
        public string relativePath
        {
            get
            {
                if (!string.IsNullOrEmpty(basePath) && !string.IsNullOrEmpty(_fileName))
                {
                    _relativePath = Path.GetRelativePath(basePath, _fileName);
                }
                return (_relativePath);
            }
            set
            {
                _relativePath = value;
                if (!string.IsNullOrEmpty(basePath) && string.IsNullOrEmpty(_fileName))
                {
                    _fileName = Path.GetFullPath(_relativePath, basePath);
                    uri = new Uri(_fileName);
                }
            }
        }

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
            var bmp = slideControl.image.Source;
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

        public Slide Clone()
        {
            Slide clone = new Slide(fileName);
            foreach (Keyframe k in keys)
            {
                clone.keys.Add(k.Clone());
            }
            return clone;
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

        public void SetBasePath(string basePath)
        {
            foreach (Slide slide in this)
            {
                slide.basePath = basePath;
            }
        }
    }
}