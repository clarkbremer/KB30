using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace KB30
{
    /// <summary>
    /// Interaction logic for SlideControl.xaml
    /// </summary>
    public partial class SlideControl : UserControl
    {
        public SlideControl()
        {
            InitializeComponent();
        }

        const int HIGHLIGHT_NONE = 0;
        const int HIGHLIGHT_ABOVE = 1;
        const int HIGHLIGHT_BELOW = 2;

        private int hightlightState = HIGHLIGHT_NONE;
 
        public void Select()
        {
            SelectBorder.BorderBrush = Brushes.Blue;
        }
        public void DeSelect()
        {
            SelectBorder.BorderBrush = Brushes.LightBlue;
        }

        public void highlightAbove()
        {
            if (hightlightState == HIGHLIGHT_ABOVE) { return; }
            UpperBorder.BorderThickness = new Thickness(10);
            LowerBorder.BorderThickness = new Thickness(0);
            hightlightState = HIGHLIGHT_ABOVE;
        }
        public void highlightBelow()
        {
            if (hightlightState == HIGHLIGHT_BELOW) { return; }
            UpperBorder.BorderThickness = new Thickness(0);
            LowerBorder.BorderThickness = new Thickness(10);
            hightlightState = HIGHLIGHT_BELOW;
        }
        public void highlightClear()
        {
            if (hightlightState == HIGHLIGHT_NONE) { return; }
            UpperBorder.BorderThickness = new Thickness(0);
            LowerBorder.BorderThickness = new Thickness(0);
            hightlightState = HIGHLIGHT_NONE;
        }
        public Boolean IsChecked(){
            return checkbox.IsChecked == true;
        }

        public void UnCheck()
        {
            checkbox.IsChecked = false;
        }
        public void Check()
        {
            checkbox.IsChecked = true;
        }

        public void ToggleCheck()
        {
            if (checkbox.IsChecked == true)
            {
                checkbox.IsChecked = false;
            }
            else
            {
                checkbox.IsChecked = true;
            }
        }

        public int slideNumber
        {
            get { return Convert.ToInt32(slideText.Text); }
            set { slideText.Text = value.ToString(); }
        }
    }
}
