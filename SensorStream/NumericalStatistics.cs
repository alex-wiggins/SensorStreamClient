using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SensorStream
{
    public class NumericalStatistics
    {
        public double variance;
        public double mean;
        public double m2;
        public double min;
        public double max;
        public double stddev;
        public UInt64 count;
        public DateTimeOffset firstEntry;
        public DateTimeOffset lastEntry;
    }
}
