using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

namespace HR_RealSense_Srv1
{
    class httpClient
    {
        private string _urlFace,_urlHand;
        public httpClient(string urlFace,string urlHand)
        {
            _urlFace = urlFace;
            _urlHand = urlHand;
        }
        public void SendHand(string st)
        {
            Send(_urlHand, st);
        }
        public void SendFace(string st)
        {
            Send(_urlFace, st);
        }
        private bool Send(string url, string PostData)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "POST";
            request.ContentLength = 0;
            request.ContentType = "application/json";
            request.Accept = "text/plain";
            request.KeepAlive = false;
            //request.CookieContainer = cookie;

            if (!string.IsNullOrEmpty(PostData) )//&& Method == HttpVerb.POST)
            {
                UTF8Encoding encoding = new UTF8Encoding();
                byte[] bytes = Encoding.GetEncoding("iso-8859-1").GetBytes(PostData);
                request.ContentLength = bytes.Length;
                request.AllowAutoRedirect = true;
                try
                {
                    using (Stream writeStream = request.GetRequestStream())
                    {
                        writeStream.Write(bytes, 0, bytes.Length);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    return false;
                }

            }

            try
            {  // Gets exception
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                {
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
            return true;
        }
    }
}
