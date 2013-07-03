namespace Fetcher
{
    using Sgml;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

    public class BibleFetcher
    {
        private static readonly TraceSource tracer = new TraceSource("BibleFetcher", SourceLevels.Warning);
        private static readonly Regex verseIndex = new Regex(@"^(?<index>\d+)(?<rest>\s.*)", RegexOptions.Compiled | RegexOptions.Singleline);
        private const string BaseUri = "http://www.vatican.va/archive/ESL0506/";
        private const string IndexUri = BaseUri + "_INDEX.HTM";

        #region bible -> vocab map

        private static readonly Dictionary<string, string> bibleToVocab = new Dictionary<string, string>
        {
            // bible -> vocab
            { "PRIMERA CARTA A LOS CORINTIOS", "1Cor" },     
            { "PRIMER LIBRO DE LAS CRONICAS", "1Par" },      
            { "PRIMERA CARTA DE SAN JUAN", "1Jn" },          
            { "PRIMER LIBRO DE LOS MACABEOS", "1Mac" },      
            { "PRIMERA CARTA DE SAN PEDRO", "1Pe" },         
            { "PRIMER LIBRO DE LOS REYES", "1Re" },          
            { "PRIMER LIBRO DE SAMUEL", "1Sa" },             
            { "PRIMERA CARTA A TIMOTEO", "1Tim" },           
            { "PRIMERA CARTA A LOS TESALONICENSES", "1Tes" },
            { "SEGUNDA CARTA A LOS CORINTIOS", "2Cor" },     
            { "SEGUNDO LIBRO DE LAS CRONICAS", "2Par" },     
            { "SEGUNDA CARTA DE SAN JUAN", "2Jn" },          
            { "SEGUNDO LIBRO DE LOS MACABEOS", "2Mac" },     
            { "SEGUNDA CARTA DE SAN PEDRO", "2Pe" },         
            { "SEGUNDO LIBRO DE LOS REYES", "2Re" },         
            { "SEGUNDO LIBRO DE SAMUEL", "2Sa" },            
            { "SEGUNDA CARTA A TIMOTEO", "2Tim" },           
            { "SEGUNDA CARTA A LOS TESALONICENSES", "2Tes" },
            { "TERCERA CARTA DE SAN JUAN", "3Jn" },          
            { "ABDIAS", "Abd" },              
            { "AGEO", "Ag" },                 
            { "AMOS", "Am" },                 
            { "APOCALIPSIS", "Ap" },          
            { "BARUC", "Bar" },               
            { "CARTA A LOS COLOSENSES", "Col" },             
            { "CANTAR DE LOS CANTARES", "Cant" },            
            { "DEUTERONOMIO", "Dt" },        
            { "DANIEL", "Dan" },             
            { "CARTA A LOS EFESIOS", "Ef" }, 
            { "EXODO", "Ex" },               
            { "ESDRAS", "Esd" },             
            { "ESTER", "Est" },              
            { "EZEQUIEL", "Ez" },            
            { "CARTA A FILEMON", "Flm" },    
            { "CARTA A LOS FILIPENSES", "Flp" },
            { "CARTA A LOS GALATAS", "Gal" },   
            { "GENESIS", "Gen" },             
            { "HABACUC", "Hab" },               
            { "CARTA A LOS HEBREOS", "Heb" },   
            { "HECHOS DE LOS APOSTOLES", "Act" },
            { "ISAIAS", "Is" },                  
            { "JOB", "Job" },               
            { "JUECES", "Jue" },            
            { "JUDIT", "Jdt" },             
            { "JOEL", "Jl" },               
            { "EVANGELIO SEGUN SAN JUAN", "Jn" },
            { "JONAS", "Jon" },                
            { "JOSUE", "Jos" },                
            { "JEREMIAS", "Jer" },               
            { "CARTA DE SAN JUDAS", "Jds" },       
            { "EVANGELIO SEGUN SAN LUCAS", "Lc" }, 
            { "LAMENTACIONES", "Lam" },            
            { "LEVITICO" ,"Lev" },                 
            { "EVANGELIO SEGUN SAN MARCOS", "Mc" },
            { "MIQUEAS", "Miq" },                  
            { "MALAQUIAS", "Mal" },                
            { "EVANGELIO SEGUN SAN MATEO", "Mt" }, 
            { "NAHUM", "Nah" },                 
            { "NEHEMIAS", "Neh" },              
            { "NUMEROS", "Num" },               
            { "OSEAS", "Os" },                  
            { "PROVERBIOS", "Prov" },           
            { "ECLESIASTES", "Ecl" },           
            { "CARTA A LOS ROMANOS", "Rom" },  
            { "RUT", "Rut" },                  
            { "SALMOS", "Sal" },               
            { "SABIDURIA", "Sab" },            
            { "ECLESIASTICO", "Eclo" },        
            { "SOFONIAS", "Sof" },             
            { "CARTA DE SANTIAGO", "Sant" },    
            { "TOBIAS", "Tob" },                
            { "CARTA A TITO", "Tit" },          
            { "ZACARIAS", "Zac" },                 
        };

        #endregion

        private string outputDir;

        public BibleFetcher(string outputDir)
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
                tracer.TraceVerbose("Downloading main index page...");
                var html = web.DownloadString(IndexUri);
                using (var reader = SgmlFactory.Create(BaseUri, html))
                {
                    tracer.TraceVerbose("Loading document from main index page...");
                    xdoc = XDocument.Load(reader, LoadOptions.SetBaseUri);
                }
            }

            xdoc.Dump("Biblia\\index.xml");

            var testaments = xdoc.XPathSelectElements("/html/body/font/ul/li");
            foreach (var testament in testaments)
            {
                FetchTestament(testament);
            }
        }

        private void FetchTestament(XElement testament)
        {
            var books = testament.XPathSelectElements("ul/li");
            var testamentName = testament.Element("font").Value;
            tracer.TraceInformation("Processing testament '{0}'...", testamentName);
            foreach (var book in books)
            {
                var bookName = book.Element("font").Value;
                var bookCode = "";
                if (bibleToVocab.TryGetValue(bookName, out bookCode))
                {
                    tracer.TraceInformation("Processing book '{0}' ({1})...", bookName, bookCode);
                    FetchBook(testamentName, bookName, bookCode, book);
                }
            }
        }

        private void FetchBook(string testamentName, string bookName, string bookCode, XElement book)
        {
            var chapters = book.XPathSelectElements("ul/li/font/a");
            using (var web = new WebClient())
            {
                foreach (var chapter in chapters)
                {
                    var uri = new Uri(new Uri(chapter.BaseUri), chapter.Attribute("href").Value);
                    tracer.TraceVerbose("Downloading book chapter from '{0}'...", uri);
                    var html = web.DownloadString(uri);
                    using (var reader = SgmlFactory.Create(chapter.BaseUri, html))
                    {
                        tracer.TraceVerbose("Loading document from book chapter...");
                        var xdoc = XDocument.Load(reader, LoadOptions.SetBaseUri);
                        var index = xdoc.XPathSelectElement("html/body/font/font/ul/li/ul/li/ul/li");
                        var chapterName = xdoc.XPathSelectElement("html/body/p[@class='Capitulo']");
                        if (chapterName == null)
                            chapterName = xdoc.XPathSelectElement("html/body/p[@class='Enunciado']");

                        WriteChapter(testamentName, bookName, bookCode,
                            chapterName == null ? null : chapterName.Value,
                            index == null ? null : index.Value, xdoc);

                        xdoc.Dump("Biblia\\" + bookCode + "-" + (index == null ? null : ("-" + index.Value)) + ".xml");
                    }
                }
            }
        }

        private void WriteChapter(string testamentName, string bookName, string bookCode, string chapterName, string chapterIndex, XDocument chapter)
        {
            Guard.NotNullOrEmpty(() => testamentName, testamentName);
            Guard.NotNullOrEmpty(() => bookName, bookName);
            Guard.NotNullOrEmpty(() => bookCode, bookCode);
            Guard.NotNullOrEmpty(() => chapterName, chapterName);
            Guard.NotNullOrEmpty(() => chapterIndex, chapterIndex);

            var fileDir = this.outputDir;
            var fileName = Path.Combine(fileDir, bookCode + (chapterIndex ?? "") + ".md");
            if (!Directory.Exists(fileDir))
                Directory.CreateDirectory(fileDir);

            tracer.TraceInformation("Writing {0} - {1} - {2}...", testamentName, bookName, chapterName);

            using (var file = File.CreateText(fileName))
            {
                file.WriteLine("# " + testamentName);
                file.WriteLine("## " + bookName);
                file.WriteLine("### " + chapterName);
                file.WriteLine();

                foreach (var para in chapter.XPathSelectElements("html/body/p[@class != 'Capitulo']").Where(p =>
                    p.Value.Trim().Length > 0 && p.Attribute("class") != null))
                {
                    if (para.Attribute("class").Value == "Titulointerior")
                    {
                        file.WriteLine();
                        file.WriteLine("#### " + para.Value);
                    }
                    else if (para.Attribute("class").Value == "MsoNormal")
                    {
                        var verseMatch = verseIndex.Match(para.Value);
                        if (verseMatch.Success)
                            file.WriteLine(verseMatch.Result("${index}. ${rest}"));
                        else
                            file.WriteLine(para.Value);
                    }
                }
            }
        }
    }
}