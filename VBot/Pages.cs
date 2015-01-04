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
    public class RootWP
    {
        public Query query { get; set; }   
    }

    public class Query
    {
        public Dictionary<int, Pages> pages { get; set; }
    }

    public class Pages
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

