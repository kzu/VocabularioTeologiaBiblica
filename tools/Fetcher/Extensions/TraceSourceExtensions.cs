namespace Fetcher
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime;

    public static class TraceSourceExtensions
    {
        [Conditional("TRACE"), TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void TraceVerbose(this TraceSource source, string message)
        {
            source.TraceEvent(TraceEventType.Verbose, 0, message, null);
        }

        [Conditional("TRACE"), TargetedPatchingOptOut("Performance critical to inline this type of method across NGen image boundaries")]
        public static void TraceVerbose(this TraceSource source, string format, params object[] args)
        {
            source.TraceEvent(TraceEventType.Verbose, 0, format, args);
        }
    }
}