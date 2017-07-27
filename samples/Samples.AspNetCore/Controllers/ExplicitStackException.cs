using System;
namespace Samples.MVC5.Controllers
{
    public partial class TestController
    {
#pragma warning disable RCS1194 // Implement exception constructors.
        private class ExplicitStackException : Exception
        {
            public override string StackTrace { get; }

            public ExplicitStackException(string message, string stackTrace) : base(message)
            {
                StackTrace = stackTrace;
            }
        }
#pragma warning restore RCS1194 // Implement exception constructors.
    }
}
