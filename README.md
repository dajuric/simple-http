<p align="center">
    <a href="https://www.nuget.org/profiles/dajuric"> <img src="Deploy/Logo/Logo-big.png" alt="SimpleHTTP logo" width="120" align="center"> </a>
</p>

<p align="center">
    <a href="https://www.nuget.org/packages/SimpleHTTP/"> <img src="https://img.shields.io/badge/SimpleHTTP-v1.x-green.svg?style=flat-square" alt="NuGet packages version"/>  </a>
</p>

**SimpleHTTP** - HTTP server for .NET    
Lightweight HTTP server for .NET written based on *System.Net.HttpListener*. Supports partial file streaming, file caching (ETag), simple templating, single-pass form parsing (no temp file).

<!--
 > **Tutorial:** <a href="https://www.codeproject.com/Articles/0000/Introducing-SimpleHTTP-library" target="_blank">CodeProject article</a>
-->


## Why SimpleHTTP ?

+ **Lightweight**   
No dependencies.

+ **Simple**   
There is only one relevant method **Route.Add** which associates a route with an action. 
Other methods are extensions on *HttpListenerRequest* and *HttpListenerRequest* classes.
Zero configuration.

 
## <a href="Samples/"> Sample</a>

The snippets below demonstrate the most common functionality. For a demonstration of the all functionalities check the sample.

 ``` csharp
//rq - request, rp -response, args - arguments
//Route.Add(...) serves "GET" requests by default

//1) serve file (supports video streaming)
Route.Add("/{file}", (rq, rp, args) => rp.AsFile(rq, args["file"]));

//2) return text with a cookie if a path matches a condition
Route.Add((rq, args)     => rq.Url.LocalPath.ToLower().EndsWith("helloworld"), 
           (rq, rp, args) => rp.WithCookie("myCookie", "myContent")
                               .AsText("Hello world!"));

//3) parse body (fields and files)
Route.Add("/myForm/", (rq, rp, args) => 
{
     var files = rq.ParseBody(args);

     //save files
     foreach (var f in files.Values)
        f.Save(f.FileName);

     //write form-fields
     foreach (var a in args)
        Console.WriteLine(a.Key + " " + a.Value);
}, 
"POST");

//run the server
Server.ListenAsync(8000, CancellationToken.None, Route.OnHttpRequestAsync)
       .Wait();
 ``` 
{empty} . 


<!--
## Getting started
+ Sample
+ <a href="https://www.codeproject.com/Articles/0000/Introducing-SimpleHTTP-library" target="_blank">CodeProject article</a>
-->

## How to Engage, Contribute and Provide Feedback  
Remember: Your opinion is important and will define the future roadmap.
+ questions, comments - Github
+ **spread the word** 

## Final word
If you like the project please **star it** in order to help to spread the word. That way you will make the framework more significant and in the same time you will motivate me to improve it, so the benefit is mutual.
