using System;
using System.Collections;
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
using System.Collections.ObjectModel;
using System.Collections.Generic;

using System.Globalization;
using System.Windows.Threading;


namespace Yam
{
    /// <summary>
    /// Interaction logic for PreferenceBox.xaml
    /// </summary>
    public partial class PreferenceBox : Window
    {
        public PreferenceBox()
        {
            InitializeComponent();
            CollapseBox();
            fontGrid.Visibility = Visibility.Visible;          
         }

        private void FontItem_Selected(object sender, RoutedEventArgs e)
        {
            CollapseBox();
            fontGrid.Visibility = Visibility.Visible;
        }

        private void ColorItem_Selected(object sender, RoutedEventArgs e)
        {
            CollapseBox();
            ColorGrid.Visibility = Visibility.Visible;
        }
        private void CollapseBox()
        {
            fontGrid.Visibility = Visibility.Collapsed;
            ColorGrid.Visibility = Visibility.Collapsed;
        }
        private void OnOKButtonClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void OnCancelButtonClicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


    }
}