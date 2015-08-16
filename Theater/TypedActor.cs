namespace Theater
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    public abstract class TypedActor : Actor
    {
        protected IDictionary <Type, MethodInfo> MessageTable = new Dictionary <Type, MethodInfo> ();

        protected TypedActor ()
        {
            var methods = GetType ()
                .GetMethods ()
                .Where (m => m.Name == "OnReceive" && m.GetParameters ().Length == 1);

            foreach (var method in methods)
            {
                var type = method.GetParameters () [0];
                MessageTable.Add (type.ParameterType, method);
            }
        }

        public override async Task ProcessMessage ()
        {
            var type = ExecutionContext.Message.GetType ();

            MethodInfo method;
            if (MessageTable.TryGetValue (type, out method))
            {
                if (method.ReturnType == typeof (Task))
                {
                    await (Task) method.Invoke (this, new [] {ExecutionContext.Message});
                }
                else
                {
                    method.Invoke (this, new [] {ExecutionContext.Message});
                }
            }
            else
            {
                ActorSystem.DeadLetter.Forward (ExecutionContext);
            }
        }
    }
}