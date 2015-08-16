namespace Theater.SystemActors
{
    using System.Threading.Tasks;

    public class DeadLetterActor : Actor
    {
        public override async Task ProcessMessage ()
        {
            Log ($"Unhandled message [{ExecutionContext.Message}]");
        }
    }
}