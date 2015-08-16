namespace Theater
{
    using System;
    using System.Collections.Generic;

    public class PredicateQueue <T>
    {
        protected LinkedList <T> Elements = new LinkedList <T> ();

        public void Enqueue (T item)
        {
            Elements.AddLast (item);
        }

        public T Dequeue ()
        {
            var node = Elements.Count > 0 ? Elements.First : null;
            if (node != null)
            {
                Elements.Remove (node);
                return node.Value;
            }
            return default(T);
        }

        public T Dequeue (Func <T, bool> predicate)
        {
            var node = Elements.Count > 0 ? Elements.First : null;
            while (node != null && !predicate (node.Value))
            {
                node = node.Next;
            }

            if (node != null)
            {
                Elements.Remove (node);
                return node.Value;
            }
            return default(T);
        }
    }
}