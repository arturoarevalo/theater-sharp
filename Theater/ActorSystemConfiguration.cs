namespace Theater
{
    using System;

    public class ActorSystemConfiguration
    {
        public ActorSystemConfiguration ()
        {
            NumberOfThreads = Environment.ProcessorCount;
        }

        public int NumberOfThreads { get; set; }
    }
}