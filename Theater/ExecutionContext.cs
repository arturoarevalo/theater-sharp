namespace Theater
{
    using System.Threading.Tasks;

    /// <summary>
    ///     Encapsulates all the information needed by an actor to execute a message.
    /// </summary>
    public class ExecutionContext
    {
        /// <summary>
        ///     The EnvelopeProcessor that is executing the message.
        /// </summary>
        public EnvelopeProcessor EnvelopeProcessor { get; set; }

        /// <summary>
        ///     The message being executed.
        /// </summary>
        public object Message { get; set; }

        /// <summary>
        ///     The actor who sent the message.
        /// </summary>
        public ActorReference Sender { get; set; }

        /// <summary>
        ///     The TaskCompletionSource we must notify if this message requires a response.
        /// </summary>
        public TaskCompletionSource <object> TaskCompletionSource { get; set; }
    }
}