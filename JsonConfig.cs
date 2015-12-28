using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication2
{
    class JsonConfig
    {
        public string exec { get; set; }
        public string user { get; set; }
        public string pass { get; set; }
        public IList<JsonConfigItem> configs { get; set; }
    }

    class JsonConfigItem
    {
        [JsonProperty("remarks")]
        public string name { get; set; }
        public string server { get; set; }
        public int server_port { get; set; }
        public string password { get; set; }
        public string method { get; set; }
        public string aaa { get; set; }
        public string bbb { get; set; }
        public string status { get; set; }
    }
}
