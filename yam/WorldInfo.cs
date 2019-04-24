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

namespace Yam
{

    public class WorldInfo
    {
        private string _worldName = String.Empty;
        private string _worldURL = String.Empty;
        private int _worldPort = 0;
        private string _username = String.Empty;
        private string _password = String.Empty;
        private bool _autoLogin = false;

        public string WorldName
        {
            get
            {
                return (_worldName);
            }
            set
            {
                _worldName = value;
            }
        }

        public string WorldURL
        {
            get
            {
                return (_worldURL);
            }
            set
            {
                _worldURL = value;
            }
        }

        public int WorldPort
        {
            get
            {
                return (_worldPort);
            }
            set
            {
                _worldPort = value;
            }
        }

        public string Username
        {
            get
            {
                return (_username);
            }
            set
            {
                _username = value;
            }
        }

        public string Password
        {
            get
            {
                return (_password);
            }
            set
            {
                _password = value;
            }
        }
        public bool AutoLogin
        {
            get
            {
                return (_autoLogin);
            }
            set
            {
                _autoLogin = value;
            }
        }

       
    }
}