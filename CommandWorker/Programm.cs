using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommandWorker
{
    class Program
    {
        static void Main(string[] args)
        {
            // Use 1, 0.002 "ajust if do nothing", 0.000005
            double _time = 1;

            CommandWorker myCommandWorker = new CommandWorker("MainApp");

            myCommandWorker.Enqueue(() => { Console.WriteLine("command1"); });
            myCommandWorker.Enqueue(() => { Console.WriteLine("command2"); });
            myCommandWorker.AddHeadAction(() => { Console.WriteLine("command3"); });
            myCommandWorker.Enqueue(() => { Console.WriteLine("command4"); });
            myCommandWorker.Enqueue(() => { Console.WriteLine("command5"); });
            myCommandWorker.AddHeadAction(() => { Console.WriteLine("command6"); });
            myCommandWorker.Enqueue(() => { Console.WriteLine("command7"); });
            myCommandWorker.Enqueue(() => { Console.WriteLine("command8"); });

            myCommandWorker.Start();

            myCommandWorker.Enqueue(() => { Console.WriteLine("command9"); });
            myCommandWorker.Enqueue(() => { Console.WriteLine("command10"); });
            myCommandWorker.AddHeadAction(() => { Console.WriteLine("command11"); });
            myCommandWorker.Enqueue(() => { Console.WriteLine("command12"); });

            // Wait to let time for command worker to exec stuff
            Task.Delay(TimeSpan.FromSeconds(_time)).Wait();

            myCommandWorker.Enqueue(() => { Console.WriteLine("command13"); });
            myCommandWorker.Enqueue(() => { Console.WriteLine("command14"); });
            myCommandWorker.AddHeadAction(() => { Console.WriteLine("command15"); });
            myCommandWorker.Enqueue(() => { Console.WriteLine("command16"); });

            // Wait to let time for command worker to exec stuff
            Task.Delay(TimeSpan.FromSeconds(_time)).Wait();

            myCommandWorker.Stop();

            myCommandWorker.Enqueue(() => { Console.WriteLine("command17"); });
            myCommandWorker.Enqueue(() => { Console.WriteLine("command18"); });
            myCommandWorker.Enqueue(() => { Console.WriteLine("command19"); });
            myCommandWorker.Enqueue(() => { Console.WriteLine("command20"); });
            myCommandWorker.AddHeadAction(() => { Console.WriteLine("command21"); });

            // Wait to let time for command worker to exec stuff
            Task.Delay(TimeSpan.FromSeconds(_time)).Wait();

            myCommandWorker.Start();

            // Wait to let time for command worker to exec stuff
            Task.Delay(TimeSpan.FromSeconds(3)).Wait();

            // Wait for user
            Console.ReadKey();
        }
    }
}
