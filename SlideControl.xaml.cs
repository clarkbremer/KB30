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
 
 
        public void Select()
        {
            SelectBorder.BorderBrush = Brushes.Blue;
            Check();
        }
        public void DeSelect(Boolean unCheck = true)
        {
            SelectBorder.BorderBrush = Brushes.LightBlue;
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl) && unCheck)
            {
                UnCheck();
            }
        }

        public void highlightAbove()
        {
            SelectBorder.BorderThickness = new Thickness(5, 25, 5, 5);
        }
        public void highlightBelow()
        {
            SelectBorder.BorderThickness = new Thickness(5, 5, 5, 25);
        }
        public void highlightClear()
        {
            SelectBorder.BorderThickness = new Thickness(5, 5, 5, 5);
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
