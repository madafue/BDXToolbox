using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BDX_TO_MIDI
{
    internal class PartTrack
    {        
        public byte[] track1, track2, track3, track4, track5, track6;
        public byte[] track1F, track2F, track3F, track4F, track5F, track6F;
        public int[] trackByteCounts;
        public bool hasDrum;
        public bool hasChord;

        public PartTrack()
        {
            trackByteCounts = new int[6];
        }

        public byte[] GetTrackByIndex(int index)
        {
            switch (index)
            {
                case 1: return track2F;
                case 2: return track3F;
                case 3: return track4F;
                case 4: return track5F;
                case 5: return track6F;
                default: return null;
            }
        }

        public int GetByteCountByIndex(int index)
        {
            if (trackByteCounts == null || index < 1 || index > 5)
                return 0;
            return trackByteCounts[index];
        }

        public void SetRawTrack(int index, byte[] data)
        {
            switch (index)
            {
                case 0:
                    track1 = data;
                    break;
                case 1:
                    track2 = data;
                    break;
                case 2:
                    track3 = data;
                    break;
                case 3:
                    track4 = data;
                    break;
                case 4:
                    track5 = data;
                    break;
                case 5:
                    track6 = data;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public byte[] GetRawTrack(int index)
        {
            switch (index)
            {
                case 0: return track1;
                case 1: return track2;
                case 2: return track3;
                case 3: return track4;
                case 4: return track5;
                case 5: return track6;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public byte[] GetFinalTrack(int index)
        {
            switch (index)
            {
                case 0: return track1F;
                case 1: return track2F;
                case 2: return track3F;
                case 3: return track4F;
                case 4: return track5F;
                case 5: return track6F;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public void SetFinalTrack(int index, byte[] data)
        {
            switch (index)
            {
                case 0: track1F = data; break;
                case 1: track2F = data; break;
                case 2: track3F = data; break;
                case 3: track4F = data; break;
                case 4: track5F = data; break;
                case 5: track6F = data; break;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }


    }
}

