using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HL7TcpClient
{
    class Program
    {
        
        static void Main(string[] args)
        {
            TcpClient _tcpClient=null;
            NetworkStream _networkStream=null;

            try
            {
                //initiate a TCP client connection to local loopback address at port 1080
                _tcpClient = new TcpClient();
                _tcpClient.Connect(new IPEndPoint(IPAddress.Loopback, 1080));

                Console.WriteLine("Connected to server....");

                //get the IO stream on this connection to write to
                _networkStream = _tcpClient.GetStream();

                //use UTF-8 and either 8-bit encoding due to MLLP-related recommendations
                string messageToTransmit = "Pruebas HL7V2.5";
                byte[] byteBuffer = Encoding.UTF8.GetBytes(messageToTransmit);

                //send a message through this connection using the IO stream
                _networkStream.Write(byteBuffer, 0, byteBuffer.Length);

                Console.WriteLine("Data was sent data to server successfully....");

                var  bytesReceivedFromServer=_networkStream.Read(byteBuffer, 0, byteBuffer.Length);

                // Our server for this example has been designed to echo back the message
                // keep reading from this stream until the message is echoed back
                while (bytesReceivedFromServer < byteBuffer.Length)
                {
                    bytesReceivedFromServer = _networkStream.Read(byteBuffer, 0, byteBuffer.Length);
                    if(byteBuffer.Length==0)
                    {
                        //exit the reading loop since there is no more data
                        break;
                    }
                }

                string messageReceived = Encoding.UTF8.GetString(byteBuffer);

                Console.WriteLine("Received message from server: {0}", messageReceived);

                Console.WriteLine("Press any key to exit program...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                //display any exceptions that occur to console
                Console.WriteLine(ex.Message);
            }
            finally
            {
                _networkStream?.Close();
                _tcpClient?.Close();
            }
        }
    }
}
