using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Specialized;


namespace VBot
{
    class Utility
    {
        public enum typeData
        {
            String,
            Monolingual,
            Item,
            Coordinate,
            Time,
            Quantity
        };


        public static Datavalue CreateDataValue(string value, typeData type)
        {
            string[] val = value.Split('|');
            switch (type)
            {
                case typeData.String: //0=string
                    DatavalueString tmpS = new DatavalueString();
                    tmpS.type = "string";
                    tmpS.value = val[0];
                    return tmpS;
                case typeData.Monolingual: //0=language, 1=text
                    DatavalueMonolingual tmpM = new DatavalueMonolingual();
                    tmpM.type = "monolingualtext";
                    tmpM.value.language = val[0];
                    tmpM.value.text = val[1];
                    return tmpM;
                case typeData.Item: //0=item with or without "Q"
                    DatavalueItem tmpW = new DatavalueItem();
                    tmpW.type = "wikibase-entityid";
                    tmpW.value.numeric_id = Convert.ToInt32(val[0].Replace("Q", "").Replace("q", ""));
                    tmpW.value.entity_type = "item";
                    return tmpW;
                case typeData.Coordinate: //0=latitude, 1=longitude, 2=precision 3=globe (if not declared use Q2)
                    DatavalueCoordinate tmpC = new DatavalueCoordinate();
                    tmpC.type = "globecoordinate";
                    tmpC.value.latitude = val[0]; // decimal: no default, 9 digits after the dot and two before, signed
                    tmpC.value.longitude = val[1]; // decimal: no default, 9 digits after the dot and three before, signed
                    tmpC.value.precision = val[2]; // decimal, representing degrees of distance, defaults to 0, 9 digits after the dot and three before, unsigned, used to save the precision of the representation
                    tmpC.value.altitude = null; //unmanaged
                    if (val.Count() < 4)
                    {
                        tmpC.value.globe = "http://www.wikidata.org/entity/Q2";
                    }
                    else
                    {
                        tmpC.value.globe = val[3];
                    }
                    return tmpC;
                case typeData.Time: //0=time, 1=timezone, 2=before, 3=after, 4=precision, 5=calendarmodel (if not declared use http://www.wikidata.org/entity/Q1985727)
                    DatavalueTime tmpT = new DatavalueTime();
                    tmpT.type = "time";
                    tmpT.value.time = val[0]; // string isotime: point in time, represented per ISO8601, they year always having 11 digits, the date always be signed, in the format +00000002013-01-01T00:00:00Z
                    tmpT.value.timezone = val[1]; // signed integer: Timezone information as an offset from UTC in minutes
                    tmpT.value.before = val[2]; // integer: If the date is uncertain, how many units before the given time could it be? the unit is given by the precision
                    tmpT.value.after = val[3]; // integer: If the date is uncertain, how many units after the given time could it be? the unit is given by the precision
                    tmpT.value.precision = val[4]; // shortint: 0 - billion years, 1 - hundred million years, ..., 6 - millenia, 7 - century, 8 - decade, 9 - year, 10 - month, 11 - day, 12 - hour, 13 - minute, 14 - second
                    if (val.Count() < 6)
                    {
                        tmpT.value.calendarmodel = "http://www.wikidata.org/entity/Q1985727"; // URI identifying the calendar model that should be used to display this time value. Note that time is always saved in proleptic Gregorian, this URI states how the value should be displayed
                    }
                    else
                    {
                        tmpT.value.calendarmodel = val[5];
                    }

                    return tmpT;
                case typeData.Quantity: //0=item without
                    DatavalueQuantity tmpQ = new DatavalueQuantity();
                    tmpQ.type = "quantity";
                    tmpQ.value.amount = val[0];
                    tmpQ.value.unit = val[1];
                    tmpQ.value.upperBound = val[2];
                    tmpQ.value.lowerBound = val[3];
                    return tmpQ;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Return a dictionary for a specific template
        /// </summary>
        /// <param name="Text">Text with template</param>
        /// <param name="TemplateName">Name of template </param>
        /// <returns>Dictionary with parameter name, parameter value</returns>
        public static StringDictionary GetTemplate(string Text, string TemplateName)
        {
            Match match = Regex.Match(Text, @"{{\s*" + TemplateName, RegexOptions.IgnoreCase);
            int Bracket = 0;
            string template="";
            //Dictionary<string, string> Template = new Dictionary<string, string>();
            StringDictionary Template = new StringDictionary();

            if (match.Success)
            {
                int start = match.Index;
                int end=0;
                Bracket = 2;
                int Cont = 0;
                for (int idx = start+2; idx <= Text.Length; idx++)
                {
                    if (Text[idx] == '}') {Bracket -= 1;}
                    if (Text[idx] == '{') { Bracket += 1; }
                    if (Bracket==0) 
                    {
                        end=idx;
                        template = Text.Substring(start, end - start);
                        template = CleanWiki(template);
                        template = template.Remove(0, 2);
                        template = template.Remove(template.Length - 2, 2);
                        string[] fields = template.Split('|');
                        for (int idx2 = 0; idx2 < fields.Count(); idx2++)
                        {
                            string[] split = fields[idx2].Split(new char[] { '=' }, 2);
                            if (split.Count() == 2)
                            {
                                // TODO Check for double parameter
                                Template.Add(split[0].Trim(), split[1].Trim());
                                Cont += 1;
                            }
                            else
                            {
                                Template.Add(Cont.ToString(), split[0].Trim());
                                Cont += 1;
                            }
                        }
                        break;
                    }
                }
            }
            return Template;
        }

        public static string GetTemplateParameter(string Text, string TemplateName,string Parameter)
        {
            //Dictionary<string, string> template = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            StringDictionary template = new StringDictionary();
            template = GetTemplate(Text, TemplateName);
            if (template.ContainsKey(Parameter))
            {
                return template[Parameter];
            }
            else
            {
                return "";
            }
        }
        /// <summary>
        /// Delete piped from wikilink
        /// </summary>
        /// <param name="Text">Text with wikilink with piped</param>
        /// <returns>Text with wikilink without piped</returns>
        private static string DelPipe(string Text)
        {
            int length=Text.Length;
            for (int idx = 0; idx < length; idx++)
            {
                if (Text[idx]=='[' && Text[idx+1]=='[')
                {
                    int pipe = -1;
                    for (int idx2 = idx; idx2 < Text.Length; idx2++)
                    {
                        if (Text[idx2]=='|')
                        {
                            pipe = idx2;
                        }
                        if (Text[idx2]==']' && Text[idx2+1]==']')
                        {
                            if (pipe != -1)
                            { 
                                Text = Text.Remove(pipe, idx2 - pipe);
                                length = Text.Length;
                            }
                            idx = idx2;
                            break;
                        }
                    }
                }
            }
            return Text;
        }

        /// <summary>
        /// Clen of wikitext: del comment, nowiki and ref
        /// </summary>
        /// <param name="Text">Wikitext</param>
        /// <returns>Clenaed wikitext</returns>
        private static string CleanWiki(string Text)
        {
            Regex regex = new Regex("<!--.*-->",  RegexOptions.Compiled);
            string result = regex.Replace(Text, "");
            regex = new Regex("(?is)<nowiki>(.*?)</nowiki>",  RegexOptions.Compiled);
            result = regex.Replace(result, "");
            regex = new Regex("<ref *>.*</ref *>", RegexOptions.Compiled);
            result = regex.Replace(result, "");

            result = DelPipe(result);
            return result;
        }


        /// <summary>
        /// Find position of a section
        /// </summary>
        /// <param name="Text">Wiki text</param>
        /// <param name="Section">Section to find with level (ex. == External link ==)</param>
        /// <returns>Position</returns>
        public static int SectionStart(string Text, string Section)
        {
            Regex regex = new Regex(@"==\s*" + Section + @"\s*==", RegexOptions.IgnoreCase);
            Match match = regex.Match(Text);
            return match.Index;
        }

        /// <summary>
        /// Delete disambiguation from a title
        /// </summary>
        /// <param name="Title">Title</param>
        /// <param name="Disambig">Must be , or ()</param>
        /// <returns>Title without disambiguation</returns>
        public static string DelDisambiguation(string Title, string Disambig)
        {
            if (Disambig == "()")
            {
                int lung = Title.Length;
                if (Title.Substring(lung - 1) == ")")
                {
                    int da = Title.LastIndexOf("(");
                    return Title.Substring(0, da - 1);
                }
            }
            else if (Disambig == ",")
            {
                int da = Title.IndexOf(",");
                if (da != -1)
                {
                    string temp = Title.Substring(0, da);
                    return temp;
                }
                else
                {
                    return Title;
                }
            }
            return Title;
        }

        /// <summary>
        /// To create chunk with max number of chunk string
        /// </summary>
        /// <param name="Text">list of item separated by |</param>
        /// <param name="Chunk">Max dimension of chunk</param>
        /// <returns>List of item</returns>
        public static List<string> SplitInChunk(string Text, int Chunk)
        {
            int cont = 0;
            List<string> chunks = new List<string>();

            string[] tmp1 = Text.Split('|');
            String tmp = "";
            foreach (string s in tmp1)
            {
                cont += 1;
                tmp += s + "|";
                if (cont == Chunk)
                {
                    cont = 0;
                    tmp = tmp.Remove(tmp.LastIndexOf("|"));
                    chunks.Add(tmp);
                    tmp = "";
                }
            }
            return chunks;
        }
    }        
}
