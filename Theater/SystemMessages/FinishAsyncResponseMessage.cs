namespace Theater.SystemMessages
{
    using System.Threading.Tasks;

    public class FinishAsyncResponseMessage : IInternalMessage
    {
        public object Result { get; set; }

        public TaskCompletionSource <object> TaskCompletionSource { get; set; }

        public void FinishReplyProcess ()
        {
            TaskCompletionSource.SetResult (Result);
        }
    }
}