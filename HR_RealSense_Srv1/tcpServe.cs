using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

namespace HR_RealSense_Srv1
{
    class tcpServe
    {
        bool ready = false;
        Socket s;
        public void tcpServeListen()
        {
            try
            {
                IPAddress ipAd = IPAddress.Parse("127.0.0.1");
                // use local m/c IP address, and 
                // use the same in the client

                /* Initializes the Listener */
                TcpListener myList = new TcpListener(ipAd, 8001);

                /* Start Listeneting at the specified port */
                myList.Start();

                Console.WriteLine("The server is running at port 8001...");
                Console.WriteLine("The local End point is  :" +
                                  myList.LocalEndpoint);
                Console.WriteLine("Waiting for a connection.....");

                s = myList.AcceptSocket();
                Console.WriteLine("Connection accepted from " + s.RemoteEndPoint);
                ready = true;
                /*
                byte[] b = new byte[100];
                int k = s.Receive(b);
                Console.WriteLine("Recieved...");
                for (int i = 0; i < k; i++)
                    Console.Write(Convert.ToChar(b[i]));

                ASCIIEncoding asen = new ASCIIEncoding();
                s.Send(asen.GetBytes("The string was recieved by the server."));
                Console.WriteLine("\nSent Acknowledgement");
                */
                // clean up
                //s.Close();
                //myList.Stop();

            }
            catch (Exception e)
            {
                Console.WriteLine("Error..... " + e.StackTrace);
            }
        }
        public void Send(string txt)
        {
            if (!ready) return;
            try
            {
                ASCIIEncoding ae = new ASCIIEncoding();
                s.Send(ae.GetBytes(txt));
            }
            catch (Exception e)
            {
                Console.WriteLine("Error..... " + e.StackTrace);
            }
        }
        public string Recv()
        {
            if (!ready) return string.Empty;
            try
            {
                byte[] bt = new byte[10];
                if (s.Available > 0)
                {
                    int k = s.Receive(bt);

                    char result='N';
                    for (int i = 0; i < k; i++)
                    {
                        result = Convert.ToChar(bt[i]);
                    }
                    if (result == 'Y') return "store";
                    if (result == 'U') return "unregister";
                }
                return string.Empty;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error..... " + e.StackTrace);
            }
            return string.Empty;
        }
    }
}
