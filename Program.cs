using System;
using Microsoft.Owin.Hosting;

namespace GitMerger
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = Startup.HostSettings.BaseAddress;
            using (WebApp.Start<Startup>(baseAddress))
            {
                Console.WriteLine("Listening at {0}", baseAddress);
                Console.WriteLine("Return to close");
                Console.ReadLine();
            }
            Startup.Shutdown();
        }
    }
}
