﻿using System.Windows.Controls;
using System.Windows.Media;

namespace KB30
{
    /// <summary>
    /// Interaction logic for KeyframeControl.xaml
    /// </summary>
    public partial class KeyframeControl : UserControl
    {
        public KeyframeControl()
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
    }
}
