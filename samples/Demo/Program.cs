using System;
using System.Threading;
using SimpleHttp;
using System.IO;

namespace Demo
{
    class Program
    {
        static void Main()
        {
            string currDir = Directory.GetCurrentDirectory();

            //description
            Route.Add("/", (rq, rp, args) => 
            {
                rp.AsText($"Serving: {currDir}");
            });

            //serve file
            Route.Add((rq, args) => 
            {
                args["file"] = Path.Combine(currDir, rq.Url.LocalPath.TrimStart('/'));
                return Path.HasExtension(args["file"]);
            }, 
            (rq, rp, args) => rp.AsFile(rq, args["file"]));

            //upload file
            Route.Add("/uploadFile/", (rq, rp, args) =>
            {
                var files = rq.ParseBody(args);

                foreach (var f in files.Values)
                    f.Save("./" + f.FileName); 
            },
            "POST");

            try
            {
                var port = 8000;
                Console.WriteLine("Running HTTP server on: " + port);
                HttpServer.ListenAsync(port, CancellationToken.None, Route.OnHttpRequestAsync).Wait();
            }
            catch (Exception ex)
            {
                if (ex is AggregateException) ex = ex.InnerException;
                Console.WriteLine(ex.Message);
            }
        }
    }
}
