using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.IO.Compression;
using System.Text;

namespace bcp47
{
    public class Registry
    {
        //being a readonly class it is inherently thread safe

        DateTime registryUpdated;
        readonly Dictionary<string, Record> languageIndex = new Dictionary<string, Record>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Record> extlangIndex = new Dictionary<string, Record>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Record> scriptIndex = new Dictionary<string, Record>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Record> regionIndex = new Dictionary<string, Record>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Record> variantIndex = new Dictionary<string, Record>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Record> grandfatheredIndex = new Dictionary<string, Record>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, Record> redundantIndex = new Dictionary<string, Record>(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<Record> AllRecords()
        {
            return languageIndex.Values.Concat(extlangIndex.Values).Concat(scriptIndex.Values).Concat(regionIndex.Values).Concat(variantIndex.Values).Concat(grandfatheredIndex.Values).Concat(redundantIndex.Values);
        }
        public IEnumerable<Record> Languages()
        {
            return languageIndex.Values.AsEnumerable();
        }

        public IEnumerable<Record> Extlangs()
        {
            return extlangIndex.Values.AsEnumerable();
        }

        public IEnumerable<Record> Scripts()
        {
            return scriptIndex.Values.AsEnumerable();
        }

        public IEnumerable<Record> Regions()
        {
            return regionIndex.Values.AsEnumerable();
        }
        public IEnumerable<Record> Variants()
        {
            return variantIndex.Values.AsEnumerable();
        }
        public IEnumerable<Record> Grandfathereds()
        {
            return grandfatheredIndex.Values.AsEnumerable();
        }

        public IEnumerable<Record> Reduntants()
        {
            return redundantIndex.Values.AsEnumerable();
        }

        public Record FindBySubTag(string tag)
        {
            if (languageIndex.ContainsKey(tag))
            {
                return this.languageIndex[tag];
            }

            if (extlangIndex.ContainsKey(tag))
            {
                return this.extlangIndex[tag];
            }

            if (scriptIndex.ContainsKey(tag))
            {
                return this.scriptIndex[tag];
            }

            if (regionIndex.ContainsKey(tag))
            {
                return this.regionIndex[tag];
            }

            if (variantIndex.ContainsKey(tag))
            {
                return this.variantIndex[tag];
            }

            if (grandfatheredIndex.ContainsKey(tag))
            {
                return this.grandfatheredIndex[tag];
            }

            if (redundantIndex.ContainsKey(tag))
            {
                return this.redundantIndex[tag];
            }

            return null;
        }

        public Record FindLanguage(string tag)
        {
            if (tag.Length == 3 && tag.ToUpperInvariant().CompareTo("qaa") >= 0 && tag.ToUpperInvariant().CompareTo("qtz") <= 0)
            {
                Record o = this.languageIndex["qaa..qtz"];
                return new Record(o.Type, tag.ToLowerInvariant(), "", "", o.Description, o.Created, o.SuppressScript, o.MacroLanguage, o.Prefix);
            }
            return languageIndex.ContainsKey(tag) ? this.languageIndex[tag] : null;
        }
        public Record FindExtlang(string tag)
        {
            return extlangIndex.ContainsKey(tag) ? this.extlangIndex[tag] : null;
        }
        public Record FindScript(string tag)
        {
            if (tag.ToUpperInvariant().CompareTo("qaaa") >= 0 && tag.ToUpperInvariant().CompareTo("qabx") <= 0)
            {
                Record o = this.scriptIndex["Qaaa..Qabx"];
                return new Record(o.Type, tag.Substring(0, 1).ToUpperInvariant() + tag.Substring(1).ToLowerInvariant(), "", "", o.Description, o.Created, o.SuppressScript, o.MacroLanguage, o.Prefix);
            }
            return scriptIndex.ContainsKey(tag) ? this.scriptIndex[tag] : null;
        }
        public Record FindRegion(string tag)
        {
            if (tag.ToUpperInvariant().CompareTo("QM") >= 0 && tag.ToUpperInvariant().CompareTo("QZ") <= 0)
            {
                Record o = this.regionIndex["QM..QZ"];
                return new Record(o.Type, tag.ToUpperInvariant(), "", "", o.Description, o.Created, o.SuppressScript, o.MacroLanguage, o.Prefix);
            }
            return regionIndex.ContainsKey(tag) ? this.regionIndex[tag] : null;
        }
        public Record FindVariant(string tag)
        {
            return variantIndex.ContainsKey(tag) ? this.variantIndex[tag] : null;
        }
        public Record FindGrandfathered(string tag)
        {
            return grandfatheredIndex.ContainsKey(tag) ? this.grandfatheredIndex[tag] : null;
        }
        public Record FindRedundant(string tag)
        {
            return redundantIndex.ContainsKey(tag) ? this.redundantIndex[tag] : null;
        }

        public static void DownloadIanaFile(string cacheFile, string sourceUri = "http://www.iana.org/assignments/language-subtag-registry/language-subtag-registry")
        {
            using (var f = File.Open(cacheFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        using (Stream stream = client.OpenRead(sourceUri))
                        {
                            using (GZipStream output = new GZipStream(f, CompressionMode.Compress))
                            {
                                int i = 0;
                                while ((i = stream.ReadByte()) > 0)
                                {
                                    output.WriteByte((byte) i);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    f.Close();
                    File.Delete(cacheFile);
                    throw ex;
                }
            }
        }

        public static Registry LoadFromRemoteUrl(string sourceUri = "http://www.iana.org/assignments/language-subtag-registry/language-subtag-registry")
        {
            using (WebClient client = new WebClient())
            {
                using (Stream stream = client.OpenRead(sourceUri))
                {
                    using (StreamReader sr = new StreamReader(stream, Encoding.UTF8))
                    {
                        return Load(sr);
                    }
                }
            }
        }

        public static Registry LoadFromLocalFile(string cacheFile)
        {
            try
            {
                using (var f = File.Open(cacheFile, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    using (GZipStream input = new GZipStream(f, CompressionMode.Decompress))
                    {
                        using (StreamReader sr = new StreamReader(input, Encoding.UTF8))
                        {
                            return Load(sr);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Registry Load(StreamReader sr)
        {
            var u = new Registry();
            Dictionary<string, string> d = NextRecord(sr);

            if (d.Count != 1 || !d.ContainsKey("File-Date") || DateTime.TryParseExact(d["File-Date"], "yyyy-mm-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AllowTrailingWhite, out u.registryUpdated) == false)
            {
                throw new FormatException();
            }

            while (!sr.EndOfStream)
            {
                d = NextRecord(sr);
                CheckRecord(d);

                if (d["Type"] == "language")
                {
                    u.languageIndex.Add(d["Subtag"], ParseRecord(d));
                }
                else
                {
                    if (d["Type"] == "extlang")
                    {
                        u.extlangIndex.Add(d["Subtag"], ParseRecord(d));
                    }
                    else
                    {
                        if (d["Type"] == "script")
                        {
                            u.scriptIndex.Add(d["Subtag"], ParseRecord(d));
                        }
                        else
                        {
                            if (d["Type"] == "region")
                            {
                                u.regionIndex.Add(d["Subtag"], ParseRecord(d));
                            }
                            else
                            {
                                if (d["Type"] == "variant")
                                {
                                    u.variantIndex.Add(d["Subtag"], ParseRecord(d));
                                }
                                else
                                {
                                    if (d["Type"] == "grandfathered")
                                    {
                                        u.grandfatheredIndex.Add(d["Tag"], ParseRecord(d));
                                    }
                                    else
                                    {
                                        if (d["Type"] == "redundant")
                                        {
                                            u.redundantIndex.Add(d["Tag"], ParseRecord(d));
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return u;
        }

        private static Record ParseRecord(Dictionary<string, string> d)
        {
            string preferred;

            string subtag = d.ContainsKey("Subtag") ? d["Subtag"] : "";
            string tag = d.ContainsKey("Tag") ? d["Tag"] : "";
            string suppress = "";
            string macrolang = "";
            string prefix = "";

            if (!d.ContainsKey("Preferred-Value"))
            {
                preferred = subtag;
            }
            else
            {
                preferred = d["Preferred-Value"];
            }

            if (d.ContainsKey("Suppress-Script"))
            {
                suppress = d["Suppress-Script"];
            }

            if (d.ContainsKey("Macrolanguage"))
            {
                macrolang = d["Macrolanguage"];
            }

            if (d.ContainsKey("Prefix"))
            {
                prefix = d["Prefix"];
            }

            return new Record(d["Type"], subtag, tag, preferred, d["Description"],
                DateTime.ParseExact(d["Added"], "yyyy-mm-dd", System.Globalization.CultureInfo.InvariantCulture), suppress, macrolang, prefix);


        }

        static readonly string[] validTypes = { "language", "extlang", "script", "region", "variant", "grandfathered", "redundant" };

        static void CheckRecord(Dictionary<string, string> d)
        {
            if (!d.ContainsKey("Type") || !validTypes.Contains(d["Type"])) //must contain Type and type must be one of validtpe
            {
                throw new FormatException();
            }

            string type = d["Type"];

            if (d.ContainsKey("Subtag") && d.ContainsKey("Tag")) //must contain only one of subtag or tag
            {
                throw new FormatException();
            }

            if (Array.IndexOf(validTypes, type) <= 4 && !d.ContainsKey("Subtag")) //if type if one of first 5 elements on validtype then must have subtag
            {
                throw new FormatException();
            }

            if (Array.IndexOf(validTypes, type) > 4 && !d.ContainsKey("Tag")) //if type if one of last elements on validtype then must have tag
            {
                throw new FormatException();
            }

            if (!d.ContainsKey("Description"))
            {
                throw new FormatException();
            }

            if (!d.ContainsKey("Added"))
            {
                throw new FormatException();
            }

            if (d.ContainsKey("Prefix") && !(type == "extlang" || type == "variant")) //prefix MUST only stay on extlang or variant
            {
                throw new FormatException();
            }

            if (d.ContainsKey("Suppress-Script") && !(type == "language" || type == "extlang")) //Suppress-Script MUST only stay on extlang or language
            {
                throw new FormatException();
            }

            if (d.ContainsKey("Macrolanguage") && !(type == "language" || type == "extlang")) //Suppress-Script MUST only stay on extlang or language
            {
                throw new FormatException();
            }
        }

        static Dictionary<string, string> NextRecord(StreamReader sr)
        {
            if (sr.EndOfStream)
            {
                return null;
            }

            var result = new Dictionary<string, string>();

            string row = sr.ReadLine();

            while (!sr.EndOfStream && row != "%%")
            {
                Tuple<string, string> ht = Headtail(row);
                if (ht.Item1 == "Description" && result.ContainsKey("Description")) //multiple values allowed on 'Description', 'Comments' and 'Prefix' fields.
                {
                    result[ht.Item1] += "\n" + ht.Item2.Trim();
                }
                else
                {
                    if (ht.Item1 == "Comments" || ht.Item1.StartsWith(" ")) //do not parse comment
                    {
                        //wont parse comments
                    }
                    else
                    {
                        if (ht.Item1 == "Prefix" && result.ContainsKey("Prefix")) //multiple values allowed on 'Description', 'Comments' and 'Prefix' fields.
                        {
                            result[ht.Item1] += "\n" + ht.Item2.Trim();
                        }
                        else
                        {
                            result.Add(ht.Item1, ht.Item2.Trim());
                        }
                    }
                }

                row = sr.ReadLine();
            }
            return result;
        }

        static Tuple<string, string> Headtail(string tagList)
        {
            int p = tagList.IndexOf(':');
            int pn = p + 1;
            if (p < 0)
            {
                pn = p = tagList.Length;
            }

            return new Tuple<string, string>(tagList.Substring(0, p), tagList.Substring(pn));
        }


        //try to use local cache first, or download a new one
        internal static Registry Load()
        {
            
            string fileName = ".iana-language-registry";
            if (!File.Exists(fileName))
            {
                string tempPath = System.IO.Path.GetTempPath();
                fileName = Path.Combine(tempPath, fileName);
                if (!File.Exists(fileName))
                {
                    DownloadIanaFile(fileName);
                }
            }
            return LoadFromLocalFile(fileName);
        }
    }
}