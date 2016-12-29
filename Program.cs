using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace bcp47
{
    class Program
    {
        //please note that the provided .iana
        static void Main(string[] args)
        {
            //Registry.DownloadIanaFile(".iana-language-registry");// if you want to ... cached file is gzipped
            var ls = new LangSet();
            ls.Add("en").Add("es").Add("fr").Add("de").Add("ja").Add("yue").Add("es-AR");
            try
            {
                ls.Add("jp");
            }
            catch (Exception ex)
            {
                Console.WriteLine("failed to add jp language");
                Console.WriteLine(ex.Message);
            }
            string pref = "es-CO";
            string best = ls.Lookup(pref).ToString();
            Console.WriteLine("Best supported language for {0} is {1}", pref,  best );
            string local = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
            string bestForLocal = ls.Lookup(local).ToString();
            Console.WriteLine("Best supported language for your current thread language {0} is {1}", local, bestForLocal);

            Console.Read();
        }
    }
}
