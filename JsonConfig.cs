using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication2
{
    class JsonConfig
    {
        public int index { get; set; }
        public bool random { get; set; }
        public int sysProxyMode { get; set; }
        public bool shareOverLan { get; set; }
        public int localPort { get; set; }
        public string localAuthPassword { get; set; }
        public string dnsServer { get; set; }
        public int reconnectTimes { get; set; }
        public int randomAlgorithm { get; set; }
        public bool randomInGroup { get; set; }
        public int TTL { get; set; }
        public int connectTimeout { get; set; }
        public int proxyRuleMode { get; set; }
        public bool proxyEnable { get; set; }
        public bool pacDirectGoProxy { get; set; }
        public int proxyType { get; set; }
        public object proxyHost { get; set; }
        public int proxyPort { get; set; }
        public object proxyAuthUser { get; set; }
        public object proxyAuthPass { get; set; }
        public object proxyUserAgent { get; set; }
        public object authUser { get; set; }
        public object authPass { get; set; }
        public bool autoBan { get; set; }
        public bool sameHostForSameTarget { get; set; }
        public int keepVisitTime { get; set; }
        public bool isHideTips { get; set; }
        public bool nodeFeedAutoUpdate { get; set; }
        public Token token { get; set; }
        public PortMap portMap { get; set; }
        public IList<ServerSubscribe> serverSubscribes { get; set; }
        public IList<JsonConfigItem> configs { get; set; }
    }

    public class Token
    {
    }

    public class PortMap
    {
    }

    class ServerSubscribe 
    { 
        public string URL { get; set; }
        public string Group { get; set; }
    }

    class JsonConfigItem
    {
        public string remarks { get; set; }
        public string id { get; set; }
        public string server { get; set; }
        public int server_port { get; set; }
        public int server_udp_port { get; set; }
        public string password { get; set; }
        public string method { get; set; }
        public string protocol { get; set; }
        public string protocolparam { get; set; }
        public string obfs { get; set; }
        public string obfsparam { get; set; }
        public string remarks_base64 { get; set; }
        public string group { get; set; }
        public bool enable { get; set; }
        public bool udp_over_tcp { get; set; }
        public string aaa { get; set; }
        public string bbb { get; set; }
        public string status { get; set; }
    }
}
