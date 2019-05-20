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
using System.Text;
using System.Collections.Generic;
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

        public OpenWorldWindow()
        {
            InitializeComponent();



            ObservableCollection<ListBoxItem> listItems = new ObservableCollection<ListBoxItem>();

            var loadedWorlds = MainWindow.ReadConfig().Worlds;
            if (loadedWorlds != null)
            {
                foreach (WorldInfo world in MainWindow.ReadConfig().Worlds)
                {
                    ListBoxItem worldItem = new ListBoxItem
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
        private void OkNewWorldButton_Click(object sender, EventArgs e)
        {
            if (worldList.SelectedValue != null)
            {
                CloseWindow();
            }
            else
            {
                this.DialogResult = false;
                this.Close();
            }            
        }
        private void CancelNewWorldButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void CloseWindow()
        {
           
            this.DialogResult = true;
            this.Close();
        }
    }

}
