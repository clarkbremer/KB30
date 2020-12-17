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
        public SlideAdorner(ScrollViewer adornedElement, int _numDragging, ImageSource imgSource, Rect rRect) : base(adornedElement)
        {
            numDragging = _numDragging;
            renderRect = rRect;

            this.IsHitTestVisible = false;
            Center = new Point(renderRect.Width / 2, renderRect.Height / 2);
            imageSource = imgSource;
        }
        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawImage(imageSource, renderRect);
            FormattedText formattedText = new FormattedText(numDragging.ToString(), CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, new Typeface("Verdana"), 24, Brushes.White, VisualTreeHelper.GetDpi(this).PixelsPerDip);
            Point textLocation = new Point(Center.X - formattedText.Width / 2, Center.Y - formattedText.Height / 2);
            drawingContext.DrawRectangle(Brushes.Blue, null, new Rect(textLocation.X, textLocation.Y, formattedText.Width, formattedText.Height));
            drawingContext.DrawText(formattedText, textLocation);
        }
    }
}
