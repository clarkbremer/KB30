using System.Collections.Generic;
using Newtonsoft.Json;
using System.Windows;

namespace KB30
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Keyframe : FrameworkElement
    {
        [JsonProperty]
        public double zoomFactor { get; set; }
        [JsonProperty]
        public double x { get; set; }
        [JsonProperty]
        public double y { get; set; }
        [JsonProperty]
        public double duration { get; set; }

        public static readonly DependencyProperty zoomFactorProperty = DependencyProperty.Register("zoomFactor", typeof(double), typeof(Keyframe));
        public static readonly DependencyProperty xProperty = DependencyProperty.Register("x", typeof(double), typeof(Keyframe));
        public static readonly DependencyProperty yProperty = DependencyProperty.Register("y", typeof(double), typeof(Keyframe));
        public static readonly DependencyProperty durationProperty = DependencyProperty.Register("duration", typeof(double), typeof(Keyframe));


        public Keyframe(double z, double x, double y, double d)
        {
            this.zoomFactor = z;
            this.x = x;
            this.y = y;
            this.duration = d;
        }

        public Keyframe() { }

        public KeyframeControl keyframeControl { get; set; }

        public void Dim()
        {
            keyframeControl.Opacity = 0.5;
        }
        public void UnDim()
        {
            keyframeControl.Opacity = 1.0;
        }

        public bool IsSelected()
        {
            return keyframeControl.selected;
        }

        public void highlightLeft() { keyframeControl.highlightLeft(); }
        public void highlightRight() { keyframeControl.highlightRight(); }
        public void highlightClear() { keyframeControl.highlightClear(); }
    }

    public class Keyframes : List<Keyframe>
    {
        public void UnDimAll()
        {
            foreach (Keyframe key in this) { key.UnDim(); }
        }
        public void ClearHighlightAll()
        {
            foreach (Keyframe key in this) { key.keyframeControl.highlightClear(); }
        }
        public void ClearHightlightsExcept(Keyframe notMe)
        {
            foreach (Keyframe k in this)
            {
                if (k != notMe)
                {
                    k.keyframeControl.highlightClear();
                }
            }
        }
    }
}
