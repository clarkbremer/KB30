using System.Text.RegularExpressions;
using System.Windows;
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
        private ImageExplorerWindow finderWindow = null;

        public KeyframeControl(ImageExplorerWindow finderWnd)
        {
            InitializeComponent();
            finderWindow = finderWnd;
        }

        public void Select()
        {
            SelectBorder.BorderBrush = Brushes.Blue;
            selected = true;
            if (finderWindow == null || !finderWindow.IsActive)
            {
                this.durTb.Focus();
            }
            this.durTb.SelectAll();
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
            if (finderWindow == null || !finderWindow.IsActive)
            {
                if (selected)
                {
                    this.durTb.Focus();
                }
            }
            this.durTb.SelectAll();
        }
        

        private void durTb_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Regex _regex = new Regex("[^0-9.-]+"); 
            e.Handled = _regex.IsMatch(e.Text);
        }
    }
}
