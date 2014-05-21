/*
   Copyright 2014 W. Z. Samuels

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

 */
 
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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

using System.Windows.Threading;

namespace Yam
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Private variables
        private asyncConnect currentWorld = new asyncConnect();
        private bool first_loop;
        private List<ColoredText> channelList = new List<ColoredText>();
        private Dictionary<Brush, bool> colorsUsed;
        private string outputBuffer;
        private int numLines = 0;
        private Brush defaultColor;
        private WorldInfo currentWorldInfo = new WorldInfo();
        private readonly BackgroundWorker worker = new BackgroundWorker();
        //private FlowDocument flowBuffer = new FlowDocument();
        
        #endregion
        
        

        public MainWindow()
        {
            InitializeComponent();
            this.Title = "Yam";
            userInputText.Focus();
            first_loop = true;
            outputBuffer = String.Empty;

            //Handle clicking on links
            mudOutputText.AddHandler(Hyperlink.RequestNavigateEvent,
                new RequestNavigateEventHandler(RequestNavigateHandler));
            //Remove padding around links
            mudOutputText.IsDocumentEnabled = true;
            


            worker.DoWork +=
               new DoWorkEventHandler(ReadFromWorld);
            worker.RunWorkerCompleted +=
                new RunWorkerCompletedEventHandler(
             worker_RunWorkerCompleted);
            worker.ProgressChanged +=
                new ProgressChangedEventHandler(DrawOutput);

            worker.WorkerReportsProgress = true;
            //Set up the colors used for channel name coloring

            BrushConverter bc = new BrushConverter();
            defaultColor = (Brush)bc.ConvertFromString("#BEBEBE");

            colorsUsed = new Dictionary<Brush, bool>();
            colorsUsed.Add(Brushes.Red, false);
            colorsUsed.Add(Brushes.Purple, false);
            colorsUsed.Add(Brushes.Pink, false);
            colorsUsed.Add(Brushes.SkyBlue, false);
            colorsUsed.Add(Brushes.Yellow, false);
            colorsUsed.Add(Brushes.Cyan, false);
            colorsUsed.Add(Brushes.Gray, false);
            colorsUsed.Add(Brushes.LimeGreen, false);

            mudOutputText.IsReadOnly = true;
            userInputText.Clear();
            userInputText.Text = "connect borowski Z*ATIVwY";
        }
        
        // Text and its color
        public struct ColoredText
        {
            public string text;
            public Brush colorName;
        
        }
        struct FormattedText
        {
            public string text;
            public Brush color;
            public string weight;
            public bool isLink;
        }
        // Handle the KeyDown event to determine the type of character entered into the control. 
        private void userInputText_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            // Send userInputText to connected world on Return
            if (e.Key == Key.Return)
            {

                string prompt = userInputText.Text;
                if (currentWorld != null)
                {
                    if (currentWorld.IsConnected)
                    {
                        currentWorld.writeToWorld(prompt);
                        userInputText.Clear();
                        mudOutputText.ScrollToEnd();
                    }
                    else
                    {
                        MessageBox.Show("Not connected to any world", "mudpants");
                    }
                }
                else
                {
                    MessageBox.Show("Not connected to any world", "mudpants");                        
                }
                
                e.Handled = true;
            }
        }


        private void RequestNavigateHandler(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }
     //   public delegate void nextDrawDelegate(List<string> text);

        // Receive output from the connected mud as long as the connection is open

        private void ReadFromWorld(object sender, DoWorkEventArgs e)
        {

            string buffer = String.Empty;
            BackgroundWorker worker = sender as BackgroundWorker;

            while (currentWorld.IsConnected)
            {

                
                // Read mud output             

                List<string> fromBuffer = new List<string>();

                buffer = currentWorld.Read();

                if (buffer != String.Empty)                 
                {

                    string[] lines = buffer.Split('\n');
                    foreach (string line in lines)
                    {
                        fromBuffer.Add(line);
                    }
                    List<FormattedText> mudBuffer = bufferOutput(fromBuffer);
                    worker.ReportProgress(1, mudBuffer);          
                }

            }
            e.Result = true;

        }
        
        private bool flag = false;
        private void worker_RunWorkerCompleted(object sender,
                                       RunWorkerCompletedEventArgs e)
        {
            //update ui once worker complete his work
        }
        private List<FormattedText> bufferOutput(List<string> fromBuffer)
        {
            List<string> newbuffer = fromBuffer;


            //List<FormattedText> mudBuffer = await Task.Factory.StartNew<List<FormattedText>>HandleBuffering(newbuffer
              //          null, TaskCreationOptions.LongRunning);
            List<FormattedText> mudBuffer = HandleBuffering(newbuffer);
            flag = true;
            return mudBuffer;
        }
        
        // This event handler updates the progress. 
        private void DrawOutput(object sender, ProgressChangedEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            mudOutputText.Focus();
            //MessageBox.Show("BEFORE");
            List<FormattedText> mudBuffer = (List<FormattedText>)e.UserState;
                    foreach (FormattedText ft in mudBuffer)
                    {
                        mudOutputText.AppendText(ft.text, ft.color, ft.weight);
                    }
                    userInputText.Focus();
                    
        }
        private List<FormattedText> HandleBuffering(List<string> outBuffer)
        {
            List<string> fromBuffer = new List<string>();
           // List<FormattedText> mudBuffer = new List<FormattedText>();
            List<FormattedText> mudBuffer = new List<FormattedText>();

            fromBuffer = outBuffer;
            foreach (string Text in fromBuffer)
            {
                if (fromBuffer.Count != 0)
                {
                    if (fromBuffer[0] != "")
                    {
                        


                        string buffer = String.Empty;
                        string result = String.Empty;
                        string channelText = String.Empty;  //Text to color
                        Brush channelColor = Brushes.AliceBlue; //Text color
                        bool isMatch = false;
                        Match matchText;
                        string pattern = @"^(\[.*?\])";
                        
                        Regex channelRgx = new Regex(pattern, RegexOptions.IgnoreCase);


                        //Text = fromBuffer[0];
                        #region Channel name coloring
                        //Detect a channel name with 'pattern' and color
                        matchText = channelRgx.Match(Text);
                        if (matchText.Success)
                        {
                            channelText = matchText.Groups[1].Value;
                            if (first_loop == true)
                            {
                                foreach (ColoredText channel in channelList)
                                {
                                    // check to see if the channel already has a color
                                    if (channel.text == channelText)
                                    {
                                        channelColor = channel.colorName;
                                        isMatch = true;
                                        break;
                                    }
                                }
                                // If it doesn't give it one
                                if (!isMatch)
                                {
                                    //If all the colors are in use, recycle the oldest
                                    if (channelList.Count == colorsUsed.Count)
                                    {
                                        colorsUsed[channelList[0].colorName] = false;
                                        channelList.RemoveAt(0);
                                    }
                                    //Loop through the colors and find one not in use
                                    foreach (KeyValuePair<Brush, bool> kvp in colorsUsed)
                                    {
                                        if (kvp.Value == false)
                                        {
                                            colorsUsed[kvp.Key] = true;
                                            channelColor = kvp.Key;
                                            channelList.Add(new ColoredText() { text = channelText, colorName = channelColor });
                                            break;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                //If this is the first channel since the program started, make it tomato, why not
                                first_loop = false;
                                colorsUsed[Brushes.Tomato] = true;
                                channelColor = Brushes.Tomato;
                                channelList.Add(new ColoredText()
                                {
                                    text = channelText,
                                    colorName = channelColor
                                });
                            }
                            //Print the colored name and then remove from Text
                            //so that Text can be further processed

                            //mudOutputText.AppendText(channelText, channelColor, "bold");
                            mudBuffer.Add(new FormattedText { text = channelText, color = 
                                channelColor, isLink = false, weight = "bold" });
                            result = channelRgx.Replace(Text, "", 1);

                            channelText = String.Empty;
                            isMatch = false;
                        }
                        #endregion

                        string[] words;

                        if (result != String.Empty)
                            words = result.Split(' ');
                        else
                            words = Text.Split(' ');

                        int i;
                        //foreach (string word in words)

                        for (i = 0; i < words.Length; i++)
                        {
                            if (words[i].Length > 4 && words[i].StartsWith("http"))
                            {
                                if (buffer != String.Empty)
                                {
                                    //mudOutputText.AppendText(buffer, defaultColor);
                                    mudBuffer.Add(new FormattedText { text = buffer, color = 
                                        defaultColor, isLink = false, weight = "normal" });
                                }
                                Run r = new Run(words[i]);

                                // Get the current caret position.
                                //TextPointer caretPos = mudOutputText.CaretPosition;
                                // Set the TextPointer to the end of the current document.
                                //caretPos = caretPos.DocumentEnd;
                                // Specify the new caret position at the end of the current document.
                                //mudOutputText.CaretPosition = caretPos;

                                //TextPointer tp = mudOutputText.CaretPosition.GetInsertionPosition(LogicalDirection.Forward);
                                //Hyperlink hlk = new Hyperlink(r, mudOutputText.CaretPosition);

                                try
                                {
                                    //hlk.NavigateUri = new Uri(words[i]);
                                    mudBuffer.Add(new FormattedText { text = words[i], color = 
                                        defaultColor, isLink = false, weight = "normal" });
                                }
                                catch(UriFormatException)
                                {
                                    //hlk.NavigateUri = new Uri("");
                                }

                                if (i != words.Length - 1)
                                    mudBuffer.Add(new FormattedText { text = " ", color = 
                                        defaultColor, isLink = false, weight = "normal" });
                                    //mudOutputText.AppendText(" ", defaultColor);

                                buffer = String.Empty;
                            }
                            else
                            {
                                buffer += words[i];
                                if (i != words.Length - 1)
                                    buffer += " ";
                            }

                        }
                        if (buffer != String.Empty)
                        {
                         //   mudOutputText.AppendText(buffer, defaultColor);
                            mudBuffer.Add(new FormattedText { text = buffer, color = 
                                        defaultColor, isLink = false, weight = "normal" });
                        }

                        

                        result = String.Empty;
                        buffer = String.Empty;

                        numLines++;

                        

                    }
                //fromBuffer.RemoveAt(0);
                //worker.ReportProgress(1, fromBuffer);
                }

            
                //numLineLabel.Text = "finished " + numLines.ToString();

            }
            return mudBuffer;
       //     MessageBox.Show("BEFORE DRAW");
            
          //  MessageBox.Show("AFTER DRAW");
        }

        // Receive output from the connected mud as long as the connection is open

        /// <summary>
        /// Automatically scroll down when output from connected world is received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mudOutputText_TextChanged(object sender, EventArgs e)
        {
            mudOutputText.ScrollToEnd();
        }
        /*
        private void contextMenuCut(object sender, System.EventArgs e)
        {
            // Takes the selected text from a text box and puts it on the clipboard. 
            if (userInputText.SelectedText != "")
            {
                Clipboard.SetDataObject(userInputText.SelectedText);
                userInputText.SelectedText = "";
            }
            else
            {

            }
        }

        private void contextMenuCopy(object sender, System.EventArgs e)
        {
            // Takes the selected text from a text box and puts it on the clipboard. 
            if (userInputText.SelectedText != "")
                Clipboard.SetDataObject(userInputText.SelectedText);
            else
            {

            }
        }

        private void contextMenuPaste(object sender, System.EventArgs e)
        {
            // Declares an IDataObject to hold the data returned from the clipboard. 
            // Retrieves the data from the clipboard.
            IDataObject iData = Clipboard.GetDataObject();

            // Determines whether the data is in a format you can use. 
            if (iData.GetDataPresent(DataFormats.Text))
            {
                // Yes it is, so display it in a text box.
                userInputText.Text = (String)iData.GetData(DataFormats.Text);
            }
            else
            {
                // No it is not.
            }
        }

        */
        // Main Menu Strip
                /// <summary>
        /// New World Menu Item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private async void newWorldMenuItem_Click(object sender, EventArgs e)
        {
            var window = new newWorld { Owner = this };
            bool? result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                currentWorldInfo.WorldName = window.WorldInfo.WorldName;
                currentWorldInfo.WorldURL = window.WorldInfo.WorldURL;
                currentWorldInfo.WorldPort = window.WorldInfo.WorldPort;

                await connectToWorld(currentWorldInfo);
            }                    
            
        }
        

        private async void openDefaultWorldMenuItem_Click(object sender, EventArgs e)
        {
            //The default mud is ifMUD for testing purposes             

            currentWorldInfo.WorldName = "ifMUD";
            currentWorldInfo.WorldURL = "ifmud.port4000.com";
            currentWorldInfo.WorldPort = 4000;

            await connectToWorld(currentWorldInfo);
        }

        private async Task connectToWorld(WorldInfo world)
        {
            //this.Title = "mudpants - " + worldName;

            currentWorld = new asyncConnect();
            await currentWorld.ConnectWorld(world.WorldURL, world.WorldPort);
           
            if(currentWorld.IsConnected)
            {
                if(worker.IsBusy != true)
                    worker.RunWorkerAsync();
            }
            
        }

        /// <summary>
        /// Quit Menu Item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void reconnectMenuItem_Click(object sender, EventArgs e)
        {
            if (currentWorld.IsConnected)
            {
                //Try to disconnect
                if (currentWorld.Disconnect())
                {
                    mudOutputText.AppendText("\nDisconnected from world\n", Brushes.Gold);
                    await connectToWorld(currentWorldInfo);
                }
            }
        }
        private void disconnectMenuItem_Click(object sender, EventArgs e)
        {
            //Try to disconnect
            if (currentWorld.Disconnect())
            {
                //inputThread.CancelAsync();
                mudOutputText.AppendText("\nDisconnected from world\n", Brushes.Gold);
                //currentWorld = new asyncConnect();
            }
        }
        private void quitMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to quit?", "Exit",
                MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                this.Close();
            }
        
        }

        private void fontMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ShowFontDialog();
        }

        private void ShowFontDialog()
        {
            NewFontPicker fontChooser = new NewFontPicker();
            fontChooser.Owner = this;

            fontChooser.SetPropertiesFromObject(mudOutputText);
            fontChooser.PreviewSampleText = "The quick brown fox jumps over the lazy dog.";

            if (fontChooser.ShowDialog().Value)
            {
                fontChooser.ApplyPropertiesToObject(mudOutputText);
            }
        }
        /*
        private void UpdateStatus(ToolStripItem item)
        {
            if (item != null)
            {
                string msg = String.Format("{0} selected", item.Text);
                this.statusStrip1.Items[0].Text = msg;
            }
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(mudOutputText.SelectedText);
        }

        /*

        */

        private void AddHyperlinkText(string linkURL, string linkName,
                  string TextBeforeLink, string TextAfterLink, Brush color)
        {
            Paragraph para = new Paragraph();
            para.Margin = new Thickness(0); // remove indent between paragraphs

            Hyperlink link = new Hyperlink();
            link.IsEnabled = true;
            link.Inlines.Add(linkName);
            link.NavigateUri = new Uri(linkURL);
            link.RequestNavigate += (sender, args) => Process.Start(args.Uri.ToString());

            para.Inlines.Add(TextBeforeLink);
            para.Inlines.Add(link);
            para.Inlines.Add(new Run(TextAfterLink));
            mudOutputText.Document.Blocks.Add(para);
        }
    }
}



public static class RichTextBoxExtensions
{

    public static void AppendText(this RichTextBox box, string text, Brush color, string fontWeight = "normal")
    {
        
        BrushConverter bc = new BrushConverter();

        TextPointer moveTo = box.CaretPosition.GetNextInsertionPosition(LogicalDirection.Forward);

        if (moveTo != null)
        {

            box.CaretPosition = moveTo;

        }
        TextRange tr = new TextRange(box.Document.ContentEnd, box.Document.ContentEnd);
        tr.Text = text;
        //box.AppendText(text);
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













