using System;
using System.Net;

namespace HttpClient
{
    internal class HttpClient : WebClient
    {
        private const int ConnectionLimit = 4096;

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = (HttpWebRequest)base.GetWebRequest(address);

            if (request != null)
            {
                request.Proxy = null;
                request.ServicePoint.Expect100Continue = false;
                request.ServicePoint.ConnectionLimit = ConnectionLimit;
            }

            return request;
        }
    }
}