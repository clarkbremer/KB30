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

        public Keyframe Clone()
        {
            return new Keyframe(zoomFactor, x, y, duration);
        }
        public Keyframe Clone(double override_duration)
        {
            return new Keyframe(zoomFactor, x, y, override_duration);
        }

        public bool SameVals(Keyframe other)
        {
            if (zoomFactor == other.zoomFactor && duration == other.duration && x == other.x && y == other.y)
            {
                return true;
            }
            return false;
        }

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

        public bool SameAs(Keyframes other)
        {
            if (this.Count != other.Count)
            {
                return false;
            }
            for (int i = 0; i < this.Count; i++)
            {
                if (!this[i].SameVals(other[i]))
                {
                    return false;
                }
            }
            return true;
        }
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
