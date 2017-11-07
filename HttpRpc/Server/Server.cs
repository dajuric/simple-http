using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleHttpRpc
{
    public static class HttpServer
    {
        public static async Task ListenAsync(string httpListenerPrefix, CancellationToken token, Action<HttpListenerRequest, HttpListenerResponse> onHttpRequest)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(httpListenerPrefix);
            listener.Start();

            while (true)
            {
                HttpListenerContext listenerContext = await listener.GetContextAsync();
                if (listenerContext.Request.IsWebSocketRequest)
                    continue;

                onHttpRequest(listenerContext.Request, listenerContext.Response);

                if (token.IsCancellationRequested)
                    break;
            }

            listener.Stop();
        }
    }
}
