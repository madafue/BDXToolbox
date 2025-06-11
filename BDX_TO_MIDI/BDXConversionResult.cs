using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDX_TO_MIDI
{
    public class BdxConversionResult
    {
        public string Title { get; set; }
        public byte[] MidiData { get; set; }
        public string[] Instruments { get; set; }
        public int TrackCount { get; set; }
        public int TempoChanges { get; set; }
        public string Message { get; set; }
    }
}
