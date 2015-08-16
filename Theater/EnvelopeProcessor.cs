namespace Theater
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using SystemMessages;

    public class EnvelopeProcessor : TypedActor
    {
        private int id;
        private bool shutdownRequested;
        private Thread workerThread;

        public override void Initialize (object initializationData)
        {
            id = (int) initializationData;
            shutdownRequested = false;

            Self.ProcessorAffinity = (uint) 1 << id;

            workerThread = new Thread (RunLoop)
                           {
                               IsBackground = false
                           };
            workerThread.Start ();
        }

        public void OnReceive (EnvelopeProcessorShutdownMessage message)
        {
            Log ("Shutting down");
            shutdownRequested = true;
        }

        private void RunLoop ()
        {
            // Make sure this thread maps to an underlying OS thead.
            Thread.BeginThreadAffinity ();

            try
            {
                // Force this thread to run on a single processor.
                CurrentThread.ProcessorAffinity = new IntPtr (Self.ProcessorAffinity);

                while (shutdownRequested == false)
                {
                    Envelope envelope;
                    Actor actor;

                    if (ActorSystem.DequeueEnvelopeAndMatchingActor (Self.ProcessorAffinity, out envelope, out actor))
                    {
                        if (envelope.Message is IInternalMessage)
                        {
                            ProcessInternalMessage (envelope);
                        }
                        else
                        {
                            ProcessStandardMessage (envelope, actor);
                        }
                    }
                    else
                    {
                        // TODO: We shouldn't be sleeping in a real-time application ... use a semaphore instead.
                        Thread.Sleep (1);
                    }
                }
            }
            finally
            {
                // Not necessary ... just for sanity.
                Thread.EndThreadAffinity ();
            }
        }

        private async void ProcessStandardMessage (Envelope envelope, Actor actor)
        {
            Debug.Assert (envelope != null);
            Debug.Assert (actor != null);

            // We're processing an standard message.
            try
            {
                actor.ExecutionContext = new ExecutionContext
                                         {
                                             EnvelopeProcessor = this,
                                             Message = envelope.Message,
                                             Sender = envelope.Sender,
                                             TaskCompletionSource = envelope.TaskCompletionSource
                                         };

                actor.Self.State = ActorStates.Processing;
                await actor.ProcessMessage ();

                // Sanity checks.
                // When we finish processing a message there should be no pending replies.
                if (actor.Self.PendingReplies != 0)
                {
                    throw new Exception ($"Actor {actor} has {actor.Self.PendingReplies} pending replies after finishing processing message [{envelope.Message}]");
                }

                if (actor.ExecutionContext.TaskCompletionSource != null)
                {
                    throw new Exception($"Actor {actor.Self} has not replied to {actor.ExecutionContext.Sender} after processing message [{envelope.Message}]");
                }
            }
            finally
            {
                // Make sure that we clear the execution context and move back the actor to its mailbox queue.
                actor.ExecutionContext = null;
                actor.Self.State = ActorStates.Idle;

                ActorSystem.EnqueueActorBackToMailbox (envelope.Target.Mailbox, actor);
            }
        }

        private void ProcessInternalMessage (Envelope envelope)
        {
            Debug.Assert (envelope != null);

            // Replies messages 
            var message = envelope.Message as FinishAsyncResponseMessage;
            if (message != null)
            {
                envelope.Target.PendingReplies--;
                message.FinishReplyProcess ();
            }
        }

        #region O/S Thread Management Code

        private static ProcessThread CurrentThread
        {
            get
            {
                var id = GetCurrentThreadId ();
                return (from ProcessThread th in Process.GetCurrentProcess ().Threads
                        where th.Id == id
                        select th).Single ();
            }
        }

        [DllImport ("kernel32.dll")]
        private static extern int GetCurrentThreadId ();

        #endregion
    }
}