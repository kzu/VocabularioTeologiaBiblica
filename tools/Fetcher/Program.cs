namespace Fetcher
{
    using Sgml;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

    class Program
    {
        static void Main(string[] args)
        {
            // NOTE: we only need to fetch the bible once.
            //new BibleFetcher(@"..\..\..\..\..\wiki").Fetch();
            new VocabFetcher(@"..\..\..\..\..\wiki").Fetch();
        }
    }
}
