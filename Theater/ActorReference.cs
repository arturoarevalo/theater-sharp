namespace Theater
{
    using System.Threading.Tasks;

    public class ActorReference
    {
        public ActorReference ()
        {
            State = ActorStates.Creating;
            PendingReplies = 0;
            ProcessorAffinity = 0xFFFFFFFF;
        }

        public string Name { get; set; }
        public ActorStates State { get; set; }
        public ActorSystem ActorSystem { get; set; }
        public Mailbox Mailbox { get; set; }
        public int PendingReplies { get; set; }
        public uint ProcessorAffinity { get; set; }

        public void Tell (object message, ActorReference source = null, Priorities priority = Priorities.Normal)
        {
            ActorSystem.Tell (this, message, source, priority);
        }

        public void Forward (ExecutionContext context)
        {
            ActorSystem.Tell (this, context.Message, context.Sender);
        }

        public Task <object> Ask (object message, ActorReference source, Priorities priority = Priorities.Normal)
        {
            return ActorSystem.Ask (this, message, source, priority);
        }
    }
}