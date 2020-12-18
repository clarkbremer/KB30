using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Windows;
using System.IO;

namespace KB30
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class KF : FrameworkElement
    {
        [JsonProperty]
        public double zoomFactor { get; set; }
        [JsonProperty]
        public double x { get; set; }
        [JsonProperty]
        public double y { get; set; }
        [JsonProperty]
        public double duration { get; set; }

        public static readonly DependencyProperty zoomFactorProperty = DependencyProperty.Register("zoomFactor", typeof(double), typeof(KF));
        public static readonly DependencyProperty xProperty = DependencyProperty.Register("x", typeof(double), typeof(KF));
        public static readonly DependencyProperty yProperty = DependencyProperty.Register("y", typeof(double), typeof(KF));
        public static readonly DependencyProperty durationProperty = DependencyProperty.Register("duration", typeof(double), typeof(KF));


        public KF(double z, double x, double y, double d)
        {
            this.zoomFactor = z;
            this.x = x;
            this.y = y;
            this.duration = d;
        }

        public KF() { }

        public KeyframeControl kfControl { get; set; }
    }

    public class Slide
    {
        public Slide()
        {
            keys = new List<KF>();
        }
        public Slide(string s)
        {
            fileName = s;
            keys = new List<KF>();
        }
        public List<KF> keys { get; set; }

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
            set {
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
            set {
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
}
