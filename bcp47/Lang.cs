using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace bcp47
{
    public class Lang : IEquatable<Lang>
    {
        //all fields are readonly thus i'm thread safe
        public readonly Record Language;
        public readonly Record ExtLang;
        public readonly Record Script;
        public readonly Record Region;
        public readonly ReadOnlyCollection<Record> Variants;
        public readonly ReadOnlyDictionary<char, ReadOnlyCollection<string>> Extensions;
        public readonly string Private;
        public readonly string Canonical;

        public static Lang Parse(string tag)
        {
            string head;
            string tail;
            var variants = new List<Record>();
            var extensions = new Dictionary<char, List<string>>();
            string @private = "";
            //Language Tag
            Tuple<string, string> ht = Headtail(tag);
            head = ht.Item1.ToLowerInvariant();
            tail = ht.Item2;

            Lang ret;
            ret = CheckGrandfathered(tag);
            if (ret != null)
            {
                return ret;
            }

            //rfc 5646 2.2.1.
            if (head == "x") //everything is private...
            {
                return new Lang(null, null, null, null, variants, tail, extensions);
            }

            Record lang;

            lang = CheckLanguageTag(head);
            if (lang == null)
            {
                throw new FormatException(string.Format("Language string '{0}' is not valid: no language '{1}' in registry", tag, head));
            }

            Record extlang = null;

            ht = Headtail(tail);
            head = ht.Item1;
            tail = ht.Item2;


            if (head.Length == 3 && Isalpha(head))
            {
                //might be an extlang
                extlang = Registry.FindExtlang(head);
                if (extlang != null)
                {
                    if (extlang.Prefix != "" && lang.Subtag != extlang.Prefix)
                    {
                        throw new FormatException(string.Format("Language string '{0}' is not valid: extended language does not match language '{1}'", tag, head));
                    }

                    if (extlang.PreferredValue != "")
                    {
                        lang = Registry.FindLanguage(extlang.PreferredValue);
                        if (lang == null)
                        {
                            throw new FormatException(string.Format("Language string '{0}' is not valid: language not found '{1}', the registry marked this language as preferred value: registry corruption", tag, extlang.PreferredValue));
                        }

                        extlang = null;
                    }
                    ht = Headtail(tail);
                    head = ht.Item1;
                    tail = ht.Item2;
                }
                //else ... was not an extlang

            }

            Record script = null;
            if (head.Length == 4 && Isalpha(head))
            {
                //Might be a script                   
                script = Registry.FindScript(head);
                if (script == null)
                {
                    throw new FormatException(string.Format("Language string '{0}' is not valid: script not found '{1}'", tag, head));
                }

                ht = Headtail(tail);
                head = ht.Item1;
                tail = ht.Item2;
            }
            else
            {
                if (lang.SuppressScript != "")
                {
                    script = Registry.FindScript(lang.SuppressScript);
                    if (script == null)
                    {
                        throw new FormatException(string.Format("Language string '{0}' is not valid: script not found '{1}' possible registry corruption", tag, lang.SuppressScript));
                    }
                }
            }
            Record region = null;
            if ((head.Length == 2 && Isalpha(head)) || (head.Length == 3 && Isdigit(head)))
            {
                //region code
                region = Registry.FindRegion(head);
                if (region == null)
                {
                    throw new FormatException(string.Format("Language string '{0}' is not valid: invalid region code '{1}'", tag, head));
                }

                ht = Headtail(tail);
                head = ht.Item1;
                tail = ht.Item2;
            }


            while ((head.Length >= 5 && head.Length <= 8 && Isalpha(head.Substring(0, 1)))
                ||
                    (head.Length == 4 && Isdigit(head.Substring(0, 1)))
                )
            {
                //this should be a variant subtag
                Record variant;
                variant = Registry.FindVariant(head);
                if (variant == null)
                {
                    throw new FormatException(string.Format("Language string '{0}' is not valid: unrecognized variant '{1}'", tag, head));
                }

                if (variants.Contains(variant))
                {
                    throw new FormatException(string.Format("Language string '{0}' is not valid: variant subtag repeated '{1}'", tag, head));
                }

                variants.Add(variant);

                ht = Headtail(tail);
                head = ht.Item1;
                tail = ht.Item2;
            }

            while (head.Length == 1)
            {
                char c = head[0];
                if (c == 'x')
                {
                    @private = tail;
                    tail = "";
                    head = "";
                    break;
                }
                if (!extensions.ContainsKey(c))
                {
                    extensions[c] = new List<string>();
                }
                else
                {
                    throw new FormatException(string.Format("Language string '{0}' is not valid: extension prefix already used '{1}'", tag, c));
                }

                ht = Headtail(tail);
                head = ht.Item1;
                tail = ht.Item2;

                while (head.Length > 1 && head.Length <= 8)
                {
                    extensions[c].Add(head);
                    ht = Headtail(tail);
                    head = ht.Item1;
                    tail = ht.Item2;
                }
                extensions[c].Sort();
            }

            if (head != "")
            {
                throw new FormatException(string.Format("Language string '{0}' is not valid: unexpected sequence '{1}'", tag, head));
            }

            return new Lang(lang, extlang, script, region, variants, @private, extensions);
        }
        public static readonly Registry Registry;
        public override string ToString()
        {
            return Canonical;
        }

        #region PRIVATE STUFF
        private Lang(Record lang, Record extLang, Record script, Record region,
                            List<Record> variants, string @private, Dictionary<char, List<string>> extensions)
        {
            if (extensions == null)
            {
                extensions = new Dictionary<char, List<string>>();
            }

            this.Language = lang ?? emptyRecord;
            this.Script = script ?? emptyRecord;
            this.Region = region ?? emptyRecord;
            this.Variants = new ReadOnlyCollection<Record>(variants ?? new List<Record>());
            this.Private = @private ?? "";
            this.ExtLang = extLang ?? emptyRecord;
            this.Extensions = new ReadOnlyDictionary<char, ReadOnlyCollection<string>>(extensions.ToDictionary(a => a.Key, a => new ReadOnlyCollection<string>(a.Value)));
            this.Canonical = PrivToString();
        }

        private string PrivToString()
        {
            var sb = new StringBuilder(80);
            if (this.Language != null)
            {
                sb.Append(Language.Subtag + Language.Tag);
            }

            if (this.ExtLang != emptyRecord)
            {
                sb.Append('-');
                sb.Append(ExtLang.Subtag);
            }
            if (this.Script != emptyRecord && Script.Subtag != Language.SuppressScript)
            {
                sb.Append('-');
                sb.Append(Script.Subtag);
            }
            if (this.Region != emptyRecord)
            {
                sb.Append('-');
                sb.Append(Region.Subtag);
            }
            foreach (var t in Variants)
            {
                sb.Append('-');
                sb.Append(t.Subtag);
            }
            foreach (var t in Extensions)
            {
                sb.Append('-');
                sb.Append(t.Key);
                foreach (var v in t.Value)
                {
                    sb.Append('-');
                    sb.Append(v);
                }
            }
            if (this.Private != "")
            {
                if (sb.Length > 0)
                {
                    sb.Append('-');
                }

                sb.Append('x');
                sb.Append('-');
                sb.Append(Private);
            }
            return sb.ToString();
        }

        #endregion
        #region PRIVATE STATICS
        private static readonly Record emptyRecord = new Record("", "", "", "", "", DateTime.MinValue, "", "", "");

        static Lang()
        {
            Registry = Registry.Load();
        }

        private static bool Isalpha(string s)
        {
            return s.ToLowerInvariant().All(a => a >= 'a' && a <= 'z');
        }

        private static bool Isdigit(string s)
        {
            return s.ToLowerInvariant().All(a => a >= '0' && a <= '9');
        }

        private static Lang CheckGrandfathered(string tag)
        {
            Record langtag = Registry.FindGrandfathered(tag);
            if (langtag == null)
            {
                return null;
            }

            if (langtag.PreferredValue != "")
            {
                return Parse(langtag.PreferredValue);
            }
            if (tag.StartsWith("i-"))
            {
                return new Lang(langtag, null, null, null, new List<Record>(), "", new Dictionary<char, List<string>>());
            }
            return null;
        }

        private static Record CheckLanguageTag(string lang)
        {
            Record l = Registry.FindLanguage(lang);
            if (l == null)
            {
                return null;
            }

            if (l.PreferredValue != "" && l.PreferredValue != lang)
            {
                return Registry.FindLanguage(l.PreferredValue);
            }
            return l;
        }

        private static Tuple<string, string> Headtail(string tagList)
        {
            int p = tagList.IndexOf('-');
            int pn = p + 1;
            if (p < 0)
            {
                pn = p = tagList.Length;
            }

            return new Tuple<string, string>(tagList.Substring(0, p), tagList.Substring(pn));
        }
        #endregion

        public override bool Equals(object obj)
        {
            return Equals(obj as Lang);
        }

        public override int GetHashCode()
        {
            return Canonical.GetHashCode();
        }

        public bool Equals(Lang other)
        {
            if (other == null)
            {
                return false;
            }

            return this.Canonical.Equals(other.Canonical);
        }


    }
}