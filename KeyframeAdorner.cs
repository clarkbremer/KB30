using System.Windows.Media;
using System.Globalization;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Controls;

namespace KB30
{
    public class KeyframeAdorner : Adorner
    {
        Rect renderRect;
        public Point Center;
        public KeyframeAdorner(ScrollViewer adornedElement, Rect rRect) : base(adornedElement)
        {
            renderRect = rRect;

            this.IsHitTestVisible = false;
            Center = new Point(renderRect.Width / 2, renderRect.Height / 2);
        }
        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawRectangle(Brushes.Blue, null, renderRect);
        }
    }
}
