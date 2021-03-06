﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KB30
{
    /// <summary>
    /// Interaction logic for KeyframeControl.xaml
    /// </summary>
    public partial class KeyframeControl : UserControl
    {

        const int HIGHLIGHT_NONE = 0;
        const int HIGHLIGHT_LEFT = 1;
        const int HIGHLIGHT_RIGHT = 2;

        private int hightlightState = HIGHLIGHT_NONE;
        public bool selected = false;

        public KeyframeControl()
        {
            InitializeComponent();
        }

        public void Select()
        {
            SelectBorder.BorderBrush = Brushes.Blue;
            selected = true;
        }
        public void DeSelect()
        {
            SelectBorder.BorderBrush = Brushes.LightBlue;
            selected = false;
        }

        public void highlightLeft()
        {
            if (hightlightState == HIGHLIGHT_LEFT) { return; }
            LeftBorder.BorderThickness = new Thickness(15);
            RightBorder.BorderThickness = new Thickness(0);
            hightlightState = HIGHLIGHT_LEFT;
        }
        public void highlightRight()
        {
            if (hightlightState == HIGHLIGHT_RIGHT) { return; }
            LeftBorder.BorderThickness = new Thickness(0);
            RightBorder.BorderThickness = new Thickness(15);
            hightlightState = HIGHLIGHT_RIGHT;
        }
        public void highlightClear()
        {
            if (hightlightState == HIGHLIGHT_NONE) { return; }
            LeftBorder.BorderThickness = new Thickness(0);
            RightBorder.BorderThickness = new Thickness(0);
            hightlightState = HIGHLIGHT_NONE;
        }

        private void KFControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            this.durTb.Focus();
            this.durTb.SelectAll();
        }
    }
}
