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

namespace Yam
{
    enum Verbs
    {
        WILL = 251,
        WONT = 252,
        DO = 253,
        DONT = 254,
        IAC = 255
    }

    enum Options
    {
        SGA = 3
    }

    // State object for receiving data from remote device.
    public class StateObject
    {
        // Client socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 256;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    public class asyncConnect
    {

        // The response from the remote device.
        private static String response = String.Empty;

        public TcpClient client = new TcpClient();
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);

        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        public asyncConnect()
        {
        }

        public bool ConnectWorld(string worldName, int Port)
        {
            try
            {
                //  client.BeginConnect(worldName, Port, new AsyncCallback(ConnectCallback), client);
                client.Connect(worldName, Port);
                if (!client.Connected)
                {
                    client.BeginConnect(worldName, Port,
                        new AsyncCallback(ConnectCallback), client);
                    connectDone.WaitOne();
                }
                if (client.Connected)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                //MessageBoxResult result = MessageBox.Show("Do you want to close this window?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                return false;
            }
        }
        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket client = (Socket)ar.AsyncState;

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

        public string Read()
        {
            StringBuilder myCompleteMessage = new StringBuilder();
            //byte[] myReadBuffer = new byte[4096];
            byte[] myReadBuffer = new byte[1024];
            string stringBuffer = String.Empty;
            int numberOfBytesRead = 0;
            NetworkStream stream = client.GetStream();

            try
            {

                if (stream.CanRead)
                {

                    // Incoming message may be larger than the buffer size. 
                    do
                    {
                        numberOfBytesRead = stream.Read(myReadBuffer, 0, myReadBuffer.Length);

                        myCompleteMessage.AppendFormat("{0}", Encoding.UTF8.GetString(myReadBuffer, 0, numberOfBytesRead));

                    }
                    while (stream.DataAvailable);

                    // Print out the received message to the console.
                    return myCompleteMessage.ToString();
                }
                else
                {
                    return myCompleteMessage.ToString();
                }
            }
            catch (SocketException)
            {
                return myCompleteMessage.ToString();
            }
            catch (ObjectDisposedException)
            {
                return myCompleteMessage.ToString();
            }
            catch (System.IO.IOException)
            {
                return myCompleteMessage.ToString();
            }

        }

        public string readFromWorld()
        {
            //Receive(client);
            //    receiveDone.WaitOne();

            return response;
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

        public void writeToWorld(string data)
        {
            //Send(client, data);
            //sendDone.WaitOne();
            byte[] byteData = Encoding.UTF8.GetBytes(data);
            NetworkStream stream = client.GetStream();
            stream.Write(byteData, 0, byteData.Length);
            byte[] newline = Encoding.UTF8.GetBytes("\n");
            stream.Write(newline, 0, newline.Length);
        }
    }
}