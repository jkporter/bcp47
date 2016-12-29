using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;

namespace bcp47
{
    public class Record
    {
        public readonly string Type;
        public readonly string Subtag;
        public readonly string Tag;
        public readonly string PreferredValue;
        public readonly string Description;
        public readonly DateTime Created;
        public readonly string SuppressScript;
        public readonly string MacroLanguage;
        public readonly string Prefix;

        public Record(string type, string subtag, string tag, string preferredValue, string description, DateTime created, string suppressScript, string macroLanguage, string prefix)
        {
            this.Type = type ?? ""; //I DO NOT WANT ANY NULL STRING CAUSE THAT WILL SIMPLIFY ALL THE SUBSEQUENT CODING
            this.Subtag = subtag ?? "";
            this.Tag = tag ?? "";
            this.PreferredValue = preferredValue ?? "";
            this.Description = description ?? "";
            this.Created = created;
            this.SuppressScript = suppressScript ?? "";
            this.MacroLanguage = macroLanguage ?? "";
            this.Prefix = prefix ?? "";
        }
    }
}