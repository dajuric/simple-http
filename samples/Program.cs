using System;
using System.Threading;
using SimpleHttp;
using System.IO;
using System.Linq;

namespace Demo
{
    class Program
    {
        static void Main()
        {
            string webDir = Path.Combine(Directory.GetCurrentDirectory(), "Site");

            //------------------- define routes -------------------
            Route.Before = (rq, rp, args) => { Console.WriteLine($"Requested: {rq.Url.PathAndQuery}"); return false; };

            //home page
            Route.Add("/", (rq, rp, args) => 
            {
                rp.AsFile(rq, Path.Combine(webDir, "Index.html"));
            });

            //serve file (and video streaming)
            Route.Add((rq, args) => 
            {
                args["file"] = Path.Combine(webDir, rq.Url.LocalPath.TrimStart('/'));
                return Path.HasExtension(args["file"]);
            }, 
            (rq, rp, args) => rp.AsFile(rq, args["file"]));

            //form parsing demo
            Route.Add("/upload/", (rq, rp, args) =>
            {
                var files = rq.ParseBody(args);

                foreach (var f in files.Values)
                    f.Save(Path.Combine(webDir, f.FileName), true);


                var txtRp = "Form fields: " + String.Join(";  ", args.Select(x => $"'{x.Key}: {x.Value}'")) + "\n" +
                            "Files:       " + String.Join(";  ", files.Select(x => $"'{x.Key}: {x.Value.FileName}, {x.Value.ContentType}'"));

                rp.AsText($"<pre>{txtRp}</pre>");
            },
            "POST");

            //URL parsing demo
            Route.Add("/{action}/{paramA}-{paramB}", (rq, rp, args) =>
            {
                rp.AsText($"<pre>"+
                          $"Parsed: \n" +
                          $"   action: {args["action"]} \n" +
                          $"   paramA: {args["paramA"]} \n" +
                          $"   paramB: {args["paramB"]} \n" + 
                          $"</pre>");
            });


            //------------------- start server -------------------           
            var port = 8001;
            Console.WriteLine("Running HTTP server on: " + port);

            var cts = new CancellationTokenSource();
            var ts = HttpServer.ListenAsync($"http://localhost:{port}/", cts.Token, Route.OnHttpRequestAsync);
            AppExit.WaitFor(cts, ts);           
        }
    }
}
