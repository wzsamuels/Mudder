﻿/*
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
using System.Xml.Serialization;

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

        private static readonly string ConfigFile1
            = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Yam","world.cfg");
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

        //Regex

        List<Trigger> triggerList = new List<Trigger>();

        public struct Trigger
        {
            public string name;
            public Regex regex;            
        }

        //Info vars bound to status bar

        private double _numLinesText = 0;
        private string _worldURLText = "Not connected";

        // Command history
        private List<string> commandHistory = new List<string>();
        private int commandIndex = 0;

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

        //public static string ConfigFile => ConfigFile1;

       // public static string ConfigFile1 => configFile;

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
            colorsUsed.Add(Brushes.Beige, false);
            colorsUsed.Add(Brushes.Aqua, false);
            colorsUsed.Add(Brushes.Orange, false);
            colorsUsed.Add(Brushes.Yellow, false);
            colorsUsed.Add(Brushes.Tomato, false);
            colorsUsed.Add(Brushes.Olive, false);
            colorsUsed.Add(Brushes.DarkTurquoise, false);
            colorsUsed.Add(Brushes.LimeGreen, false);
            colorsUsed.Add(Brushes.DarkOliveGreen, false);
            colorsUsed.Add(Brushes.RoyalBlue, false);
            colorsUsed.Add(Brushes.Sienna, false);
            colorsUsed.Add(Brushes.Violet, false);
            
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
                        commandHistory.Add(prompt);
                        commandIndex = commandHistory.Count - 1;
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
            //Scroll up throw command history
            else if(e.Key == Key.Up)
            {

                if ((commandHistory.Count > 0) && userInputText.CaretIndex == 0)
                {
                    userInputText.Clear();
                    userInputText.Text = commandHistory.ElementAt(commandIndex);
                  //  MessageBox.Show(commandIndex.ToString());
                    if (commandIndex != 0)
                        commandIndex--;
                }                
            }
            else if (e.Key == Key.Down)
            {

                if ((commandHistory.Count > 0) && userInputText.CaretIndex == 0)
                {
                    userInputText.Clear();
                    userInputText.Text = commandHistory.ElementAt(commandIndex);
                    //  MessageBox.Show(commandIndex.ToString());
                    if (commandIndex != commandHistory.Count - 1)
                        commandIndex++;
                }
            }
            //PageUp/Down should scroll the output up and down instead of the input box
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
                //Logging
                /*
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"log.txt", true))
                {
                    file.Write(DateTime.Now.ToString());
                    file.Write(" ");
                    file.Write(mudBuffer[i].text);
                }
                */

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

            // Logging - totally broken!

            /*
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"log.txt", true))
            {
                file.Write(DateTime.Now.ToString());
                file.Write(" ");
             //   file.Write();
            }
            */

            mudOutputText.Document.Blocks.Add(newPara);
            //mudOutputText.Document.PagePadding = new Thickness(10);                         
            numLinesText = _numLinesText;
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
                        Match matchText = channelRgx.Match(Text);

                        Regex connectRgx = new Regex(connectPattern, RegexOptions.IgnoreCase);
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
                                //Flush the buffer
                                if (buffer != String.Empty)
                                {
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
                            else if(words[i] == "Zach" || words[i] == "You")
                            {
                                if (buffer != String.Empty)
                                {
                                    mudBuffer.Add(new FormattedText { text = buffer, color = defaultColor,
                                        isLink = false, weight = FontWeights.Normal });
                                }

                                mudBuffer.Add(new FormattedText { text = words[i], color = Brushes.Green,
                                    isLink = false, weight = FontWeights.Normal });

                                if (i != words.Length - 1)
                                {
                                    mudBuffer.Add(new FormattedText { text = " ", color = Brushes.Green,
                                        isLink = false, weight = FontWeights.Normal });
                                }

                                buffer = String.Empty;
                            }
                            // Everything else
                            else
                            {                                   
                                buffer += words[i];
                                if (i != words.Length - 1)
                                    buffer += " ";                               
                            }

                        }
                        
                        if (buffer != String.Empty)
                        {
                            mudBuffer.Add(new FormattedText { text = buffer, color = 
                                        defaultColor, isLink = false, weight = FontWeights.Normal});
                        }
                        
                        result = String.Empty;
                        buffer = String.Empty;
                    }
                }
                _numLinesText++;                
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

            // When new text is added, scroll down only if already
            // scrolled to the end (otherwise the user is reading scrollback)
            if ((rtb.VerticalOffset + rtb.ViewportHeight >= rtb.ExtentHeight)
                || (rtb.ExtentHeight < rtb.ViewportHeight))
            {
                rtb.ScrollToEnd();
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
                    
                    reconnectWorldMenuItem.IsEnabled = true;
                    disconnectWorldMenuItem.IsEnabled = true;
                    //Enable timer that reads from world
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
            window.newWorldSelect = true;
            Nullable<bool> result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                if (window.newWorldSelect)
                {
                    currentWorldInfo = window.WorldInfo;
                    if (window.saveLogin)
                    {
                        //WorldInfo tempWorld = new WorldInfo();
                        //tempWorld = window.WorldInfo;
                        WriteWorld(currentWorldInfo);
                    }
                }
                    //Load a saved world
                else
                {
                    
                    string path = window.worldList.SelectedValue.ToString();
                    string[] temparray = path.Split(' ');

                    //Load the selected world if it's in the save file
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
            //if (currentWorld.IsConnected)
            //{
                //Try to disconnect
            mudOutputText.AppendText("\nTrying to disconnect...");
                if (currentWorld.Disconnect())
                {
                    mudOutputText.AppendText("\nDisconnected from world", Brushes.Gold);
                    connectToWorld(currentWorldInfo);
                }
            //}
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
            //Test to see if config directory exists and create if not
            if (!Directory.Exists(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Yam")))
            {
                MessageBox.Show(string.Format("Creating dir {0}", System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Yam")));
                try
                {
                    Directory.CreateDirectory(System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Yam"));
                }
                catch
                {
                    MessageBox.Show("Could not create directory!\nNot able to save file!");               
                }
            }
            WorldCollection tempwc = new WorldCollection();
            ///If there's already saved worlds, load them
            if (File.Exists(ConfigFile1))
            {
                tempwc = ReadWorld();
            }
            
            XmlSerializer wcSerializer =
                 new XmlSerializer(typeof(WorldCollection));
            StreamWriter wcWriter = null; 
            try
            {
                wcWriter = new StreamWriter(ConfigFile1);
                tempwc.AddWorld(data); //Add world to list (not overwriting)
                wcSerializer.Serialize(wcWriter, tempwc);
            }
            catch (FileNotFoundException)
            {
                MessageBox.Show("Config file not found");
            }
            catch (IOException)
            {
                MessageBox.Show("An I/O error has occurred.");
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show("There is insufficient memory to read the file.");
            }
            finally
            {
                if (wcWriter != null) wcWriter.Dispose();
            }

            //Make sure the target directory exists, otherwise create it
            /* */


            //   file.Close();
        }

        public static WorldCollection ReadWorld()
        {
            WorldCollection data = new WorldCollection();     


            //WorldCollection data = null;
            var wcSerializer = new XmlSerializer(typeof(WorldCollection));
            StreamReader wcReader = null;
            try
            { 
                wcReader = new StreamReader(ConfigFile1);
                try
                {
                    // Deserialize the content of the file 
                    data = (WorldCollection)wcSerializer.Deserialize(wcReader);
                }
                catch (Exception)
                {
                    _ = MessageBox.Show("Error with Deserialize");
                }
            }
            catch (FileNotFoundException) {
                MessageBox.Show("Error reading file");
            }
            catch (IOException)
            {
                MessageBox.Show("An I/O error has occurred.");
            }
            catch (OutOfMemoryException)
            {
                MessageBox.Show("There is insufficient memory to read the file.");
            }
            finally
            {
                if (wcReader != null) wcReader.Dispose();
            }
            return data;
        }

        private void PrefMenuItem_Click(object sender, RoutedEventArgs e)
        {


            PreferenceBox fontChooser = new PreferenceBox();
            fontChooser.Owner = this;

            fontChooser.fontGrid.SetPropertiesFromObject(mudOutputText);

            SolidColorBrush newBrush = (SolidColorBrush)defaultColor;
            fontChooser.ColorGrid.foreColor.SelectedColor = newBrush.Color;

            newBrush = (SolidColorBrush)mudOutputText.Background;
            fontChooser.ColorGrid.backColor.SelectedColor = newBrush.Color;

            // fontChooser.PreviewSampleText = "The quick brown fox jumps over the lazy dog.";

            if (fontChooser.ShowDialog().Value)
            {
                fontChooser.fontGrid.ApplyPropertiesToObject(mudOutputText.Document);
                //         MessageBox.Show(FontSizeListItem.PixelsToPoints(fontChooser.SelectedFontSize).ToString());
                //       mudOutputText.FontSize = fontChooser.SelectedFontSize;

                defaultColor =
                    new SolidColorBrush(fontChooser.ColorGrid.foreColor.SelectedColor);

                mudOutputText.Background = 
                    new SolidColorBrush(fontChooser.ColorGrid.backColor.SelectedColor);
              
                foreach (Block block in mudOutputText.Document.Blocks)
                {
                    block.Foreground = defaultColor;
                    block.FontSize = fontChooser.fontGrid.SelectedFontSize;                    
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

