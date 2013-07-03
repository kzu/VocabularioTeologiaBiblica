namespace Fetcher
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    public static class XDocumentExtensions
    {
        [Conditional("DEBUG")]
        public static void Dump(this XDocument xdoc, string fileName)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            using (var file = File.Create(fileName))
            using (var writer = XmlWriter.Create(file, new XmlWriterSettings { Indent = true }))
            {
                xdoc.WriteTo(writer);
            }
        }
    }
}