namespace Theater
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    public abstract class StringTypedActor : Actor
    {
        protected IDictionary <string, MethodInfo> MessageTable = new Dictionary <string, MethodInfo> ();

        protected StringTypedActor ()
        {
            const string ExpectedMethodName = @"OnReceive";
            const int SubstringStartIndex = 9;

            var methods = GetType ()
                .GetMethods ()
                .Where (m => m.Name.StartsWith (ExpectedMethodName, StringComparison.InvariantCultureIgnoreCase) && m.GetParameters ().Length == 0);

            foreach (var method in methods)
            {
                var name = method.Name.Substring (SubstringStartIndex);
                MessageTable.Add (name, method);
            }
        }

        public override async Task ProcessMessage ()
        {
            MethodInfo method;
            var name = ExecutionContext.Message as string;
            if (!string.IsNullOrEmpty (name) && MessageTable.TryGetValue (name, out method))
            {
                if (method.ReturnType == typeof (Task))
                {
                    await (Task) method.Invoke (this, null);
                }
                else
                {
                    method.Invoke (this, null);
                }
            }
            else
            {
                ActorSystem.DeadLetter.Forward (ExecutionContext);
            }
        }
    }
}