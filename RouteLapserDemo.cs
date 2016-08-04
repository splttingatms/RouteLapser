using System;
using System.Diagnostics;

namespace RouteVideoPreviewer
{
    class RouteLapserDemo
    {
        static void Main(string[] args)
        {
            Trace.Listeners.Add(new ConsoleTraceListener());

            var lapser = new RouteLapser("UW_to_Gasworks.tcx");
            lapser.CreateHyperlapse("UW_to_Gasworks_hyperlapse.mp4").Wait();

            Console.WriteLine("Done!");
            Console.ReadLine();
        }
    }
}
