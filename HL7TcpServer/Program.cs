using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HL7TcpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var ourTcpServer = new MultiThreadedTcpServer();
            //starting the server
            ourTcpServer.StartTcpServer(1080);

            Console.WriteLine("Press any key to exit program...");
            Console.ReadLine();
        

        }
    }
}
