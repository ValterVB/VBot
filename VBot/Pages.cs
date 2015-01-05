using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace VBot
{
    public class Pages
    {
        public Query query { get; set; }
    }

    public class Query
    {
        public Dictionary<int, Page> pages { get; set; }
        public string FirstPageText = "";
        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            foreach (KeyValuePair<int, Page> page in this.pages)
            {
                FirstPageText = page.Value.revisions[0].text;
                break;
            }
        }
    }

    public class Page
    {
        public string pageid { get; set; }
        public string ns { get; set; }
        public string title { get; set; }
        public List<Revision> revisions { get; set; }
        public Pageprops pageprops { get; set; }

        public bool IsRedirect()
        {
            string redirectTag = "REDIRECT|RINVIA";
            Regex regex=new Regex(@"(?i)^#(?:" + redirectTag + @")\s*:?\s*\[\[(.+?)(\|.+)?]]", RegexOptions.Compiled);
            return regex.IsMatch(this.revisions[0].text);
        }
        public string RedirectTo()
        {
            string redirectTag = "REDIRECT|RINVIA";
            Regex regex = new Regex(@"(?i)^#(?:" + redirectTag + @")\s*:?\s*\[\[(.+?)(\|.+)?]]", RegexOptions.Compiled);
            return regex.Match(this.revisions[0].text).Groups[1].ToString().Trim();
        }
    }

    public class Revision
    {
        [JsonProperty("*")]
        public string text { get; set; }
    }

    public class Pageprops
    {
        public string wikibase_item { get; set; }
    }
}

