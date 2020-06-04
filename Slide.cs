using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Windows;

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
        [JsonIgnore]
        public SlideControl slideControl { get; set; }

        [JsonIgnore]
        public Uri uri;

        private string _fileName;
        public string fileName 
        {  
            get { return _fileName; }
            set { _fileName = value;  uri = new Uri(_fileName); }
        }

        public List<KF> keys { get; set; }
        public Slide()
        {
            keys = new List<KF>();
        }
        public Slide(string s)
        {
            fileName = s;
            keys = new List<KF>();
        }


    }
}
