using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDX_TO_MIDI
{
    internal class BDX2MIDIWrapper
    {

        private byte[] bdxData;

        public BDX2MIDIWrapper(byte[] data)
        {
            bdxData = data;
        }

        public byte[] ConvertToMidi()
        {
            var converter = new BDX2MIDI();
            return converter.Convert(bdxData);
        }

        public BdxConversionResult Analyze(byte[] bdxData)
        {
            String Title = Encoding.ASCII.GetString(bdxData, 0x48, 32).TrimEnd('\0');

            string line2 = Encoding.ASCII.GetString(bdxData, 0x68, 32).TrimEnd('\0');
            string line3 = Encoding.ASCII.GetString(bdxData, 0x88, 32).TrimEnd('\0');

            if (!string.IsNullOrWhiteSpace(line2)) Title += " " + line2;
            if (!string.IsNullOrWhiteSpace(line3)) Title += " " + line3;

            string[] instrumentNames = new string[8];
            int activeTrackCount = 0;
            for (int i = 0; i < 8; i++)
            {
                int index = 0xCA + i * 0x10;
                byte val = bdxData[index];
                if (val != 0)
                {
                    instrumentNames[i] = GetInstrumentName(val);
                    activeTrackCount++;
                }
            }
            string[] Instruments = instrumentNames.Where(n => !string.IsNullOrEmpty(n)).ToArray();
            int TrackCount = activeTrackCount;

            int tempoCount = 0;
            while ((bdxData[0x4249 + tempoCount * 4] << 8 | bdxData[0x4248 + tempoCount * 4]) != 0xFFFF)
                tempoCount++;
            int TempoChanges = tempoCount;

            return new BdxConversionResult
            {
                Instruments = Instruments,
                TrackCount = TrackCount,
                TempoChanges = TempoChanges,
                Title = Title
            };
        }

        private string GetInstrumentName(byte code)
        {
            switch (code)
            {
                case 0x01: return "Piano";
                case 0x02: return "Electric Piano";
                case 0x03: return "Rock Organ";
                case 0x04: return "Synth Lead";
                case 0x05: return "Synth Bell";
                case 0x06: return "Pipe Organ";
                case 0x07: return "Folk Guitar";
                case 0x08: return "E. Guitar";
                case 0x09: return "D. Guitar";
                case 0x0A: return "Rock Guitar";
                case 0x0B: return "Pick Bass";
                case 0x0C: return "Synth Bass";
                case 0x0D: return "A. Bass";
                case 0x0E: return "Strings";
                case 0x0F: return "Violin";
                case 0x10: return "Double Bass";
                case 0x11: return "Harp";
                case 0x12: return "Pizzicato";
                case 0x13: return "Piccolo";
                case 0x14: return "Flute";
                case 0x15: return "Clarinet";
                case 0x16: return "Oboe";
                case 0x17: return "Soprano Sax";
                case 0x18: return "Alto Sax";
                case 0x19: return "Brass";
                case 0x1A: return "Trumpet";
                case 0x1B: return "Trombone";
                case 0x1C: return "Horn";
                case 0x1D: return "Tuba";
                case 0x1E: return "Harmonica";
                case 0x1F: return "Pan Flute";
                case 0x20: return "Ocarina";
                case 0x21: return "Vibraphone";
                case 0x22: return "Marimba";
                case 0x23: return "Timpani";
                case 0x24: return "Steel Drum";
                case 0x25: return "Chorus";
                case 0x26: return "Shamisen";
                case 0x27: return "Koto";
                case 0x28: return "Shakuhachi";
                case 0x29: return "Famicom";
                case 0x83: return "Harpsichord";
                case 0x84: return "Accordion";
                case 0x85: return "Mt. Trumpet";
                case 0x86: return "Music Box";
                case 0x87: return "Banjo";
                case 0x88: return "Square Lead";
                case 0x89: return "Classical Guitar";
                case 0x8A: return "Clean Guitar";
                case 0x8B: return "OD Guitar";
                case 0x8C: return "Slap Bass";
                default: return "Unknown";
            }
        }


    }
}
