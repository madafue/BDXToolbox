using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDX_TO_MIDI
{
    public static class BDXConverter
    {

        public static void Convert(string inputPath, string outputPath)
        {
            byte[] bdxData = File.ReadAllBytes(inputPath);
            if (!IsValidBdx(bdxData))
                throw new InvalidDataException("Not a valid BDX file.");

            var converter = new BDX2MIDIWrapper(bdxData);
            byte[] midiData = converter.ConvertToMidi();
            File.WriteAllBytes(outputPath, midiData);
        }

        private static bool IsValidBdx(byte[] data)
        {
            return data.Length >= 0x10 &&
                data[0x4] == 'B' && data[0x5] == 'B' &&
                data[0x6] == 'D' && data[0x7] == 'X' &&
                data[0x8] == '1' && data[0x9] == '2' &&
                data[0xA] == '3' && data[0xB] == '4' &&
                data[0xC] == '3' && data[0xD] == '0' &&
                data[0xE] == '0' && data[0xF] == '0';
        }
    }
}
