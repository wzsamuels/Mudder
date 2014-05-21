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
//using System.Drawing;
using System.Linq;
using System.IO;
using System.Net;
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
using System.Timers;
using System.Windows.Threading;

namespace Yam
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
    {
        #region Private variables
        private asyncConnect currentWorld = new asyncConnect();

        //Variables for channel coloring
        private bool first_loop = true; 
        private List<ColoredText> channelList = new List<ColoredText>();
        private Dictionary<Brush, bool> colorsUsed;
        private WorldInfo currentWorldInfo = new WorldInfo();
        private Brush defaultColor;
        private const string configFile = "world_data";
        //Timer to read from open world
        private readonly System.Timers.Timer _readTimer;
        
        #endregion        

        public MainWindow()
        {
            InitializeComponent();

            this.Title = "YAM";
            this.DataContext = this; //So variables can bind to UI

            userInputText.Focus();
            userInputText.Clear();

            mudOutputText.IsReadOnly = true;            
            mudOutputText.IsDocumentEnabled = true;

            disconnectWorldMenuItem.IsEnabled = false;
            reconnectWorldMenuItem.IsEnabled = false;

            _readTimer = new System.Timers.Timer(10); //For getting data from world
            _readTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);                     
            
            //Set up the colors used for channel name coloring

            BrushConverter bc = new BrushConverter();
            defaultColor = (Brush)bc.ConvertFromString("#BEBEBE");

            colorsUsed = new Dictionary<Brush, bool>();
            colorsUsed.Add(Brushes.Red, false);
            colorsUsed.Add(Brushes.Moccasin, false);
            colorsUsed.Add(Brushes.LightBlue, false);
            colorsUsed.Add(Brushes.Teal, false);
            colorsUsed.Add(Brushes.Orange, false);
            colorsUsed.Add(Brushes.Yellow, false);
            colorsUsed.Add(Brushes.Cyan, false);
            colorsUsed.Add(Brushes.DarkGray, false);
            colorsUsed.Add(Brushes.LimeGreen, false);
            
            numLinesText = 0;

            CommandBinding openWorldCommandBinding = new CommandBinding(
            openWorldCommand, openWorldMenuItem_Command, openWorldCanExecute);

            // attach CommandBinding to root window 
            this.CommandBindings.Add(openWorldCommandBinding);

            cutMenu.Icon = new System.Windows.Controls.Image
            {
                Source = new BitmapImage(new Uri("Resources\\cut.png", UriKind.Relative))
            };

            KeyGesture openWorldGesture = new KeyGesture(
                Key.O, ModifierKeys.Control);

            KeyBinding openWorldBinding = new KeyBinding(
                openWorldCommand, openWorldGesture);

            this.InputBindings.Add(openWorldBinding);
            openWorldMenuItem.Command = openWorldCommand;
        }
        public void Dispose()
        {
            _readTimer.Dispose();
            this.Dispose();
        }

        public static RoutedCommand openWorldCommand = new RoutedCommand();
        public int numLines = 0;
        private int _numLinesText = 0;
        private string _worldURLText = "Not connected";
        
        //Info vars bound to status bar
        public string worldURLText
        {
            get { return _worldURLText; }
            set
            {
                _worldURLText = value;
                //Notify the binding that the value has changed.
                this.OnPropertyChanged("worldURLText");
            }
        }
        public int numLinesText
        {
            get { return _numLinesText; }
            set
            {
                _numLinesText = value;
                //Notify the binding that the value has changed.
                this.OnPropertyChanged("numLinesText");
            }
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
            public FontWeight weight;
            public bool isLink;
        }
        
      #region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string strPropertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(strPropertyName));
		}

		#endregion
                
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
            //Scroll the output up and down instead of the input box
            else if (e.Key == Key.PageUp)
            {
                mudOutputText.PageUp();
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                mudOutputText.PageDown();
                e.Handled = true;
            }
        }

        private void RequestNavigateHandler(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.ToString());
        }

        private delegate void NoArgDelegate();
        private delegate void OneArgDelegate(List<FormattedText> arg);

        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            if (currentWorld.IsConnected)
            {
            //    NoArgDelegate fetcher = new NoArgDelegate(
              //      this.ReadFromWorld);

                //fetcher.BeginInvoke(null, null);
                ReadFromWorld();
            }
        }

                // Read mud output             
        private void ReadFromWorld()
        {
            string buffer = String.Empty;
            List<string> fromBuffer = new List<string>();
            if (currentWorld.IsConnected)
            {
                _readTimer.Enabled = false;
                buffer = currentWorld.Read();

                if (buffer != String.Empty)
                {

                    string[] lines = buffer.Split('\n');
                    foreach (string line in lines)
                    {
                        fromBuffer.Add(line);
                    }
                    //List<FormattedText> mudBuffer = HandleBuffering(fromBuffer);
                    
                    /*
                    App.Current.Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.SystemIdle,
                        new OneArgDelegate(DrawOutput), mudBuffer);                    
                     */
                    ScheduleDisplayUpdate(fromBuffer);
                }
                _readTimer.Enabled = true;
            }
            

        }
        List<string> mudBufferGlobal = new List<string>();

        //Because the display is updated async, make sure that it doesn't
        //get scheduled twice. Otherwise text might get processed and displayed
        //in non-chronological order. Which is cool but weird.
        private void ScheduleDisplayUpdate(List<string> fromBuffer)
        {
            //If an update isn't already happening
            if (!_displayUpdatePending)
            {
                List<FormattedText> mudBuffer = new List<FormattedText>();
                List<FormattedText> temp = new List<FormattedText>();

                if (mudBufferGlobal.Count != 0)
                {                    
                    temp = ParseBuffer(mudBufferGlobal);
                    foreach (FormattedText ft in temp)
                        mudBuffer.Add(ft);
                    mudBufferGlobal.Clear();
                }

                temp = ParseBuffer(fromBuffer);
                foreach (FormattedText ft in temp)
                    mudBuffer.Add(ft);

                Dispatcher.BeginInvoke(
                    System.Windows.Threading.DispatcherPriority.SystemIdle,
                        new OneArgDelegate(DrawOutput), mudBuffer);

                _displayUpdatePending = true;
            }
            //Otherwise save the sent text for later
            else
            {
                foreach (string line in fromBuffer)
                {
                    mudBufferGlobal.Add(line);
                }
            }
        }
        private bool _displayUpdatePending = false;

        // This updates mudOutputText
        private void DrawOutput(List<FormattedText> mudBuffer)
        {
            _displayUpdatePending = false;

            //Use newPara and newSpan for buffering
            Paragraph newPara = new Paragraph();
            Span newSpan = new Span();
       
            newPara.FontSize = mudOutputText.Document.FontSize; 
            newPara.LineHeight = mudOutputText.Document.FontSize + 4;

            char[] charsToTrim = {'\n', '\r'};
            
            for (int i = 0; i < mudBuffer.Count; i++)
            {                
                if (mudBuffer[i].isLink)
                {                    
                    string linktext = mudBuffer[i].text;
                    int index;
                    //Trim off any extra line break the world adds to the end
                    if (i == mudBuffer.Count - 1)
                    {
                        index = linktext.LastIndexOf("\n");

                        if (index != -1)
                        {
                            linktext = linktext.Remove(index, 1);
                            //linktext = linktext.Insert(index, "\n");
                        }

                        index = linktext.LastIndexOf("\r");

                        if (index != -1)
                        {
                            linktext = linktext.Remove(index, 1);
                        }
                    }

                    Hyperlink hlk = new Hyperlink(new Run(linktext));

                    try
                    {
                        //Remove any extra quote marks from around the URL
                        char[] trim = { '\"', '\n', '\r' };
                        linktext = mudBuffer[i].text.Trim(trim);
                        index = linktext.LastIndexOf("\"");
                        if (index != -1)
                        {
                            linktext = linktext.Remove(index, 1);
                        }
                        
                        
                        hlk.NavigateUri = new Uri(linktext);
                    }
                    catch(UriFormatException)
                    {
                        hlk.NavigateUri = new Uri("http://something.went.wrong");
                    }
                    hlk.RequestNavigate += new RequestNavigateEventHandler(link_RequestNavigate);
                    newPara.Inlines.Add(hlk);
                }
                else
                {
                    newSpan = new Span();
                    string temp = mudBuffer[i].text;
                    string text;
                    //Trim off any extra line break the world adds to the end
                    if (i == mudBuffer.Count - 1)
                    {
                        text = temp.TrimEnd(charsToTrim);
                    }
                    else
                    {
                        text = temp;
                    }
                    newSpan = new Span(new Run(text));
                    newSpan.Foreground = mudBuffer[i].color;
                    newSpan.FontWeight = mudBuffer[i].weight;
                    newPara.Inlines.Add(newSpan);
                }
                
            }
            mudOutputText.Document.Blocks.Add(newPara);
            mudOutputText.Document.PagePadding = new Thickness(10);
            
            numLinesText = numLines;            
        }
        private void link_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var hyperlink = (Hyperlink)sender;
            Process.Start(hyperlink.NavigateUri.ToString());
        }
        public static string ReplaceLastOccurrence(string Source, string Find, string Replace)
        {
            int Place = Source.LastIndexOf(Find);
            string result = Source.Remove(Place, Find.Length).Insert(Place, Replace);
            return result;
        }

        private List<FormattedText> ParseBuffer(List<string> fromBuffer)
        {
            List<FormattedText> mudBuffer = new List<FormattedText>();

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
                        string pattern = @"^(\[.*?\])";
                        string connectPattern = @"^(<.+>)";
                        
                        Regex channelRgx = new Regex(pattern, RegexOptions.IgnoreCase);
                        Regex connectRgx = new Regex(connectPattern, RegexOptions.IgnoreCase);

                        Match matchText = channelRgx.Match(Text);
                        Match connectMatch = connectRgx.Match(Text);
                        #region Channel name coloring
                        //Detect a channel name with 'pattern' and color
                        
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
                                colorsUsed[Brushes.Red] = true;
                                channelColor = Brushes.Red;
                                channelList.Add(new ColoredText()
                                {
                                    text = channelText,
                                    colorName = channelColor
                                });
                            }
                            //Add the colored name and then remove from Text
                            //so that Text can be further processed                        
                            mudBuffer.Add(new FormattedText 
                            { 
                                text = channelText, 
                                color = channelColor, 
                                isLink = false, 
                                weight = FontWeights.Normal
                            });
                            result = channelRgx.Replace(Text, "", 1);

                            channelText = String.Empty;
                            isMatch = false;
                        }
                        #endregion
                        //Color <connect> and </disconnect> messages purple
                        else if(connectMatch.Success)
                        {
                            string connectText = connectMatch.Groups[1].Value;
                            mudBuffer.Add(new FormattedText
                            {
                                text = connectText,
                                color = Brushes.Purple,
                                isLink = false, 
                                weight = FontWeights.Normal
                            });
                            result = connectRgx.Replace(Text, "", 1);
                        }

                        string[] words;

                        if (result != String.Empty)
                            words = result.Split(' ');
                        else
                            words = Text.Split(' ');

                        int i;
                        //foreach (string word in words)

                        for (i = 0; i < words.Length; i++)
                        {
                            if (words[i].Length > 4 && (words[i].StartsWith("http") 
                                || (words[i].StartsWith("\"http"))))
                                //(words[i].StartsWith("www")) || (words[i].StartsWith("\"www")))))
                            {
                                if (buffer != String.Empty)
                                {
                                    //mudOutputText.AppendText(buffer, defaultColor);
                                    mudBuffer.Add(new FormattedText { text = buffer, color = 
                                        defaultColor, isLink = false, weight = FontWeights.Normal});
                                }
                                /*
                                char[] charsToTrim = { '\"' };
                                string temp = words[i].TrimEnd(charsToTrim);
                                string temp2 = temp.TrimStart(charsToTrim);
                                 */
                                mudBuffer.Add(new FormattedText { text = words[i], color = 
                                    defaultColor, isLink = true, weight = FontWeights.Normal });
                                
                                //Add a space after every word unless it's the end of the line
                                if (i != words.Length - 1)
                                    mudBuffer.Add(new FormattedText { text = " ", color = 
                                        defaultColor, isLink = false, weight = FontWeights.Normal });
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
                                        defaultColor, isLink = false, weight = FontWeights.Normal});
                        }

                        result = String.Empty;
                        buffer = String.Empty;
                    }
                }
                numLines++;                
            }
            return mudBuffer;     
        }        

        /// <summary>
        /// Automatically scroll down when output from connected world is received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mudOutputText_TextChanged(object sender, EventArgs e)
        {
            mudOutputText.ScrollToEnd();
        }
       
        private void connectToWorld(WorldInfo world)
        {            
            if (!currentWorld.IsConnected)
            {
                currentWorld = new asyncConnect();
                mudOutputText.AppendText("\nConnecting...", Brushes.Gold);
                
                if (currentWorld.ConnectWorld(world.WorldURL, world.WorldPort))
                {
                    mudOutputText.AppendText("\nConnected!", Brushes.Gold);

                    this.Title = "YAM - " + world.WorldName;

                    IPHostEntry Host = Dns.GetHostEntry(world.WorldURL);
                    string ipAddress = String.Empty;
                    for (int i = 0; i < Host.AddressList.Length; i++)
                    {
                        ipAddress += Host.AddressList[i].ToString();
                        if (i != Host.AddressList.Length - 1)
                            ipAddress += ", ";
                    }

                    worldURLText = world.WorldURL + " (" + ipAddress
                        + ") at port " + world.WorldPort;

                    if (world.AutoLogin)
                    {
                        try
                        {
                            string loginString = String.Empty;

                            loginString = "connect " + world.Username + " " +
                                world.Password + "\n";

                            currentWorld.writeToWorld(loginString);

                        }
                        catch (FileNotFoundException)
                        {
                            //If no login info is saved, don't do anything    
                        }
                        catch (Exception)
                        {
                            MessageBox.Show("Loading login info error");
                        }
                    }
                    //Enable timer that reads from world
                    reconnectWorldMenuItem.IsEnabled = true;
                    disconnectWorldMenuItem.IsEnabled = true;
                    _readTimer.Enabled = true;
                }
                else
                {
                    string temp = "\nError connecting to " + world.WorldName +
                        " at " + world.WorldURL + " at port " + world.WorldPort;
                    mudOutputText.AppendText(temp, Brushes.Gold);
                }
            }
            else
                mudOutputText.AppendText("\nAlready connected to a world", Brushes.Gold);
        }
        #region Main Menu

        private void openWorldCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }
        private void openWorldMenuItem_Command(object sender, ExecutedRoutedEventArgs e)
        {
            OpenWorld();         
        }
        
        private void OpenWorld()
        {
            var window = new newWorld { Owner = this };
            Nullable<bool> result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                if (window.newWorldSelect)
                {
                    currentWorldInfo = window.WorldInfo;
                    if (window.saveLogin)
                    {
                        WorldInfo tempWorld = new WorldInfo();
                        tempWorld = window.WorldInfo;
                        try
                        {
                            WriteWorld(tempWorld);
                        }
                        catch
                        {
             //               MessageBox.Show("Saving world failed");
                        }
                    }
                }
                else
                {
                    
                    string path = window.worldList.SelectedValue.ToString();
                    string[] temparray = path.Split(' ');
                  
                    WorldInfo tempWorld = ReadWorld();                   
                    if (temparray[1] == tempWorld.WorldName)
                    {
                        currentWorldInfo = tempWorld;
                    }                   
                }                                
               
                connectToWorld(currentWorldInfo);
            }
        }
        
        private void saveWorldMenuItem_Click(object sender, EventArgs e)
        {
            WriteWorld(currentWorldInfo);
        }
        private void reconnectMenuItem_Click(object sender, EventArgs e)
        {
            if (currentWorld.IsConnected)
            {
                //Try to disconnect
                if (currentWorld.Disconnect())
                {
                    mudOutputText.AppendText("\nDisconnected from world", Brushes.Gold);
                    connectToWorld(currentWorldInfo);
                }
            }
        }

        private void findMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not implemented yet");
            FindAndReplaceManager frm;
            TextRange selectedText = new TextRange(mudOutputText.Document.ContentStart,
                mudOutputText.Document.ContentEnd);

            frm = new FindAndReplaceManager(mudOutputText.Document);
            Loaded += (s, f) =>
            {
                SearchWindow searchWindow = new SearchWindow();
                searchWindow.SearchText += (sT, fT) =>
                {
                    mudOutputText.Focus();
                    mudOutputText.SetValue(TextElement.BackgroundProperty, null);
                    if (null != selectedText) selectedText.ApplyPropertyValue(TextElement.BackgroundProperty, null);
                    var tr = frm.FindNext(fT.Text, FindOptions.None);
                    if (null != tr)
                    {
                        tr.ApplyPropertyValue(TextElement.BackgroundProperty, Brushes.Yellow);
                        selectedText = tr;
                    }
                    searchWindow.FocusFind();
                };
                searchWindow.Show();

            };

        }
        private void disconnectMenuItem_Click(object sender, EventArgs e)
        {
            //Try to disconnect
            if (currentWorld.Disconnect())
            {
                mudOutputText.AppendText("\nDisconnected from world", Brushes.Gold);
            }
            else
            {
                mudOutputText.AppendText("\nCould not disconnect. Something must have gone horribly wrong.", Brushes.Gold);
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
           // fontChooser.PreviewSampleText = "The quick brown fox jumps over the lazy dog.";

            if (fontChooser.ShowDialog().Value)
            {               
                fontChooser.ApplyPropertiesToObject(mudOutputText.Document);
       //         MessageBox.Show(FontSizeListItem.PixelsToPoints(fontChooser.SelectedFontSize).ToString());
         //       mudOutputText.FontSize = fontChooser.SelectedFontSize;
                foreach (Block block in mudOutputText.Document.Blocks)
                {
                    block.FontSize = fontChooser.SelectedFontSize;
                }                     
            }
        }
        
        private void aboutMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox box = new AboutBox();
            box.Show();
        }
        #endregion

        public void WriteWorld(WorldInfo data)
        {
            System.Xml.Serialization.XmlSerializer writer =
                 new System.Xml.Serialization.XmlSerializer(data.GetType());
            System.IO.StreamWriter file =
               new System.IO.StreamWriter(configFile);

            writer.Serialize(file, data);
            file.Close();
        }

        public WorldInfo ReadWorld()
        {
            WorldInfo data = new WorldInfo();

            System.Xml.Serialization.XmlSerializer reader = new
                System.Xml.Serialization.XmlSerializer(data.GetType());

            // Read the XML file.
            System.IO.StreamReader file =
                   new System.IO.StreamReader(Stream.Null);
            try
            {
                file = new System.IO.StreamReader(configFile);
            }
            catch (Exception)
            {
               // MessageBox.Show("Error reading world");
            }

            // Deserialize the content of the file 
            try
            {
                data = (WorldInfo)reader.Deserialize(file);
            }
            catch (Exception)
            {

            }

            file.Close();

            return data;
        }
    }
}



public static class RichTextBoxExtensions
{
    //Appending text with color and weight
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