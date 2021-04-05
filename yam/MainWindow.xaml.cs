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
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Timers;
using System.Windows.Threading;
using System.Net.Sockets;
using System.Globalization;

namespace Yam
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IDisposable
    {
        #region Private variables
        private WorldConnection _currentWorldConnection = new();
        private WorldInfo _currentWorldInfo = new();
        private bool disposed = false;

        public static string ConfigFilePath;

        //Timer to read from open world
        private readonly Timer _readTimer;

        //Variables for channel coloring
        private readonly List<ColoredText> channelList = new();
        //Default colors for channel coloring
        private readonly Dictionary<Brush, bool> colorsUsed = new()
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

        //Info vars bound to status bar
        private double _numLinesText = 0;
        private string _worldNameText = "None";
        private string _worldURLText = "Not connected";

        // Command history
        private readonly List<string> commandHistory = new();
        private int commandIndex = 0;
        #endregion

        public string WorldURLText
        {
            get { return _worldURLText; }
            set
            {
                _worldURLText = value;
                //Notify the binding that the value has changed.
                OnPropertyChanged("worldURLText");
            }
        }
        public double NumLinesText
        {
            get { return _numLinesText; }
            set
            {
                _numLinesText = value;
                //Notify the binding that the value has changed.
                OnPropertyChanged("numLinesText");
            }
        }
        public string WorldNameText
        {
            get { return _worldNameText; }
            set
            {
                _worldNameText = value;
                OnPropertyChanged("WorldNameText");
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
            // Create the default 
            string directoryPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Yam");

            if (!Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                }
                catch(IOException e)            
                {
                    _ = MessageBox.Show(this, $"{e}", "Error Creating Directory",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            ConfigFilePath = Path.Combine(directoryPath, "world.bin");

            InitializeComponent();

            Title = "YAM";
            DataContext = this; //So variables can bind to UI

            userInputText.Focus();
            userInputText.Clear();

            DisconnectWorldMenuItem.IsEnabled = false;
            ReconnectWorldMenuItem.IsEnabled = false;

            //For getting data from world
            _readTimer = new Timer(60);
            _readTimer.Elapsed += new ElapsedEventHandler(OnTimedEventAsync);
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
                _currentWorldConnection.Dispose();
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
        private async void UserInputText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Send userInputText to connected world on Return
            if (e.Key == Key.Return)
            {
                string prompt = userInputText.Text;
                if (_currentWorldConnection.IsConnected)
                {
                    await _currentWorldConnection.WriteAsync(prompt).ConfigureAwait(false);
                    commandHistory.Add(prompt);
                    commandIndex = commandHistory.Count - 1;
                    Dispatcher.Invoke(() =>
                    {
                        userInputText.Clear();
                    });                    
                }
                else
                {
                    mudOutputText.AppendText("\nNot connected to any world!", Brushes.Gold);
                }
                e.Handled = true;
            }
            //Scroll up through command history
            else if (e.Key == Key.Up)
            {
                if ((commandHistory.Count > 0) && userInputText.CaretIndex == 0)
                {
                    userInputText.Clear();
                    userInputText.Text = commandHistory.ElementAt(commandIndex);
                    if (commandIndex != 0)
                        commandIndex--;
                    e.Handled = true;
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
                    e.Handled = true;
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

        private delegate void OneArgDelegate(List<FormattedText> arg);

        /// <summary>
        /// Read from the connect world every time _readTimer is triggered
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private async void OnTimedEventAsync(object source, ElapsedEventArgs e)
        {                  
            if (_currentWorldConnection.IsConnected)
            {
                _readTimer.Enabled = false;
                string rawInput = await _currentWorldConnection.ReadyAsync().ConfigureAwait(false);

                if (!String.IsNullOrEmpty(rawInput))
                {
                    ScheduleDisplayUpdate(rawInput);
                }
                _readTimer.Enabled = true;
            }
        }

        #region mudOutput Updating
        private void ScheduleDisplayUpdate(string rawInput)
        {
            List<FormattedText> formattedBuffer = ParseBuffer(rawInput);

            //Use Dispatcher to safely update UI elements
            Dispatcher.BeginInvoke(
                DispatcherPriority.SystemIdle,
                    new OneArgDelegate(DrawOutput), formattedBuffer);
        }

        // This updates mudOutputText on the UI thread
        private void DrawOutput(List<FormattedText> mudBuffer)
        {
            //Use newPara and newSpan for buffering
            Paragraph newPara = new ();
            Span newSpan = new();

            newPara.FontSize = mudOutputText.Document.FontSize;
            //Add a little bit of padding around the text lines
            //dependent on the font size
            newPara.LineHeight = mudOutputText.Document.FontSize +
                (mudOutputText.Document.FontSize / 4);

            for (int i = 0; i < mudBuffer.Count; i++)
            {
                // Handle hyperlinks
                string linktext = mudBuffer[i].text;
                if (mudBuffer[i].isLink)
                {
                    //Trim off any extra line break the world adds to the end
                    if (i == mudBuffer.Count - 1)
                    {
                        linktext = linktext.TrimEnd(new char[] { '\n', '\r' } );
                    }
                    Hyperlink hlk = new(new Run(linktext));
                    try
                    {
                        //Remove any extra quote marks from around the URL
                        linktext = linktext.Trim('"');
                        hlk.NavigateUri = new Uri(linktext);
                    }
                    catch (UriFormatException)
                    {
                        MessageBox.Show($"Not a valid URL: {linktext}");
                        hlk.NavigateUri = new Uri("http://something.went.wrong");
                    }
                    hlk.RequestNavigate += new RequestNavigateEventHandler(Link_RequestNavigate);
                    newPara.Inlines.Add(hlk);
                }
                else
                {
                    newSpan = new Span();
                    string text = mudBuffer[i].text;
                    //Trim off any extra line break the world adds to the end
                    if (i == mudBuffer.Count - 1)
                    {
                        char[] charsToTrim = { '\n', '\r', ' ' };
                        text = text.TrimEnd(charsToTrim);
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
        /// <param name="buffer"></param>
        /// <returns></returns>
        private List<FormattedText> ParseBuffer(string rawInput)
        {
            string[] lines = rawInput.Split('\n');

            List<FormattedText> formattedBuffer = new();

            string channelPattern = @"^(\[.*?\])";     // Find [channel] names
            string connectPattern = @"^(<.+>)";         // Find <connect> messages

            foreach (string line in lines)
            {
                string buffer = string.Empty;
                Brush channelColor = Brushes.AliceBlue; //Text color
                bool isMatch = false;

                string[] words = line.Split(' ');

                for (int i = 0; i < words.Length; i++)
                {
                    #region Channel name coloring
                    //Detect a channel name with 'pattern' and color             
                    Regex channelRgx = new(channelPattern, RegexOptions.IgnoreCase);
                    Match channelMatch = channelRgx.Match(words[i]);

                    Regex connectRgx = new(connectPattern, RegexOptions.IgnoreCase);
                    Match connectMatch = connectRgx.Match(words[i]);

                    if (channelMatch.Success)
                    {
                        string channelName = channelMatch.Groups[1].Value;

                        foreach (ColoredText channel in channelList)
                        {
                            // check to see if the channel already has a color
                            if (channel.text == channelName)
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
                                    channelList.Add(new ColoredText() { text = channelName, colorName = channelColor });
                                    break;
                                }
                            }
                        }
                        //Add the colored name                      
                        formattedBuffer.Add(new FormattedText
                        {
                            text = channelName,
                            color = channelColor,
                            isLink = false,
                            weight = FontWeights.Normal
                        });

                        AddDefaultFormattedText(formattedBuffer, " ");
                    }
                    #endregion
                    //Color <connect> and </disconnect> messages purple                                                                        
                    else if (connectMatch.Success)
                    {
                        string connectText = connectMatch.Groups[1].Value;
                        formattedBuffer.Add(new FormattedText
                        {
                            text = connectText,
                            color = Brushes.DarkMagenta,
                            isLink = false,
                            weight = FontWeights.Normal
                        });

                        AddDefaultFormattedText(formattedBuffer, " ");
                    }
                    else if (words[i].Length > 4 && (words[i].StartsWith("http", StringComparison.OrdinalIgnoreCase)
                        || (words[i].StartsWith("\"http", StringComparison.OrdinalIgnoreCase))))
                    {
                        //Flush the buffer
                        AddDefaultFormattedText(formattedBuffer, buffer);
                        buffer = String.Empty;

                        // Add formatted hyperlink
                        formattedBuffer.Add(new FormattedText
                        {
                            text = words[i],
                            color =
                            defaultColor,
                            isLink = true,
                            weight = FontWeights.Normal
                        });

                        //Add a space after every word unless it's the end of the line
                        if (i != words.Length - 1)
                        {
                            AddDefaultFormattedText(formattedBuffer, " ");
                        }
                    }
                    else if (words[i] == _currentWorldInfo.Username || words[i] == "You")
                    {
                        // Flush buffer
                        AddDefaultFormattedText(formattedBuffer, buffer);
                        buffer = String.Empty;

                        formattedBuffer.Add(new FormattedText
                        {
                            text = words[i],
                            color = Brushes.Green,
                            isLink = false,
                            weight = FontWeights.Normal
                        });

                        if (i != words.Length - 1)
                        {
                            AddDefaultFormattedText(formattedBuffer, " ");
                        }
                    }
                    // Everything else
                    else
                    {
                        buffer += words[i];
                        if (i != words.Length - 1)
                            buffer += " ";
                    }
                }

                // Add what's accumulated in the text buffer to the formatted
                // buffer
                AddDefaultFormattedText(formattedBuffer, buffer);

                _numLinesText++;
            }
            return formattedBuffer;
        }

        private void AddDefaultFormattedText(List<FormattedText> buffer, string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                buffer.Add(new FormattedText
                {
                    text = text,
                    color = defaultColor,
                    isLink = false,
                    weight = FontWeights.Normal
                });
            }
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
        #endregion
        #region Networking
        private async void ConnectToWorld(WorldInfo world)
        {
            if (!_currentWorldConnection.IsConnected)
            {
                _currentWorldConnection = new WorldConnection();
                mudOutputText.AppendText($"\nConnecting to {world.WorldName} at {world.WorldURL} at port {world.WorldPort}...", Brushes.Gold);

                try
                {
                    await _currentWorldConnection.ConnectAsync(world.WorldURL, world.WorldPort).ConfigureAwait(false);

                    if (world.AutoLogin)
                    {
                        string loginString = "connect " + world.Username + " " +
                            Encoding.UTF8.GetString(world.GetProtectedPassword()) + "\n";
                        await _currentWorldConnection.WriteAsync(loginString).ConfigureAwait(false);
                    }

                    // Update UI elements
                    Dispatcher.Invoke(() =>
                    {
                        mudOutputText.AppendText("\nConnected!", Brushes.Gold);

                        Title = "YAM - " + world.WorldName;

                        //Display the world URL and IP in the status bar
                        IPHostEntry Host = Dns.GetHostEntry(world.WorldURL.DnsSafeHost);
                        string ipAddress = String.Empty;
                        for (int i = 0; i < Host.AddressList.Length; i++)
                        {
                            ipAddress += Host.AddressList[i].ToString();
                            if (i != Host.AddressList.Length - 1)
                                ipAddress += ", ";
                        }

                        WorldURLText = world.WorldURL + " (" + ipAddress
                            + ") at port " + world.WorldPort;
                        WorldNameText = world.WorldName;
                    
                        ReconnectWorldMenuItem.IsEnabled = true;
                        DisconnectWorldMenuItem.IsEnabled = true;
                    });
                    //Enable timer that reads from world
                    _readTimer.Enabled = true;
                }
                catch(SocketException)
                {
                    Dispatcher.Invoke(() => 
                    { 
                        mudOutputText.AppendText($"\nError connecting to {world.WorldName} at {world.WorldURL} at port {world.WorldPort}.", Brushes.Gold);
                    });
                }
                catch(Exception)
                {
                    throw;
                }
            }
            else
                mudOutputText.AppendText("\nAlready connected to a world.", Brushes.Gold);
        }

        private void DisconnectFromWorld()
        {
            mudOutputText.AppendText($"\nDisconecting from {_worldNameText}...", Brushes.Gold);
            if (_currentWorldConnection.Disconnect())
            {
                mudOutputText.AppendText("\nDisconnected.", Brushes.Gold);
            }
            else
            {
                mudOutputText.AppendText("\nNot connected to any world.", Brushes.Gold);               
            }
            DisconnectWorldMenuItem.IsEnabled = false;
            ReconnectWorldMenuItem.IsEnabled = false;
            _readTimer.Enabled = false;
        }
        #endregion
        #region Main Menu
        #region File Menu
        private void NewCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var window = new NewWorldWindow { Owner = this };
            bool? result = window.ShowDialog();
           
            if (result == true)
            {              
                _currentWorldInfo = window.WorldInfo;
                if (window.SaveLogin)
                {
                    WorldFile.Write(_currentWorldInfo, ConfigFilePath);
                }
                ConnectToWorld(_currentWorldInfo);
            }
        }
        private void OpenCommandBinding_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            OpenWorld();
        }

        private void OpenWorld()
        {
            var window = new OpenWorldWindow { Owner = this };
            bool? result = window.ShowDialog();

            if (result == true)
            {
                string name = window.worldList.SelectedValue.ToString().Split(' ')[1];

                foreach (WorldInfo world in WorldFile.Read(ConfigFilePath).WorldList)
                {
                    if (name == world.WorldName)
                    {
                        _currentWorldInfo = world;
                        break;
                    }
                }

                ConnectToWorld(_currentWorldInfo);
            }
        }

        private void SaveCommandBinding_Executed(object sender, RoutedEventArgs e)
        {
            WorldFile.Write(_currentWorldInfo, ConfigFilePath);
        }
        private void ReconnectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DisconnectFromWorld();
            ConnectToWorld(_currentWorldInfo);          
        }
        private void DisconnectMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DisconnectFromWorld();
        }
        private void QuitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to quit?", "Exit",
                MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                Close();
            }
        }
        #endregion
        #region About Menu
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            About box = new();
            box.Show();
        }
        #endregion       
        #region Edit Menu Click Code

        private void ToolsMenuFontItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FontDialog fontDialog = new();
                   
            TypeConverter converter =
                TypeDescriptor.GetConverter(typeof(System.Drawing.Font));
            string tmpstring = string.Format(CultureInfo.CurrentCulture, "{0}, {1}", mudOutputText.Document.FontFamily.ToString().Split(',')[0],
                mudOutputText.Document.FontSize.ToString(CultureInfo.CurrentCulture));
            System.Drawing.Font font1 = (System.Drawing.Font)converter.ConvertFromString(tmpstring);
            fontDialog.Font = font1;

            fontDialog.ShowApply = true;
            fontDialog.Apply += new EventHandler((s, e) => FontDialog_Apply(fontDialog));

            // Save the old font settings
            FontFamily oldFamily = mudOutputText.Document.FontFamily;
            double oldSize = mudOutputText.Document.FontSize;
            FontWeight oldWeight = mudOutputText.Document.FontWeight;
            FontStyle oldStyle = mudOutputText.Document.FontStyle;

            var result = fontDialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                FontDialog_Apply(fontDialog);
            }           
            else if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                mudOutputText.Document.FontFamily = oldFamily;
                mudOutputText.Document.FontSize = oldSize;
                mudOutputText.Document.FontWeight = oldWeight;
                mudOutputText.Document.FontStyle = oldStyle;
            }
        }

        private void FontDialog_Apply(System.Windows.Forms.FontDialog fontDialog)
        {
            mudOutputText.Document.FontFamily = new FontFamily(fontDialog.Font.Name);
            mudOutputText.Document.FontSize = fontDialog.Font.Size;// * 96.0 / 72.0;
            mudOutputText.Document.FontWeight = fontDialog.Font.Bold ? FontWeights.Bold : FontWeights.Regular;
            mudOutputText.Document.FontStyle = fontDialog.Font.Italic ? FontStyles.Italic : FontStyles.Normal;

            // Apply the new font size to existing text
            mudOutputText.SelectAll();
            mudOutputText.Selection.ApplyPropertyValue(FontSizeProperty, mudOutputText.Document.FontSize);
        }

        private void ToolsMenuColorItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.ColorDialog cd = new();
            var result = cd.ShowDialog();
            
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                // Convert selected color to Brush and save as the new default colorl
                defaultColor = new SolidColorBrush(Color.FromArgb(cd.Color.A, cd.Color.R, cd.Color.G, cd.Color.B));                
                mudOutputText.Document.Foreground = defaultColor;

                // Apply new color to existing text
                mudOutputText.SelectAll();
                mudOutputText.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, defaultColor);
            }
        }

        //Clear the output window
        private void ClearMenuItem_Click(object sender, RoutedEventArgs e)
        {
            mudOutputText.Document.Blocks.Clear();
        }

        private void ToolsMenuOptionsItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new OptionsWindow() { Owner = this };
            var result = window.ShowDialog();
        }
    }
    #endregion
    #endregion
}