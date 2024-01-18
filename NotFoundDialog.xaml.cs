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
        public NotFoundDialog()
        {
            InitializeComponent();
        }

        private void btnDialogAbort_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void btnDialogSkip_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
