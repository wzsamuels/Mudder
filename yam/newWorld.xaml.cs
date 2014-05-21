using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Yam
{
    /// <summary>
    /// Interaction logic for newWorld.xaml
    /// </summary>
    public partial class newWorld : Window
    {
        public newWorld()
        {
            InitializeComponent();
            

            //Set up UI defaults

            usernameText.IsEnabled = false;
            passwordText.IsEnabled = false;
            usernameBlock.Foreground = Brushes.Gray;
            passwordBlock.Foreground = Brushes.Gray;

            //Load saved worlds

            WorldInfo tempWorld = ReadWorld();
            if (tempWorld.WorldName != String.Empty)
            {
                ListBoxItem world = new ListBoxItem();
                world.Content = tempWorld.WorldName;
                world.Name = tempWorld.WorldName;
                ObservableCollection<ListBoxItem> oc = new ObservableCollection<ListBoxItem>();
                oc.Add(world);
                worldList.ItemsSource = oc;
            }
            
            if (worldList.Items.Count > 0)
            {
                worldList.SelectedIndex = 0;
                savedWorldButton.IsChecked = true;
                worldList.Focus();
            }
            else
            {
                newWorldButton.IsChecked = true;
                worldNameText.Focus();
            }

        }

        WorldInfo _ui = new WorldInfo();
        public bool newWorldSelect = false;
        public bool autoLogin = false;
        public bool saveLogin = false;

        public WorldInfo WorldInfo
        {
            get
            {
                return (_ui);
            }
        }

        private void LoginCheck_Checked(object sender, RoutedEventArgs e)
        {
            LoginCheck_Handle(sender as CheckBox);
        }

        private void LoginCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            LoginCheck_Handle(sender as CheckBox);
        }

        void LoginCheck_Handle(CheckBox checkBox)
        {
            // Use IsChecked.
            if (loginCheck.IsChecked.HasValue && loginCheck.IsChecked.Value)
            {
                usernameText.IsEnabled = true;
                passwordText.IsEnabled = true;

                usernameBlock.Foreground = Brushes.Black;
                passwordBlock.Foreground = Brushes.Black;

                autoLogin = true;
            }
            else
            {
                usernameText.IsEnabled = false;
                passwordText.IsEnabled = false;

                usernameBlock.Foreground = Brushes.Gray;
                passwordBlock.Foreground = Brushes.Gray;

                autoLogin = false;
            }
            
        }

        private void SaveLoginCheck_Checked(object sender, RoutedEventArgs e)
        {
            SaveLogin_Handle(sender as CheckBox);
        }

        private void SaveLoginCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            SaveLogin_Handle(sender as CheckBox);
        }

        void SaveLogin_Handle(CheckBox checkBox)
        {
            // Use IsChecked.
            if (passwordCheck.IsChecked.HasValue && passwordCheck.IsChecked.Value)
            {
                saveLogin = true;
            }
            else
            {
                saveLogin = false;
            }

        }

        private void okNewWorldButton_Click(object sender, EventArgs e)
        {
            string worldNameTemp = worldNameText.Text.Trim();
            string worldURLTemp = worldURLText.Text.Trim();

            bool errorMessage = false;

            if (newWorldButton.IsChecked.HasValue && newWorldButton.IsChecked.Value)
            {
                newWorldSelect = true;
                if (worldNameTemp.Length > 0)
                {
                    _ui.WorldName = worldNameTemp;
                }
                else
                {
                    errorMessage = true;
                }

                if (worldURLTemp.Length > 0)
                {
                    _ui.WorldURL = worldURLTemp;
                }
                else
                {
                    errorMessage = true;
                }

                int worldPortTemp = 1;

                if (worldPortText.Text.Trim().Length > 0)
                {
                    try
                    {
                        worldPortTemp = Convert.ToInt32(worldPortText.Text.Trim());
                    }
                    catch (FormatException)
                    {
                        errorMessage = true;
                        worldPortTemp = 0;
                    }
                    _ui.WorldPort = worldPortTemp;
                }
                else
                {
                    errorMessage = true;
                }
                if (errorMessage)
                {
                    MessageBox.Show("Input Field(s) are empty or invalid");
                }
                else
                {
                    newWorldSelect = true;
                    CloseWindow();
                }
                _ui.AutoLogin = autoLogin;
            }
            else
            {
                if (worldList.SelectedValue != null)
                {
                    newWorldSelect = false;
                    CloseWindow();
                }
                else
                {
                    this.DialogResult = false;
                    this.Close();
                }
                
            }
            if (autoLogin)
            {
                _ui.Username = usernameText.Text.Trim();
                _ui.Password = passwordText.Text.Trim();                
            }
            
        }
        private void cancelNewWorldButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void CloseWindow()
        {
           
            this.DialogResult = true;

            this.Close();
        }
        private const string configFile = "world_data";
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
