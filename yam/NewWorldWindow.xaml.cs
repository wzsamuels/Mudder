using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Yam
{
    /// <summary>
    /// Interaction logic for NewWorldWindow.xaml
    /// </summary>
    public partial class NewWorldWindow : Window
    { 

        public WorldInfo WorldInfo { get; set; } = new WorldInfo();
        private bool AutoLogin = false;
        public bool SaveLogin { get; set; } = false;

        public NewWorldWindow()
        {
            InitializeComponent();

            //Set up UI defaults
            usernameText.IsEnabled = false;
            passwordText.IsEnabled = false;
            usernameBlock.Foreground = Brushes.Gray;
            passwordBlock.Foreground = Brushes.Gray;

            worldNameText.Focus();
        }

        /*
         */ 
        private void LoginCheck_Checked(object sender, RoutedEventArgs e)
        {
            usernameText.IsEnabled = true;
            passwordText.IsEnabled = true;

            usernameBlock.Foreground = Brushes.Black;
            passwordBlock.Foreground = Brushes.Black;
        }

        private void LoginCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            usernameText.IsEnabled = false;
            passwordText.IsEnabled = false;

            usernameBlock.Foreground = Brushes.Gray;
            passwordBlock.Foreground = Brushes.Gray;
        }

        private void OkNewWorldButton_Click(object sender, EventArgs e)
        {
            string worldNameTemp = worldNameText.Text.Trim();
            string worldURLTemp = worldURLText.Text.Trim();

            StringBuilder errorMessage = new();

            /* 
             * Check that the input fields are valid. If not build a error 
             * message string explaining the problems.
             */
            if (worldNameTemp.Length > 0)
            {
                WorldInfo.WorldName = worldNameTemp;
            }
            else
            {
                errorMessage.AppendLine("Please enter a world name.");
            }

            if (worldURLTemp.Length > 0)
            {
                WorldInfo.WorldURL = worldURLTemp;
            }
            else
            {
                errorMessage.AppendLine("Please enter a world URL.");
            }

            if (worldPortText.Text.Trim().Length > 0)
            {
                try
                {
                    WorldInfo.WorldPort = Convert.ToInt32(worldPortText.Text.Trim(), NumberFormatInfo.CurrentInfo);
                }
                catch (FormatException)
                {
                    errorMessage.AppendLine("Please enter a valid port address.");
                }
            }
            else
            {
                errorMessage.AppendLine("Please enter a port address.");
            }

            if (errorMessage.Length != 0)
            {
                MessageBox.Show(this, $"{errorMessage}", "Invalid World Information",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                SaveLogin = (bool)passwordCheck.IsChecked;
                AutoLogin = (bool)loginCheck.IsChecked;

                if (AutoLogin)
                {
                    WorldInfo.AutoLogin = AutoLogin;
                    WorldInfo.Username = usernameText.Text.Trim();
                    string tempPass = passwordText.Text.Trim();
                    byte[] bytePass = Encoding.UTF8.GetBytes(tempPass);
                    WorldInfo.ProtectedPassword = bytePass;
                }

                DialogResult = true;
                Close();
            }                              
        }

        private void CancelNewWorldButton_Click(object sender, EventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
