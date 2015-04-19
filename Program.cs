using System;
using System.Web.Http;
using Microsoft.Owin.Hosting;

namespace GitMerger
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = "http://localhost:12345/test";
            using (WebApp.Start<Startup>(baseAddress))
            {
                Console.WriteLine("Return to close");
                Console.ReadLine();
            }
        }
    }
}
