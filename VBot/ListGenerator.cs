using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Web;
using System.Reflection;

namespace VBot
{
    class ListGenerator
    {
        public enum ReturnType
        {
            Page,
            Item
        };

        private string Version = "VBot ver." + Assembly.GetExecutingAssembly().GetName().Version.ToString();

        /// <summary>
        /// Generate a List of item using WDQ
        /// </summary>
        /// <param name="WDQ">Query</param>
        /// <param name="Chunk">Default=0. Max dimension of chunk, 0=all in one chunk</param>
        /// <returns>List of item</returns>
        public List<string> WDQ(string WDQ, int Chunk=0)
        {
            WebClient client = new WebClient();
            client.Headers.Add("user-agent", Version);
            Stream data = client.OpenRead("http://wdq.wmflabs.org/api?q=" + HttpUtility.UrlEncode(WDQ));
            StreamReader reader = new StreamReader(data);
            string result = reader.ReadToEnd();
            data.Close();
            reader.Close();

            List<string> chunks = new List<string>();
            if (result.IndexOf("\"error\":\"OK\"")==-1)
            {
                return chunks;
            }

            int da = -1; int a = -1;
            da = result.IndexOf("\"items\":[") + 9;
            a = result.IndexOf("]", da);
            string tmp = result.Substring(da, a - da);

            // Split in chunk
            if (Chunk != 0)
            {
                int cont = 0;
                string[] tmp1 = tmp.Split(',');
                tmp = "";
                foreach (string s in tmp1)
                {
                    cont += 1;
                    tmp += "Q" + s + "|";
                    if (cont == Chunk)
                    {
                        cont = 0;
                        tmp = tmp.Remove(tmp.LastIndexOf("|"));
                        chunks.Add(tmp);
                        tmp = "";
                    }
                }
                tmp = tmp.Remove(tmp.LastIndexOf("|"));
            }
            else
            {
                string[] tmp1 = tmp.Split(',');
                tmp = "";
                foreach (string s in tmp1)
                {
                    tmp += "Q" + s + "|";
                }
                tmp = tmp.Remove(tmp.LastIndexOf("|"));
                chunks.Add(tmp);
            }
            return chunks;
        }

        /// <summary>
        /// Generates a List of item or wiki page using CatScan 2
        /// </summary>
        /// <param name="Lang">language of the wiki (it or en etc.)</param>
        /// <param name="Project">wikipedia, wikinews etc.</param>
        /// <param name="Cat">List of category separated by |. pages have these categories</param>
        /// <param name="negCat">List of category separated by |. pages don't have these categories</param>
        /// <param name="yesTemplate">List of template separated by |. pages have all these templates</param>
        /// <param name="anyTemplate">List of template separated by |. pages have at least one of these templates</param>
        /// <param name="noTemplate">List of template separated by |. pages don't have these templates</param>
        /// <param name="Return">item or page</param>
        /// <param name="NoItem">returno only elemnt withou Wikidata item</param>
        /// <param name="Chunk">Default=0. Max dimension of chunk, 0=all in one chunk</param>
        /// <returns>List of chunk</returns>
        public List<string> CatScan (string Lang, string Project, string Cat, string negCat, string yesTemplate, string anyTemplate, string noTemplate, ReturnType Return, bool NoItem=false, int Chunk=0)
        {
            if (Cat != "") { Cat = "&categories=" + Cat.Replace("|", "%0D%0A"); }
            if (negCat != "") { negCat = "&negcats="+ negCat.Replace("|", "%0D%0A"); }
            if (yesTemplate != "") { yesTemplate = "&templates_yes" + yesTemplate.Replace("|", "%0D%0A"); }
            if (anyTemplate != "") { anyTemplate = "&templates_any" + anyTemplate.Replace("|", "%0D%0A"); }
            if (noTemplate != "") { noTemplate = "&templates_no" + noTemplate.Replace("|", "%0D%0A"); }


            string url = "http://tools.wmflabs.org/catscan2/catscan2.php?show_redirects=no&format=tsv&comb%5Bunion%5D=1&doit=1&get_q=1&language=" + Lang + "&project=" + Project + Cat + negCat + yesTemplate + anyTemplate + noTemplate;

            WebClient client = new WebClient();
            client.Headers.Add("user-agent", Version);
            Stream data = client.OpenRead(url);
            StreamReader reader = new StreamReader(data);
            string result = reader.ReadToEnd();
            data.Close();
            reader.Close();

            string tmp = "";

            
            foreach (string strline in result.Split('\n'))
            {
                if (strline.IndexOf("(Article)")!=-1)
                {
                    string[] field = strline.Split('\t');
                    if (Return == ReturnType.Item)
                    {
                        if (field[5] != "")
                        {
                            tmp += field[5] + "|";
                        }
                    }
                    if (Return == ReturnType.Page)
                    {
                        if (field[0] != "")
                        {
                            tmp += field[0] + "|";
                        }
                    }
                }
            }
            tmp = tmp.Remove(tmp.LastIndexOf("|"));
            
            // Split in chunk
            List<string> chunks = new List<string>();
            if (Chunk != 0)
            {
                int cont = 0;
                string[] tmp1 = tmp.Split(',');
                tmp = "";
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
                tmp = tmp.Remove(tmp.LastIndexOf("|"));
            }
            else
            {
                string[] tmp1 = tmp.Split('|');
                tmp = "";
                foreach (string s in tmp1)
                {
                    tmp += s + "|";
                }
                tmp = tmp.Remove(tmp.LastIndexOf("|"));
                chunks.Add(tmp);
            }
            return chunks;
        }

        /// <summary>
        /// Generates a List of item or wiki page using Quick intersection
        /// </summary>
        /// <param name="Lang">Lang of the wiki (ex. it)</param>
        /// <param name="Category">Name of the category (ex. Film del 1930)</param>
        /// <param name="Depth"> 0 means no subcategories;</param>
        /// <param name="NoItem">false=all, true= only page without item</param>
        /// <param name="Return">Item return the list of item separated by "|", Page return the list of page separated by "|", other return list of page with category and item separated by tab</param>
        /// <param name="Chunk">Default=0. Only for Item and Page. Max dimension of chunk, 0=all in one chunk</param>
        /// <returns>List of item or List of page</returns>
        public List<string> QuickIntersection(string Lang, string Category, int Depth, bool NoItem, ReturnType Return, int Chunk = 0)
        {
            string Max = "100000";
            string url = "";
            if (NoItem)
            {
                url = "http://tools.wmflabs.org/quick-intersection/index.php?lang=" + Lang + "&project=wikipedia&cats=" + HttpUtility.UrlEncode(Category) + "&ns=0&depth=" + Depth + "&max=" + Max + "&start=0&format=wiki&catlist=1&redirects=none&wikidata=1&wikidata_no_item=1&callback=";
            }
            else
            {
                url = "http://tools.wmflabs.org/quick-intersection/index.php?lang=" + Lang + "&project=wikipedia&cats=" + HttpUtility.UrlEncode(Category) + "&ns=0&depth=" + Depth + "&max=" + Max + "&start=0&format=wiki&catlist=1&redirects=none&wikidata=1&callback=";
            }
            WebClient client = new WebClient();
            client.Headers.Add("user-agent", Version);
            Stream data = client.OpenRead(url);
            StreamReader reader = new StreamReader(data);
            string result = reader.ReadToEnd();
            data.Close();
            reader.Close();

            string tmp = "";
            foreach (string strline in result.Split('\n'))
            {
                if (strline.IndexOf("|[[") == 0)
                {
                    string[] line = strline.Split(new[] { "||" }, StringSplitOptions.None);
                    string Title = line[0].Split('|')[1].Replace("[[", "").Trim();
                    string Cat = line[5].Split('|')[0].Replace("[[", "").Replace(":Category:", "").Trim();
                    string Item = line[6].Split('|')[0].Replace("[[", "").Replace("d:", "").Trim();

                    if (Return == ReturnType.Item && Item != "")
                    {
                        tmp += Item + "|";
                    }
                    else if (Return == ReturnType.Page)
                    {
                        tmp += Title + "|";
                    }
                    else
                    {
                        tmp += Title + "\t" + Cat + "\t" + Item + Environment.NewLine;
                    }
                }
            }
            if (Return == ReturnType.Item || Return == ReturnType.Page)
            {
                tmp = tmp.Remove(tmp.LastIndexOf("|"));
            }

            // Split in chunk
            List<string> chunks=new List<string>();
            if (Chunk != 0 && (Return == ReturnType.Item || Return == ReturnType.Page))
            {
                int cont = 0;
                string[] tmp1 = tmp.Split('|');
                tmp = "";
                foreach (string s in tmp1)
                {
                    cont += 1;
                    tmp += s + "|";
                    if (cont==Chunk)
                    {
                        cont = 0;
                        tmp = tmp.Remove(tmp.LastIndexOf("|"));
                        chunks.Add(tmp);
                        tmp = "";
                    }
                }
            }
            else
            {
                chunks.Add(tmp);
            }
            return chunks;
        }

        /// <summary>
        /// "Whats link here" list of item. Only ns0 and without redirect
        /// </summary>
        /// <param name="Item">Q number to use with "Whats link here"</param>
        /// <param name="WD">Object</param>
        /// <param name="Continue">Default="" Used to change page</param>
        /// <param name="Max">Default=5000. Max items to return</param>
        /// <returns>Array. index 0 is continue parameter, index 1 is the list of item separated by "|"</returns>
        public string[] WhatsLinskHereWDQ(string Item, WikimediaAPI WD, string Continue = "", int Max = 5000)
        {
            string PostData = WD.URL() + WD.API() + "?action=query&prop=linkshere&format=xml&lhprop=title&lhnamespace=0&lhlimit=" + Max.ToString() + "&lhcontinue=" + Continue + "&lhshow=!redirect&titles=" + Item;
            string respStr = WD.PostRequest(PostData,"");

            // Extract of continue number
            int da = -1; int a = -1;
            da = respStr.IndexOf("<linkshere lhcontinue") + 23;
            if (da != 22)
            {
                a = respStr.IndexOf("\"", da);
                Continue = respStr.Substring(da, a - da);
            }

            //Extract of Qnumber
            string ret = "";
            da = respStr.IndexOf("<linkshere>") + 11;
            a = respStr.IndexOf("</linkshere>", da);
            respStr = respStr.Substring(da, a - da);
            string[] list = respStr.Split(new char[] { '>' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string item in list)
            {
                ret += item.Replace("<lh ns=\"0\" title=\"", "").Replace("\" /", "") + "|";
            }
            ret = ret.Remove(ret.LastIndexOf("|"));

            return new string[] { Continue, ret };
        }
    }
}
