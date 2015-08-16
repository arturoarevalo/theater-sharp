namespace Theater
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using SystemActors;
    using SystemMessages;

    public class ActorSystem
    {
        protected IDictionary <string, ActorReference> ActorReferences = new Dictionary <string, ActorReference> ();
        protected ActorSystemConfiguration Configuration;
        protected ConcurrentPriorityQueue <Envelope> EnvelopeQueue = new ConcurrentPriorityQueue <Envelope> ();
        protected IDictionary <string, Mailbox> Mailboxes = new Dictionary <string, Mailbox> ();
        protected IList <ActorReference> Processors = new List <ActorReference> ();
        protected SpinLock Spin = new SpinLock ();

        public ActorSystem (ActorSystemConfiguration configuration)
        {
            Configuration = configuration;
            DeadLetter = ActorOf <DeadLetterActor> ();

            for (var i = 0; i < configuration.NumberOfThreads; i++)
            {
                Processors.Add (ActorOf <EnvelopeProcessor> ($"EnvelopeProcessor@{i}", i));
            }
        }

        public ActorSystem () : this (new ActorSystemConfiguration ())
        {
        }

        public ActorReference DeadLetter { get; set; }

        public void Shutdown ()
        {
            foreach (var processor in Processors)
            {
                processor.Tell (new EnvelopeProcessorShutdownMessage
                                {
                                    Cause = @"ActorSystem requested shutdown"
                                }, null, Priorities.RealTime);
            }
        }

        public void Tell (ActorReference target, object message, ActorReference sender = null, Priorities priority = Priorities.Normal)
        {
            var envelope = new Envelope
                           {
                               Message = message,
                               Sender = sender,
                               Target = target
                           };

            EnvelopeQueue.Enqueue (envelope, priority);
        }

        public Task <object> Ask (ActorReference target, object message, ActorReference sender, Priorities priority = Priorities.Normal)
        {
            if (sender == target)
            {
                throw new Exception ("An actor cannot Ask to itself.");
            }

            var envelope = new Envelope
                           {
                               Message = message,
                               Sender = sender,
                               Target = target,
                               TaskCompletionSource = new TaskCompletionSource <object> ()
                           };

            sender.PendingReplies++;

            EnvelopeQueue.Enqueue (envelope, priority);

            return envelope.TaskCompletionSource.Task;
        }

        public ActorReference ActorOf <T> (string name, object initializationData, Mailbox mailbox) where T : Actor, new ()
        {
            var reference = CreateActorReference (name, mailbox);

            Actor actor = new T ();
            actor.Self = reference;

            reference.State = ActorStates.Initializing;
            actor.Initialize (initializationData);

            var lockTaken = false;
            try
            {
                Spin.Enter (ref lockTaken);

                reference.State = ActorStates.Idle;
                mailbox.EnqueueActor (actor);
            }
            finally
            {
                if (lockTaken)
                {
                    Spin.Exit (false);
                }
            }

            return reference;
        }

        public ActorReference ActorOf <T> (string name = null, object initializationData = null) where T : Actor, new ()
        {
            var normalizedName = NormalizeName <T> (name);

            return ActorOf <T> (normalizedName, initializationData, CreateMailbox (normalizedName));
        }

        public ActorReference ClusterOf <T> (string name = null, object initializationData = null) where T : Actor, new ()
        {
            var normalizedName = NormalizeName <T> (name);

            var mailbox = CreateMailbox (normalizedName);
            var reference = CreateActorReference (normalizedName, mailbox);

            for (var i = 0; i < Configuration.NumberOfThreads; i++)
            {
                ActorOf <T> ($"{normalizedName}@{i}", initializationData, mailbox);
            }

            return reference;
        }

        protected string NormalizeName <T> (string name)
        {
            if (string.IsNullOrEmpty (name))
            {
                name = typeof (T).Name;
            }

            return name;
        }

        protected Mailbox CreateMailbox (string name)
        {
            var lockTaken = false;
            try
            {
                Spin.Enter (ref lockTaken);

                if (Mailboxes.ContainsKey (name))
                {
                    throw new Exception ("There's already a Mailbox with this name. Possible duplicate actor.");
                }

                var mailbox = new Mailbox ();
                Mailboxes.Add (name, mailbox);

                return mailbox;
            }
            finally
            {
                if (lockTaken)
                {
                    Spin.Exit (false);
                }
            }
        }

        protected ActorReference CreateActorReference (string name, Mailbox mailbox)
        {
            var lockTaken = false;
            try
            {
                Spin.Enter (ref lockTaken);

                ActorReference reference;
                if (!ActorReferences.TryGetValue (name, out reference))
                {
                    reference = new ActorReference
                                {
                                    Name = name,
                                    ActorSystem = this,
                                    Mailbox = mailbox
                                };

                    ActorReferences.Add (name, reference);
                }
                else
                {
                    reference.Mailbox = mailbox;
                }

                return reference;
            }
            finally
            {
                if (lockTaken)
                {
                    Spin.Exit (false);
                }
            }
        }

        public ActorReference FindActor (string name)
        {
            var lockTaken = false;
            try
            {
                Spin.Enter (ref lockTaken);
                ActorReference reference;

                if (ActorReferences.TryGetValue (name, out reference))
                {
                    return reference;
                }
                else
                {
                    reference = new ActorReference
                                {
                                    Name = name,
                                    ActorSystem = this
                                };

                    ActorReferences.Add (name, reference);

                    return reference;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Spin.Exit (false);
                }
            }
        }

        public bool DequeueEnvelopeAndMatchingActor (uint processorAffinity, out Envelope envelope, out Actor actor)
        {
            var lockTaken = false;
            try
            {
                Spin.Enter (ref lockTaken);

                envelope = EnvelopeQueue.Dequeue (item => ((item.Target.ProcessorAffinity & processorAffinity) != 0)
                                                          && ((item.Message is IInternalMessage) || item.Target.Mailbox.Available));

                if (envelope != null)
                {
                    actor = (envelope.Message is IInternalMessage)
                        ? null
                        : envelope.Target.Mailbox.DequeueActor ();

                    return true;
                }
                else
                {
                    actor = null;
                    return false;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Spin.Exit (false);
                }
            }
        }

        public void EnqueueActorBackToMailbox (Mailbox mailbox, Actor actor)
        {
            var lockTaken = false;
            try
            {
                Spin.Enter (ref lockTaken);

                mailbox.EnqueueActor (actor);
            }
            finally
            {
                if (lockTaken)
                {
                    Spin.Exit (false);
                }
            }
        }
    }
}