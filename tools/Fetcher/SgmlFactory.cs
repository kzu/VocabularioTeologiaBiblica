namespace Fetcher
{
    using Sgml;
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml;

    public static class SgmlFactory
    {
        public static XmlReader Create(Uri baseUri, string html)
        {
            return Create(baseUri.ToString(), html);
        }

        public static XmlReader Create(string baseUri, string html)
        {
            var assembly = typeof(SgmlReader).Assembly;
            var name = "Html.dtd";
            var dtd = default(SgmlDtd);

            using (var resource = assembly.GetManifestResourceStream(name))
            {
                var input = new StreamReader(resource);
                dtd = SgmlDtd.Parse(new Uri(baseUri), "HTML", input, null, null, null);
            }

            var reader = new SgmlReader
            {
                WhitespaceHandling = WhitespaceHandling.All,
                CaseFolding = CaseFolding.ToLower,
                Dtd = dtd,
                IgnoreDtd = true,
                InputStream = new StringReader(html),
            };

            reader.SetBaseUri(baseUri);

            return reader;
        }
    }
}