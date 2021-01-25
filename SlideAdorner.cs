using System.Windows.Media;
using System.Globalization;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Controls;

namespace KB30
{
    public class SlideAdorner : Adorner
    {
        Rect renderRect;
        public Point Center;
        public ImageSource imageSource;
        private int numDragging = 99;
        string hint = "hint hint";
        public SlideAdorner(ScrollViewer adornedElement, int _numDragging, ImageSource imgSource, Rect rRect) : base(adornedElement)
        {
            numDragging = _numDragging;
            renderRect = rRect;

            this.IsHitTestVisible = false;
            Center = new Point(renderRect.Width / 2, renderRect.Height / 2);
            imageSource = imgSource;
        }

        public void SetHint(string _hint)
        {
            hint = _hint;
            InvalidateVisual();
        }
        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawImage(imageSource, renderRect);
            if (numDragging > 1)
            {
                FormattedText countText = new FormattedText(numDragging.ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Verdana"), 24, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
                Point countLocation = new Point(Center.X - countText.Width / 2, Center.Y - countText.Height / 2);
                drawingContext.DrawRectangle(Brushes.Blue, null, new Rect(countLocation.X, countLocation.Y, countText.Width, countText.Height));
                drawingContext.DrawText(countText, countLocation);
            }

            FormattedText hintText = new FormattedText(hint, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Verdana"), 12, Brushes.Black, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            Point hintLocation = new Point(Center.X - hintText.Width / 2, renderRect.Height - hintText.Height);
            drawingContext.DrawRectangle(Brushes.White, null, new Rect(hintLocation.X, hintLocation.Y, hintText.Width, hintText.Height));
            drawingContext.DrawText(hintText, hintLocation);
        }
    }
}
