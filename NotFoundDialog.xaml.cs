using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KB30
{
    /// <summary>
    /// Interaction logic for NotFoundDialog.xaml
    /// </summary>
    public partial class NotFoundDialog : Window
    {
        public const int ACTION_SKIP = 1;
        public const int ACTION_BLACK = 2;
        public const int ACTION_WHITE = 3;
        public const int ACTION_FIND = 4;

        public int result = 0;
        public NotFoundDialog()
        {
            InitializeComponent();
        }

        private void btnDialogAbort_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void btnDialogBlack_Click(object sender, RoutedEventArgs e)
        {
            result = ACTION_BLACK;
            DialogResult = true;
        }

        private void btnDialogWhite_Click(object sender, RoutedEventArgs e)
        {
            result = ACTION_WHITE;
            DialogResult = true;
        }

        private void btnDialogSkip_Click(object sender, RoutedEventArgs e)
        {
            result = ACTION_SKIP;
            DialogResult = true;
        }

        private void btnDialogFind_Click(object sender, RoutedEventArgs e)
        {
            result = ACTION_FIND;
            DialogResult = true;
        }
    }
}
