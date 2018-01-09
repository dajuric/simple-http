#region License
// Copyright © 2018 Darko Jurić
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SimpleHttp
{
    #region Delegates

    /// <summary>
    /// Delegate which runs before all route-methods and returns if the processing should finish (true) or continue (false).
    /// </summary>
    /// <param name="request">HTTP request.</param>
    /// <param name="response">HTTP response.</param>
    /// <returns>True if the request is handled, false otherwise.</returns>
    public delegate bool OnBefore(HttpListenerRequest request, HttpListenerResponse response);

    /// <summary>
    /// Delegate which runs before any route-action is invoked to determine which route should be executed.
    /// </summary>
    /// <param name="request">HTTP request.</param>
    /// <param name="arguments">
    /// Empty collection of key-value pairs populated by this function. 
    /// <para>If <see cref="OnBefore"/> is run it may contain some data.</para>
    /// </param>
    /// <returns>True if the route action should be executed, false otherwise.</returns>
    public delegate bool ShouldProcessFunc(HttpListenerRequest request, Dictionary<string, string> arguments);

    /// <summary>
    /// Delegate which runs when a route is matched.
    /// </summary>
    /// <param name="request">HTTP request.</param>
    /// <param name="response">HTTP response.</param>
    /// <param name="arguments">Collection of key-value pairs populated by the <see cref="ShouldProcessFunc"/>.</param>
    /// <returns>Action task.</returns>
    public delegate Task HttpActionAsync(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> arguments);
    /// <summary>
    /// Delegate which runs when a route is matched.
    /// </summary>
    /// <param name="request">HTTP request.</param>
    /// <param name="response">HTTP response.</param>
    /// <param name="arguments">Collection of key-value pairs populated by the <see cref="ShouldProcessFunc"/>.</param>
    public delegate void HttpAction(HttpListenerRequest request, HttpListenerResponse response, Dictionary<string, string> arguments);

    /// <summary>
    /// Delegate which runs if an error occurs.
    /// </summary>
    /// <param name="request">HTTP request.</param>
    /// <param name="response">HTTP response.</param>
    /// <param name="exception">Thrown exception.</param>
    public delegate void OnError(HttpListenerRequest request, HttpListenerResponse response, Exception exception);

    #endregion

    #region Exceptions

    /// <summary>
    /// Represents error that occur when a route is not found.
    /// </summary>
    public class RouteNotFoundException : Exception
    {
        /// <summary>
        /// Creates a new instance of the route not found exception.
        /// </summary>
        /// <param name="route"></param>
        public RouteNotFoundException(string route)
            :base($"Route {route} not found.")
        { }
    }

    #endregion

    /// <summary>
    /// Class defining all the required actions for route-processing and error handling.
    /// /// </summary>
    public static class Route
    {
        static OnBefore before = null;
        /// <summary>
        /// Action executed before all route-methods.
        /// <para>It may be null.</para>
        /// </summary>
        public static OnBefore Before
        {
            get { return before; } set { before = value; }
        }

        static OnError error = null;
        /// <summary>
        /// Action executed if an error occurs.
        /// <para>By default it outputs exception message as text with an existing status code. In case of 200-299, 'BadRequest' is used.</para>
        /// </summary>
        public static OnError Error
        {
            get { return error; }
            set { error = value ?? throw new ArgumentException($"The {nameof(Error)} must have non-null value."); }
        }

        /// <summary>
        /// Gets or sets the route methods.
        /// </summary>
        static readonly List<(ShouldProcessFunc ShouldProcessFunc, HttpActionAsync Action)> Methods;

        static Route()
        {
            Methods = new List<(ShouldProcessFunc, HttpActionAsync)>();
            Error = (rq, rp, ex) =>
            {
                if(rp.StatusCode >= 200 && rp.StatusCode <= 299)
                    rp.StatusCode = (int)HttpStatusCode.BadRequest;

                rp.AsText(ex.Message, "text/plain");
            };
        }

        /// <summary>
        /// Entry function executed on the incoming HTTP request.
        /// </summary>
        /// <param name="request">HTTP request.</param>
        /// <param name="response">HTTP response.</param>
        /// <returns>Request processing task.</returns>
        public static async Task OnHttpRequestAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            //run the 'before' method
            try
            {
                var isHandled = Before?.Invoke(request, response);
                if (isHandled.HasValue && (bool)isHandled) return;
            }
            catch (Exception ex)
            {
                try { Error?.Invoke(request, response, ex); }
                catch { }
            }

            //select and run an action
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
                    await action(request, response, args);
                }
                catch (Exception ex)
                {
                    try { Error?.Invoke(request, response, ex); }
                    catch { }
                }

                return;
            }

            //handle if no route is selected
            try
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                Error?.Invoke(request, response, new RouteNotFoundException(request.Url.PathAndQuery));
            }
            catch { }
        }

        #region Add (pattern)

        /// <summary>
        /// Adds the specified action to the route collection.
        /// <para>The order of actions defines the priority.</para>
        /// </summary>
        /// <param name="pattern">
        /// String pattern optionally containing named arguments in {}. 
        /// <para>
        /// Example: "/page-{pageNumber}/". 'pageNumber' will be parsed and added to 'arguments' key-value pair collection.
        /// The last argument is parsed as greedy one.
        /// </para>
        /// </param>
        /// <param name="action">Action executed if the specified pattern matches the URL path.</param>
        /// <param name="method">HTTP method (GET, POST, DELETE, HEAD).</param>
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

        /// <summary>
        /// Adds the specified action to the route collection.
        /// <para>The order of actions defines the priority.</para>
        /// </summary>
        /// <param name="pattern">
        /// String pattern optionally containing named arguments in {}. 
        /// <para>
        /// Example: "/page-{pageNumber}/". 'pageNumber' will be parsed and added to 'arguments' key-value pair collection.
        /// The last argument is parsed as greedy one.
        /// </para>
        /// </param>
        /// <param name="action">Action executed if the specified pattern matches the URL path.</param>
        /// <param name="method">HTTP method (GET, POST, DELETE, HEAD).</param>
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

        #endregion

        #region Add

        /// <summary>
        /// Adds the specified action to the route collection.
        /// <para>The order of actions defines the priority.</para>
        /// </summary>
        /// <param name="shouldProcess">Function defining whether the specified action should be executed or not.</param>
        /// <param name="action">Action executed if the specified pattern matches the URL path.</param>
        public static void Add(ShouldProcessFunc shouldProcess, HttpActionAsync action)
        {
            Methods.Add((shouldProcess, action));
        }

        /// <summary>
        /// Adds the specified action to the route collection.
        /// <para>The order of actions defines the priority.</para>
        /// </summary>
        /// <param name="shouldProcess">Function defining whether the specified action should be executed or not.</param>
        /// <param name="action">Action executed if the specified pattern matches the URL path.</param>
        public static void Add(ShouldProcessFunc shouldProcess, HttpAction action)
        {
            Methods.Add((shouldProcess, (rq, rp, args) => 
            { 
                action(rq, rp, args);
                return Task.FromResult(true);
            }));
        }

        #endregion

        static void Deconstruct<T1, T2>(this KeyValuePair<T1, T2> tuple, out T1 key, out T2 value)
        {
            key = tuple.Key;
            value = tuple.Value;
        }
    }
}
