using System;
using System.Threading;
using SimpleHttp;

namespace Demo
{
    class Program
    {
        static void Main()
        {
            Route.Add("/", (rq, rp, args) => 
            {
                rp.AsFile(rq, "./Site/Index.html");
            });

            Route.Add("/uploadFile/", (rq, rp, args) =>
            {
                var files = rq.ParseBody(args);

                foreach (var f in files.Values)
                    f.Save("./" + f.FileName); 
            },
            "POST");

            try
            {
                HttpServer.ListenAsync(8000, CancellationToken.None, Route.OnHttpRequestAsync).Wait();
            }
            catch (Exception ex)
            {
                if (ex is AggregateException) ex = ex.InnerException;
                Console.WriteLine(ex.Message);
            }
        }
    }
}
