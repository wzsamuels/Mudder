using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yam
{
    public static class RichTextBoxExtensions
    {
        //Appending text with color and weight
        public static void AppendText(this RichTextBox box, string text, Brush color, string fontWeight = "normal")
        {
            TextRange tr = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd);
            tr.Text = text;
            try
            {
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, color);
                if (fontWeight == "normal")
                    tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);
                else if (fontWeight == "bold")
                    tr.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
            }
            catch (FormatException) { }
        }
    }
}
