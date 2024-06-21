using System;
using System.Reactive.Linq;

namespace NewsAPI
{
    class Program
    {
        static void Main()
        {
            var source = Observable.Interval(TimeSpan.FromSeconds(1));

            using (source.Subscribe(x => Console.WriteLine($"Received: {x}")))
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }

}
