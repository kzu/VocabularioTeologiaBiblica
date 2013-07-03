namespace Fetcher
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using System.Xml.XPath;

    public class VocabFetcher
    {
        private static readonly TraceSource tracer = new TraceSource("VocabFetcher", SourceLevels.Warning);
        private static readonly Regex bookRef = new Regex(@"(?<book>[^\s]+)\s\d+", RegexOptions.Compiled);
        private const string BaseUri = "http://hjg.com.ar/vocbib/";
        private const string IndexUri = BaseUri + "index.html";

        private string outputDir;

        public VocabFetcher(string outputDir)
        {
            this.outputDir = outputDir;
        }

        public void Fetch()
        {
            if (!Directory.Exists(this.outputDir))
                Directory.CreateDirectory(this.outputDir);

            tracer.TraceInformation("Processing main index page...");

            var xdoc = default(XDocument);

            using (var web = new WebClient())
            {
                tracer.TraceEvent(TraceEventType.Verbose, 0, "Downloading main index page...");
                var html = web.DownloadString(IndexUri);
                using (var reader = SgmlFactory.Create(BaseUri, html))
                {
                    tracer.TraceVerbose("Loading document from main index page...");
                    xdoc = XDocument.Load(reader, LoadOptions.SetBaseUri);
                }
            }

            xdoc.Dump("Vocabulario\\index.xml");

            FetchIndex(xdoc);
        }

        private void FetchIndex(XDocument xdoc)
        {
            var fileName = Path.Combine(this.outputDir, "Vocabulario.md");

            tracer.TraceInformation("Writing main index...");

            var articles = new List<XElement>();

            using (var file = File.CreateText(fileName))
            using (var toc = File.CreateText(Path.Combine(this.outputDir, "_Sidebar.md")))
            {
                file.WriteLine("# Vocabulario de Teología Bíblica");
                toc.WriteLine("## Contenido");
                toc.WriteLine("### Vocabulario de Teología Bíblica");
                toc.WriteLine();

                foreach (var para in xdoc.XPathSelectElements("html/body/div/div/table/tr/td/p"))
                {
                    // The paragraph can be the start of a new letter (for the index)
                    // or the article content itself.
                    if (para.Attribute("class") != null && para.Attribute("class").Value == "letraInicial")
                    {
                        var letter = para.Value.Trim();
                        file.WriteLine();
                        file.WriteLine("## " + letter);
                        toc.WriteLine("- [{0}](Vocabulario#{1})", letter, letter.ToLowerInvariant());
                    }
                    else
                    {
                        var article = para.Element("a");
                        if (article != null)
                        {
                            var href = article.Attribute("href").Value;
                            var wikiLink = Path.GetFileNameWithoutExtension(href.Substring(href.IndexOf('/') + 1));
                            file.WriteLine("[{0}]({1}) - ", article.Value, wikiLink);

                            articles.Add(article);
                        }
                    }
                }
            }

            foreach (var article in articles)
            {
                var href = article.Attribute("href").Value;
                var targetFile = Path.ChangeExtension(href.Substring(href.IndexOf('/') + 1), ".md");

                FetchArticle(article.Value, new Uri(new Uri(xdoc.BaseUri), href), targetFile);
            }
        }

        private void FetchArticle(string title, Uri uri, string targetFile)
        {
            using (var web = new WebClient())
            {
                tracer.TraceVerbose("Downloading article '{0}' from {1}...", title, uri);
                var html = web.DownloadString(uri);
                using (var reader = SgmlFactory.Create(uri, html))
                {
                    tracer.TraceVerbose("Loading document from article page...");
                    var xdoc = XDocument.Load(reader, LoadOptions.SetBaseUri);

                    xdoc.Dump("Vocabulario\\" + Path.ChangeExtension(targetFile, ".xml"));

                    tracer.TraceInformation("Writing article '{0}' from {1} to {2}...", title, uri, targetFile);

                    using (var file = File.CreateText(Path.Combine(this.outputDir, targetFile)))
                    {
                        file.WriteLine("# " + title);
                        file.WriteLine();

                        foreach (var para in xdoc.XPathSelectElements("html/body/div/div/div[@class='frag']/p"))
                        {
                            var css = para.Attribute("class");
                            // Detect title and and subtitles within the article. Some, like the 
                            // one on Abraham, use this.
                            if (css != null && css.Value == "f2")
                            {
                                file.WriteLine();
                                file.WriteLine("## " + para.Value);
                            }
                            else if (css != null && css.Value == "f1")
                            {
                                file.WriteLine();
                                file.WriteLine("### " + para.Value);
                            }
                            else
                            {
                                // Point links to vocabulary markdown files
                                // NOTE: we use links with no extension, which 
                                // makes them work in Github which automatically 
                                // links to the HTML rendered version :).
                                foreach (var anchor in para.Elements("a"))
                                {
                                    anchor.SetValue(string.Format(
                                        "[{0}]({1})", anchor.Value,
                                        Path.GetFileNameWithoutExtension(anchor.Attribute("href").Value)));
                                }

                                var citeBook = "";
                                // Build links for citations
                                foreach (var cite in para.Elements("cite"))
                                {
                                    var wikiLink = cite.Value;
                                    var bookMatch = bookRef.Match(wikiLink);
                                    if (!bookMatch.Success)
                                        // This is for typical links where the book 
                                        // name is not repeated across links.
                                        wikiLink = citeBook + " " + wikiLink;
                                    else
                                        // We carry over the last book we recognized, 
                                        // to support automatic proper linking when 
                                        // a link without the book appears next in the 
                                        // text.
                                        citeBook = bookMatch.Groups["book"].Value;

                                    // NOTE: final link does not have the extension either.
                                    wikiLink = Regex.Replace(bookRef.Match(wikiLink).Value, @"\s", "");

                                    // Replace the text of the citation, so that the easy XLinq 
                                    // .Value string rendering below automatically renders the 
                                    // right markdown for the link :)
                                    cite.SetValue(string.Format("[{0}]({1})", cite.Value, wikiLink));
                                }

                                file.WriteLine(para.Value);
                            }
                        }
                    }
                }
            }
        }
    }
}