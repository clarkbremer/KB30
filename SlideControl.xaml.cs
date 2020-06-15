using System;
using System.Windows.Controls;
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
        }
        public void DeSelect()
        {
            SelectBorder.BorderBrush = Brushes.LightBlue;
        }

        public Boolean IsChecked(){
            return checkbox.IsChecked == true;
        }

        public void UnCheck()
        {
            checkbox.IsChecked = false;
        }
    }
}
