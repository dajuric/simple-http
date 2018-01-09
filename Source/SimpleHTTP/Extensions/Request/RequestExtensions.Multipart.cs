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
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleHttp
{
    /// <summary>
    /// Delegate executed when a file is about to be read from a body stream.
    /// </summary>
    /// <param name="fieldName">Field name.</param>
    /// <param name="fileName">name of the file.</param>
    /// <param name="contentType">Content type.</param>
    /// <returns>Stream to be populated.</returns>
    public delegate Stream OnFile(string fieldName, string fileName, string contentType);

    static partial class RequestExtensions
    {
        static Dictionary<string, HttpFile> ParseMultipartForm(HttpListenerRequest request, Dictionary<string, string> args, OnFile onFile)
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
                var (n, v, fn, ct) = parseSection(inputStream, "\r\n" + boundary, onFile);
                if (String.IsNullOrEmpty(n)) break;

                v.Position = 0;
                if (!String.IsNullOrEmpty(fn))
                    files.Add(n, new HttpFile(fn, v, ct));
                else
                    args.Add(n, readAsString(v));
            }

            return files;
        }

        private static (string Name, Stream Value, string FileName, string ContentType) parseSection(Stream source, string boundary, OnFile onFile)
        {
            var (n, fn, ct) = readContentDisposition(source);
            source.ReadByte(); source.ReadByte(); //\r\n (empty row)

            var dst = String.IsNullOrEmpty(fn) ? new MemoryStream() : onFile(n, fn, ct);
            if (dst == null)
                throw new ArgumentException(nameof(onFile), "The on-file callback must return a stream.");

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

    /// <summary>
    /// HTTP file data container.
    /// </summary>
    public class HttpFile: IDisposable
    {
        /// <summary>
        /// Creates new HTTP file data container.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <param name="value">Data.</param>
        /// <param name="contentType">Content type.</param>
        internal HttpFile(string fileName, Stream value, string contentType)
        {
            Value = value;
            FileName = fileName;
            ContentType = contentType;
        }

        /// <summary>
        /// Gets the name of the file.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Gets the data.
        /// <para>If a stream is created <see cref="OnFile"/> it will be closed when this HttpFile object is disposed.</para>
        /// </summary>
        public Stream Value { get; private set; }

        /// <summary>
        /// Content type.
        /// </summary>
        public string ContentType { get; private set; }

        /// <summary>
        /// Saves the data into a file.
        /// <para>Directory path will be auto created if does not exists.</para>
        /// </summary>
        /// <param name="fileName">File path with name.</param>
        /// <param name="overwrite">True to overwrite the existing file, false otherwise.</param>
        /// <returns>True if the file is saved/overwritten, false otherwise.</returns>
        public bool Save(string fileName, bool overwrite = false)
        {
            if (File.Exists(Path.GetFullPath(fileName)))
                return false;

            var dir = Path.GetDirectoryName(Path.GetFullPath(fileName));
            Directory.CreateDirectory(dir);

            Value.Position = 0;
            using (var outStream = File.OpenWrite(fileName))
                Value.CopyTo(outStream);

            return true;
        }

        /// <summary>
        /// Disposes the current instance.
        /// </summary>
        public void Dispose()
        {
            if (Value != null)
            {
                Value?.Dispose();
                Value = null;
            }
        }

        /// <summary>
        /// Disposes the current instance.
        /// </summary>
        ~HttpFile()
        {
            Dispose();
        }
    }
}
