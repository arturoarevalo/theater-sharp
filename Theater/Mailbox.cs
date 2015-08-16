namespace Theater
{
    using System.Collections.Generic;

    public class Mailbox
    {
        protected Queue <Actor> Actors = new Queue <Actor> ();

        /// <summary>
        ///     Indicates that there are actors queued.
        /// </summary>
        public bool Available => Actors.Count > 0;

        public Actor DequeueActor ()
        {
            if (Actors.Count > 0)
            {
                return Actors.Dequeue ();
            }

            return null;
        }

        public void EnqueueActor (Actor actor)
        {
            Actors.Enqueue (actor);
        }
    }
}