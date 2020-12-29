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

        public void Dim() { slideControl.image.Opacity = 0.4; }
        public void UnDim() { slideControl.image.Opacity = 1.0; }

        public bool IsChecked() { return (bool)(slideControl?.IsChecked()); }
        public void Check() { slideControl?.Check(); }
        public void UnCheck() { slideControl?.UnCheck(); }
        public void highlightAbove() { slideControl.highlightAbove(); }
        public void highlightBelow() { slideControl.highlightBelow(); }
        public void highlightClear() { slideControl.highlightClear(); }

    }

    public class Slides : List<Slide>
    {
        public void UncheckAll()
        {
            foreach (Slide slide in this) { slide.slideControl.UnCheck(); }
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

    }

  
}