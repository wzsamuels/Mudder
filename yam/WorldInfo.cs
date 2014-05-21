using System;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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