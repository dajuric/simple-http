using HeyRed.Mime;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace SimpleHttpRpc
{
    public static partial class ResponseExtensions
    {
        public static void AsFile(this HttpListenerResponse response, HttpListenerRequest request, string fileName)
        {
            if (!File.Exists(fileName))
            {
                response.WithCode(HttpStatusCode.NotFound);
                throw new FileNotFoundException(nameof(fileName));
            }

            var sourceStream = File.OpenRead(fileName);
            fromStream(request, response, sourceStream, MimeTypesMap.GetMimeType(Path.GetExtension(fileName)));
        }

        public static void AsBytes(this HttpListenerResponse response, HttpListenerRequest request, byte[] data, string mime = "octet/stream")
        {
            if (data == null)
            {
                response.WithCode(HttpStatusCode.BadRequest);
                throw new ArgumentNullException(nameof(data));
            }

            var sourceStream = new MemoryStream(data);
            fromStream(request, response, sourceStream, mime);
        }

        public static void AsStream(this HttpListenerResponse response, HttpListenerRequest request, Stream stream, string mime = "octet/stream")
        {
            if (stream == null)
            {
                response.WithCode(HttpStatusCode.BadRequest);
                throw new ArgumentNullException(nameof(stream));
            }

            fromStream(request, response, stream, mime);
        }


        static void fromStream(HttpListenerRequest request, HttpListenerResponse response, Stream stream, string mime)
        {
            try
            {
                if (request.Headers.AllKeys.Contains("Range"))
                    fromPartialStream(request, response, stream, mime);
                else
                    fromEntireStream(response, stream, mime);
            }
            catch(Exception ex) when (ex is HttpListenerException) //request canceled
            {
                stream.Close();
                response.StatusCode = (int)HttpStatusCode.NoContent;
                response.Close();
            }
        }

        static void fromEntireStream(HttpListenerResponse response, Stream stream, string mime)
        {
            response.WithContentType(mime);
            response.ContentLength64 = stream.Length;

            copyStream(stream, response.OutputStream);
        }

        static void fromPartialStream(HttpListenerRequest request, HttpListenerResponse response, Stream stream, string mime)
        {
            const string BYTES_RANGE_HEADER = "Range";

            if (request.Headers.AllKeys.Count(x => x == BYTES_RANGE_HEADER) != 1)
                throw new NotSupportedException();

            var rangeStr = request.Headers[BYTES_RANGE_HEADER];
            var range = rangeStr.Replace("bytes=", String.Empty)
                                .Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(x => Int32.Parse(x))
                                .ToArray();

            var start = (range.Length > 0) ? range[0] : 0;
            var end = (range.Length > 1) ? range[1] : (int)(stream.Length - 1);

            response.WithContentType(mime)
                    .WithHeader("Accept-Ranges", "bytes")
                    .WithHeader("Content-Range", "bytes " + start + "-" + end + "/" + stream.Length)
                    .WithCode(HttpStatusCode.PartialContent);

            response.KeepAlive = true;
            response.ContentLength64 = (end - start + 1);
            copyStream(stream, response.OutputStream, start, end);
        }


        static void copyStream(Stream source, Stream destination, long start = 0, long end = -1, int bufferLength = 64 * 1024)
        {
            start = Math.Max(0, start);
            end = (end < 0) ? source.Length: end;

            source.Position = start;
            var toRead = end - start + 1;

            var buffer = new byte[bufferLength];
            var read = 0;

            while ((read = source.Read(buffer, 0, buffer.Length)) > 0 && toRead > 0)
            {
                destination.Write(buffer, 0, read);
                toRead -= read;
            }
        }
    }
}
