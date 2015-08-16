namespace Theater
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using SystemMessages;

    public abstract class Actor
    {
        public ActorReference Self { get; set; }
        public ExecutionContext ExecutionContext { get; set; }
        protected ActorSystem ActorSystem => Self.ActorSystem;

        public virtual void Initialize (object initializationData)
        {
            // Nothing to do here.
            // Override this method if an actor needs to perform some kind of initialization just after its creation.
        }

        public abstract Task ProcessMessage ();

        public void Reply (object message)
        {
            if (ExecutionContext.TaskCompletionSource == null)
            {
                throw new Exception ($"Actor {Self} tried to reply a message [{ExecutionContext.Message}] it has not been asked for");
            }

            var tcs = ExecutionContext.TaskCompletionSource;
            ExecutionContext.TaskCompletionSource = null;

            ExecutionContext.Sender.Tell (new FinishAsyncResponseMessage
                                          {
                                              Result = message,
                                              TaskCompletionSource = tcs
                                          }, Self, Priorities.RealTime);
        }

        protected void Log (string message)
        {
            Console.WriteLine ("Actor {0} in Thread {1}: {2}", GetType ().Name, Thread.CurrentThread.ManagedThreadId, message);
        }
    }
}