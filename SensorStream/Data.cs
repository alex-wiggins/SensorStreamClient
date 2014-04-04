using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SensorStream
{
    public class Data
    {
        [JsonProperty("StreamID")]
        public string StreamID;

        [JsonProperty("Time")]
        public string Time;

        [JsonProperty("Value")]
        public string Value;

        [JsonProperty("Values")]
        public Dictionary<string, string> Values;
    }

    public class DataAddResponse
    {
        [JsonProperty("Time")]
        public DateTimeOffset Time;

        [JsonProperty("Status")]
        public string Status;
    }

    public class DataGetResponse
    {
        [JsonProperty("DeviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("UserName")]
        public string UserName { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("Stream")]
        public DeviceStream Stream;
    }
}
