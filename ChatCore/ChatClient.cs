using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace ChatCore
{
    public class ChatClient
    {
        private TcpClient m_client;
        private List<KeyValuePair<string, string>> m_messageList;
        private byte[] messageType = new byte[2];

        public ChatClient()
        {
            m_messageList = new List<KeyValuePair<string, string>>();
        }

        public bool Connect(string address, int port)
        {
            m_client = new TcpClient();

            try
            {
                Console.WriteLine("Connecting to chat server {0}:{1}", address, port);
                m_client.Connect(address, port);

                Console.WriteLine("Connected to chat server");
                return m_client.Connected;
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
                return false;
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                return false;
            }
        }

        public void Disconnect()
        {
            m_client.Close();
            Console.WriteLine("Disconnected");
        }

        public void Refresh()
        {
            if (m_client.Available > 0)
            {
                HandleReceiveMessages(m_client);
            }
        }

        public List<KeyValuePair<string, string>> GetMessages()
        {
            var messages = new List<KeyValuePair<string, string>>(m_messageList);
            m_messageList.Clear();

            return messages;
        }

        private void HandleReceiveMessages(TcpClient client)
        {
            var stream = client.GetStream();
            var numBytes = client.Available;
            var buffer = new byte[numBytes];
            var bytesRead = stream.Read(buffer, 0, numBytes);

            int position = 0;
            byte[] byteLength = new byte[4];
            while (position < bytesRead)
            {
                Array.Copy(buffer, position, byteLength, 0, 4);
                int length = BitConverter.ToInt32(byteLength, 0);
                if (length == 0)
                {
                    break;
                }
                position += 4;

                var request = System.Text.Encoding.ASCII.GetString(buffer).Substring(position, length);
                var tokens = request.Split(':');
                var sender = tokens[0];
                var message = tokens[1];
                m_messageList.Add(new KeyValuePair<string, string>(sender, message));

                position += length;
            }
        }

        public void SetName(string name)
        {
            messageType = BitConverter.GetBytes('n');
            SendData(name);
        }

        public void SendMessage(string message)
        {
            messageType = BitConverter.GetBytes('m');
            SendData(message);
        }

        private void SendData(string data)
        {
            byte[] sendBuffer = new byte[1024];
            var requestBuffer = System.Text.Encoding.ASCII.GetBytes(data);
            var lengthBuffer = BitConverter.GetBytes(requestBuffer.Length);

            int position = 0;
            Array.Copy(messageType, 0, sendBuffer, position, messageType.Length);
            position += messageType.Length;

            Array.Copy(lengthBuffer, 0, sendBuffer, position, lengthBuffer.Length);
            position += lengthBuffer.Length;

            Array.Copy(requestBuffer, 0, sendBuffer, position, requestBuffer.Length);
            m_client.GetStream().Write(sendBuffer, 0, sendBuffer.Length);
        }
    }
}
