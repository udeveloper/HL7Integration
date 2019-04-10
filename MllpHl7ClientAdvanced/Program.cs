using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MllpHl7ClientAdvanced
{
    class Program
    {
        private static char END_OF_BLOCK = '\u001c';
        private static char START_OF_BLOCK = '\u000b';
        private static char CARRIAGE_RETURN = (char)13;

        static void Main(string[] args)
        {
            TcpClient ourTcpClient = null;
            NetworkStream networkStream = null;

            var testHl7MessageToTransmit = new StringBuilder();

            //a HL7 test message that is enveloped with MLLP as described in my article
            testHl7MessageToTransmit.Append(START_OF_BLOCK)
                .Append("MSH|^~\\&|AcmeHIS|StJohn|CATH|StJohn|20061019172719||ORM^O01|MSGID12349876|P|2.3")
                .Append(CARRIAGE_RETURN)
                .Append("PID|||20301||Durden^Tyler^^^Mr.||19700312|M|||88 Punchward Dr.^^Los Angeles^CA^11221^USA|||||||")
                .Append(CARRIAGE_RETURN)
                .Append("PV1||O|OP^^||||4652^Paulson^Robert|||OP|||||||||9|||||||||||||||||||||||||20061019172717|20061019172718")
                .Append(CARRIAGE_RETURN)
                .Append("ORC|NW|20061019172719")
                .Append(CARRIAGE_RETURN)
                .Append("OBR|1|20061019172719||76770^Ultrasound: retroperitoneal^C4|||12349876")
                .Append(CARRIAGE_RETURN)
                .Append(END_OF_BLOCK)
                .Append(CARRIAGE_RETURN);

            try
            {
                //initiate a TCP client connection to local loopback address at port 1080
                ourTcpClient = new TcpClient();

                ourTcpClient.Connect(new IPEndPoint(IPAddress.Loopback, 1080));

                Console.WriteLine("Connected to server....");

                //get the IO stream on this connection to write to
                networkStream = ourTcpClient.GetStream();

                //use UTF-8 and either 8-bit encoding due to MLLP-related recommendations
                var sendMessageByteBuffer = Encoding.UTF8.GetBytes(testHl7MessageToTransmit.ToString());

                if (networkStream.CanWrite)
                {
                    //send a message through this connection using the IO stream
                    networkStream.Write(sendMessageByteBuffer, 0, sendMessageByteBuffer.Length);

                    Console.WriteLine("Data was sent data to server successfully....");
                                       
                    var receivedMessage = GetResponse(networkStream);
                                       

                    using (StreamWriter writer = new StreamWriter("N:\\Response.txt", true))
                    {
                        writer.AutoFlush = true;
                        writer.WriteLine(receivedMessage);
                    }

                    Console.WriteLine("Received message from server: {0}", receivedMessage);
                }

                Console.WriteLine("Press any key to exit...");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                //display any exceptions that occur to console
                Console.WriteLine(ex.Message);
            }
            finally
            {
                //close the IO strem and the TCP connection
                networkStream?.Close();
                ourTcpClient?.Close();
            }
        }

        private  static string GetResponse(NetworkStream stream)
        {
            var memoryStream = new MemoryStream();
            byte[] data = new byte[1024];
            int numBytesRead;

            do
            {
                numBytesRead = stream.Read(data, 0, data.Length);
                memoryStream.Write(data, 0, numBytesRead);

            } while (numBytesRead == data.Length);

            var stringMessageACK = Encoding.UTF8.GetString(memoryStream.ToArray());

            return stringMessageACK;
        }
    }
}