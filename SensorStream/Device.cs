using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorStream
{
    public class Device
    {
        [JsonProperty("UserName")]
        public string UserName;

        [JsonProperty("DeviceName")]
        public string DeviceName;

        [JsonProperty("guid")]
        public Guid guid;

        [JsonProperty("Created")]
        public DateTimeOffset Created;

        [JsonProperty("LatestIP")]
        public string LatestIP;
        
        [JsonProperty("Description")]
        public string Description;

        [JsonProperty("Streams")]
        public List<DeviceStream> Streams = new List<DeviceStream>();
    }
    public class DeviceList
    {
        [JsonProperty("Devices")]
        public List<Device> Devices { get; set; }
    }

}
