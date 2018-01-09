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
            Route.Before = (rq, rp) => { Console.WriteLine($"Requested: {rq.Url.PathAndQuery}"); return false; };

            //home page
            Route.Add("/", (rq, rp, args) => 
            {
                rp.AsFile(rq, Path.Combine(webDir, "Index.html"));
            });

            //1) URL parsing demo
            Route.Add("/{action}/{paramA}-{paramB}", (rq, rp, args) =>
            {
                var txt = Templating.RenderFile(Path.Combine(webDir, "UrlParsingResponse.thtml"), args); //populate template
                rp.AsText(txt);
            });

            //2) serve file (and video streaming)
            Route.Add((rq, args) => 
            {
                args["file"] = Path.Combine(webDir, rq.Url.LocalPath.TrimStart('/'));
                return Path.HasExtension(args["file"]);
            }, 
            (rq, rp, args) => rp.AsFile(rq, args["file"]));

            //3) form parsing demo
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

            //4) handle exception demo
            Route.Add("/handleException/", (rq, rp, args) =>
            {
                //to override/stylize default error response define a custom error function: Route.Error = (rq, rp, ex) => { };
                throw new NotImplementedException("My not implemented exception.");
            });


            //------------------- start server -------------------           
            var port = 4443;
            Console.WriteLine("Running HTTP server on: " + port);

            var cts = new CancellationTokenSource();
            var ts = HttpServer.ListenAsync(port, cts.Token, Route.OnHttpRequestAsync, useHttps: true);
            AppExit.WaitFor(cts, ts);     
        }
    }
}
