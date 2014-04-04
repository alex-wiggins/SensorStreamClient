using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorStream
{
    public class DeviceStream
    {
        [JsonProperty("StreamID")]
        public string StreamID;

        [JsonProperty("Type")]
        public string Type;

        [JsonProperty("Name")]
        public string Name;

        [JsonProperty("Description")]
        public string Description;

        [JsonProperty("Statistics")]
        public NumericalStatistics Statistics;

        [JsonProperty("Streams")]
        public List<ComplexStreamInfo> Streams = new List<ComplexStreamInfo>();

        [JsonProperty("Units")]
        public string Units;

        [JsonProperty("Data")]
        public List<Data> Data = new List<Data>();
    }

    public class ComplexStreamInfo
    {
        [JsonProperty("Name")]
        public string Name;

        [JsonProperty("Units")]
        public string Units;

        [JsonProperty("Type")]
        public string Type;

    }

    /*
    class DeviceWithComplexStream : DeviceStream
    {
        [JsonProperty("DeviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("Type")]
        public string Type;

        [JsonProperty("UserName")]
        public string UserName { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("Streams")]
        public List<ComplexStream> Streams = new List<ComplexStream>();
    }
     
    class DeviceWithSimpleStream : DeviceStream {
    
        [JsonProperty("DeviceName")]
        public string DeviceName { get; set; }

        [JsonProperty("UserName")]
        public string UserName { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("Streams")]
        public List<SimpleStream> Streams = new List<SimpleStream>();

    }     
    */
}
