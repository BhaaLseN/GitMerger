﻿using System.ServiceProcess;

namespace GitMerger
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase.Run(new ServiceBase[] 
            { 
                new GitMergerService() 
            });
        }
    }
}
