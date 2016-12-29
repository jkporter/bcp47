using System;
using System.Collections.Generic;
using System.Linq;

namespace bcp47
{
    public class LangSet
    {
        readonly HashSet<Lang> supported = new HashSet<Lang>();
        Lang defaultLang = null;
        /// <summary>
        /// Add a language to the supported language
        /// </summary>
        /// <param name="language"></param>
        /// <returns>It returns this</returns>
        /// <remarks>Thread Safe at expense of speeed</remarks>
        public LangSet Add(string language, bool defaultVal = false)
        {
            lock (supported)
            {
                Lang l = Lang.Parse(language);
                supported.Add(l);
                if (this.defaultLang == null || defaultVal)
                {
                    this.defaultLang = l;
                }
            }
            return this;
        }

        /// <summary>
        /// Return a copy of the internal list of supported languages
        /// </summary>
        /// <returns>The clone of the internal list of supported language, you can then elaborate the result</returns>
        public HashSet<Lang> GetSupportedListClone()
        {
            lock (supported)
            {
                return new HashSet<Lang>(supported);
            }
        }

        public Lang GetDefaultLanguage()
        {
            return defaultLang; // default is immutable so no threading problem            
        }

        /// <summary>
        /// Return the language closest to langDef in the supported list
        /// </summary>
        /// <param name="langDef">language to find </param>
        /// <returns>best match found or default</returns>
        /// <remarks>
        /// The idea is to find the closest language using this policy:
        /// 
        /// - a language whose is in the same family of the searched language is always better than another language
        /// 
        /// for example
        /// 
        /// xx-xxx-Xxxx-XX-x-xxxxxxxx-xxxxx-xxxxxx-x-xxxxxx-xxxx
        /// is a better match to
        /// yy-yyy-Yyyy-YY-y-yyyyyyyy-yyyyy 
        /// 
        /// if yy is a sublanguage of the same macrolanguage than the default 
        /// 
        /// and so on the same policy is applied in cascade
        /// 
        /// example: ( X = Y means X match Y, X = Y > Z means that Y match X better than Z)
        /// 
        /// de-CH is a better match to de-DE than the default
        /// 
        ///
        /// de-CH-1901 = de-CH > de > default ; cause it match language and region even if orthography is old.
        /// 
        /// to match the variants: greater number of variant matches is always better than lower match of matches
        /// 
        /// In the case of equal matching, the shortest one is preferred
        /// </remarks>

        public Lang Lookup(string language)
        {
            return Lookup(Lang.Parse(language));
        }

        private Lang Lookup(Lang langDef)
        {

            Lang retval;
            HashSet<Lang> copy;
            lock (supported)
            {
                retval = this.defaultLang;
                copy = GetSupportedListClone();
            }



            //1st step: all language whose language subtag does not match are not match 
            string lang = langDef.Language.Subtag;
            //1.1 if a language is not a match, a better match than the default could be another language that is a macrolanguage for this one

            if (!copy.Any(a => a.Language.Subtag == lang))
            {
                //no one language is matching, as a second chanche we will try to see if the requested language have any macrolanguage
                //in this case we will match against the macrolanguage instead of the language
                if (langDef.Language.MacroLanguage != "") //Macrolanguage cant be null, see Record constructor
                {
                    lang = langDef.Language.MacroLanguage;
                }
            }

            //now lang is the subtag of the requested language or of the macrolanguage

            //1.2 now we see if we have some match 
            if (copy.Any(a => a.Language.Subtag == lang))
            {
                //we have at least a match.. remove mismatch from the list
                copy.RemoveWhere(a => a.Language.Subtag != lang);
            }
            else
            {
                //there is no match, we should return default language, but, 
                //as last resort try to see if there is any language on the bunch that share the same macrolanguage

                //remove from the list all language that are not on the same family
                copy.RemoveWhere(a => a.Language.MacroLanguage != lang);
            }

            //now the list contains the best possible match for the given language or contains nothing

            if (copy.Count == 0)
            {
                return retval;
            }

            if (copy.Count == 1)
            {
                return copy.First(); //we matched at least the language, this is better than the default, so return this
            }

            //2nd step, we have some match, now see if we can refine using extlang... btw the extlang should have been canonicalized, so 99% wil do nothing
            string extlang = langDef.ExtLang.Subtag;

            if (copy.Any(a => a.ExtLang.Subtag == extlang))
            {
                //ok we can refine the list using the extlang
                copy.RemoveWhere(a => a.ExtLang.Subtag != extlang);
                if (copy.Count == 1)
                {
                    return copy.First(); //we cannot find any better match
                }
            }


            //3rd step, we have some match, now go to see the scripts

            string script = langDef.Script.Subtag;
            //note that script value is blank only if the language supports many scripts and the user have not choosen one, otherwise the 
            //registry provide a SuppressScript for the language tag so even if the user have not specified it the script value does contain the 
            //default script value

            //3.1 if we do match any script, remove all other languages that does not match
            if (copy.Any(a => a.Script.Subtag == script))
            {
                copy.RemoveWhere(a => a.Script.Subtag != script);

                if (copy.Count == 1)
                {
                    return copy.First(); //we cannot find any better match
                }
            }

            //4th step, we have more than 1 match and we see if we can refine it by using the region

            string region = langDef.Region.Subtag;
            if (copy.Any(a => a.Region.Subtag == region))
            {
                //ok we can refine the list using the extlang
                copy.RemoveWhere(a => a.Region.Subtag != region);
                if (copy.Count == 1)
                {
                    return copy.First(); //we cannot find any better match
                }
            }
            else
            {
                if (copy.Any(a => a.Region.Subtag == ""))
                {
                    //we keep those language that have generic region and remove those with unmatching specific regions
                    copy.RemoveWhere(a => a.Region.Subtag != "");
                    if (copy.Count == 1)
                    {
                        return copy.First(); //we cannot find any better match
                    }
                }
            }

            //5th step, we still have more than 1 match, try to refine using the VARIANTS
            var variants = new HashSet<string>(langDef.Variants.Select(a => a.Subtag));
            //now, let's see who is the supported language that have the greatest number of variants and filter the other
            var queryVariants = copy.Select
                (
                    a =>
                        new
                        {
                            Language = a,
                            Num = a.Variants.Count(b => variants.Contains(b.Subtag))
                        }
                ).Where(c => c.Num > 0).ToList();

            int maxVariants = queryVariants.Count == 0 ? 0 : queryVariants.Select(a => a.Num).Max();

            if (maxVariants > 0)
            {
                var bestVariants = new HashSet<Lang>(queryVariants.Where(a => a.Num == maxVariants).Select(a => a.Language));
                //ok we have a set of elements that best fits user requested language... remove other languages from the set
                copy.RemoveWhere(a => !bestVariants.Contains(a));

                if (copy.Count == 1)
                {
                    return copy.First(); //we cannot find any better match
                }
            }

            //6th step, this is becoming a long task, we might find if we are matching some extension
            Dictionary<char, HashSet<string>> extensions = langDef.Extensions.ToDictionary(a => a.Key, b => new HashSet<string>(b.Value));
            //now, let's see who is the supported language that have the greatest number of extension matching and filter out others
            var queryExtensions = copy.Select
                (
                    a => new
                    {
                        Language = a,
                        Num = a.Extensions.Sum(b => b.Value.Count(c => extensions.ContainsKey(b.Key) && extensions[b.Key].Contains(c)))
                    }
                ).Where(d => d.Num > 0).ToList();
            int maxExtensions = queryExtensions.Count == 0 ? 0 : queryExtensions.Select(a => a.Num).Max();
            if (maxExtensions > 0)
            {
                var bestExtensions = new HashSet<Lang>(queryExtensions.Where(a => a.Num == maxExtensions).Select(a => a.Language));
                //ok we have a set of elements that best fits user requested language... remove other languages from the set
                copy.RemoveWhere(a => !bestExtensions.Contains(a));

                if (copy.Count == 1)
                {
                    return copy.First(); //we cannot find any better match
                }
            }

            //7th step, last resort is matching the private section, we will not do that...

            //8th step, consider languages with shorter name to be better match than ones with long names
            //for example when matching "de-Qaaa" against "de" and "de-Qaab" prefer "de"

            return copy.OrderBy(a => a.Private.Length).ThenBy(s => s.Canonical.Length).ThenBy(a => a.Canonical).First();
        }
    }
}