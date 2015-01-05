using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace VBot
{
    class WikimediaAPI
    {
        #region Variables

        public enum LoadTypeWD
        {
            All,
            Label,
            Description,
            LabelDescriptionSitelink
        };

        private string _URL; // URL of repository
        private CookieContainer _cookies = new CookieContainer();  // Cookie from the site
        private string _User;
        private string _Password;
        private string _API = "/w/api.php";
        private string Version = "VBot ver." + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        private string editSessionToken = "";

        public string URL()
        {
            return _URL;
        }

        public string API()
        {
            return _API;
        }

        public string User
        {
            get { return _User; }
        }
        #endregion

        #region Constructor
        public WikimediaAPI(string Site, string User, string Password)
        {
            this._URL = Site;
            this._User = User;
            this._Password = Password;
            Login();
        }

        /// <summary>
        /// Login to Mediawiki site and obtain an edit token
        /// </summary>
        /// <see cref="https://www.mediawiki.org/wiki/API:Login"/>
        /// <see cref="https://www.mediawiki.org/wiki/API:Tokens"/>
        private void Login()
        {
                                                          
            string strToken = PostRequest(this._URL + this._API + "?action=login&lgname=" + HttpUtility.UrlEncode(this._User) + "&lgpassword=" + HttpUtility.UrlEncode(this._Password) + "&format=xml", "", true);
            int Da = strToken.IndexOf("token=") + 7;
            int A = strToken.IndexOf("\"", Da);
            strToken = strToken.Substring(Da, A - Da);
            strToken = PostRequest(this._URL + this._API + "?action=login&lgname=" + HttpUtility.UrlEncode(this._User) + "&lgpassword=" + HttpUtility.UrlEncode(this._Password) + "&format=xml" + "&lgtoken=" + HttpUtility.UrlEncode(strToken), "", true);
            if (strToken.IndexOf("login result=\"Success\"") == -1)
            {
                this._User = "";
                this._Password = "";
            }
            string resp = PostRequest(this._URL + this._API,"action=query&meta=tokens&format=xml");
            Da = resp.IndexOf("<tokens csrftoken=\"")+19;
            A = resp.IndexOf('"', Da);
            editSessionToken = resp.Substring(Da, A-Da);
        }
        #endregion

        /// <summary>
        /// Return the JSON string of the entities
        /// </summary>
        /// <param name="Site">Mediawiki site (ex. itwiki)</param>
        /// <param name="Pages">List of pages separated by |</param>
        /// <param name="Type">What retrieve</param>
        /// <returns>JSON string</returns>
        public string LoadWD(string Site, string Pages, LoadTypeWD Type = LoadTypeWD.All)
        {
            string Titles = "&sites=" + HttpUtility.UrlEncode(Site) + "&titles=" + HttpUtility.UrlEncode(Pages);
            string post = "";
            switch (Type)
            {
                case LoadTypeWD.All:
                    post = "action=wbgetentities&format=json" +  Titles + "&redirects=yes";
                    break;
                case LoadTypeWD.Label:
                    post = "action=wbgetentities&format=json" + Titles + "&redirects=yes&props=labels";
                    break;
                case LoadTypeWD.Description:
                    post = "action=wbgetentities&format=json=" + Titles + "&redirects=yes&props=descriptions";
                    break;
                case LoadTypeWD.LabelDescriptionSitelink:
                    post = "action=wbgetentities&format=json" + Titles + "&redirects=yes&props=labels|descriptions|sitelinks";
                    break;
                default:
                    break;
            }
            return PostRequest(this._URL + this._API, post);
        }

        /// <summary>
        /// Return the JSON string of the entities
        /// </summary>
        /// <param name="Entities">List of entities separated by |</param>
        /// <param name="Type">What retrieve</param>
        /// <returns>JSON string</returns>
        public string LoadWD(string Entities, LoadTypeWD Type = LoadTypeWD.All)
        {
            string post = "";
            switch (Type)
            {
                case LoadTypeWD.All:
                    post = "action=wbgetentities&format=json&ids=" + Entities + "&redirects=yes";
                    break;
                case LoadTypeWD.Label:
                    post = "action=wbgetentities&format=json&ids=" + Entities + "&redirects=yes&props=labels";
                    break;
                case LoadTypeWD.Description:
                    post = "action=wbgetentities&format=json&ids=" + Entities + "&redirects=yes&props=descriptions";
                    break;
                case LoadTypeWD.LabelDescriptionSitelink:
                    post = "action=wbgetentities&format=json&ids=" + Entities + "&redirects=yes&props=labels|descriptions|sitelinks";
                    break;
                default:
                    break;
            }
            return PostRequest(this._URL + this._API, post);
        }

        /// <summary>
        /// Load pages 
        /// </summary>
        /// <param name="Pages">Title of the page, multiple pages must separated by |</param>
        /// <returns>JSON string of the pages</returns>
        /// <see cref="https://www.mediawiki.org/wiki/API:Query"/>
        public string LoadWP(string Pages)
        {
            string request = "";
            request = "action=query&prop=pageprops|revisions&format=json&rvprop=content&titles=" + Pages;
            return PostRequest(this._URL + this._API, request);
        }

        #region Edit Entity
        /// <summary>
        /// Edit/create item
        /// </summary>
        /// <param name="id">Q number, empty string if you create a new item</param>
        /// <param name="links">Dictionary of sitelink</param>
        /// <param name="labels">Dictionary of label</param>
        /// <param name="descriptions">Dictionary of desription</param>
        /// <param name="aliases">Dictionary of list of alias</param>
        /// <param name="claims">List of claim object</param>
        /// <param name="New">true to create a new item</param>
        /// <param name="summary">Comment of the edit, empty string to auto descriptio</param>
        /// <returns>entity id for new entity, empty string for edit OK, result of post reques for errors</returns>
        public string EditEntity(string id, Dictionary<string, string> links, Dictionary<string, string> labels, Dictionary<string, string> descriptions, Dictionary<string, List<string>> aliases, List<Claim> claims, string summary)
        {
            string data = "{";
            if (links != null && links.Count > 0)
            {
                data += getJsonLinks(links);
            }
            if (labels != null && labels.Count > 0)
            {
                if (data == "{")
                {
                    data += getJsonLabels(labels);
                }
                else
                {
                    data += ", " + getJsonLabels(labels);
                }
            }
            if (descriptions != null && descriptions.Count > 0)
            {
                if (data == "{")
                {
                    data += getJsonDescriptions(descriptions);
                }
                else
                {
                    data += ", " + getJsonDescriptions(descriptions);
                }
            }
            if (aliases != null && aliases.Count > 0)
            {
                if (data == "{")
                {
                    data += getJsonAliases(aliases);
                }
                else
                {
                    data += ", " + getJsonAliases(aliases);
                }
            }
            if (claims != null && claims.Count > 0)
            {
                if (data == "{")
                {
                    data += getJsonClaims(claims);
                }
                else
                {
                    data += ", " + getJsonClaims(claims);
                }
            }
            data += "}";
            data = data.Replace("\n", " ");
            string respStr;

            if (id=="")
            {
                // Add &assert=bot to check if is logged
                string postData = string.Format("token={0}&bot=bot&data={1}&summary={2}&new=item", HttpUtility.UrlEncode(editSessionToken), HttpUtility.UrlEncode(data), HttpUtility.UrlEncode(summary));
                respStr = PostRequest(this._URL + this._API + "?action=wbeditentity&format=xml", postData);
                if (respStr.IndexOf("<api success=\"1\">")!=-1)
                {
                    return respStr;
                }
                //Extract new entity id
                int from = respStr.IndexOf("<entity id=\"") + 12;
                int to = respStr.IndexOf("\"", from);
                string tmpQ = respStr.Substring(from, to - from);
                return tmpQ;
            }
            else
            {
                // Add &assert=bot to check if is logged
                string postData = string.Format("id={0}&token={1}&bot=bot&data={2}&summary={3}", id, HttpUtility.UrlEncode(editSessionToken), HttpUtility.UrlEncode(data), HttpUtility.UrlEncode(summary));
                respStr = PostRequest(this._URL + this._API + "?action=wbeditentity&format=xml", postData);
                if (respStr.IndexOf("<api success=\"1\">")!=-1)
                {
                    return respStr;
                }
            }
            return "";
        }

        /// <summary>
        /// Create JSON string of sitelinks to use with AP
        /// </summary>
        /// <param name="links">Dictionary links</param>
        /// <returns>JSON string</returns>
        private string getJsonLinks(Dictionary<string, string> links)
        {
            string Json = "\"sitelinks\":{"; // "sitelinks":{
            foreach (KeyValuePair<string, string> pair in links)
            {
                Json += "\"" + pair.Key.Replace("-", "_") + "\":{\"site\":\"" + pair.Key.Replace("-", "_") + "\",\"title\":\"" + pair.Value + "\"},";
            }
            Json = Json.Remove(Json.LastIndexOf(","));
            Json += "}";
            return Json;
        }

        /// <summary>
        /// Create JSON string of labels to use with API
        /// </summary>
        /// <param name="labels">Dictionary of labels</param>
        /// <returns>JSON string</returns>
        private string getJsonLabels(Dictionary<string, string> labels)
        {
            string Json = "\"labels\":{";
            foreach (KeyValuePair<string, string> pair in labels)
            {
                Json += "\"" + pair.Key + "\":{\"language\":\"" + pair.Key + "\",\"value\":\"" + pair.Value + "\"},";
            }
            Json = Json.Remove(Json.LastIndexOf(","));
            Json += "}";
            return Json;
        }

        /// <summary>
        /// Create JSON string of descriptions to use with API
        /// </summary>
        /// <param name="descriptions">Dictionary of decriptions</param>
        /// <returns>JSON string</returns>
        private string getJsonDescriptions(Dictionary<string, string> descriptions)
        {
            string Json = "\"descriptions\":{";
            foreach (KeyValuePair<string, string> pair in descriptions)
            {
                Json += "\"" + pair.Key + "\":{\"language\":\"" + pair.Key + "\",\"value\":\"" + pair.Value.Replace("\"", "'") + "\"},";
            }
            Json = Json.Remove(Json.LastIndexOf(","));
            Json += "}";
            return Json;
        }

        /// <summary>
        /// Create JSON string of aliases to use with API
        /// </summary>
        /// <param name="aliases">Dictionary of aliases</param>
        /// <returns>JSON string</returns>
        private string getJsonAliases(Dictionary<string, List<string>> aliases)
        {
            string Json = "\"aliases\": {";
            foreach (KeyValuePair<string, List<string>> pair in aliases)
            {
                Json += "\"" + pair.Key + "\":[";
                foreach (string alias in pair.Value)
                {
                    Json += "{" + "\"language\": \"" + pair.Key + "\", \"value\": \"" + alias + "\"},";
                }
                Json = Json.Remove(Json.LastIndexOf(","));
                Json += "],";
            }
            Json = Json.Remove(Json.LastIndexOf(","));
            Json += "}";
            return Json;
        }

        /// <summary>
        /// Create JSON string of claims to use with API
        /// </summary>
        /// <param name="snaks">List of snak</param>
        /// <returns>JSON string</returns>
        private string getJsonClaims(List<Claim> claims)
        {
            //Claim
            string Json = "\"claims\":[";
            foreach (Claim claim in claims)
            {
                if (claim.mainsnak.snaktype == "somevalue")
                {
                    Json += "\"mainsnak\":{\"snaktype\":\"somevalue\",\"property\":\"" + claim.mainsnak.property + "\"},";
                }
                else if (claim.mainsnak.snaktype == "novalue")
                {
                    Json += "\"mainsnak\":{\"snaktype\":\"novalue\",\"property\":\"" + claim.mainsnak.property + "\"},";
                }
                else
                {
                    Json += "{";
                    Json += "\"mainsnak\":{\"snaktype\":\"value\",\"property\":\"" + claim.mainsnak.property + "\",\"datavalue\":" + claim.mainsnak.datavalue.Json;
                    Json += "\"type\":\"statement\",";
                    Json += "\"rank\":\"normal\",";
                }

                //Qualificatori
                if (claim.qualifiers != null && claim.qualifiers.Count > 0)
                {
                    Json += "\"qualifiers\":{";
                    foreach (KeyValuePair<string, List<Qualifier>> qualifiers in claim.qualifiers)
                    {
                        Json += "\"" + qualifiers.Key + "\":[";
                        List<Qualifier> _qualifiers = qualifiers.Value;
                        foreach (Qualifier qualifier in _qualifiers)
                        {
                            Json += "{\"snaktype\":\"value\",\"property\":\"" + qualifiers.Key + "\",\"datavalue\":" + qualifier.datavalue.Json;
                        }
                        Json = Json.Remove(Json.LastIndexOf(","));
                        Json += "],";
                    }
                    Json = Json.Remove(Json.LastIndexOf(","));
                    Json += "},";
                }

                //References
                if (claim.references != null && claim.references.Count > 0)
                {
                    Json += "\"references\":[";
                    foreach (Reference reference in claim.references)
                    {
                        foreach (KeyValuePair<string, List<Snak>> snaks in reference.snaks)
                        {
                            Json += "{\"snaks\":{\"" + snaks.Key + "\":[";
                            List<Snak> _snaks = snaks.Value;
                            foreach(Snak snak in _snaks)
                            {
                                Json += "{\"snaktype\":\"value\",\"property\":\"" + snaks.Key + "\",\"datavalue\":" + snak.datavalue.Json ;
                            }
                            Json = Json.Remove(Json.LastIndexOf(","));
                            Json += "]}},";
                        }
                    }
                    Json = Json.Remove(Json.LastIndexOf(","));
                    Json += "],";
                }
                
                Json = Json.Remove(Json.LastIndexOf(","));
                Json += "},";
            }
            Json = Json.Remove(Json.LastIndexOf(","));
            Json += "]";
            return Json;
        }
        #endregion
        
        /// <summary>
        /// Add or delete badge a Badge
        /// </summary>
        /// <param name="Item">Item number with Q (ex. Q42)</param>
        /// <param name="Sitelink">Sitelink where to add the badge (ex. itwiki)</param>
        /// <param name="Badge">Item of the badge, empty string to delete badge</param>
        /// <returns>Result of post request</returns>
        public string SetBadge(string Item, string Sitelink, string Badge)
        {
            string postData = string.Format("id={0}&token={1}&bot=bot&linksite={2}&badges={3}", Item, HttpUtility.UrlEncode(editSessionToken), HttpUtility.UrlEncode(Sitelink), Badge);
            string respStr = PostRequest(this._URL + this._API + "?action=wbsetsitelink&format=json", postData);
            return respStr;
        }

        /// <summary>
        /// Save a page. Creates it if it doesn't exist
        /// </summary>
        /// <param name="Page">Title of the page</param>
        /// <param name="Text">Text to be saved</param>
        /// <param name="Summary">Comment for the edit</param>
        /// <returns>Result of post request</returns>
        public string SavePage(string Page, string Text, string Summary)
        {
            string respStr = PostRequest(this._URL + this._API, "action=edit&bot=&format=xml&title=" + HttpUtility.UrlEncode(Page) + "&summary=" + Summary + "&text=" + HttpUtility.UrlEncode(Text) + "&token=" + HttpUtility.UrlEncode(editSessionToken));
            return respStr;
        }

        /// <summary>
        /// HTTP Post request
        /// </summary>
        /// <param name="PostData">Data to use in Post request</param>
        /// <param name="Logon">true to logon</param>
        /// <returns>Content of the page</returns>
        /// <remarks>http://msdn.microsoft.com/it-it/library/debx8sh9(v=vs.110).aspx?cs-save-lang=1&cs-lang=csharp#code-snippet-27</remarks>
        /// <remarks>Modified to use cookies and compression</remarks>
        public string PostRequest(string URL, string PostData, bool Logon = false)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL); //Create a request using a URL that can receive a post
            request.UserAgent = this.Version;
            request.Method = "POST";
            request.UserAgent = this.Version; //Set User agent Test with false
            request.AllowAutoRedirect = true;
            //request.Timeout = 30000;
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            request.ContentType = "application/x-www-form-urlencoded"; // Set the ContentType property of the WebRequest
            byte[] byteArray = Encoding.UTF8.GetBytes(PostData); // Create POST data and convert it to a byte array
            request.ContentLength = byteArray.Length; // Set the ContentLength property of the WebRequest

            if (_cookies.Count == 0)
            {
                request.CookieContainer = new CookieContainer();
            }
            else
            {
                request.CookieContainer = _cookies;
            }

            Stream dataStream = request.GetRequestStream(); // Get the request stream
            dataStream.Write(byteArray, 0, byteArray.Length); // Write the data to the request stream
            dataStream.Close(); // Close the Stream object

            HttpWebResponse response = (HttpWebResponse)request.GetResponse(); // Get the response
            dataStream = response.GetResponseStream(); // Get the stream containing content returned by the server.
            Stream respStream = response.GetResponseStream();
            if (response.ContentEncoding.ToLower().Contains("gzip"))
            {
                respStream = new GZipStream(respStream, CompressionMode.Decompress);
            }
            else if (response.ContentEncoding.ToLower().Contains("deflate"))
            {
                respStream = new DeflateStream(respStream, CompressionMode.Decompress);
            }

            if (Logon)
            {
                foreach (Cookie cookie in response.Cookies)
                {
                    _cookies.Add(cookie);
                }
            }
            StreamReader reader = new StreamReader(respStream, Encoding.UTF8); // Open the stream using a StreamReader for easy access
            string strResponse = reader.ReadToEnd(); // Read the content
            reader.Close();
            dataStream.Close();
            response.Close();
            return strResponse;
        }
    }
}
