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
        private readonly ManualResetEvent sendDone = new(false);
        private readonly ManualResetEvent readDone = new(false);

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

                //stream = client.GetStream();

                // Signal that the connection has been made.
                connectDone.Set();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public string Read()
        {
            try
            {
                readDone.Reset();
                StateObject state = new StateObject();

                // Begin receiving the data from the remote device.  
                client.Client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
                readDone.WaitOne();

                return state.readString;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;

                // Read data from the remote device.  
                int bytesRead = client.Client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.  
                    if (client.Available > 0)
                    {
                        client.Client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                    }

                    else
                    {
                        // All the data has arrived; put it in response.  
                        if (state.sb.Length > 1)
                        {
                            state.readString = state.sb.ToString();
                        }
                        // Signal that all bytes have been received.  
                        readDone.Set();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
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

        public void Write(String data)
        {
            data += "\n";
            byte[] byteData = Encoding.UTF8.GetBytes(data);

            sendDone.Reset();
            // Begin sending the data to the remote device.  
            client.Client.BeginSend(byteData, 0, byteData.Length, 0,
                new AsyncCallback(SendCallback), null);
            sendDone.WaitOne();
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Complete sending the data to the remote device.  
                int bytesSent = client.Client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception)
            {
                throw;
            }
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
            public StringBuilder sb = new StringBuilder();
            public string readString = string.Empty;
        }
    }
}