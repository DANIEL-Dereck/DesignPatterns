using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FineStateMachine
{
    class Program
    {
        static void Main(string[] args)
        {
            NPCControl controlBot1 = new NPCControl("bot1");

            controlBot1.Start();

            controlBot1.SetTransition(Transition.SawPlayer);
            controlBot1.FixedUpdate();

            controlBot1.SetTransition(Transition.LostPlayer);
            controlBot1.FixedUpdate();

            Console.ReadKey();
        }
    }
}
