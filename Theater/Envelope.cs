namespace Theater
{
    using System.Threading.Tasks;

    /// <summary>
    ///     Encapsulates a message being sent to a target actor from a sender.
    /// </summary>
    public class Envelope
    {
        /// <summary>
        ///     The sender actor reference.
        /// </summary>
        public ActorReference Sender { get; set; }

        /// <summary>
        ///     The target actor reference.
        /// </summary>
        public ActorReference Target { get; set; }

        /// <summary>
        ///     The TaskCompletionSource that must be executed if the message has been sent waiting for a reply.
        /// </summary>
        public TaskCompletionSource <object> TaskCompletionSource { get; set; }

        /// <summary>
        ///     The message being sent.
        /// </summary>
        public object Message { get; set; }
    }
}