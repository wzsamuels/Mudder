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
//using Xceed.Wpf.Toolkit;
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
        private WorldInfo currentWorldInfo = new WorldInfo();
        public static RoutedCommand openWorldCommand = new RoutedCommand();
        
        private const string configFile = "world_data";
        //Timer to read from open world
        private readonly System.Timers.Timer _readTimer;


        //Drawing the output RTB
        List<string> mudBufferGlobal = new List<string>();
        private bool _displayUpdatePending = false;

        //Variables for channel coloring
        private List<ColoredText> channelList = new List<ColoredText>();
        private Dictionary<Brush, bool> colorsUsed;
        private Brush defaultColor;
        private bool first_loop = true;

        //Info vars bound to status bar

        public double numLines = 0;  //TODO: Remove this
        private double _numLinesText = 0;
        private string _worldURLText = "Not connected";

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
        public double numLinesText
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
        
        #endregion        

        public MainWindow()
        {
            InitializeComponent();

            this.Title = "YAM";
            this.DataContext = this; //So variables can bind to UI

            userInputText.Focus();
            userInputText.Clear();

//            mudOutputText.IsReadOnly = true;            
  //          mudOutputText.IsDocumentEnabled = true;

            disconnectWorldMenuItem.IsEnabled = false;
            reconnectWorldMenuItem.IsEnabled = false;

            //For getting data from world
            _readTimer = new System.Timers.Timer(10); 
            _readTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);                     
            
            //Set up the colors used for channel name coloring

            BrushConverter bc = new BrushConverter();
            defaultColor = (Brush)bc.ConvertFromString("#BEBEBE");

            colorsUsed = new Dictionary<Brush, bool>();
            colorsUsed.Add(Brushes.Maroon, false);
            colorsUsed.Add(Brushes.Ivory, false);
            //colorsUsed.Add(Brushes.Aquamarine, false);
            //colorsUsed.Add(Brushes.Aqua, false);
            colorsUsed.Add(Brushes.Aqua, false);
            colorsUsed.Add(Brushes.Orange, false);
            colorsUsed.Add(Brushes.Yellow, false);
            colorsUsed.Add(Brushes.Olive, false);
            colorsUsed.Add(Brushes.DarkTurquoise, false);
            colorsUsed.Add(Brushes.LimeGreen, false);
            colorsUsed.Add(Brushes.DarkOliveGreen, false);
            colorsUsed.Add(Brushes.RoyalBlue, false);
            colorsUsed.Add(Brushes.Sienna, false);
            colorsUsed.Add(Brushes.Violet, false);
            colorsUsed.Add(Brushes.Tomato, false);
            
            numLinesText = 0;

      //      mudOutputText.AddHandler(Hyperlink.RequestNavigateEvent,
          //      new RoutedEventHandler(this.link_RequestNavigate));

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
                        //mudOutputText.ScrollToEnd();
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
                numLinesText = 0;
                mudOutputText.PageUp();
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                numLinesText = 0;
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
            numLinesText = mudOutputText.VerticalOffset;
            if (currentWorld.IsConnected)
            {            
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
                    ScheduleDisplayUpdate(fromBuffer);
                }
                _readTimer.Enabled = true;
            }
            

        }

        //Because the display is updated async, make sure that it doesn't
        //get scheduled twice. Otherwise text might get processed and displayed
        //in non-chronological order. Which is cool but weird.
        private void ScheduleDisplayUpdate(List<string> fromBuffer)
        {
            //If an update isn't already happening
        //    if (!_displayUpdatePending)
      //      {
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

             //   _displayUpdatePending = true;
       //     }
            //Otherwise save the sent text for later
        //    else
       //     {
          //      foreach (string line in fromBuffer)
          //      {
               //     mudBufferGlobal.Add(line);
           //     }
      //      }
        }

        // This updates mudOutputText
        private void DrawOutput(List<FormattedText> mudBuffer)
        {
            _displayUpdatePending = false;

           
            //Use newPara and newSpan for buffering
            Paragraph newPara = new Paragraph();
            Span newSpan = new Span();
       
            newPara.FontSize = mudOutputText.Document.FontSize; 
            //Add a little bit of padding around the text lines
            //dependent on the font size
            newPara.LineHeight = mudOutputText.Document.FontSize + 
                (mudOutputText.Document.FontSize / 4);

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
                                colorsUsed[Brushes.Maroon] = true;
                                channelColor = Brushes.Maroon;
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
                                color = Brushes.DarkMagenta,
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
            RichTextBox rtb = sender as RichTextBox;
            //rtb.ScrollToEnd();
            double dVer = rtb.VerticalOffset;



            //get the vertical size of the scrollable content area

            double dViewport = rtb.ViewportHeight;



            //get the vertical size of the visible content area

            double dExtent = rtb.ExtentHeight;

            if (dVer != 0)
            {

                if (dVer + dViewport >= dExtent)
                   // || rtb.ExtentHeight < rtb.ViewportHeight)
                    rtb.ScrollToEnd();

                //else MessageBox.Show("It is not at the bottom now");

            }

            else
            {

                //MessageBox.Show("ActualyIt is at the top now!");

            }
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

                    //Display the world URL and IP in the status bar
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
                        string loginString = String.Empty;

                        loginString = "connect " + world.Username + " " +
                            world.Password + "\n";

                        currentWorld.writeToWorld(loginString);                        
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

                    List<WorldInfo> loadedWorlds = new List<WorldInfo>();
                    WorldCollection wc = MainWindow.ReadWorld();
                    loadedWorlds = wc.Worlds;

                    foreach (WorldInfo world in loadedWorlds)
                    {
                        if (temparray[1] == world.WorldName)
                        {
                            currentWorldInfo = world;
                        }
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

                disconnectWorldMenuItem.IsEnabled = false;
                reconnectWorldMenuItem.IsEnabled = false;
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
           
        }
        
        private void aboutMenuItem_Click(object sender, EventArgs e)
        {
            About box = new About();
            box.Show();
        }
        #endregion

        public static void WriteWorld(WorldInfo data)
        {
            WorldCollection tempwc = ReadWorld();

            System.Xml.Serialization.XmlSerializer writer =
                 new System.Xml.Serialization.XmlSerializer(tempwc.GetType());
            System.IO.StreamWriter file =
               new System.IO.StreamWriter(configFile);

            tempwc.AddWorld(data);
            writer.Serialize(file, tempwc);

            file.Close();
        }

        public static WorldCollection ReadWorld()
        {
            WorldCollection data = new WorldCollection();

            System.Xml.Serialization.XmlSerializer reader = new
                System.Xml.Serialization.XmlSerializer(data.GetType());

            // Read the XML file.
            System.IO.StreamReader file =
                   new System.IO.StreamReader(Stream.Null);

            bool FileExists = File.Exists(configFile);
            if (FileExists)
            {
                try
                {
                    file = new System.IO.StreamReader(configFile);
                }
                catch (Exception)
                {
                    MessageBox.Show("Error reading file");
                }

                // Deserialize the content of the file 
                try
                {
                    data = (WorldCollection)reader.Deserialize(file);
                }
                catch (Exception)
                {
                    MessageBox.Show("Error with Deserialize");
                }
            }
            file.Close();

            return data;
        }

        private void PrefMenuItem_Click(object sender, RoutedEventArgs e)
        {


            PreferenceBox fontChooser = new PreferenceBox();
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

       
    }
}

public static class RichTextBoxExtensions
{
    //Appending text with color and weight
    public static void AppendText(this RichTextBox box, string text, Brush color, string fontWeight = "normal")
    {
        
        BrushConverter bc = new BrushConverter();
/*
        TextPointer moveTo = box.CaretPosition.GetNextInsertionPosition(LogicalDirection.Forward);

        if (moveTo != null)
        {

            box.CaretPosition = moveTo;

        }*/
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

    public static bool ScrolledUp(this RichTextBox box)
    {

        // get the vertical scroll position
        double dVer = box.VerticalOffset;

        //get the vertical size of the scrollable content area
        double dViewport = box.ViewportHeight;

        //get the vertical size of the visible content area
        double dExtent = box.ExtentHeight;
        //Scrolled up
        if (box.VerticalOffset == box.ViewportHeight)
            return true;
                //Not scrolled up
        else
            return false;                                               
    }
    public class RichTextBoxThing : DependencyObject
    {
        public static bool GetIsAutoScroll(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsAutoScrollProperty);
        }

        public static void SetIsAutoScroll(DependencyObject obj, bool value)
        {
            obj.SetValue(IsAutoScrollProperty, value);
        }

        public static readonly DependencyProperty IsAutoScrollProperty =
            DependencyProperty.RegisterAttached("IsAutoScroll", typeof(bool), typeof(RichTextBoxThing), new PropertyMetadata(false, new PropertyChangedCallback((s, e) =>
            {
                RichTextBox richTextBox = s as RichTextBox;
                if (richTextBox != null)
                {
                    if ((bool)e.NewValue)
                        richTextBox.TextChanged += richTextBox_TextChanged;
                    else if ((bool)e.OldValue)
                        richTextBox.TextChanged -= richTextBox_TextChanged;

                }
            })));

        static void richTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RichTextBox richTextBox = sender as RichTextBox;
            if ((richTextBox.VerticalOffset + richTextBox.ViewportHeight) == richTextBox.ExtentHeight || richTextBox.ExtentHeight < richTextBox.ViewportHeight)
                richTextBox.ScrollToEnd();
        }
    }

}

