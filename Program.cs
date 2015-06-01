using System;

namespace GitMerger
{
    class Program
    {
        static void Main(string[] args)
        {
            string baseAddress = Startup.Start();
            Console.WriteLine("Listening at {0}", baseAddress);
            Console.WriteLine("Return to close");
            Console.ReadLine();
            Startup.Shutdown();
        }
    }
}
