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
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Yam
{
    /// <summary>
    /// Interaction logic for newWorld.xaml
    /// </summary>
    public partial class OpenWorldWindow : Window
    {
        public WorldInfo UI { get; set; } = new WorldInfo();
        private WorldCollection worlds = new();

        public OpenWorldWindow()
        {
            InitializeComponent();

            //Set up UI defaults
            usernameTextBox.IsEnabled = false;
            passwordTextBox.IsEnabled = false;
            usernameTextBlock.Foreground = Brushes.Gray;
            passwordTextBlock.Foreground = Brushes.Gray;

            ObservableCollection<ListBoxItem> listItems = new();

            worlds = WorldFile.Read(MainWindow.ConfigFilePath);

            if (worlds != null && worlds.WorldList != null)
            {
                foreach (WorldInfo world in worlds.WorldList)
                {
                    ListBoxItem worldItem = new()
                    {
                        Content = world.WorldName,
                        Name = world.WorldName
                    };

                    listItems.Add(worldItem);
                }
                worldList.ItemsSource = listItems;
                worldList.SelectedIndex = 0;
                worldList.Focus();
            }          
        }        
        
        public WorldInfo WorldInfo
        {
            get
            {
                return (UI);
            }
        }
        private void OkOpenWorldButton_Click(object sender, EventArgs e)
        {
            if (worldList.SelectedValue != null)
            {
                CloseWindow();
            }
            else
            {
                DialogResult = false;
                Close();
            }            
        }
        private void CancelOpenWorldButton_Click(object sender, EventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseWindow()
        {
           
            DialogResult = true;
            Close();
        }

        private void WorldList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WorldInfo selectedWorld = worlds.GetWorld(worldList.SelectedItem.ToString().Split(' ')[1]);

            if (selectedWorld != null)
            {
                nameTextBox.Text = selectedWorld.WorldName;
                urlTextBox.Text = selectedWorld.WorldURL.ToString();
                portTextBox.Text = selectedWorld.WorldPort.ToString();
                if (selectedWorld.AutoLogin)
                {
                    loginCheck.IsChecked = true;
                    usernameTextBox.Text = selectedWorld.Username;
                    passwordTextBox.Password = selectedWorld.GetProtectedPassword().ToString();
                }
            }
        }

        private void LoginCheck_Checked(object sender, RoutedEventArgs e)
        {
            usernameTextBox.IsEnabled = true;
            passwordTextBox.IsEnabled = true;

            usernameTextBlock.Foreground = Brushes.Black;
            passwordTextBlock.Foreground = Brushes.Black;
        }

        private void LoginCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            usernameTextBox.IsEnabled = false;
            passwordTextBox.IsEnabled = false;

            usernameTextBlock.Foreground = Brushes.Gray;
            passwordTextBlock.Foreground = Brushes.Gray;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }

}
