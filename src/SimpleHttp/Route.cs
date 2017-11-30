using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SimpleHttp
{
    public delegate bool ShouldProcessFunc(HttpListenerRequest request, Dictionary<string, string> arguments);

    public delegate Task HttpActionAsync(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> arguments);
    public delegate void HttpAction(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> arguments);

    public delegate void OnError(HttpListenerRequest request, HttpListenerResponse response, Exception exception);
    public delegate bool OnBefore(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> arguments);

    public static class Route
    {
        public static readonly List<(ShouldProcessFunc ShouldProcessFunc, HttpActionAsync Action)> Methods;
        public static OnBefore Before = null;
        public static OnError Error = null;

        static Route()
        {
            Methods = new List<(ShouldProcessFunc, HttpActionAsync)>();
            Error = (rq, rp, ex) =>
            {
                rp.StatusCode = (int)HttpStatusCode.InternalServerError;
                rp.AsText(ex.Message);
            };
        }

        public static async Task OnHttpRequestAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var args = new Dictionary<string, string>();
            foreach (var (shouldProcessFunc, action) in Methods)
            {
                if (shouldProcessFunc(request, args) == false)
                {
                    args.Clear(); //if something was written
                    continue;
                }

                try
                {
                    var isHandled = Before?.Invoke(request, response, args);
                    if (isHandled.HasValue && (bool)isHandled) return;

                    await action(request, response, args);
                }
                catch (Exception ex)
                {
                    try { Error?.Invoke(request, response, ex); }
                    catch { }
                }

                return;
            }

            try
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                Error?.Invoke(request, response, new FileNotFoundException($"Route {request.Url.PathAndQuery} not found.")); //TODO: replace with RouteNotFoundException
            }
            catch { }
        }


        public static void Add(string pattern, HttpAction action, string method = "GET")
        {
            Add((rq, args) =>
                {
                    if (rq.HttpMethod != method)
                        return false;

                    return rq.Url.PathAndQuery.TryMatch(pattern, args);
                },
                action);
        }

        public static void Add(string pattern, HttpActionAsync action, string method = "GET")
        {
            Add((rq, args) =>
                {
                    if (rq.HttpMethod != method)
                        return false;

                    return rq.Url.PathAndQuery.TryMatch(pattern, args);
                },
                action);
        }



        public static void Add(ShouldProcessFunc shouldProcess, HttpActionAsync action)
        {
            Methods.Add((shouldProcess, action));
        }

        public static void Add(ShouldProcessFunc shouldProcess, HttpAction action)
        {
            Methods.Add((shouldProcess, (rq, rp, args) => 
            { 
                action(rq, rp, args);
                return Task.FromResult(true);
            }));
        }



        static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
    }
}
