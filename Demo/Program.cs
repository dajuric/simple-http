using System;
using System.Threading;
using SimpleHttpRpc;

namespace Demo
{
    class Program
    {
        static void Main()
        {
            HttpRPC.Add("/", (rq, rp, args) => 
            {
                rp.AsFile("./Site/Index.html");
            });

            HttpRPC.Add("/uploadFile/", (rq, rp, args) =>
            {
                var files = rq.ParseBody(args);

                foreach (var f in files.Values)
                    f.Save("./" + f.FileName); 
            },
            "POST");

            HttpServer.ListenAsync("http://localhost:8000/", CancellationToken.None, HttpRPC.OnHttpRequest).Wait();
        }
    }
}
