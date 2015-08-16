using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PingPong
{
    using Theater;

    class TestActor : StringTypedActor
    {
        public void OnReceiveHello ()
        {
            Log ("Received Hello");
        }

        public void OnReceiveGoodbye ()
        {
            Log ("Received Goodbye");
        }
    }


    public class AsyncPing : Actor
    {
        private ActorReference pong;

        public override void Initialize (object initializationData)
        {
            pong = ActorSystem.FindActor ("AsyncPong");
        }

        public override async Task ProcessMessage ()
        {
            Log ("Ping Received");
            Log ("Asking to Pong");
            var tsk1 = pong.Ask (null, Self);
            var tsk2 = pong.Ask (null, Self);
            Log ("Waiting Pong's reply");

            //Task.WaitAll (tsk1, tsk2);
            //Task.WaitAll (tsk1);

            await Task.WhenAll (tsk1, tsk2);

            Log ("Got Pong's first reply: " + await tsk1);
            Log ("Got Pong's second reply: " + await tsk2);

            Log ("Waiting two seconds to reschedule a self ping ...");
            Thread.Sleep (2000);
            //Self.Tell (null);
        }
    }

    public class AsyncPong : Actor
    {
        private int count;

        public override async Task ProcessMessage ()
        {
            Log ("Pong received");
            Log ("Calculating response ... should take two seconds ...");
            Thread.Sleep (2000);
            Log ("Replying ...");
            Reply ("hello world! (" + (++count) + ")");
            //Thread.Sleep (2000);
            Log ("Pong finished");
        }
    }


    class Program
    {

        static void Test1 (ActorSystem system)
        {
            var actor = system.ActorOf<TestActor> ();
            system.DeadLetter = actor;
            actor.Tell ("Hello");
            actor.Tell ("Goodbye");
            actor.Tell ("Test");
        }

        private static void Test2 (ActorSystem system)
        {
            var ping = system.ActorOf <AsyncPing> ();
            var pong = system.ClusterOf <AsyncPong> ();

            ping.Tell (null);
        }

        static void Main (string [] args)
        {
            var system = new ActorSystem ();

            Test2 (system);

            Console.ReadKey ();
            system.Shutdown ();
            Console.ReadKey ();
        }
    }
}
