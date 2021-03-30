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
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Yam
{
    /*
     * Basic structure for async connection taken from
     * https://docs.microsoft.com/en-us/dotnet/framework/network-programming/asynchronous-client-socket-example
     */
    public class WorldConnection : IDisposable
    {
        private readonly TcpClient client = new();
        private readonly ManualResetEvent connectDone = new(false);

        bool disposed = false;

        public bool ConnectWorld(string worldName, int Port)
        {
            try
            {
                connectDone.Reset();
                client.BeginConnect(worldName, Port,
                    new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();
            }
            catch (SocketException e)
            {
                MessageBox.Show($"{e.Message}");
            }
            catch (ObjectDisposedException e)
            {
                MessageBox.Show($"{e.Message}");
            }
            catch (ArgumentNullException e)
            {
                MessageBox.Show($"{e.Message}");
            }

            return client.Connected;
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                TcpClient client = (TcpClient)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<string> ReadyAsync()
        {
            StringBuilder sb = new();
            NetworkStream stream = client.GetStream();

            while (stream.DataAvailable)
            {
                byte[] buffer = new byte[client.Available];
                await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                sb.AppendFormat("{0}", Encoding.UTF8.GetString(buffer));
            }

            return sb.ToString();
        }

        public bool IsConnected
        {
            get { return client.Connected; }
        }

        public bool Disconnect()
        {
            if (client.Connected)
            {
                client.GetStream().Close();
                client.Close();

                return true;
            }
            else
                return false;
        }

        public async Task WriteAsync(String data)
        {
            data += "\n";
            byte[] byteData = Encoding.UTF8.GetBytes(data);

            await client.GetStream().WriteAsync(byteData, 0, byteData.Length).ConfigureAwait(false);
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
                _ = Disconnect();
            }
            disposed = true;
        }

        private class StateObject
        {
            // Size of receive buffer.  
            public const int BufferSize = 256;
            // Receive buffer.  
            public byte[] buffer = new byte[BufferSize];
            // Received data string.  
            public StringBuilder sb = new();
            public string readString = string.Empty;
        }
    }
}