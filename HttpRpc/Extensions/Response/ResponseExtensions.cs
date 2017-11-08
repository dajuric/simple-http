using HeyRed.Mime;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace SimpleHttpRpc
{
    public static partial class ResponseExtensions
    {
        //partly according to: https://williambert.online/2013/06/allow-cors-with-localhost-in-chrome/
        public static HttpListenerResponse WithCORS(this HttpListenerResponse response)
        {
            response.WithHeader("Access-Control-Allow-Origin", "*");
            response.WithHeader("Access-Control-Allow-Headers", "Cache-Control, Pragma, Accept, Origin, Authorization, Content-Type, X-Requested-With");
            response.WithHeader("Access-Control-Allow-Methods", "GET, POST");
            response.WithHeader("Access-Control-Allow-Credentials", "true");

            return response;
        }


        public static HttpListenerResponse WithContentType(this HttpListenerResponse response, string contentType)
        {
            response.ContentType = contentType;
            return response;
        }

        public static HttpListenerResponse WithHeader(this HttpListenerResponse response, string name, string value)
        {
            response.Headers[name] = value;
            return response;
        }

        public static HttpListenerResponse WithCode(this HttpListenerResponse response, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            response.StatusCode = (int)statusCode;
            return response;
        }

        public static HttpListenerResponse WithCookie(this HttpListenerResponse response, string name, string value)
        {
            response.Cookies.Add(new Cookie(name, value));
            return response;
        }

        public static HttpListenerResponse WithCookie(this HttpListenerResponse response, string name, string value, DateTime expires)
        {
            response.Cookies.Add(new Cookie { Name = name, Value = value, Expires = expires });
            return response;
        }

        public static HttpListenerResponse WithCookie(this HttpListenerResponse response, Cookie cookie)
        {
            response.Cookies.Add(cookie);
            return response;
        }


        public static void AsText(this HttpListenerResponse response, string txt, string mime = "text/html")
        {
            var data = Encoding.ASCII.GetBytes(txt);

            response.ContentLength64 = data.Length;
            response.ContentType = mime;
            response.OutputStream.Write(data, 0, data.Length);
        }

        public static void AsFile(this HttpListenerResponse response, string filePath)
        {
            if (!File.Exists(filePath))
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                throw new FileNotFoundException(nameof(filePath));
            }

            var data = File.ReadAllBytes(filePath);
            var mime = MimeTypesMap.GetMimeType(Path.GetExtension(filePath));
            response.AsBytes(data, mime);
        }

        public static void AsBytes(this HttpListenerResponse response, byte[] data, string mime = "octet/stream")
        {
            response.ContentLength64 = data.Length;
            response.ContentType = mime;
            response.OutputStream.Write(data, 0, data.Length);
        }

        public static void AsRedirect(this HttpListenerResponse response, string url)
        {
            response.StatusCode = (int)HttpStatusCode.Redirect;
            response.RedirectLocation = url;
            response.Close();
        }
    }
}
