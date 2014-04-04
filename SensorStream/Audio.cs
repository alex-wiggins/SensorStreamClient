using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorStream
{
    public class Audio
    {
        [JsonProperty("StreamID")]
        public string StreamID;

        [JsonProperty("Time")]
        public string Time;

        [JsonProperty("Value")]
        public string Value;

        [JsonProperty("SpeechToText")]
        public string SpeechToText;

        [JsonProperty("Words")]
        public List<String> Words;

        [JsonProperty("WordScores")]
        public List<double> WordScores;
    }
}
