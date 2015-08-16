namespace Theater
{
    using System.Threading.Tasks;

    public abstract class UntypedActor : Actor
    {
        public override async Task ProcessMessage ()
        {
            OnReceive (ExecutionContext.Message);
        }

        public abstract void OnReceive (object message);
    }
}