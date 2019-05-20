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
    public class AsyncConnect : IDisposable
    {
        private TcpClient client = new TcpClient();
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);

        bool disposed = false;

        public bool ConnectWorld(string worldName, int Port)
        {
            try
            {
                connectDone.Reset();
                client.BeginConnect(worldName, Port,
                    new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();
                if (client.Connected)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (SocketException e)
            {
                MessageBox.Show($"{e.Message}");
                return false;
            }
            catch(ObjectDisposedException e)
            {
                MessageBox.Show($"{e.Message}");
                return false;
            }
        }
        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                connectDone.Set();
                // Retrieve the socket from the state object.
                TcpClient client = (TcpClient)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                // Signal that the connection has been made.              
            }
            catch (Exception)
            {
                throw;
            }
        }

        public string Read()
        {
            StringBuilder myCompleteMessage = new StringBuilder();
            NetworkStream stream = client.GetStream();

            if (stream.CanRead && client.Available > 0)
            {
                // Incoming message may be larger than the buffer size. 
                while (stream.DataAvailable)
                {
                    //byte[] myReadBuffer = new byte[1024];
                    byte[] myReadBuffer = new byte[client.Available];
                    int numberOfBytesRead = stream.Read(myReadBuffer, 0, myReadBuffer.Length);

                    myCompleteMessage.AppendFormat("{0}", Encoding.UTF8.GetString(myReadBuffer, 0, numberOfBytesRead));
                }
                // Return the received message
                return myCompleteMessage.ToString();
            }
            else
            {
                return myCompleteMessage.ToString();
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

        public void WriteToWorld(string data)
        {
            byte[] byteData = Encoding.UTF8.GetBytes(data);
            NetworkStream stream = client.GetStream();
            stream.Write(byteData, 0, byteData.Length);
            byte[] newline = Encoding.UTF8.GetBytes("\n");
            stream.Write(newline, 0, newline.Length);
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
                client.GetStream().Close();
                client.Close();
                //curr
                // Free any other managed objects here.
                //
            }
            disposed = true;
        }
    }
}