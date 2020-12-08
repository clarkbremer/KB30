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
        private void mouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Package the data.
                DataObject data = new DataObject();
                data.SetData(DataFormats.StringFormat, "I am a slide");
                data.SetData("Double", slideNumber);
                data.SetData("Object", this);

                // Inititate the drag-and-drop operation.
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
            }
        }

         private void giveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            // These Effects values are set in the drop target's
            // DragOver event handler.
          
            if (e.Effects.HasFlag(DragDropEffects.Move))
            {
                Mouse.SetCursor(Cursors.Pen);
            }
            else
            {
                Mouse.SetCursor(Cursors.No);
            }
            e.Handled = true;
        }

        public void Select()
        {
            SelectBorder.BorderBrush = Brushes.Blue;
            Check();
        }
        public void DeSelect()
        {
            SelectBorder.BorderBrush = Brushes.LightBlue;
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl)){
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
