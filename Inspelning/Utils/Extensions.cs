using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inspelning.Recorder.Utils
{
    public static class Extensions
    {
        public enum SizeUnits
        {
            Byte, Kb, Mb, Gb, Tb, Pb, Eb, Zb, Yb
        }

        public static string ToSize(this ulong value, SizeUnits unit)
        {
            return (value / Math.Pow(1024, (ulong)unit)).ToString("0");
        }
    }
}
