namespace Theater
{
    using System;
    using System.Threading;

    public class ConcurrentPriorityQueue <T> where T : class
    {
        protected PredicateQueue <T> [] Queues = new PredicateQueue <T>[7]
                                                 {
                                                     new PredicateQueue <T> (),
                                                     new PredicateQueue <T> (),
                                                     new PredicateQueue <T> (),
                                                     new PredicateQueue <T> (),
                                                     new PredicateQueue <T> (),
                                                     new PredicateQueue <T> (),
                                                     new PredicateQueue <T> ()
                                                 };

        protected SpinLock Spin = new SpinLock ();

        public void Enqueue (T item, Priorities prioritiy = Priorities.Low)
        {
            var lockTaken = false;
            try
            {
                Spin.Enter (ref lockTaken);

                Queues [(int) prioritiy].Enqueue (item);
            }
            finally
            {
                if (lockTaken)
                {
                    Spin.Exit ();
                }
            }
        }

        public T Dequeue (Func <T, bool> predicate)
        {
            var lockTaken = false;
            try
            {
                Spin.Enter (ref lockTaken);

                return Queues [0].Dequeue (predicate) ??
                       Queues [1].Dequeue (predicate) ??
                       Queues [2].Dequeue (predicate) ??
                       Queues [3].Dequeue (predicate) ??
                       Queues [4].Dequeue (predicate) ??
                       Queues [5].Dequeue (predicate) ??
                       Queues [6].Dequeue (predicate);
            }
            finally
            {
                if (lockTaken)
                {
                    Spin.Exit ();
                }
            }
        }
    }
}