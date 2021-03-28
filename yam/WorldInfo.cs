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
using System.Security.Cryptography;

namespace Yam
{
    /* Class for storing information about a mud world */
    [Serializable]
    public class WorldInfo
    {
        public string WorldName { get; set; } = string.Empty;
        public string WorldURL { get; set; } = string.Empty;
        public int WorldPort { get; set; } = 0;
        public string Username { get; set; } = string.Empty;
        public bool AutoLogin { get; set; } = false;
        private byte[] protectedPassword;
        private static readonly byte[] entropy = { 9, 8, 7, 6, 5 };
        public WorldInfo()
        {
            /*
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(entropy);
            }*/
        }
        public byte[] ProtectedPassword
        {
            get
            {
                if (protectedPassword != null)
                {
                    return (byte[])ProtectedData.Unprotect(protectedPassword, entropy, DataProtectionScope.CurrentUser).Clone();
                }
                return (byte[])protectedPassword.Clone();
            }

            set => protectedPassword =
                            ProtectedData.Protect(value, entropy, DataProtectionScope.CurrentUser);
        }
    }
}