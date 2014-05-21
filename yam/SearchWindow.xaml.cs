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

namespace Yam
{
    /// <summary>
    /// Interaction logic for SearchWindow.xaml
    /// </summary>
    public partial class SearchWindow : Window
    {
         public SearchWindow()
        {
            InitializeComponent();
            button.Click += (s, e) =>
                {
                    if (null != SearchText) SearchText(this, new SearchTextEventArgs { Text = find.Text });
                };
        }

        public void FocusFind()
        { find.Focus(); }

        public event EventHandler<SearchTextEventArgs> SearchText;

        public class SearchTextEventArgs : EventArgs
        {
            public string Text { get; set; }
        }
    }
}
