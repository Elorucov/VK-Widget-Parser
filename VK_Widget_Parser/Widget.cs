using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VK_Widget_Parser {
    public class Widget {
        [JsonProperty("item")]
        public WidgetItem Item { get; private set; }
    }

    public class WidgetItem {
        [JsonProperty("type")]
        public string Type { get; private set; }

        [JsonProperty("payload")]
        public JObject Payload { get; private set; }
    }
}