using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using System.Windows.Threading;

using System.ComponentModel;

namespace Yam
{

    public class myParagraph : Paragraph
    {
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            int inlinesCount = this.Inlines.Count;
            for (int i = 0; i < inlinesCount; i++)
            {
                Inline inline = this.Inlines.ElementAt(i);
                if (inline is Run)
                {
                    if ((inline as Run).Text == Convert.ToChar(32).ToString()) //ACSII 32 is the white space
                    {
                        (inline as Run).Text = string.Empty;
                    }
                }
            }
        }
    }   
}
