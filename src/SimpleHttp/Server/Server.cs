using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleHttp
{
    public static class HttpServer
    {
        public static async Task ListenAsync(int port, CancellationToken token, Func<HttpListenerRequest, HttpListenerResponse, Task> onHttpRequestAsync)
        {
            await ListenAsync($"http://+:{port}/", token, onHttpRequestAsync);
        }

        public static async Task ListenAsync(string httpListenerPrefix, CancellationToken token, Func<HttpListenerRequest, HttpListenerResponse, Task> onHttpRequestAsync)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(httpListenerPrefix);

            try { listener.Start(); }
            catch (Exception ex) when ((ex as HttpListenerException)?.ErrorCode == 5)
            {
                throw new UnauthorizedAccessException($"The HTTP server can not be started, as the namespace reservation does not exist.\n" +
                                                      $"Please run (elevated): 'netsh add urlacl url={httpListenerPrefix} user=\"Everyone\"'.");
            }

            while (true)
            {
                HttpListenerContext listenerContext = await listener.GetContextAsync();
                if (listenerContext.Request.IsWebSocketRequest)
                    continue;

                Task.Run(() => onHttpRequestAsync(listenerContext.Request, listenerContext.Response)).Wait(0);

                if (token.IsCancellationRequested)
                    break;
            }

            listener.Stop();
        }
    }
}
