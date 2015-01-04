using System;
using System.Collections;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace VBot
{
    /// <summary>
    /// Main class for dump entities
    /// </summary>
    /// <see cref="http://dumps.wikimedia.org/other/wikidata/"/>
    public class EntitiesDump
    {
        public Entity entity { get; set; }
    }

    /// <summary>
    /// Main class for entities
    /// </summary>
    public class Entities
    {
        public Dictionary<string, Entity> entities { get; set; } //If the key is != title is a redirect
        public int success { get; set; }
        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            if (this.entities !=null)
            {
            foreach (KeyValuePair<string, Entity> tmp in this.entities)
            {
                if (tmp.Key!=tmp.Value.id)
                {
                    tmp.Value.IsRedirect = true;
                    tmp.Value.RedirectFrom = tmp.Key;
                }
                else
                {
                    tmp.Value.IsRedirect = false;
                    tmp.Value.RedirectFrom = "";
                }
            }
        }
        }
    }

    /// <summary>
    /// Main class for dump entities
    /// </summary>
    /// <see cref="https://www.mediawiki.org/wiki/Wikibase/DataModel/Primer"/>
    public class Entity
    {
        public int pageid { get; set; }
        public int ns { get; set; }
        public string title { get; set; }
        public int lastrevid { get; set; }
        public DateTime modified { get; set; }
        public string id { get; set; }
        public string type { get; set; }
        public string datatype { get; set; } //Only for Property
        public Dictionary<string, Labels> labels { get; set; }
        public Dictionary<string, Descriptions> descriptions { get; set; }
        public Dictionary<string, List<Aliases>> aliases { get; set; }
        public Dictionary<string, List<Claim>> claims { get; set; }
        public Dictionary<string, SiteLink> sitelinks { get; set; }
        public string missing { get; set; }
        public bool IsRedirect { get; set; }
        public string RedirectFrom { get; set; }

        /// <summary>
        /// Return true if property exist
        /// </summary>
        /// <param name="Property">property to check (ex P123)</param>
        /// <returns></returns>
        public bool PropertyExist(string Property)
        {
            foreach (KeyValuePair<string, List<Claim>> claim in this.claims)
            {
                if (claim.Key==Property)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Return a list of the datavalue of the property
        /// </summary>
        /// <param name="Property">property to check (ex P123)</param>
        /// <returns>list of datavalue</returns>
        public List<Datavalue> PropertyValue(string Property)
        {
            List<Datavalue> tmpList=new List<Datavalue>();
            if (this.claims != null)
            {
                foreach (KeyValuePair<string, List<Claim>> tmpClaims in this.claims)
                {
                    if (tmpClaims.Key == Property)
                    {
                        List<Claim> tmpListClaim = tmpClaims.Value;
                        foreach (Claim tmpClaim in tmpListClaim)
                        {
                            tmpList.Add(tmpClaim.mainsnak.datavalue);
                        }
                    }
                }
            }
            return tmpList;
        }
    }

    public class Language // Abstract class
    {
        public string language { get; set; }
        public string value { get; set; }
    }
    public class Labels:Language { }
    public class Descriptions:Language { }
    public class Aliases:Language { }
    public class Claim
    {
        public string id { get; set; }
        public string type { get; set; }
        public string rank { get; set; }
        public Mainsnak mainsnak { get; set; }
        public Dictionary<string, List<Qualifier>> qualifiers { get; set; }
        [JsonProperty("qualifiers-order")]
        public List<string> qualifiers_order { get; set; }
        public List<Reference> references { get; set; }
        public Claim()
        {
            mainsnak = new Mainsnak();
            qualifiers = new Dictionary<string, List<Qualifier>>();
        }
    }
    public class SiteLink
    {
        public string site { get; set; }
        public string title { get; set; }
        public List<string> badges { get; set; }

        public SiteLink() { }
    }

    public class Mainsnak
    {
        public string snaktype { get; set; }
        public string property { get; set; }
        public string datatype { get; set; }
        public Datavalue datavalue { get; set; }
        public Mainsnak() { datavalue=new Datavalue(); }
    }
    public class Qualifier
    {
        public string hash { get; set; }
        public string snaktype { get; set; }
        public string property { get; set; }
        public string datatype { get; set; }
        public Datavalue datavalue { get; set; }
    }
    public class Reference
    {
        public string hash { get; set; }
        public Dictionary<string, List<Snak>> snaks { get; set; }
        [JsonProperty("snaks-order")]
        public List<string> snaks_order { get; set; }
        public Reference()
        {
            snaks = new Dictionary<string, List<Snak>>();
        }
    }

    public class Snak
    {
        private string _Json;
        public string snaktype { get; set; }
        public string property { get; set; }
        public string datatype { get; set; }
        public Datavalue datavalue { get; set; }
        public string Json
        {
            get
            {
                _Json = "{\"snaktype\":\"value\",\"property\":\"" + property + "\",\"datavalue\":";
                _Json += datavalue.Json;
                return _Json;
            }
        }
    }

    #region Datavalue
    public class Datavalue //Abstract class
    {
        private string _Json = "";
        private string _FormatValue = "";
        public string type { get; set; }
        public virtual string Json
        {
            get
            {
                return _Json;
            }
        }
        public virtual string FormatValue
        {
            get
            {
                return _FormatValue;
            }
        }
    }
    public class DatavalueString : Datavalue
    {
        private string _Json;
        private string _FormatValue;
        public string value { get; set; }
        public override string Json
        {
            get
            {
                string tmp = "{\"value\":";
                tmp += "\"" + value + "\",";
                tmp += "\"type\":\"string\"}},";
                _Json= tmp;
                return _Json;
            }   
        }
        public override string FormatValue
        {
            get
            {
                _FormatValue = value;
                return _FormatValue;
            }
        }
    }
    public class DatavalueMonolingual : Datavalue
    {
        private string _Json;
        private string _FormatValue;
        public ValueMonolingual value { get; set; }
        public DatavalueMonolingual() { value = new ValueMonolingual(); }
        public override string Json
        {
            get
            {
                _Json = "{\"value\":";
                _Json += "{\"text\":\"" + value.text + "\",\"language\":\"" + value.language + "\"},";
                _Json += "\"type\":\"monolingualtext\"}},";
                return _Json;
            }
        }
        public override string FormatValue
        {
            get
            {
                _FormatValue = "(" + value.language + ")" + value.text;
                return _FormatValue;
            }
        }
    }
    public class DatavalueItem : Datavalue
    {
        private string _Json;
        private string _FormatValue;
        public ValueItem value { get; set; }
        public DatavalueItem() { value = new ValueItem(); }
        public override string Json
        {
            get
            {
                _Json = "{\"value\":";
                _Json += "{\"entity-type\":\"item\",\"numeric-id\":" + value.numeric_id + "},";
                _Json += "\"type\":\"wikibase-entityid\"}},";
                return _Json;
            }
        }
        public override string FormatValue
        {
            get
            {
                _FormatValue = "Q" + value.numeric_id;
                return _FormatValue;
            }
        }
    }
    public class DatavalueCoordinate : Datavalue
    {
        private string _Json;
        private string _FormatValue;
        public ValueCoordinate value { get; set; }
        public DatavalueCoordinate() { value = new ValueCoordinate(); }
        public override string Json
        {
            get
            {
                _Json = "{\"value\":";
                //_Json += "{\"latitude\":" + value.latitude + ",\"longitude\":" + value.longitude + ", \"altitude\":" + value.altitude + ", \"precision\":" + value.precision + ", \"calendarmodel\":\"" + value.globe + "\"},";
                _Json += "{\"latitude\":" + value.latitude + ",\"longitude\":" + value.longitude + ", \"precision\":" + value.precision + ", \"calendarmodel\":\"" + value.globe + "\"},";
                _Json += "\"type\":\"globecoordinate\"}},";
                return _Json;
            }
        }
        public override string FormatValue
        {
            get
            {
                _FormatValue = value.latitude + " " + value.longitude;
                return _FormatValue;
            }
        }
    }
    public class DatavalueTime : Datavalue
    {
        private string _Json;
        private string _FormatValue;
        public ValueTime value { get; set; }
        public DatavalueTime() { value = new ValueTime(); }
        public override string Json
        {
            get
            {
                _Json = "{\"value\":";
                _Json += "{\"time\":\"" + value.time + "\",\"timezone\":" + value.timezone + ", \"before\":" + value.before + ", \"after\":" + value.after + ", \"precision\":" + value.precision + ", \"calendarmodel\":\"" + value.calendarmodel + "\"},";
                _Json += "\"type\":\"time\"}},";
                return _Json;
            }
        }
        public override string FormatValue
        {
            get
            {
                _FormatValue = value.time;
                return _FormatValue;
            }
        }
    }
    public class DatavalueQuantity : Datavalue
    {
        private string _Json;
        private string _FormatValue;
        public ValueQuantity value { get; set; }
        public DatavalueQuantity() { value = new ValueQuantity(); }
        public override string Json
        {
            get
            {
                _Json = "{\"value\":";
                _Json += "{\"amount\":\"" + value.amount + "\",\"unit\":\"" + value.unit + "\", \"upperBound\":\"" + value.upperBound + "\", \"lowerBound\":\"" + value.lowerBound + "\"},";
                _Json += "\"type\":\"quantity\"}},";
                return _Json;
            }
        }
        public override string FormatValue
        {
            get
            {
                _FormatValue = value.amount;
                return _FormatValue;
            }
        }
    }

    public class ValueMonolingual
    {
        public string text { get; set; }
        public string language { get; set; }
    }
    public class ValueItem
    {
        [JsonProperty("entity-type")]
        public string entity_type { get; set; }
        [JsonProperty("numeric-id")] 
        public int numeric_id { get; set; }
    }
    public class ValueCoordinate
    {
        public string latitude { get; set; }
        public string longitude { get; set; }
        public string altitude { get; set; }
        public string precision { get; set; }
        public string globe { get; set; }
    }
    public class ValueTime
    {
        public string time { get; set; }
        public string timezone { get; set; }
        public string before { get; set; }
        public string after { get; set; }
        public string precision { get; set; }
        public string calendarmodel { get; set; }
    }
    public class ValueQuantity
    {
        public string amount { get; set; }
        public string unit { get; set; }
        public string upperBound { get; set; }
        public string lowerBound { get; set; }
    }
    #endregion

    /// <summary>
    /// Deserializazion of Datavalue
    /// </summary>
    public class DatavalueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Datavalue).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject item = JObject.Load(reader);
            string discriminator = (string)item["type"];
            Datavalue datavalue;
            switch (discriminator)
            {
                case "string":
                    datavalue = new DatavalueString();
                    break;
                case "wikibase-entityid":
                    datavalue = new DatavalueItem();
                    break;
                case "globecoordinate":
                    datavalue = new DatavalueCoordinate();
                    break;
                case "time":
                    datavalue = new DatavalueTime();
                    break;
                case "quantity":
                    datavalue = new DatavalueQuantity();
                    break;
                case "monolingualtext":
                    datavalue = new DatavalueQuantity();
                    break;
                default :
                    datavalue = null;
                    break;
            }
            serializer.Populate(item.CreateReader(), datavalue);
            return datavalue;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
