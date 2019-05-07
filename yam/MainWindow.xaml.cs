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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Timers;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace Yam
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
    {
        #region Private variables
        private AsyncConnect currentWorld = new AsyncConnect();
        private WorldInfo currentWorldInfo = new WorldInfo();
        private static RoutedCommand openWorldCommand = new RoutedCommand();
        private bool disposed = false;

        private static readonly string ConfigFile1
            = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Yam", "world.cfg");
        //Timer to read from open world
        private readonly Timer _readTimer;

        //Variables for channel coloring
        private List<ColoredText> channelList = new List<ColoredText>();
        //Default colors for channel coloring
        private Dictionary<Brush, bool> colorsUsed = new Dictionary<Brush, bool>
        {
            { Brushes.Maroon, false },
            { Brushes.Beige, false },
            { Brushes.Aqua, false },
            { Brushes.Orange, false },
            { Brushes.Yellow, false },
            { Brushes.Tomato, false },
            { Brushes.Olive, false },
            { Brushes.DarkTurquoise, false },
            { Brushes.LimeGreen, false },
            { Brushes.DarkOliveGreen, false },
            { Brushes.RoyalBlue, false },
            { Brushes.Sienna, false },
            { Brushes.Violet, false }
        };
        //I'm very picky about my shade of gray
        private Brush defaultColor = (SolidColorBrush)new BrushConverter().ConvertFromString("#BEBEBE");
        private bool first_loop = true;

        //Info vars bound to status bar
        private double _numLinesText = 0;
        private string _worldURLText = "Not connected";

        // Command history
        private List<string> commandHistory = new List<string>();
        private int commandIndex = 0;
        #endregion

        public string WorldURLText
        {
            get { return _worldURLText; }
            set
            {
                _worldURLText = value;
                //Notify the binding that the value has changed.
                this.OnPropertyChanged("worldURLText");
            }
        }
        public double NumLinesText
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
        struct ColoredText
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

        

        public MainWindow()
        {
            InitializeComponent();

            this.Title = "YAM";
            this.DataContext = this; //So variables can bind to UI

            userInputText.Focus();
            userInputText.Clear();

            DisconnectWorldMenuItem.IsEnabled = false;
            ReconnectWorldMenuItem.IsEnabled = false;

            //For getting data from world
            _readTimer = new Timer(60);
            _readTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

            Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

            //NumLinesText = 0;

            // attach CommandBinding to root window 
            CommandBinding openWorldCommandBinding = new CommandBinding(
            openWorldCommand, OpenWorldMenuItem_Command, OpenWorldCanExecute);
            this.CommandBindings.Add(openWorldCommandBinding);

            KeyGesture openWorldGesture = new KeyGesture(
                Key.O, ModifierKeys.Control);

            KeyBinding openWorldBinding = new KeyBinding(
                openWorldCommand, openWorldGesture);

            this.InputBindings.Add(openWorldBinding);
            openWorldMenuItem.Command = openWorldCommand;
        }

        private void Current_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("An unhandled exception just occurred: " + e.Exception.Message, Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            e.Handled = true;
        }

        public void Dispose()
        {
            
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                _readTimer.Dispose();
                currentWorld.Dispose();
                // Free any other managed objects here.
                //
            }
            disposed = true;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
	    protected void OnPropertyChanged(string strPropertyName)
	    {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(strPropertyName));
        }
        #endregion

        /// <summary>
        /// Handle the KeyDown event to determine the type of character entered into the control. 
        /// </summary>
        private void UserInputText_PreviewKeyDown(object sender, KeyEventArgs e)
        {            
            // Send userInputText to connected world on Return
            if (e.Key == Key.Return)
            {
                string prompt = userInputText.Text;
                if (currentWorld.IsConnected)
                {
                    currentWorld.WriteToWorld(prompt);
                    commandHistory.Add(prompt);
                    commandIndex = commandHistory.Count - 1;
                    userInputText.Clear();
                }
                else
                {
                    mudOutputText.AppendText("\nNot connected to any world!", Brushes.Gold);
                }
                e.Handled = true;
            }
            //Scroll up through command history
            else if(e.Key == Key.Up)
            {

                if ((commandHistory.Count > 0) && userInputText.CaretIndex == 0)
                {
                    userInputText.Clear();
                    userInputText.Text = commandHistory.ElementAt(commandIndex);
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
        //Open URLs in a browser when clicked
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
            List<string> rawInput = new List<string>();
            if (currentWorld.IsConnected)
            {
                _readTimer.Enabled = false;
                string buffer = currentWorld.Read();

                //Text is read 
                if (buffer != String.Empty)
                {
                    string[] lines = buffer.Split('\n');
                    foreach (string line in lines)
                    {
                        rawInput.Add(line);
                    }                    
                    ScheduleDisplayUpdate(rawInput);
                }
                _readTimer.Enabled = true;
            }           
        }

        //Because the display is updated async, make sure that it doesn't
        //get scheduled twice. Otherwise text might get processed and displayed
        //in non-chronological order. Which is cool but weird.
        private void ScheduleDisplayUpdate(List<string> fromBuffer)
        {
            List<FormattedText> mudBuffer = new List<FormattedText>();

            foreach (FormattedText ft in ParseBuffer(fromBuffer))
                mudBuffer.Add(ft);
            
            //Use Dispatcher to safely update UI elements
            Dispatcher.BeginInvoke(
                DispatcherPriority.SystemIdle,
                    new OneArgDelegate(DrawOutput), mudBuffer); 
        }

        // This updates mudOutputText
        private void DrawOutput(List<FormattedText> mudBuffer)
        {       
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
                    hlk.RequestNavigate += new RequestNavigateEventHandler(Link_RequestNavigate);
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

                    newSpan = new Span(new Run(text))
                    {
                        Foreground = mudBuffer[i].color,
                        FontWeight = mudBuffer[i].weight
                    };
                    newPara.Inlines.Add(newSpan);
                }                
            }
            mudOutputText.Document.Blocks.Add(newPara);                     
            NumLinesText = _numLinesText;
        }
        /// <summary>
        /// Open a clicked URL in the default system browser
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var hyperlink = (Hyperlink)sender;
            Process.Start(hyperlink.NavigateUri.ToString());
        }
        
        /// <summary>
        /// Apply a variety of rules to the text received from the connected world
        /// </summary>
        /// <param name="fromBuffer"></param>
        /// <returns></returns>
        private List<FormattedText> ParseBuffer(List<string> fromBuffer)
        {            
            List<FormattedText> mudBuffer = new List<FormattedText>();

            foreach (string Text in fromBuffer)
            {
                if (fromBuffer.Count != 0)
                {
                    if (fromBuffer[0] != "")
                    {
                        string buffer = string.Empty;
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
                                if (!String.IsNullOrEmpty(buffer))
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
                            
                            else if(words[i] == currentWorldInfo.Username || words[i] == "You")
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
                        
                        if (!String.IsNullOrEmpty(buffer))
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
        private void MudOutputText_TextChanged(object sender, EventArgs e)
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

        private void ConnectToWorld(WorldInfo world)
        {            
            if (!currentWorld.IsConnected)
            {
                currentWorld = new AsyncConnect();
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

                    WorldURLText = world.WorldURL + " (" + ipAddress
                        + ") at port " + world.WorldPort;

                    if (world.AutoLogin)
                    {
                        //string loginString = String.Empty;

                        string loginString = "connect " + world.Username + " " +
                            world.Password + "\n";

                        currentWorld.WriteToWorld(loginString);                        
                    }
                    
                    ReconnectWorldMenuItem.IsEnabled = true;
                    DisconnectWorldMenuItem.IsEnabled = true;
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

        private void OpenWorldCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
            e.Handled = true;
        }
        private void OpenWorldMenuItem_Command(object sender, ExecutedRoutedEventArgs e)
        {
            OpenWorld();         
        }
        
        private void OpenWorld()
        {
            var window = new NewWorld { Owner = this };
            window.NewWorldSelect = true;
            bool? result = window.ShowDialog();

            if (result.HasValue && result.Value)
            {
                if (window.NewWorldSelect)
                {
                    currentWorldInfo = window.WorldInfo;
                    if (window.SaveLogin)
                    {
                        WriteConfig(currentWorldInfo);
                    }
                }
                    //Load a saved world
                else
                {
                    
                    string path = window.worldList.SelectedValue.ToString();
                    string[] temparray = path.Split(' ');

                    //Load the selected world if it's in the save file
                    //List<WorldInfo> loadedWorlds = new List<WorldInfo>();
                    //WorldCollection wc = MainWindow.ReadWorld().Worlds;
                    //var loadedWorlds = wc.Worlds;

                    foreach (WorldInfo world in ReadConfig().Worlds)
                    {
                        if (temparray[1] == world.WorldName)
                        {
                            currentWorldInfo = world;
                        }
                    }
                }                                
               
                ConnectToWorld(currentWorldInfo);
            }
        }
        
        private void SaveWorldMenuItem_Click(object sender, EventArgs e)
        {                   
            WriteConfig(currentWorldInfo);
        }
        private void ReconnectMenuItem_Click(object sender, EventArgs e)
        {
            mudOutputText.AppendText("\nTrying to disconnect...");
            if (currentWorld.Disconnect())
            {
                mudOutputText.AppendText("\nDisconnected from world", Brushes.Gold);
                ConnectToWorld(currentWorldInfo);
            }
        }
        private void DisconnectMenuItem_Click(object sender, EventArgs e)
        {
            //Try to disconnect
            if (currentWorld.Disconnect())
            {
                mudOutputText.AppendText("\nDisconnected from world", Brushes.Gold);

                DisconnectWorldMenuItem.IsEnabled = false;
                ReconnectWorldMenuItem.IsEnabled = false;
            }
            else
            {
                mudOutputText.AppendText("\nCould not disconnect. Must already be disconnected!", Brushes.Gold);
                DisconnectWorldMenuItem.IsEnabled = false;
                ReconnectWorldMenuItem.IsEnabled = false;
            }
        }
        private void QuitMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to quit?", "Exit",
                MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {

                this.Close();
            }        
        }
        
        private void AboutMenuItem_Click(object sender, EventArgs e)
        {
            About box = new About();
            box.Show();
        }
        #endregion
        #region Config File Handling
        public static void WriteConfig(WorldInfo data)
        {
            //Test to see if config directory exists and create if not
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Yam")))
            {
                MessageBox.Show($"Creating dir {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))}", "Yam");
                try
                {
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Yam"));
                }
                catch (UnauthorizedAccessException e)
                {
                    MessageBox.Show($"{e.Message}");               
                }
                catch (IOException e)
                {
                    MessageBox.Show($"{e.Message}");
                }
                finally
                {
                    MessageBox.Show("Created dir!");
                }
            }
            WorldCollection tempwc = new WorldCollection();
            ///If there's already saved worlds, load them
            if (File.Exists(ConfigFile1))
            {
                tempwc = ReadConfig();
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

        public static WorldCollection ReadConfig()
        {
            WorldCollection data = new WorldCollection();
            //WorldCollection data = null;           
            using (StreamReader stream = new StreamReader(ConfigFile1))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(WorldCollection));
                XmlReader reader = XmlReader.Create(stream, new XmlReaderSettings() { XmlResolver = null });
                data = (WorldCollection)serializer.Deserialize(reader);

            }
            return data;
        }
        #endregion
        #region Edit Menu Click Code
        private void PrefMenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FontDialog fd = 
                new System.Windows.Forms.FontDialog();
            System.ComponentModel.TypeConverter converter =
                System.ComponentModel.TypeDescriptor.GetConverter(typeof(System.Drawing.Font));

            string tmpstring = string.Format("{0}, {1}", mudOutputText.Document.FontFamily.ToString().Split(',')[0],
                mudOutputText.Document.FontSize.ToString());
            //MessageBox.Show(tmpstring);
            System.Drawing.Font font1 = (System.Drawing.Font)converter.ConvertFromString(tmpstring);
            fd.Font = font1;

            //fd.Color = textBox1.ForeColor;
            fd.ShowColor = true; //Enable choosing text color
            var result = fd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                //MessageBox.Show(String.Format("Font: {0}", fd.Font));
                mudOutputText.Document.FontFamily = new FontFamily(fd.Font.Name);
                mudOutputText.Document.FontSize = fd.Font.Size;// * 96.0 / 72.0;
                mudOutputText.Document.FontWeight = fd.Font.Bold ? FontWeights.Bold : FontWeights.Regular;
                mudOutputText.Document.FontStyle = fd.Font.Italic ? FontStyles.Italic : FontStyles.Normal;
                defaultColor = 
                    new SolidColorBrush(Color.FromArgb(fd.Color.A, fd.Color.R, fd.Color.G, fd.Color.B));
            }
        }        
        //Clear the output window
        private void ClearMenuItem_Click(object sender, RoutedEventArgs e)
        {
            mudOutputText.Document.Blocks.Clear();
        }
        #endregion
    }
}

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

