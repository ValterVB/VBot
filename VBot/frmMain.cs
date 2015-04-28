using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using VBot;
using System.Web;
using System.Net;
using System.Reflection;

namespace VBot
{
    public partial class frmMain : Form
    {
        string User = "";
        string Password = "";

        public frmMain()
        {
            InitializeComponent();
            this.Text = "VBot ver. " + Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            txtPageList.Text = "";
            CompleteExample();
            MessageBox.Show("Done");
        }

        #region Example


        private void CompleteExample()
        {
            //Wikidata query
            string strWDQ = "CLAIM[31:24862] AND CLAIM[57] AND BETWEEN[577,+00000001908-00-00T00:00:00Z,+00000001908-12-31T00:00:00Z]";
            ListGenerator lg = new ListGenerator();
            List<string> chunks = lg.WDQ(strWDQ, 50);

            //Connection to Wikipedia
            WikimediaAPI WP = new WikimediaAPI("https://it.wikipedia.org", User, Password);
            Pages PageList = new Pages();
            //Connection to Wikidata
            WikimediaAPI WD = new WikimediaAPI("https://www.wikidata.org", User, Password);
            Entities EntityList = new Entities();
            Dictionary<string, string> Labels = new Dictionary<string, string>();

            foreach (string list in chunks)
            {
                // Load all the entity of the chunk
                string strJson = WD.LoadWD(list);
                EntityList = new Entities();
                EntityList = JsonConvert.DeserializeObject<Entities>(strJson, new DatavalueConverter());

                foreach (KeyValuePair<string, Entity> entity in EntityList.entities)
                {
                    if (entity.Value.sitelinks.ContainsKey("itwiki"))
                    {
                        // Load Wikipage
                        string Pages = WP.LoadWP(entity.Value.sitelinks["itwiki"].title);
                        PageList = JsonConvert.DeserializeObject<Pages>(Pages, new DatavalueConverter());
                     
                        //Director from template
                        string director = Utility.GetTemplateParameter(PageList.query.FirstPageText, "film","Regista").Replace("[","").Replace("]", "");
                        Labels = new Dictionary<string, string>();
                        if (director=="")
                        {
                            Labels.Add("en", "1908 short movie");
                        }
                        else
                        {
                            Labels.Add("en", "1908 short movie directed by " + director);
                        }
                        // Update Wikidata
                        WD.EditEntity(entity.Value.id, null, Labels, null, null, null, "BOT: Update en label");
                    }
                }
            }
        }

        /// <summary>
        /// Add Label, description, alias, sitelink, claim with qualifier and reference, all datatype.
        /// </summary>
        private void EditExample()
        {
            string list = "Q938";

            WikimediaAPI WD = new WikimediaAPI("https://test.wikidata.org", User, Password);
            string strJson = WD.LoadWD(list, WikimediaAPI.LoadTypeWD.All);

            Entities itemList = new Entities();
            itemList = JsonConvert.DeserializeObject<Entities>(strJson, new DatavalueConverter());

            Entity item = itemList.entities["Q938"];

            List<Datavalue> dvList = item.PropertyValue("P245");

            WD.SetBadge("Q89", "itwiki", "Q608");

            // Edit entity
            Dictionary<string, string> Labels = new Dictionary<string, string>();
            Dictionary<string, string> Descriptions = new Dictionary<string, string>();
            Dictionary<string, string> Sitelinks = new Dictionary<string, string>();
            Dictionary<string, List<string>> Aliases = new Dictionary<string, List<string>>();
            List<Claim> Claims = new List<Claim>();

            // To add one or more Label
            Labels.Add("it", "Label1 IT");
            Labels.Add("en", "Label1 EN");

            // To add one or more Description
            Descriptions.Add("it", "Description1 IT");
            Descriptions.Add("en", "Description1 EN");

            // To add one or more Sitelink
            Sitelinks.Add("itwiki", "Sandbox");
            Sitelinks.Add("enwiki", "Sandbox (computer security)");

            // To add one or more Alias
            List<string> itAlias = new List<string>();
            itAlias.Add("Alias1 IT");
            itAlias.Add("Alias2 IT");
            Aliases.Add("it", itAlias);
            List<string> enAlias = new List<string>();
            enAlias.Add("Alias1 EN");
            enAlias.Add("Alias2 EN");
            Aliases.Add("en", enAlias);

            // To add one or more Claim with qualifiers and references
            Claim claim = new Claim();

            claim.mainsnak.property = "P40";
            Datavalue dv = new Datavalue();
            dv = Utility.CreateDataValue("Stringa", Utility.typeData.String); // for string
            claim.mainsnak.datavalue = dv;
            Claims.Add(claim);

            claim = new Claim();
            claim.mainsnak.property = "P40";
            dv = Utility.CreateDataValue("Stringa2", Utility.typeData.String); // for string
            claim.mainsnak.datavalue = dv;
            Claims.Add(claim);

            claim = new Claim();
            claim.mainsnak.property = "P37";
            dv = Utility.CreateDataValue("Q77", Utility.typeData.Item); // for item
            claim.mainsnak.datavalue = dv;
            Claims.Add(claim);

            claim = new Claim();
            claim.mainsnak.property = "P285";
            dv = Utility.CreateDataValue("it|testo", Utility.typeData.Monolingual); // for monolingual
            claim.mainsnak.datavalue = dv;
            Claims.Add(claim);

            claim = new Claim();
            claim.mainsnak.property = "P268";
            dv = Utility.CreateDataValue("+00000002013-01-01T00:00:00Z|0|0|0|11", Utility.typeData.Time); // for time
            claim.mainsnak.datavalue = dv;
            Claims.Add(claim);

            claim = new Claim();
            claim.mainsnak.property = "P253";
            dv = Utility.CreateDataValue("8.10|9.12|0", Utility.typeData.Coordinate); // for coordinate
            claim.mainsnak.datavalue = dv;
            Claims.Add(claim);

            claim = new Claim();
            claim.mainsnak.property = "P245";
            dv = Utility.CreateDataValue("+10|1|+10|+10", Utility.typeData.Quantity); // for quantity
            claim.mainsnak.datavalue = dv;
            Claims.Add(claim);

            // To add one or more reference
            List<Reference> refs = new List<Reference>();

            Reference reference = new Reference();
            List<Snak> snaks = new List<Snak>();
            Snak snak = new Snak();
            dv = Utility.CreateDataValue("StringaFonte1", Utility.typeData.String); // for string
            snak.datavalue = dv;
            snaks.Add(snak);

            snak = new Snak();
            dv = Utility.CreateDataValue("StringaFonte2", Utility.typeData.String); // for string
            snak.datavalue = dv;
            snaks.Add(snak);

            reference.snaks.Add("P40", snaks);
            refs.Add(reference);

            //Another ref
            reference = new Reference();
            snaks = new List<Snak>();
            snak = new Snak();
            dv = Utility.CreateDataValue("it|testo", Utility.typeData.Monolingual); // for monolingual
            snak.datavalue = dv;
            snaks.Add(snak);

            reference.snaks.Add("P285", snaks);
            refs.Add(reference);

            claim.references = refs;

            //To add One or more qualifiers
            Dictionary<string, List<Qualifier>> qualifiers = new Dictionary<string, List<Qualifier>>();

            List<Qualifier> QualList = new List<Qualifier>();

            Qualifier qualifier = new Qualifier();
            dv = Utility.CreateDataValue("it|testo1", Utility.typeData.Monolingual); // for monolingual
            qualifier.datavalue = dv;
            QualList.Add(qualifier);

            qualifier = new Qualifier();
            dv = Utility.CreateDataValue("it|testo2", Utility.typeData.Monolingual); // for monolingual
            qualifier.datavalue = dv;
            QualList.Add(qualifier);

            qualifiers.Add("P285", QualList);

            QualList = new List<Qualifier>();
            qualifier = new Qualifier();
            dv = Utility.CreateDataValue("StringaQualificatore", Utility.typeData.String); // for string
            qualifier.datavalue = dv;
            QualList.Add(qualifier);

            qualifiers.Add("P40", QualList);

            claim.qualifiers = qualifiers;

            Claims.Add(claim);

            WD.EditEntity(item.title, Sitelinks, null, Descriptions, Aliases, Claims, "BOT:Test");
        }

        /// <summary>
        /// Use of WikiDataQuery - Tested: OK
        /// </summary>
        /// <see cref="http://wdq.wmflabs.org/api_documentation.html"/>
        private void WDQExample()
        {
            string strWDQ = "claim[171:10630160]"; //This is the query

            WikimediaAPI WD = new WikimediaAPI("https://www.wikidata.org", User, Password);
            Entities EntityList = new Entities();

            ListGenerator lg = new ListGenerator();
            List<string> chunks = lg.WDQ(strWDQ, 5);

            foreach (string list in chunks)
            {
                string strJson = WD.LoadWD(list);
                EntityList = new Entities();
                EntityList = JsonConvert.DeserializeObject<Entities>(strJson, new DatavalueConverter());
                
                // Show Label, Description and Sitelink
                string Text = "";
                string lang = "it";
                foreach (KeyValuePair<string, Entity> entity in EntityList.entities)
                {
                    Text += entity.Value.id;
                    if (entity.Value.labels!= null && entity.Value.labels.ContainsKey(lang))
                    {
                        Text += " Label: " + entity.Value.labels[lang].value; 
                    }
                    if (entity.Value.descriptions != null && entity.Value.descriptions.ContainsKey(lang))
                    {
                        Text += " Descriptions: " + entity.Value.descriptions[lang].value; 
                    }
                    if (entity.Value.sitelinks != null && entity.Value.sitelinks.ContainsKey(lang + "wiki"))
                    {
                        Text += " Sitelink: " + entity.Value.sitelinks[lang].title;
                    }
                    Text += Environment.NewLine;
                }
                txtPageList.AppendText(Text);
                Text = "";
            }
        }

        /// <summary>
        /// Use of "Whats links here"  - Tested: OK
        /// </summary>
        private void WhatsLinkExample()
        {
            WikimediaAPI WD = new WikimediaAPI("https://www.wikidata.org", User, Password);
            Entities EntityList = new Entities();

            ListGenerator lg = new ListGenerator();
            string[] list = { "", "" };

            do
            {
                list = lg.WhatsLinskHereWDQ("Q877358", WD, list[0]);
                string strJson = WD.LoadWD(list[1]);
                EntityList = new Entities();
                EntityList = JsonConvert.DeserializeObject<Entities>(strJson, new DatavalueConverter());

                // Show Label, Description and Sitelink
                string Text = "";
                string lang = "it";
                foreach (KeyValuePair<string, Entity> entity in EntityList.entities)
                {
                    Text += entity.Value.id;
                    if (entity.Value.labels != null && entity.Value.labels.ContainsKey(lang))
                    {
                        Text += " Label: " + entity.Value.labels[lang].value;
                    }
                    if (entity.Value.descriptions != null && entity.Value.descriptions.ContainsKey(lang))
                    {
                        Text += " Descriptions: " + entity.Value.descriptions[lang].value;
                    }
                    if (entity.Value.sitelinks != null && entity.Value.sitelinks.ContainsKey(lang + "wiki"))
                    {
                        Text += " Sitelink: " + entity.Value.sitelinks[lang + "wiki"].title;
                    }
                    Text += Environment.NewLine;
                }
                txtPageList.AppendText(Text);
                Text = "";

            } while (list[0] != "");
        }

        /// <summary>
        /// Use of Quick intersection - Tested: OK
        /// </summary>
        /// <see cref="http://tools.wmflabs.org/quick-intersection/index.php"/>

        private void QuickIntersectionExample()
        {
            WikimediaAPI WD = new WikimediaAPI("https://www.wikidata.org", User, Password);
            Entities EntityList = new Entities();

            ListGenerator lg = new ListGenerator();
            List<string> chunks=lg.QuickIntersection("it", "Film del 1930", 0, false, ListGenerator.ReturnType.Item, 10);
            foreach (string list in chunks)
            {
                string strJson = WD.LoadWD(list);
                EntityList = new Entities();
                EntityList = JsonConvert.DeserializeObject<Entities>(strJson, new DatavalueConverter());

                // Show Label, Description and Sitelink
                string Text = "";
                string lang = "it";
                foreach (KeyValuePair<string, Entity> entity in EntityList.entities)
                {
                    Text += entity.Value.id;
                    if (entity.Value.labels != null && entity.Value.labels.ContainsKey(lang))
                    {
                        Text += " Label: " + entity.Value.labels[lang].value;
                    }
                    if (entity.Value.descriptions != null && entity.Value.descriptions.ContainsKey(lang))
                    {
                        Text += " Descriptions: " + entity.Value.descriptions[lang].value;
                    }
                    if (entity.Value.sitelinks != null && entity.Value.sitelinks.ContainsKey(lang + "wiki"))
                    {
                        Text += " Sitelink: " + entity.Value.sitelinks[lang + "wiki"].title;
                    }
                    Text += Environment.NewLine;
                }
                txtPageList.AppendText(Text);
                Text = "";
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <see cref=""/>
        private void CatScanExample()
        {
            WikimediaAPI WD = new WikimediaAPI("https://www.wikidata.org", User, Password);
            Entities EntityList = new Entities();

            ListGenerator lg = new ListGenerator();
            List<string> chunks = lg.CatScan("it", "wikipedia", "Film del 1930|Film del 1931", "", "Film", "", "",ListGenerator.ReturnType.Item);
            foreach (string list in chunks)
            {
                string strJson = WD.LoadWD(list);
                EntityList = new Entities();
                EntityList = JsonConvert.DeserializeObject<Entities>(strJson, new DatavalueConverter());
                // Do something with entity list
            }

            Console.Write(chunks.Count());
        }

        /// <summary>
        /// Read the Json dump. Item and property  - Tested: OK
        /// </summary>
        /// <see cref="http://dumps.wikimedia.org/other/wikidata/"/>
        private void ReadDumpExample()
        {
            string line;
            System.IO.StreamReader file = new System.IO.StreamReader(@"D:\Wikipedia\Dump\20150427.json", Encoding.UTF8);
            line = file.ReadLine(); // first line is "["
            EntitiesDump item = new EntitiesDump();
            txtPageList.Text = "";
            while ((line = file.ReadLine()) != null)
            {
                line = line.Remove(line.Length - 1);
                string strJson = "{\"entity\":" + line + "}"; //necessary for different structure of json dump
                try
                {
                    item = JsonConvert.DeserializeObject<EntitiesDump>(strJson, new DatavalueConverter());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }                
            }
            file.Close();
        }

        /// <summary>
        /// List of all the property
        /// </summary>
        /// <param name="lang">language to use</param>
        private void PropertyTable(string lang)
        {
            WikimediaAPI WD = new WikimediaAPI("https://www.wikidata.org", User, Password);

            int From = 1;
            int To = 2000;
            int cont = 0;
            int cont1 = 0;

            string result = "{| class=\"wikitable sortable\"" + Environment.NewLine;
            result += "! Property !! type !! label !! description" + Environment.NewLine;
            string list = "";
            for (cont = From; cont <= To; cont += 1)
            {
                list = "";
                for (cont1 = cont; cont1 <= cont + 500 - 1; cont1 += 1) //era 200
                {
                    list += "P" + cont1 + "|";
                }
                cont = cont1 - 1;
                list = list.Remove(list.LastIndexOf("|"));
                string strJson = WD.LoadWD(list, WikimediaAPI.LoadTypeWD.All);
                Entities itemList = new Entities();
                itemList = JsonConvert.DeserializeObject<Entities>(strJson, new DatavalueConverter());
                foreach (KeyValuePair<string, Entity> entity in itemList.entities)
                {
                    Entity ent = entity.Value;
                    if (ent.datatype != null)
                    {
                        string lab = "";
                        if (ent.labels.ContainsKey(lang) && ent.labels != null)
                        {
                            lab = ent.labels[lang].value;
                        }
                        string desc = "";
                        if (ent.descriptions != null && ent.descriptions.ContainsKey("it"))
                        {
                            desc = ent.descriptions[lang].value;
                        }
                        result += "|-" + Environment.NewLine;
                        result += "| " + "[[Property:" + ent.id + "|" + ent.id + "]]" + " || " + ent.datatype + " ||" + lab + " || " + desc + Environment.NewLine;
                    }
                }
            }
            result += "|}";
            string res = WD.SavePage("User:ValterVBot/Labels and descriptions/it/Property", result, "Upd");
        }
        #endregion


        #region Create Label and descrption missing. To be finished
        private void LabelDescription()
        {
            WikimediaAPI WD = new WikimediaAPI("https://www.wikidata.org", User, Password);
            txtPageList.AppendText(DateTime.Now.ToLongTimeString() + Environment.NewLine);
            int From = 1;
            int To = 111500;
            int cont = 0;
            int cont1 = 0;
            string list = "";
            int daScrivere = 5000;

            List<string> LingueList=new List<string>{"de","en","es","fr", "it","hu", "ru",}; //Add language here
            int Lingue = LingueList.Count();

            Dictionary<string, List<LabDesc>> elenco = new Dictionary<string, List<LabDesc>>();
            foreach (string lang in LingueList)
            {
                elenco.Add(lang, new List<LabDesc>());
            }

            for (cont = From; cont <= To; cont += 1)
            {
                list = "";
                for (cont1 = cont; cont1 <= cont + 500 - 1; cont1 += 1) //era 200
                {
                    list += "Q" + cont1 + "|";
                }
                cont = cont1 - 1;
                list = list.Remove(list.LastIndexOf("|"));
                string strJson = WD.LoadWD(list, WikimediaAPI.LoadTypeWD.LabelDescriptionSitelink);

                Entities itemList = new Entities();
                itemList = JsonConvert.DeserializeObject<Entities>(strJson, new DatavalueConverter());
                Lingue = LingueList.Count();
                foreach (KeyValuePair<string, Entity> entity in itemList.entities)
                {
                    Entity ent = entity.Value;
                    Lingue = LingueList.Count();
                    if (ent.type != null && !ent.IsRedirect)
                    {
                        foreach (string lang in LingueList)
                        {
                            if (elenco[lang].Count == daScrivere)
                            {
                                Lingue -= 1;
                            }
                            else
                            { 
                                List<LabDesc> list2 = elenco[lang];
                                LabDesc labdesc = new LabDesc(ent, lang);
                                if (labdesc.item != null)
                                {
                                    list2.Add(labdesc);
                                }
                            }
                        }
                    }
                    if (Lingue == 0)
                    {
                        break;
                    }
                }
                if (Lingue == 0)
                {
                    break;
                }
            }
            string Tab = "";

            foreach (string lang in LingueList)
            {
                string header = "";
                switch (lang)
                {
                    case "de":
                        header = "== Liste der Objekte ohne deutsche Bezeichnung und/oder Beschreibung ==" + Environment.NewLine;
                        header += "Die ersten 5000 Objekte mit Interwikilinks. Aggiornato al : " + DateTime.Now + Environment.NewLine;
                        break;
                    case "en":
                        header = "== List of item without English labels and/or descriptions ==" + Environment.NewLine;
                        header += "First 5000 items with wiki sitelink. Update : " + DateTime.Now + Environment.NewLine;
                        break;
                    case "es":
                        header = "== List of item without Spanish labels and/or descriptions ==" + Environment.NewLine;
                        header += "First 5000 items with wiki sitelink. Update : " + DateTime.Now + Environment.NewLine;
                        break;
                    case "it":
                        header="== Lista di elementi senza etichetta e/o descrizione in italiano ==" + Environment.NewLine;
                        header += "Primi 5000 elementi con wiki sitelink. Aggiornato al : " + DateTime.Now + Environment.NewLine;
                        break;
                    default:
                        header = "== List of item without " + lang + " labels and/or descriptions ==" + Environment.NewLine;
                        header += "First 5000 items with wiki sitelink. Update : " + DateTime.Now + Environment.NewLine;
                        break;
                }
                Tab = header + Environment.NewLine;
                Tab += "{| class=\"wikitable sortable\"" + Environment.NewLine;
                Tab += "! Item !! label !! description !! sitelink" + Environment.NewLine;
                foreach (LabDesc ld in elenco[lang])
                {
                    Tab += "|-" + Environment.NewLine;
                    Tab += "| [[" + ld.item + "]] || " + ld.label + " || " + ld.description + " || " + ld.sitelink + Environment.NewLine ;
                }
                Tab += "|}";
                string res = WD.SavePage("User:ValterVBot/Labels and descriptions/" + lang, Tab, "Update");
                Console.Write("");
            }
            txtPageList.AppendText(DateTime.Now.ToLongTimeString() + Environment.NewLine);
        }
    }

    public class LabDesc
    {
        public string item { get; set; }
        public string label { get; set; }
        public string description { get; set; }
        public string sitelink { get; set; }
        public int type { get; set; }
        public bool link { get; set; }

        public LabDesc(Entity ent,string lang)
        {
            
            bool hasLabel=false;
            bool hasDescription=false;
            if (ent.labels!=null && ent.labels.ContainsKey(lang)) 
            { 
                hasLabel = true;
                this.label = ent.labels[lang].value;
            }
            if (ent.descriptions!= null && ent.descriptions.ContainsKey(lang)) 
            { 
                hasDescription = true;
                this.description  = ent.descriptions[lang].value;
            }
            if (ent.sitelinks != null && ent.sitelinks.ContainsKey(lang + "wiki"))
            {
                this.sitelink = ent.sitelinks[lang + "wiki"].title;
            }
            if (!hasLabel && hasDescription)
            {
                this.item = ent.id;
                this.type = 0; //No label
            }
            if (hasLabel && !hasDescription)
            {
                this.item = ent.id;
                this.type = 1; //No description
            }
            if (!hasLabel && !hasDescription)
            {
                this.item = ent.id;
                this.type = 2; //No all
            }
            if (ent.sitelinks!=null && ent.sitelinks.ContainsKey(lang))
            {
                this.link = true;
            }
        }
    }
    #endregion
}
