using System;
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

        public WorldInfo UI { get; set; } = new WorldInfo();
        public bool NewWorldSelect { get; set; } = false;
        private bool AutoLogin { get; set; } = false;
        public bool SaveLogin { get; set; } = false;

        public NewWorldWindow()
        {
            InitializeComponent();

            usernameText.IsEnabled = false;
            passwordText.IsEnabled = false;
            usernameBlock.Foreground = Brushes.Gray;
            passwordBlock.Foreground = Brushes.Gray;

            //Set up UI defaults

            worldNameText.Focus();
        }

        public WorldInfo WorldInfo
        {
            get
            {
                return (UI);
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

        private void LoginCheck_Handle(CheckBox checkBox)
        {
            // Use IsChecked.
            if (loginCheck.IsChecked.HasValue && loginCheck.IsChecked.Value)
            {
                usernameText.IsEnabled = true;
                passwordText.IsEnabled = true;

                usernameBlock.Foreground = Brushes.Black;
                passwordBlock.Foreground = Brushes.Black;

                AutoLogin = true;
            }
            else
            {
                usernameText.IsEnabled = false;
                passwordText.IsEnabled = false;

                usernameBlock.Foreground = Brushes.Gray;
                passwordBlock.Foreground = Brushes.Gray;

                AutoLogin = false;
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

        private void SaveLogin_Handle(CheckBox checkBox)
        {
            // Use IsChecked.
            if (passwordCheck.IsChecked.HasValue && passwordCheck.IsChecked.Value)
            {
                SaveLogin = true;
            }
            else
            {
                SaveLogin = false;
            }

        }
        private void OkNewWorldButton_Click(object sender, EventArgs e)
        {
            string worldNameTemp = worldNameText.Text.Trim();
            string worldURLTemp = worldURLText.Text.Trim();

            bool errorMessage = false;

            NewWorldSelect = true;
            if (worldNameTemp.Length > 0)
            {
                UI.WorldName = worldNameTemp;
            }
            else
            {
                errorMessage = true;
            }

            if (worldURLTemp.Length > 0)
            {
                UI.WorldURL = worldURLTemp;
            }
            else
            {
                errorMessage = true;
            }

            if (worldPortText.Text.Trim().Length > 0)
            {
                int worldPortTemp;
                //Let's make sure the user actually entered a number for the port
                try
                {
                    worldPortTemp = Convert.ToInt32(worldPortText.Text.Trim());
                }
                catch (FormatException)
                {
                    errorMessage = true;
                    worldPortTemp = 0;
                }
                UI.WorldPort = worldPortTemp;
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
                UI.AutoLogin = AutoLogin;
                NewWorldSelect = true;
                CloseWindow();
            }       
            if (AutoLogin)
            {
                UI.Username = usernameText.Text.Trim();
                string tempPass = passwordText.Text.Trim();
                byte[] bytePass = Encoding.UTF8.GetBytes(tempPass);
                UI.ProtectedPassword = bytePass;
            }
        }

        private void CancelNewWorldButton_Click(object sender, EventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseWindow()
        {
            DialogResult = true;
            Close();
        }
    }
}
