namespace Fetcher
{
    using System;
    using System.Diagnostics;
    using System.Linq;

    public class LinkVerifier
    {
        private static readonly TraceSource tracer = new TraceSource("LinkVerifier", SourceLevels.Warning);
        private string rootDir;

        public LinkVerifier(string rootDir)
        {
            this.rootDir = rootDir;
        }

        public void Run()
        {

        }
    }
}