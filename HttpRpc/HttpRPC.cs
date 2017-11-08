using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;

namespace SimpleHttpRpc
{
    public delegate bool ShouldProcessFunc(HttpListenerRequest request, Dictionary<string, string> arguments);

    public delegate Task HttpActionAsync(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> arguments);
    public delegate void HttpAction(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> arguments);

    public static class HttpRPC
    {
        public static readonly List<(ShouldProcessFunc ShouldProcessFunc, HttpActionAsync Action)> Methods;
        public static Action<HttpListenerRequest, HttpListenerResponse, Exception> Error = null;

        static HttpRPC()
        {
            Methods = new List<(ShouldProcessFunc, HttpActionAsync)>();
            Error = (rq, rp, ex) =>
            {
                rp.StatusCode = (int)HttpStatusCode.InternalServerError;
                rp.AsText(ex.Message);
            };
        }

        public static void OnHttpRequest(HttpListenerRequest request, HttpListenerResponse response)
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
                    action(request, response, args).Wait();
                    return;
                }
                catch (Exception ex)
                {
                    Error?.Invoke(request, response, ex);
                    return;
                }
            }

            try
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                Error?.Invoke(request, response, new Exception($"Route {request.Url.PathAndQuery} not found."));
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
