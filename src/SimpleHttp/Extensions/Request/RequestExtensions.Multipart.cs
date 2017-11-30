using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleHttp
{
    static partial class RequestExtensions
    {
        static Dictionary<string, HttpFile>ParseMultipartForm(HttpListenerRequest request, Dictionary<string, string> args)
        {
            if (request.ContentType.StartsWith("multipart/form-data") == false)
                throw new InvalidDataException("Not 'multipart/form-data'.");

            var boundary = Regex.Match(request.ContentType, "boundary=(.+)").Groups[1].Value;
            boundary = "--" + boundary;


            var files = new Dictionary<string, HttpFile>();
            var inputStream = new BufferedStream(request.InputStream);

            parseUntillBoundaryEnd(inputStream, new MemoryStream(), boundary);
            while(true)
            {
                var (n, v, fn, ct) = parseSection(inputStream, "\r\n" + boundary);
                if (String.IsNullOrEmpty(n)) break;

                v.Position = 0;
                if (!String.IsNullOrEmpty(fn))
                    files.Add(n, new HttpFile(fn, v, ct));
                else
                    args.Add(n, readAsString(v));
            }

            return files;
        }

        private static (string Name, Stream Value, 
                        string FileName, string ContentType) 
            parseSection(Stream source, string boundary)
        {
            var (n, fn, ct) = readContentDisposition(source);
            source.ReadByte(); source.ReadByte(); //\r\n (empty row)

            var dst = new MemoryStream();
            parseUntillBoundaryEnd(source, dst, boundary);

            return (n, dst, fn, ct);
        }

        private static (string Name, string FileName, string ContentType) readContentDisposition(Stream stream)
        {
            const string UTF_FNAME = "utf-8''";

            var l = readLine(stream);
            if (String.IsNullOrEmpty(l))
                return (null, null, null);

            //(regex matches are taken from NancyFX) and modified
            var n = Regex.Match(l, @"name=""?(?<n>[^\""]*)").Groups["n"].Value;
            var f = Regex.Match(l, @"filename\*?=""?(?<f>[^\"";]*)").Groups["f"]?.Value;

            string cType = null;
            if (!String.IsNullOrEmpty(f))
            {
                if (f.StartsWith(UTF_FNAME))
                    f = Uri.UnescapeDataString(f.Substring(UTF_FNAME.Length));

                l = readLine(stream);
                cType = Regex.Match(l, "Content-Type: (?<cType>.+)").Groups["cType"].Value;
            }

            return (n, f, cType);
        }

        private static void parseUntillBoundaryEnd(Stream source, Stream destination, string boundary)
        {
            var checkBuffer = new byte[boundary.Length]; //for boundary checking

            int b, i = 0;
            while ((b = source.ReadByte()) != -1)
            {
                if (i == boundary.Length) //boundary found -> go to the end of line
                {
                    if (b == '\n') break;
                    continue;
                }

                if (b == boundary[i]) //start filling the check buffer
                {
                    checkBuffer[i] = (byte)b;
                    i++;
                }
                else
                {
                    var idx = 0;
                    while (idx < i) //write the buffer data to stream
                    {
                        destination.WriteByte(checkBuffer[idx]);
                        idx++;
                    }

                    i = 0;
                    destination.WriteByte((byte)b); //write the current byte
                }
            }
        }

        private static string readLine(Stream stream)
        {
            var sb = new StringBuilder();

            int b;
            while ((b = stream.ReadByte()) != -1 && b != '\n')
                sb.Append((char)b);

            if (sb.Length > 0 && sb[sb.Length - 1] == '\r')
                sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }

        private static string readAsString(Stream stream)
        {
            var sb = new StringBuilder();

            int b;
            while ((b = stream.ReadByte()) != -1)
                sb.Append((char)b);

            return sb.ToString();
        }
    }


    public class HttpFile
    {
        public HttpFile(string fileName, Stream value, string contentType)
        {
            Value = value;
            FileName = fileName;
            ContentType = contentType;
        }

        public string FileName { get; private set; }

        public Stream Value { get; private set; }

        public string ContentType { get; private set; }

        public void Save(string fileName)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(fileName));
            Directory.CreateDirectory(dir);

            Value.Position = 0;
            using (var outStream = File.OpenWrite(fileName))
                Value.CopyTo(outStream);
        }
    }
}
