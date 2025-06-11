using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace BDX_TO_MIDI
{
    internal class BDX2MIDI
    {
        public byte[] Convert(byte[] bdxData)
        {
            bool valid = TryParseBDXFile(bdxData);
            if (!valid)
                throw new InvalidOperationException("Invalid BDX file format.");

            bdx_Load();
            return midiOutput;
        }

        static int NormalizeInstrumentByte(int bdxByte)
        {
            if (bdxByte >= 0x31 && bdxByte <= 0x59) return bdxByte - 0x30;
            if (bdxByte >= 0x5A && bdxByte <= 0x82) return bdxByte - 0x59;
            if (bdxByte >= 0x8D && bdxByte <= 0x96) return bdxByte - 0x0A;
            if (bdxByte >= 0x97 && bdxByte <= 0xA0) return bdxByte - 0x14;
            return bdxByte;
        }

        // Mapping from normalized BDX instrument codes to General MIDI program numbers
        static readonly Dictionary<int, int> InstrumentMap = new Dictionary<int, int>
        {
            { 0x01, 0x00 }, // Piano
            { 0x02, 0x05 }, // Electric Piano
            { 0x03, 0x11 }, // Rock Organ
            { 0x04, 0x51 }, // Synth Lead
            { 0x05, 0x0C }, // Synth Bell
            { 0x06, 0x13 }, // Pipe Organ
            { 0x07, 0x19 }, // Folk Guitar
            { 0x08, 0x1A }, // E. Guitar
            { 0x09, 0x1E }, // D. Guitar
            { 0x0A, 0x1E }, // Rock Guitar
            { 0x0B, 0x22 }, // Pick Bass
            { 0x0C, 0x26 }, // Synth Bass
            { 0x0D, 0x20 }, // Acoustic Bass
            { 0x0E, 0x2C }, // Strings
            { 0x0F, 0x28 }, // Violin
            { 0x10, 0x2B }, // Double Bass
            { 0x11, 0x2E }, // Harp
            { 0x12, 0x2D }, // Pizzicato
            { 0x13, 0x48 }, // Piccolo
            { 0x14, 0x49 }, // Flute
            { 0x15, 0x47 }, // Clarinet
            { 0x16, 0x44 }, // Oboe
            { 0x17, 0x40 }, // Soprano Sax
            { 0x18, 0x41 }, // Alto Sax
            { 0x19, 0x3D }, // Brass
            { 0x1A, 0x38 }, // Trumpet
            { 0x1B, 0x39 }, // Trombone
            { 0x1C, 0x3C }, // Horn
            { 0x1D, 0x3A }, // Tuba
            { 0x1E, 0x16 }, // Harmonica
            { 0x1F, 0x4B }, // Pan Flute
            { 0x20, 0x4F }, // Ocarina
            { 0x21, 0x0B }, // Vibraphone
            { 0x22, 0x0D }, // Marimba
            { 0x23, 0x2F }, // Timpani
            { 0x24, 0x72 }, // Steel Drum
            { 0x25, 0x34 }, // Chorus
            { 0x26, 0x6A }, // Shamisen
            { 0x27, 0x6B }, // Koto
            { 0x28, 0x4D }, // Shakuhachi
            { 0x29, 0x50 }, // Famicom / Square Lead
            { 0x83, 0x06 }, // Harpsichord
            { 0x84, 0x15 }, // Accordion
            { 0x85, 0x3B }, // Mt. Trumpet
            { 0x86, 0x0A }, // Music Box
            { 0x87, 0x69 }, // Banjo
            { 0x88, 0x50 }, // Square Lead (duplicate of Famicom)
            { 0x89, 0x18 }, // Classical Guitar
            { 0x8A, 0x1B }, // Clean Guitar
            { 0x8B, 0x1D }, // Overdrive Guitar
            { 0x8C, 0x24 }  // Slap Bass
        };


        static int instSet(int bdxByte)
        {
            int normalized = NormalizeInstrumentByte(bdxByte);
            return InstrumentMap.TryGetValue(normalized, out int midiInstrument)
                ? midiInstrument
                : 0x01; // default: Piano
        }

        enum DrumButton
        {
            None, B, A, Y, X, Up, Down, Left, Right, L, R
        }

        static readonly Dictionary<string, Dictionary<DrumButton, int>> DrumKits =
            new Dictionary<string, Dictionary<DrumButton, int>>
            {
                { "Rock Drum", new Dictionary<DrumButton, int>
                    {
                        { DrumButton.None, 0x00 }, { DrumButton.B, 0x23 }, { DrumButton.A, 0x26 }, { DrumButton.Y, 0x25 },
                        { DrumButton.X, 0x36 }, { DrumButton.Up, 0x2A }, { DrumButton.Down, 0x2F },
                        { DrumButton.Left, 0x2D }, { DrumButton.Right, 0x30 }, { DrumButton.L, 0x2E }, { DrumButton.R, 0x31 }
                    }
                },
                { "E. Drum", new Dictionary<DrumButton, int>
                    {
                        { DrumButton.None, 0x00 }, { DrumButton.B, 0x23 }, { DrumButton.A, 0x28 }, { DrumButton.Y, 0x33 },
                        { DrumButton.X, 0x36 }, { DrumButton.Up, 0x2A }, { DrumButton.Down, 0x2F },
                        { DrumButton.Left, 0x2D }, { DrumButton.Right, 0x30 }, { DrumButton.L, 0x2E }, { DrumButton.R, 0x31 }
                    }
                },
                { "Synth Drum", new Dictionary<DrumButton, int>
                    {
                        { DrumButton.None, 0x00 }, { DrumButton.B, 0x20 }, { DrumButton.A, 0x28 }, { DrumButton.Y, 0x1E },
                        { DrumButton.X, 0x36 }, { DrumButton.Up, 0x46 }, { DrumButton.Down, 0x2F },
                        { DrumButton.Left, 0x2D }, { DrumButton.Right, 0x30 }, { DrumButton.L, 0x2E }, { DrumButton.R, 0x31 }
                    }
                },
                { "Gakudan Set", new Dictionary<DrumButton, int>
                    {
                        { DrumButton.None, 0x00 }, { DrumButton.B, 0x23 }, { DrumButton.A, 0x26 }, { DrumButton.Y, 0x51 },
                        { DrumButton.X, 0x26 }, { DrumButton.Up, 0x34 }, { DrumButton.Down, 0x36 },
                        { DrumButton.Left, 0x26 }, { DrumButton.Right, 0x36 }, { DrumButton.L, 0x37 }, { DrumButton.R, 0x31 }
                    }
                },
                { "Bongo Set", new Dictionary<DrumButton, int>
                    {
                        { DrumButton.None, 0x00 }, { DrumButton.B, 0x3D }, { DrumButton.A, 0x3C }, { DrumButton.Y, 0x5C },
                        { DrumButton.X, 0x5D }, { DrumButton.Up, 0x36 }, { DrumButton.Down, 0x57 },
                        { DrumButton.Left, 0x27 }, { DrumButton.Right, 0x45 }, { DrumButton.L, 0x42 }, { DrumButton.R, 0x41 }
                    }
                },
                { "Conga Set", new Dictionary<DrumButton, int>
                    {
                        { DrumButton.None, 0x00 }, { DrumButton.B, 0x40 }, { DrumButton.A, 0x3F }, { DrumButton.Y, 0x3E },
                        { DrumButton.X, 0x3A }, { DrumButton.Up, 0x53 }, { DrumButton.Down, 0x52 },
                        { DrumButton.Left, 0x55 }, { DrumButton.Right, 0x38 }, { DrumButton.L, 0x4D }, { DrumButton.R, 0x4C }
                    }
                },
                { "Daiko Set", new Dictionary<DrumButton, int>
                    {
                        { DrumButton.None, 0x00 }, { DrumButton.B, 0x36 }, { DrumButton.A, 0x26 }, { DrumButton.Y, 0x3D },
                        { DrumButton.X, 0x39 }, { DrumButton.Up, 0x4B }, { DrumButton.Down, 0x40 },
                        { DrumButton.Left, 0x43 }, { DrumButton.Right, 0x2D }, { DrumButton.L, 0x2E }, { DrumButton.R, 0x34 }
                    }
                },
            };

        static int GetDrumNote(string kitName, DrumButton button)
        {
            if (DrumKits.ContainsKey(kitName) && DrumKits[kitName].ContainsKey(button))
                return DrumKits[kitName][button];
            else
                return 0; // Default to silence if invalid kit/button
        }

        static string GetDrumKitName(int bdxValue)
        {
            switch (bdxValue)
            {
                case 0x2A: return "Rock Drum";
                case 0x2B: return "E. Drum";
                case 0x2C: return "Synth Drum";
                case 0x2D: return "Gakudan Set";
                case 0x2E: return "Bongo Set";
                case 0x2F: return "Conga Set";
                case 0x30: return "Daiko Set";
                default: return "Rock Drum"; // fallback
            }
        }


        //=====================================================

        [DllImport("msvcrt")] static extern int WaitForKeyPress();

        static byte[] bdxData = new byte[0x8000];
        static byte[] midiOutput;

        static int[] tempos, tempoDeltas, tempoDeltaLengths;

        static byte[] tempoTrackRaw, tempoTrackFinal;

        static int tempoTrackByteCount;

        static PartTrack[] partTracks = new PartTrack[8];

        struct GuitarChord
        {
            public int[] frets;
            public bool[] mutes;
        }
        static GuitarChord[] originalChords = new GuitarChord[16];



        static bool TryParseBDXFile(byte[] input)
        {
            string expectedHeader = "BBDX12343000";
            string actualHeader = Encoding.ASCII.GetString(input, 0x4, expectedHeader.Length);

            Console.WriteLine(actualHeader);

            if (actualHeader == expectedHeader)
            {
                bdxData = input;
                return true;
            }

            return false;
        }

        static void bdx_Load()
        {
            // Initialize all part tracks with default values
            for (int i = 0; i < partTracks.Length; i++)
            {
                partTracks[i] = new PartTrack();
                partTracks[i].trackByteCounts = new int[6];
                partTracks[i].hasDrum = false;
                partTracks[i].hasChord = false;
            }

            //Tempo and Text
            int tempoEventCount = 0;
            tempoTrackRaw = new byte[0x10000];

            while ((bdxData[0x4249 + tempoEventCount * 4] << 8 | bdxData[0x4248 + tempoEventCount * 4]) != 0xFFFF)
            {
                tempoEventCount++;
            }

            tempos = new int[tempoEventCount];
            tempoDeltas = new int[tempoEventCount];
            tempoDeltaLengths = new int[tempoEventCount];

            for (int i = 0; i < tempoEventCount; i++)
            {
                // Tempo is stored as 2 bytes (big endian) at offset 0x424A + i * 4
                int bpm = (bdxData[0x424B + i * 4] << 8) | bdxData[0x424A + i * 4];
                tempos[i] = 60000000 / bpm;

                if (i == 0)
                {
                    tempoDeltas[i] = 0;
                }
                else
                {
                    // Time offset delta between current and previous tempo change
                    int currentTime = (bdxData[0x4249 + i * 4] << 8) | bdxData[0x4248 + i * 4];
                    int previousTime = (bdxData[0x4249 + (i - 1) * 4] << 8) | bdxData[0x4248 + (i - 1) * 4];
                    int deltaTicks = (currentTime - previousTime) * 40;

                    tempoDeltas[i] = DTcalc(deltaTicks, ref tempoDeltaLengths[i]);
                }
            }


            int j = 8;

            // Write MTrk header
            tempoTrackRaw[0] = 0x4D; // 'M'
            tempoTrackRaw[1] = 0x54; // 'T'
            tempoTrackRaw[2] = 0x72; // 'r'
            tempoTrackRaw[3] = 0x6B; // 'k'

            // Reserve space for track length (will be filled later)
            for (int i = 4; i < 8; i++) { 
                tempoTrackRaw[i] = 0; 
            }

            // Insert "bdx2midi" as a text meta event
            tempoTrackRaw[j++] = 0x00; // Delta time
            tempoTrackRaw[j++] = 0xFF; // Meta event
            tempoTrackRaw[j++] = 0x01; // Text event
            tempoTrackRaw[j++] = 0x08; // Length = 8 bytes
            tempoTrackRaw[j++] = (byte)'b';
            tempoTrackRaw[j++] = (byte)'d';
            tempoTrackRaw[j++] = (byte)'x';
            tempoTrackRaw[j++] = (byte)'2';
            tempoTrackRaw[j++] = (byte)'m';
            tempoTrackRaw[j++] = (byte)'1';
            tempoTrackRaw[j++] = (byte)'d';
            tempoTrackRaw[j++] = (byte)'1';
            tempoTrackRaw[j++] = 0x00; // End of event delta time before next

            // Write each tempo event
            for (int i = 0; i < tempoEventCount; i++)
            {
                // Write variable-length delta time
                int length = tempoDeltaLengths[i];
                int delta = tempoDeltas[i];

                for (int b = length - 1; b >= 0; b--)
                {
                    tempoTrackRaw[j++] = (byte)((delta >> (8 * b)) & 0xFF);
                }

                // Write MIDI tempo meta event: FF 51 03 tt tt tt
                tempoTrackRaw[j++] = 0xFF;
                tempoTrackRaw[j++] = 0x51;
                tempoTrackRaw[j++] = 0x03;
                tempoTrackRaw[j++] = (byte)((tempos[i] >> 16) & 0xFF);
                tempoTrackRaw[j++] = (byte)((tempos[i] >> 8) & 0xFF);
                tempoTrackRaw[j++] = (byte)(tempos[i] & 0xFF);
            }

            // End of track marker
            tempoTrackRaw[j++] = 0x00;
            tempoTrackRaw[j++] = 0xFF;
            tempoTrackRaw[j++] = 0x2F;
            tempoTrackRaw[j++] = 0x00;

            // Insert actual track length (overwrite dummy zeros)
            int trackLength = j - 8;
            tempoTrackRaw[4] = (byte)((trackLength >> 24) & 0xFF);
            tempoTrackRaw[5] = (byte)((trackLength >> 16) & 0xFF);
            tempoTrackRaw[6] = (byte)((trackLength >> 8) & 0xFF);
            tempoTrackRaw[7] = (byte)(trackLength & 0xFF);

            // Finalize track data
            tempoTrackFinal = new byte[trackLength + 8];
            Array.Copy(tempoTrackRaw, tempoTrackFinal, trackLength + 8);
            tempoTrackByteCount = trackLength + 8;

            // Apply score changes to each part
            for (int i = 0; i < 8; i++)
            {
                scoreChanger(i, ref partTracks[i]);
            }

            // Calculate total byte count and track count for drum parts
            int totalDrumBytes = 0;
            int drumTrackCount = 0;
            for (int i = 0; i < 8; i++)
            {
                if (partTracks[i].hasDrum)
                {
                    totalDrumBytes += partTracks[i].trackByteCounts[1]; // Drum is always track2
                    drumTrackCount++;
                }
            }

            // Calculate total byte count and track count for chord parts (tracks 2–6)
            int totalChordBytes = 0;
            int chordTrackCount = 0;
            for (int i = 0; i < 8; i++)
            {
                if (partTracks[i].hasChord)
                {
                    for (int n = 1; n < 6; n++)
                        totalChordBytes += partTracks[i].trackByteCounts[n];
                    chordTrackCount += 5; // Chords always use 5 tracks
                }
            }

            // Calculate total byte count for main melody parts (track1)
            int totalMelodyBytes = 0;
            for (int i = 0; i < 8; i++)
                totalMelodyBytes += partTracks[i].trackByteCounts[0];

            // Allocate enough space for the final MIDI file
            int totalBytes = 14 + tempoTrackByteCount + totalMelodyBytes + totalDrumBytes + totalChordBytes;
            midiOutput = new byte[totalBytes];

            // Write MIDI header chunk (MThd)
            midiOutput[0] = 0x4D; // 'M'
            midiOutput[1] = 0x54; // 'T'
            midiOutput[2] = 0x68; // 'h'
            midiOutput[3] = 0x64; // 'd'
            midiOutput[4] = 0x00;
            midiOutput[5] = 0x00;
            midiOutput[6] = 0x00;
            midiOutput[7] = 0x06;
            midiOutput[8] = 0x00;
            midiOutput[9] = 0x01; // Format type 1
            midiOutput[10] = 0x00;
            midiOutput[11] = (byte)(9 + drumTrackCount + chordTrackCount); // total track count
            midiOutput[12] = 0x01;
            midiOutput[13] = 0xE0; // Ticks per quarter note (480)

            // Begin writing track data after header
            int offset = 14;

            // Tempo track
            WriteMidiChunk(tempoTrackByteCount, tempoTrackFinal, ref offset);

            // Each part
            for (int i = 0; i < 8; i++)
            {
                // Melody (track1)
                WriteMidiChunk(partTracks[i].trackByteCounts[0], partTracks[i].track1F, ref offset);

                if (partTracks[i].hasDrum)
                {
                    // Drum (track2 only)
                    WriteMidiChunk(partTracks[i].trackByteCounts[1], partTracks[i].track2F, ref offset);
                }
                else if (partTracks[i].hasChord)
                {
                    // Chord uses track2 through track6
                    for (int t = 1; t < 6; t++)
                        WriteMidiChunk(partTracks[i].trackByteCounts[t], partTracks[i].GetTrackByIndex(t), ref offset);
                }
            }
        }

        static void WriteMidiChunk(int trackLength, byte[] trackData, ref int writeIndex)
        {
            for (int i = 0; i < trackLength; i++)
            {
                midiOutput[writeIndex] = trackData[i];
                writeIndex++;
            }
        }

        static void scoreChanger(int partIndex, ref PartTrack track)
        {
            byte partType = bdxData[0xCB + partIndex * 0x10];
            byte rawInstrument = bdxData[0xCA + partIndex * 0x10];

            if (rawInstrument == 0)
            {
                nonepart(ref track);
                return;
            }

            int midiInstrument = instSet(rawInstrument);

            switch (partType)
            {
                case 0: // Doremi (Monophonic)
                    track.track1 = new byte[0x10000];
                    GenerateMonoTrack(partIndex, midiInstrument, ref track.track1, ref track.track1F, ref track.trackByteCounts[0]);
                    break;

                case 1: // Drum
                    track.hasDrum = true;
                    track.track1 = new byte[0x10000];
                    track.track2 = new byte[0x10000];
                    CreateDrumTrack(partIndex, ref track.track1, ref track.track1F, ref track.trackByteCounts[0], 4);
                    CreateDrumTrack(partIndex, ref track.track2, ref track.track2F, ref track.trackByteCounts[1], 0);
                    break;

                default: // Chord
                    track.hasChord = true;

                    // Load original chord definitions if type is 2
                    if (partType == 2)
                    {
                        for (int i = 0; i < originalChords.Length; i++)
                        {
                            int offset = 0x4AC8 + i * 4;
                            int chordBinary =
                                (bdxData[offset + 3] << 24) |
                                (bdxData[offset + 2] << 16) |
                                (bdxData[offset + 1] << 8) |
                                bdxData[offset];

                            for (int j = 0; j < 6; j++)
                            {
                                originalChords[i].frets[j] = (chordBinary >> (j * 5)) & 0xF;
                                originalChords[i].mutes[j] = ((chordBinary >> (4 + j * 5)) & 0x1) != 0;
                            }
                        }
                    }

                    // Allocate and generate tracks for each string
                    for (int stringIndex = 0; stringIndex < 6; stringIndex++)
                    {
                        // Create temp arrays
                        byte[] rawTrack = new byte[0x10000];
                        byte[] finalTrack = null;

                        // Pass by ref
                        chordChange(
                            partIndex,
                            midiInstrument,
                            ref rawTrack,
                            ref finalTrack,
                            ref track.trackByteCounts[stringIndex],
                            stringIndex
                        );

                        // Store them back in the struct
                        track.SetRawTrack(stringIndex, rawTrack);
                        track.SetFinalTrack(stringIndex, finalTrack);
                    }
                    break;
            }
        }




        static void nonepart(ref PartTrack part)
        {
            part.track1F = new byte[]
            {
                0x4D, 0x54, 0x72, 0x6B, // "MTrk"
                0x00, 0x00, 0x00, 0x04, // Track length = 4 bytes
                0x00,                   // Delta time
                0xFF, 0x2F, 0x00        // End of Track meta event
            };

            part.trackByteCounts[0] = part.track1F.Length;
        }

        //Delta time calculation. Integer value → Variable length value
        static int DTcalc(int ticks, ref int byteCount)
        {
            int dtA = ((ticks >> 21) & 0x7F) | 0x80;
            int dtB = ((ticks >> 14) & 0x7F) | 0x80;
            int dtC = ((ticks >> 7) & 0x7F) | 0x80;
            int dtD = ticks & 0x7F;

            int encoded;
            if (ticks <= 0x7F)
            {
                byteCount = 1;
                encoded = dtD;
            }
            else if (ticks <= 0x3FFF)
            {
                byteCount = 2;
                encoded = (dtC << 8) | dtD;
            }
            else if (ticks <= 0x1FFFFF)
            {
                byteCount = 3;
                encoded = (dtB << 16) | (dtC << 8) | dtD;
            }
            else if (ticks <= 0x0FFFFFFF)
            {
                byteCount = 4;
                encoded = (dtA << 24) | (dtB << 16) | (dtC << 8) | dtD;
            }
            else
            {
                byteCount = 1;
                encoded = 0;
                Console.WriteLine("Delta time is too large; setting to 0.");
            }

            return encoded;
        }

        //Divide the calculated delta time into 1 byte each
        static void WriteDeltaTimeBytes(ref byte[] trackBuffer, int byteCount, ref int index, ref int deltaTime)
        {
            switch (byteCount)
            {
                case 4:
                    trackBuffer[index++] = (byte)((deltaTime >> 24) & 0xFF);
                    goto case 3;

                case 3:
                    trackBuffer[index++] = (byte)((deltaTime >> 16) & 0xFF);
                    goto case 2;

                case 2:
                    trackBuffer[index++] = (byte)((deltaTime >> 8) & 0xFF);
                    goto case 1;

                case 1:
                default:
                    trackBuffer[index++] = (byte)(deltaTime & 0xFF);
                    break;
            }
        }

        //Score (Monophonic)
        static void GenerateMonoTrack(int partIndex, int midiInstrument, ref byte[] rawTrackBuffer, ref byte[] finalTrack, ref int finalTrackLength)
        {
            int[] midiEvents = new int[0x800];
            int midiEventCount = 0;

            int[] tripletOffsets = { 0, -3, -2, -1 };
            int tripletCounter = 0;
            bool tripletActive = false;

            int[] volumeEnvelope = GetVolumeMap(partIndex);
            int baseVolume = bdxData[0xC8 + partIndex * 0x10];

            int maxTicks = bdxData[0xAD] * bdxData[0xAA] * 4;
            int trackOffset = 0x248 + partIndex * 0x800;

            for (int i = 0; i < maxTicks; i++)
            {
                byte note = bdxData[trackOffset + i];

                if (note < 0x80)
                {
                    int timing = (i * 3) + tripletOffsets[tripletCounter];
                    int volume = (int)(127f * (baseVolume / 127f) * (volumeEnvelope[i] / 127f));

                    midiEvents[midiEventCount++] = (timing << 16) | (volume << 8) | note;
                }
                else if (note == 0xFF)
                {
                    tripletActive = true;
                }

                if (tripletActive)
                {
                    tripletCounter++;
                    if (tripletCounter > 3)
                    {
                        tripletActive = false;
                        tripletCounter = 0;
                    }
                }
            }

            MidiBinSet(partIndex, midiInstrument, ref rawTrackBuffer, ref finalTrack, ref finalTrackLength, midiEvents, midiEventCount);
        }

        //Score (chord)
        static void chordChange(int partIndex, int midiInst, ref byte[] rawTrack, ref byte[] finalTrack, ref int byteCount, int strIndex)
        {
            //Convert to single note and create MIDI instructions using chordSet()
            byte[,] chordMatrix = new byte[6, 0x800];
            int[] toChord = new int[0x800];
            for (int i = 0; i < 6; i++) { for (int j = 0; j < 0x800; j++) chordMatrix[i, j] = 0; } // bdxc initialization
            for (int i = 0; i < 0x800; i++) toChord[i] = 0x8080; // toChord initialization
            int[] tripletOffsetsChord = new int[3] { 0, 2, 1 };

            for (int i = 0; i < bdxData[0x6918]; i++) //Write where the code changes
            {
                toChord[(bdxData[0x691D + i * 4] * 0x100 + bdxData[0x691C + i * 4]) / 3
                    + tripletOffsetsChord[(bdxData[0x691D + i * 4] * 0x100 + bdxData[0x691C + i * 4]) % 3]]
                    = (bdxData[0x691F + i * 4] * 0x100 + bdxData[0x691E + i * 4]);
            }
            for (int i = 0; i < 0x800; i++) //Fill in the places where the code has not changed with the code from the last time it changed.
            {
                if (i == 0) i++;
                if (toChord[i] == 0x8080) toChord[i] = toChord[i - 1];
            }

            for (int a = 0; a < 0x800; a++)
            {
                if (bdxData[0x248 + partIndex * 0x800 + a] == 0) //If it's a rest
                {
                    for (int b = 0; b < 6; b++) chordMatrix[b, a] = 0;
                }
                else if (bdxData[0x248 + partIndex * 0x800 + a] < 0x80) //If it's a musical note
                {
                    //play a piano chord 
                    if (bdxData[0xCB + partIndex * 0x10] == 3)
                    {
                        //Unless it's original code
                        if ((toChord[a] >> 8 & 0xFF) != 0xFF)
                        {
                            int voicingTop = bdxData[0x7F18];
                            int voicingSameNum = bdxData[0x7F19] / 3; //Simultaneous polyphony 0...3 pieces, 1...4 pieces
                            int voicingInterval = bdxData[0x7F19] % 3; //Sound interval 0 dense, 1 medium, 2 coarse
                            if (voicingSameNum == 0) //If the number of simultaneous polyphony is 3
                            {
                                int[] onp = new int[3];
                                for (int v = 0; v < 3; v++)
                                {
                                    int MIDI_R = GetMidiRoot(toChord[a] >> 8 & 0xFF); //Determine the base sound from the root note notation
                                    int[] MIDI_S = GetTriadChord(toChord[a] & 0xFF); //Deciding the sound to play from the notation of the constituent sounds
                                    onp[v] = 0x30 + 12 * (voicingTop / 12 - 4) + MIDI_R + MIDI_S[v]; //Octave adjustment
                                    while (onp[v] > voicingTop) onp[v] -= 12; //Lower the voicing by one octave while exceeding the highest note
                                }
                                sortAscending(onp, 3); //line up in ascending order of numbers
                                if (voicingInterval == 1) onp[1] -= 12; //If the interval is medium
                                else if (voicingInterval == 2) //If the interval is rough
                                {
                                    onp[1] -= 24;
                                    onp[0] -= 12;
                                }
                                for (int v = 0; v < 3; v++) //Write to MIDI instructions
                                {
                                    chordMatrix[v, a] = (byte)onp[v];
                                }
                            }
                            else
                            {
                                if ((toChord[a] & 0xFF) == 0 ||
                                    (toChord[a] & 0xFF) == 1 ||
                                    (toChord[a] & 0xFF) == 7 ||
                                    (toChord[a] & 0xFF) == 8) //The number of simultaneous notes is 4, but the 4th note is an octave different from the root note.
                                {
                                    int[] onp = new int[3];
                                    int lastOnp = 0;
                                    for (int v = 0; v < 3; v++)
                                    {
                                        int MIDI_R = GetMidiRoot(toChord[a] >> 8 & 0xFF); //Determine the base sound from the root note notation
                                        int[] MIDI_S = GetTriadChord(toChord[a] & 0xFF); //Deciding the sound to play from the notation of the constituent sounds
                                        onp[v] = 0x30 + 12 * (voicingTop / 12 - 4) + MIDI_R + MIDI_S[v]; //octave adjustment
                                        if (onp[v] > voicingTop)
                                        {
                                            while (onp[v] > voicingTop) onp[v] -= 12; //Lower the voicing by one octave while exceeding the highest note
                                        }
                                        else lastOnp = onp[v] - 12; //Decide on the fourth note
                                    }
                                    sortAscending(onp, 3); //line up in ascending order of numbers
                                    if (voicingInterval == 1) onp[1] -= 12; //If the interval is medium
                                    else if (voicingInterval == 2) //If the spacing is rough
                                    {
                                        onp[1] -= 12;
                                        lastOnp -= 12;
                                    }
                                    for (int v = 0; v < 3; v++) //MIDI指示書に書き込む
                                    {
                                        chordMatrix[v, a] = (byte)onp[v];
                                    }
                                    chordMatrix[4, a] = (byte)lastOnp;
                                }
                                else //When the number of simultaneous polyphony is 4 and all 4 have different sounds
                                {
                                    int[] onp = new int[4];
                                    for (int v = 0; v < 4; v++)
                                    {
                                        int MIDI_R = GetMidiRoot(toChord[a] >> 8 & 0xFF); //Determine the base sound from the root note notation
                                        int[] MIDI_S = GetTetradChord(toChord[a] & 0xFF); //Deciding the sound to play from the notation of the constituent sounds
                                        onp[v] = 0x30 + 12 * (voicingTop / 12 - 4) + MIDI_R + MIDI_S[v]; //octave adjustment
                                        while (onp[v] > voicingTop) onp[v] -= 12; //Lower the voicing by one octave while exceeding the highest note
                                    }
                                    sortAscending(onp, 4); //line up in ascending order of numbers
                                    if (voicingInterval == 1) onp[2] -= 12; //If the interval is medium
                                    else if (voicingInterval == 2) //If the spacing is rough
                                    {
                                        onp[2] -= 12;
                                        onp[0] -= 12;
                                    }
                                    for (int v = 0; v < 4; v++) //Write to MIDI instructions
                                    {
                                        chordMatrix[v, a] = (byte)onp[v];
                                    }
                                }
                            }
                        }
                        else //If the original code
                        {
                            for (int v = 0; v < 4; v++)
                            {
                                if (bdxData[0x664B + (toChord[a] & 0xFF) * 4 - v] != 0) //If there is no sound (x mark), it will not be pronounced.
                                {
                                    chordMatrix[v, a] = (byte)bdxData[0x664B + (toChord[a] & 0xFF) * 4 - v]; //Write to MIDI instructions
                                }
                            }
                        }
                    }
                    else //If it's a guitar chord
                    {
                        //Unless it's original code
                        if ((toChord[a] >> 8 & 0xFF) != 0xFF)
                        {
                            int[] plus = { 0x4C, 0x47, 0x43, 0x3E, 0x39, 0x34 }; //the lowest note of the 6 notes
                            int _bin = GetMidiRoot(toChord[a] >> 8 & 0xFF); //Determine the root sound based on the notation of the root sound
                            bool[] Xcheck = GetMutedStrings(_bin * 0x10 + (toChord[a] & 0xFF)); //Check if the code has an x ​​mark
                            int[] ot = GetGuitarChordFrets(_bin * 0x10 + (toChord[a] & 0xFF)); //Decide on the constituent sounds
                            for (int v = 0; v < 6; v++)
                            {
                                if (Xcheck[v] == false) //Cutting (x mark) does not sound
                                {
                                    chordMatrix[v, a] = (byte)(ot[v] + plus[v]); //Write to MIDI instructions
                                }
                            }
                        }
                        else //If the original code
                        {
                            int[] plus = { 0x4C, 0x47, 0x43, 0x3E, 0x39, 0x34 }; //the lowest note of the 6 notes

                            for (int v = 0; v < 6; v++)
                            {
                                if (originalChords[(toChord[a] & 0xFF)].mutes[v] == false) //Cutting (x mark) does not sound
                                {
                                    chordMatrix[v, a] = (byte)(originalChords[(toChord[a] & 0xFF)].frets[v] + plus[v]); //Write to MIDI instructions
                                }
                            }
                        }
                    }
                }
                else if (bdxData[0x248 + partIndex * 0x800 + a] == 0xFF) //If it's a triplet symbol
                {
                    for (int b = 0; b < 6; b++) chordMatrix[b, a] = 0xFF;
                }
                else //If it's a stretched sound, it's usually +0x80, but here anything over 0x80 is fine, so substitute 0x81 for now.
                {
                    for (int b = 0; b < 6; b++) chordMatrix[b, a] = 0x81;
                }
            }
            chordSet(partIndex, midiInst, ref rawTrack, ref finalTrack, ref byteCount, chordMatrix, strIndex);
        }

        static int GetMidiRoot(int chord)
        {
            // Determine accidental offset based on high nibble:
            int accidental = 0;
            int modifier = (chord >> 4) & 0xF;
            if (modifier == 2) accidental = -1; // flat
            else if (modifier == 1) accidental = 1; // sharp

            // Extract root note from low nibble and apply accidental
            switch (chord & 0xF)
            {
                case 0: return 0 + accidental;   // C
                case 1: return 2 + accidental;   // D
                case 2: return 4 + accidental;   // E
                case 3: return 5 + accidental;   // F
                case 4: return 7 + accidental;   // G
                case 5: return 9 + accidental;   // A
                case 6: return 11 + accidental;  // B
                default: return 0;               // fallback to C
            }
        }

        static void chordSet(int partIndex, int instrument, ref byte[] rawTrack, ref byte[] finalTrack, ref int byteCount, byte[,] noteMatrix, int stringIndex)
        {
            int[] midiEvents = new int[0x800];
            int eventCount = 0;

            int[] tripletOffsets = new int[] { 0, -3, -2, -1 };
            int tripletIndex = 0;
            bool isTripletActive = false;

            int[] volumes = GetVolumeMap(partIndex);
            int totalSteps = bdxData[0xAD] * bdxData[0xAA] * 4;

            for (int i = 0; i < totalSteps; i++)
            {
                byte note = noteMatrix[stringIndex, i];

                if (note < 0x80)
                {
                    int time = (i * 3) + tripletOffsets[tripletIndex];
                    int velocity = (int)(127f * (bdxData[0xC8 + partIndex * 0x10] / 127f) * (volumes[i] / 127f));
                    midiEvents[eventCount++] = (time << 16) | (velocity << 8) | note;
                }
                else if (note == 0xFF)
                {
                    isTripletActive = true;
                }

                if (isTripletActive)
                {
                    tripletIndex++;
                    if (tripletIndex > 3)
                    {
                        tripletIndex = 0;
                        isTripletActive = false;
                    }
                }
            }

            MidiBinSet(partIndex, instrument, ref rawTrack, ref finalTrack, ref byteCount, midiEvents, eventCount);
        }

        static void MidiBinSet(
            int partIndex,
            int instrument,
            ref byte[] rawTrack,
            ref byte[] finalTrack,
            ref int byteCount,
            int[] midiEvents,
            int eventCount)
        {
            int writeIndex = 8;

            // MIDI track header: "MTrk"
            rawTrack[0] = 0x4D; rawTrack[1] = 0x54;
            rawTrack[2] = 0x72; rawTrack[3] = 0x6B;

            // Reserve space for track length
            for (int i = 4; i < 8; i++)
                rawTrack[i] = 0;

            rawTrack[writeIndex++] = 0x00;
            rawTrack[writeIndex++] = (byte)(0xC0 + partIndex); // Program Change
            rawTrack[writeIndex++] = (byte)instrument;
            rawTrack[writeIndex++] = 0x00;

            int deltaLength = 0;
            int deltaTime = 0;
            int finalDeltaTime = 0;

            for (int i = 0; i < eventCount; i++)
            {
                if (i == 0)
                {
                    if ((midiEvents[i] & 0xFF) != 0)
                    {
                        rawTrack[writeIndex++] = (byte)(0x90 + partIndex);
                        rawTrack[writeIndex++] = (byte)(midiEvents[i] & 0xFF);
                        rawTrack[writeIndex++] = (byte)((midiEvents[i] >> 8) & 0xFF);
                    }
                    else
                    {
                        rawTrack[writeIndex++] = (byte)(0x80 + partIndex);
                        rawTrack[writeIndex++] = 0x00;
                        rawTrack[writeIndex++] = (byte)((midiEvents[i] >> 8) & 0xFF);
                    }
                }
                else
                {
                    int currentTime = (midiEvents[i] >> 16) & 0xFFFF;
                    int prevTime = (midiEvents[i - 1] >> 16) & 0xFFFF;
                    deltaTime = DTcalc((currentTime - prevTime) * 40, ref deltaLength);
                    WriteDeltaTimeBytes(ref rawTrack, deltaLength, ref writeIndex, ref deltaTime);

                    rawTrack[writeIndex++] = (byte)(0x80 + partIndex);
                    rawTrack[writeIndex++] = (byte)(midiEvents[i - 1] & 0xFF);
                    rawTrack[writeIndex++] = (byte)((midiEvents[i - 1] >> 8) & 0xFF);

                    if ((midiEvents[i] & 0xFF) != 0)
                    {
                        rawTrack[writeIndex++] = 0x00;
                        rawTrack[writeIndex++] = (byte)(0x90 + partIndex);
                        rawTrack[writeIndex++] = (byte)(midiEvents[i] & 0xFF);
                        rawTrack[writeIndex++] = (byte)((midiEvents[i] >> 8) & 0xFF);
                    }

                    finalDeltaTime = DTcalc((currentTime - prevTime) * 40, ref deltaLength);
                }
            }

            // Final note off
            WriteDeltaTimeBytes(ref rawTrack, deltaLength, ref writeIndex, ref finalDeltaTime);
            rawTrack[writeIndex++] = (byte)(0x80 + partIndex);
            rawTrack[writeIndex++] = (byte)(midiEvents[eventCount - 2] & 0xFF);
            rawTrack[writeIndex++] = (byte)((midiEvents[eventCount - 2] >> 8) & 0xFF);

            // End of track meta event
            rawTrack[writeIndex++] = 0x00;
            rawTrack[writeIndex++] = 0xFF;
            rawTrack[writeIndex++] = 0x2F;
            rawTrack[writeIndex++] = 0x00;

            // Write final track length
            int trackLength = writeIndex - 8;
            rawTrack[4] = (byte)((trackLength >> 24) & 0xFF);
            rawTrack[5] = (byte)((trackLength >> 16) & 0xFF);
            rawTrack[6] = (byte)((trackLength >> 8) & 0xFF);
            rawTrack[7] = (byte)(trackLength & 0xFF);

            // Create trimmed final track
            finalTrack = new byte[trackLength + 8];
            Array.Copy(rawTrack, finalTrack, trackLength + 8);
            byteCount = trackLength + 8;
        }


        //Sheet music (drums)
        static void CreateDrumTrack(
            int partIndex,
            ref byte[] rawTrack,
            ref byte[] finalTrack,
            ref int byteCount,
            int shiftBits)
        {
            int[] midiData = new int[0x800];
            int eventCount = 0;
            int[] tripletAdjustments = new int[4] { 0, -3, -2, -1 };
            int tripletIndex = 0;
            bool tripletPending = false;
            int[] volumeMap = GetVolumeMap(partIndex);

            for (int i = 0; i < bdxData[0xAD] * bdxData[0xAA] * 4; i++)
            {
                int value = (bdxData[0x248 + partIndex * 0x800 + i] >> shiftBits) & 0xF;
                if (value != 0xF)
                {
                    int pitch = bdxData[0x248 + partIndex * 0x800 + i];
                    float partVolume = bdxData[0xC8 + partIndex * 0x10] / 127f;
                    float localVolume = volumeMap[i] / 127f;
                    int velocity = (int)(127f * partVolume * localVolume);
                    midiData[eventCount++] = ((i * 3 + tripletAdjustments[tripletIndex]) << 16) | (velocity << 8) | pitch;
                }
                else
                {
                    tripletPending = true;
                }

                if (tripletPending)
                {
                    tripletIndex++;
                    if (tripletIndex > 3)
                    {
                        tripletPending = false;
                        tripletIndex = 0;
                    }
                }
            }

            int writeIndex = 8;
            rawTrack[0] = 0x4D; rawTrack[1] = 0x54; rawTrack[2] = 0x72; rawTrack[3] = 0x6B;
            for (int i = 4; i < 8; i++) rawTrack[i] = 0;

            rawTrack[writeIndex++] = 0;

            int deltaLength = 0;
            int deltaTime = 0;
            int finalDeltaTime = 0;
            string drumKit = GetDrumKitName(bdxData[0xCA + partIndex * 0x10]);

            for (int i = 0; i < eventCount; i++)
            {
                DrumButton currentButton = (DrumButton)((midiData[i] >> shiftBits) & 0xF);
                int velocity = (midiData[i] >> 8) & 0xFF;
                int note = GetDrumNote(drumKit, currentButton);

                if (i == 0)
                {
                    if (currentButton != DrumButton.None)
                    {
                        rawTrack[writeIndex++] = 0x99;
                        rawTrack[writeIndex++] = (byte)note;
                        rawTrack[writeIndex++] = (byte)velocity;
                    }
                    else
                    {
                        rawTrack[writeIndex++] = 0x89;
                        rawTrack[writeIndex++] = 0;
                        rawTrack[writeIndex++] = (byte)velocity;
                    }
                }
                else
                {
                    int currentTick = (midiData[i] >> 16) & 0xFFFF;
                    int prevTick = (midiData[i - 1] >> 16) & 0xFFFF;
                    deltaTime = DTcalc((currentTick - prevTick) * 40, ref deltaLength);
                    WriteDeltaTimeBytes(ref rawTrack, deltaLength, ref writeIndex, ref deltaTime);

                    DrumButton prevButton = (DrumButton)((midiData[i - 1] >> shiftBits) & 0xF);
                    int prevNote = GetDrumNote(drumKit, prevButton);
                    int prevVelocity = (midiData[i - 1] >> 8) & 0xFF;

                    rawTrack[writeIndex++] = 0x89;
                    rawTrack[writeIndex++] = (byte)prevNote;
                    rawTrack[writeIndex++] = (byte)prevVelocity;

                    if (currentButton != DrumButton.None)
                    {
                        rawTrack[writeIndex++] = 0;
                        rawTrack[writeIndex++] = 0x99;
                        rawTrack[writeIndex++] = (byte)note;
                        rawTrack[writeIndex++] = (byte)velocity;
                    }

                    finalDeltaTime = DTcalc((currentTick - prevTick) * 40, ref deltaLength);
                }
            }

            WriteDeltaTimeBytes(ref rawTrack, deltaLength, ref writeIndex, ref finalDeltaTime);

            rawTrack[writeIndex++] = 0xFF;
            rawTrack[writeIndex++] = 0x2F;
            rawTrack[writeIndex++] = 0;

            int trackLength = writeIndex - 8;
            rawTrack[4] = (byte)(trackLength >> 24 & 0xFF);
            rawTrack[5] = (byte)(trackLength >> 16 & 0xFF);
            rawTrack[6] = (byte)(trackLength >> 8 & 0xFF);
            rawTrack[7] = (byte)(trackLength & 0xFF);

            finalTrack = new byte[trackLength + 8];
            Array.Copy(rawTrack, finalTrack, trackLength + 8);
            byteCount = trackLength + 8;
        }


        // Instructions for volume change settings
        static int[] GetVolumeMap(int partIndex)
        {
            int[] volumeMap = new int[0x800];
            for (int i = 0; i < volumeMap.Length; i++) volumeMap[i] = 0x8080; // Mark as unset

            int[] tripletAdjust = { 0, 2, 1 };
            int volumeEventCount = 0;

            while ((bdxData[0x42C9 + partIndex * 0x100 + volumeEventCount * 8] << 8 | bdxData[0x42C8 + partIndex * 0x100 + volumeEventCount * 8]) != 0xFFFF)
            {
                int tickBase = (bdxData[0x42C9 + partIndex * 0x100 + volumeEventCount * 8] << 8) | bdxData[0x42C8 + partIndex * 0x100 + volumeEventCount * 8];
                int volumeValue = bdxData[0x42CC + partIndex * 0x100 + volumeEventCount * 8];
                int index = (tickBase / 3) + tripletAdjust[tickBase % 3];
                volumeMap[index] = volumeValue;
                volumeEventCount++;
            }

            // Fill in unset volume values using the last known volume
            for (int i = 1; i < volumeMap.Length; i++)
            {
                if (volumeMap[i] == 0x8080)
                {
                    volumeMap[i] = volumeMap[i - 1];
                }
            }

            return volumeMap;
        }

        //Piano chord basic chord information
        static int[] GetTriadChord(int chordType)
        {
            switch (chordType)
            {
                case 0: return new[] { 0, 4, 7 };   // major
                case 1: return new[] { 0, 3, 7 };   // minor
                case 2: return new[] { 0, 4, 10 };  // 7
                case 3: return new[] { 0, 4, 11 };  // major 7
                case 4: return new[] { 0, 3, 10 };  // minor 7
                case 5: return new[] { 3, 6, 9 };   // diminished
                case 6: return new[] { 3, 6, 10 };  // half-diminished (m7♭5)
                case 7: return new[] { 0, 4, 8 };   // augmented
                case 8: return new[] { 0, 5, 7 };   // sus4
                case 9: return new[] { 0, 5, 10 };  // 7sus4
                case 10: return new[] { 4, 7, 9 };  // 6
                case 11: return new[] { 4, 7, 14 }; // add9
                default: return new[] { 0, 0, 0 };  // fallback
            }
        }

        static int[] GetTetradChord(int chordType)
        {
            switch (chordType)
            {
                case 2: return new int[] { 0, 4, 7, 10 };  //7
                case 3: return new int[] { 0, 4, 7, 11 };  //M7
                case 4: return new int[] { 0, 3, 7, 10 };  //m7
                case 5: return new int[] { 0, 3, 6, 9 };   //dim
                case 6: return new int[] { 0, 3, 6, 10 };  //m7♭5
                case 9: return new int[] { 0, 5, 7, 10 };  //7sus4
                case 10: return new int[] { 0, 4, 7, 9 };  //6
                case 11: return new int[] { 0, 4, 7, 14 }; //add9
                default: return new int[] { 0, 0, 0 };     //Others are not possible, but other than that
            }
        }

        static int[] sortAscending(int[] data, int length)
        {
            int i = 0;
            while (i < length)
            {
                if (i == 0) { i++; continue; }
                if (data[i - 1] <= data[i])
                {
                    i++;
                }
                else
                {
                    // Swap elements
                    int temp = data[i - 1];
                    data[i - 1] = data[i];
                    data[i] = temp;
                    i--;
                }
            }
            return data;
        }

        //Guitar chord basic chord information
        static bool[] GetMutedStrings(int chordCode)
        {
            switch (chordCode)
            {
                case 0xBA:
                case 0xB2:
                case 0xAA:
                case 0x97:
                case 0x95:
                case 0x3A:
                case 0x2B:
                case 0x2A:
                case 0x29:
                case 0x28:
                case 0x24:
                case 0x23:
                case 0x22:
                case 0x21:
                case 0x20:
                case 0x1A:
                case 0x0A:
                case 0x02:
                    return new[] { false, false, false, false, false, true };

                case 0xB7:
                case 0xB5:
                case 0xA7:
                case 0xA5:
                case 0x8B:
                case 0x87:
                case 0x85:
                case 0x7B:
                case 0x75:
                case 0x6B:
                case 0x67:
                case 0x65:
                case 0x5B:
                case 0x57:
                case 0x56:
                case 0x55:
                case 0x53:
                case 0x47:
                case 0x46:
                case 0x45:
                case 0x36:
                case 0x35:
                case 0x27:
                case 0x26:
                case 0x25:
                case 0x15:
                case 0x05:
                    return new[] { false, false, false, false, true, true };

                case 0xB6:
                case 0xA6:
                case 0x37:
                case 0x17:
                case 0x16:
                case 0x07:
                case 0x06:
                    return new[] { true, false, false, false, false, true };

                case 0x5A:
                    return new[] { true, false, false, false, false, false };

                case 0x66:
                    return new[] { false, false, false, false, true, false };

                case 0x96:
                case 0x8A:
                case 0x86:
                case 0x76:
                case 0x6A:
                    return new[] { true, false, false, false, true, false };

                default:
                    return new[] { false, false, false, false, false, false };
            }
        }

        static int[] GetGuitarChordFrets(int chordIndex) //What number of notes is being played from the bottom of each note?
        {
            switch (chordIndex)
            {
                //C
                case 0x00: return new int[] { 0, 1, 0, 2, 3, 0 };
                case 0x01: return new int[] { 3, 4, 5, 5, 3, 3 };
                case 0x02: return new int[] { 0, 1, 3, 2, 3, 0 };
                case 0x03: return new int[] { 0, 0, 0, 2, 3, 0 };
                case 0x04: return new int[] { 3, 4, 3, 5, 3, 3 };
                case 0x05: return new int[] { 2, 1, 2, 1, 0, 0 };
                case 0x06: return new int[] { 0, 4, 3, 4, 3, 0 };
                case 0x07: return new int[] { 0, 1, 1, 2, 3, 0 };
                case 0x08: return new int[] { 3, 6, 5, 5, 3, 3 };
                case 0x09: return new int[] { 3, 6, 3, 5, 3, 3 };
                case 0x0A: return new int[] { 5, 5, 5, 5, 3, 0 };
                case 0x0B: return new int[] { 0, 3, 0, 2, 3, 0 };
                //C#
                case 0x10: return new int[] { 4, 6, 6, 6, 4, 4 };
                case 0x11: return new int[] { 4, 5, 6, 6, 4, 4 };
                case 0x12: return new int[] { 4, 6, 4, 6, 4, 4 };
                case 0x13: return new int[] { 4, 6, 5, 6, 4, 4 };
                case 0x14: return new int[] { 4, 5, 4, 6, 4, 4 };
                case 0x15: return new int[] { 3, 2, 3, 2, 0, 0 };
                case 0x16: return new int[] { 0, 5, 4, 5, 4, 0 };
                case 0x17: return new int[] { 0, 2, 2, 3, 4, 0 };
                case 0x18: return new int[] { 4, 7, 6, 6, 4, 4 };
                case 0x19: return new int[] { 4, 7, 4, 6, 4, 4 };
                case 0x1A: return new int[] { 6, 6, 6, 6, 4, 0 };
                case 0x1B: return new int[] { 4, 4, 6, 6, 4, 4 };
                //D
                case 0x20: return new int[] { 2, 3, 2, 0, 0, 0 };
                case 0x21: return new int[] { 1, 3, 2, 0, 0, 0 };
                case 0x22: return new int[] { 2, 1, 2, 0, 0, 0 };
                case 0x23: return new int[] { 2, 2, 2, 0, 0, 0 };
                case 0x24: return new int[] { 1, 1, 2, 0, 0, 0 };
                case 0x25: return new int[] { 1, 0, 1, 0, 0, 0 };
                case 0x26: return new int[] { 1, 1, 1, 0, 0, 0 };
                case 0x27: return new int[] { 2, 3, 3, 0, 0, 0 };
                case 0x28: return new int[] { 3, 3, 2, 0, 0, 0 };
                case 0x29: return new int[] { 3, 1, 2, 0, 0, 0 };
                case 0x2A: return new int[] { 2, 0, 2, 0, 0, 0 };
                case 0x2B: return new int[] { 0, 3, 2, 0, 0, 0 };
                //D#
                case 0x30: return new int[] { 6, 8, 8, 8, 6, 6 };
                case 0x31: return new int[] { 6, 7, 8, 8, 6, 6 };
                case 0x32: return new int[] { 6, 8, 6, 8, 6, 6 };
                case 0x33: return new int[] { 6, 8, 7, 8, 6, 6 };
                case 0x34: return new int[] { 6, 7, 6, 8, 6, 6 };
                case 0x35: return new int[] { 2, 1, 2, 1, 0, 0 };
                case 0x36: return new int[] { 2, 2, 2, 1, 0, 0 };
                case 0x37: return new int[] { 0, 4, 4, 5, 6, 0 };
                case 0x38: return new int[] { 6, 9, 8, 8, 6, 6 };
                case 0x39: return new int[] { 6, 9, 6, 8, 6, 6 };
                case 0x3A: return new int[] { 8, 8, 8, 8, 6, 0 };
                case 0x3B: return new int[] { 6, 6, 8, 8, 6, 6 };
                //E
                case 0x40: return new int[] { 0, 0, 1, 2, 2, 0 };
                case 0x41: return new int[] { 0, 0, 0, 2, 2, 0 };
                case 0x42: return new int[] { 0, 0, 1, 0, 2, 0 };
                case 0x43: return new int[] { 0, 0, 1, 1, 2, 0 };
                case 0x44: return new int[] { 0, 0, 0, 0, 2, 0 };
                case 0x45: return new int[] { 3, 2, 3, 2, 0, 0 };
                case 0x46: return new int[] { 3, 3, 3, 2, 0, 0 };
                case 0x47: return new int[] { 0, 1, 1, 2, 0, 0 };
                case 0x48: return new int[] { 0, 0, 2, 2, 2, 0 };
                case 0x49: return new int[] { 0, 0, 2, 0, 2, 0 };
                case 0x4A: return new int[] { 0, 2, 1, 2, 2, 0 };
                case 0x4B: return new int[] { 2, 0, 1, 2, 2, 0 };
                //F
                case 0x50: return new int[] { 1, 1, 2, 3, 3, 1 };
                case 0x51: return new int[] { 1, 1, 1, 3, 3, 1 };
                case 0x52: return new int[] { 1, 1, 2, 1, 3, 1 };
                case 0x53: return new int[] { 0, 1, 2, 3, 0, 0 };
                case 0x54: return new int[] { 1, 1, 1, 1, 3, 1 };
                case 0x55: return new int[] { 1, 0, 1, 0, 0, 0 };
                case 0x56: return new int[] { 4, 4, 4, 3, 0, 0 };
                case 0x57: return new int[] { 1, 2, 2, 3, 0, 0 };
                case 0x58: return new int[] { 1, 1, 3, 3, 3, 1 };
                case 0x59: return new int[] { 1, 1, 3, 1, 3, 1 };
                case 0x5A: return new int[] { 0, 3, 2, 0, 3, 1 };
                case 0x5B: return new int[] { 3, 1, 2, 3, 0, 0 };
                //F#
                case 0x60: return new int[] { 2, 2, 3, 4, 4, 2 };
                case 0x61: return new int[] { 2, 2, 2, 4, 4, 2 };
                case 0x62: return new int[] { 2, 2, 3, 2, 4, 2 };
                case 0x63: return new int[] { 0, 0, 0, 2, 3, 0 };
                case 0x64: return new int[] { 2, 2, 2, 2, 4, 2 };
                case 0x65: return new int[] { 2, 1, 2, 1, 0, 0 };
                case 0x66: return new int[] { 0, 1, 2, 2, 0, 2 };
                case 0x67: return new int[] { 2, 3, 3, 4, 0, 0 };
                case 0x68: return new int[] { 2, 2, 4, 4, 4, 2 };
                case 0x69: return new int[] { 2, 2, 4, 2, 4, 2 };
                case 0x6A: return new int[] { 0, 2, 3, 1, 0, 2 };
                case 0x6B: return new int[] { 4, 2, 3, 4, 0, 0 };
                //G
                case 0x70: return new int[] { 3, 0, 0, 0, 2, 3 };
                case 0x71: return new int[] { 3, 3, 3, 5, 5, 3 };
                case 0x72: return new int[] { 1, 0, 0, 0, 2, 3 };
                case 0x73: return new int[] { 2, 0, 0, 0, 2, 3 };
                case 0x74: return new int[] { 3, 3, 3, 3, 5, 3 };
                case 0x75: return new int[] { 3, 2, 3, 2, 0, 0 };
                case 0x76: return new int[] { 0, 2, 3, 3, 0, 3 };
                case 0x77: return new int[] { 3, 4, 4, 5, 0, 0 };
                case 0x78: return new int[] { 3, 3, 5, 5, 5, 3 };
                case 0x79: return new int[] { 3, 3, 5, 3, 5, 3 };
                case 0x7A: return new int[] { 0, 0, 0, 0, 2, 3 };
                case 0x7B: return new int[] { 5, 3, 4, 5, 0, 0 };
                //G#
                case 0x80: return new int[] { 4, 4, 5, 6, 6, 4 };
                case 0x81: return new int[] { 4, 4, 4, 6, 6, 4 };
                case 0x82: return new int[] { 4, 4, 5, 4, 6, 4 };
                case 0x83: return new int[] { 4, 4, 5, 5, 6, 4 };
                case 0x84: return new int[] { 4, 4, 4, 4, 6, 4 };
                case 0x85: return new int[] { 1, 0, 1, 0, 0, 0 };
                case 0x86: return new int[] { 0, 3, 4, 4, 0, 4 };
                case 0x87: return new int[] { 4, 5, 5, 6, 0, 0 };
                case 0x88: return new int[] { 4, 4, 6, 6, 6, 4 };
                case 0x89: return new int[] { 4, 4, 6, 4, 6, 4 };
                case 0x8A: return new int[] { 0, 4, 5, 3, 0, 4 };
                case 0x8B: return new int[] { 6, 4, 5, 6, 0, 0 };
                //A
                case 0x90: return new int[] { 0, 2, 2, 2, 0, 0 };
                case 0x91: return new int[] { 0, 1, 2, 2, 0, 0 };
                case 0x92: return new int[] { 0, 2, 0, 2, 0, 0 };
                case 0x93: return new int[] { 0, 2, 1, 2, 0, 0 };
                case 0x94: return new int[] { 0, 1, 0, 2, 0, 0 };
                case 0x95: return new int[] { 2, 1, 2, 1, 0, 0 };
                case 0x96: return new int[] { 0, 4, 5, 5, 0, 5 };
                case 0x97: return new int[] { 1, 2, 2, 3, 0, 0 };
                case 0x98: return new int[] { 0, 3, 2, 2, 0, 0 };
                case 0x99: return new int[] { 0, 3, 0, 2, 0, 0 };
                case 0x9A: return new int[] { 2, 2, 2, 2, 0, 0 };
                case 0x9B: return new int[] { 0, 0, 2, 2, 0, 0 };
                //A#
                case 0xA0: return new int[] { 1, 3, 3, 3, 1, 1 };
                case 0xA1: return new int[] { 1, 2, 3, 3, 1, 1 };
                case 0xA2: return new int[] { 1, 3, 1, 3, 1, 1 };
                case 0xA3: return new int[] { 1, 3, 2, 3, 1, 1 };
                case 0xA4: return new int[] { 1, 2, 1, 3, 1, 1 };
                case 0xA5: return new int[] { 3, 2, 3, 2, 0, 0 };
                case 0xA6: return new int[] { 0, 2, 1, 2, 1, 0 };
                case 0xA7: return new int[] { 2, 3, 3, 4, 0, 0 };
                case 0xA8: return new int[] { 1, 4, 3, 3, 1, 1 };
                case 0xA9: return new int[] { 1, 4, 1, 3, 1, 1 };
                case 0xAA: return new int[] { 3, 3, 3, 3, 1, 0 };
                case 0xAB: return new int[] { 1, 1, 3, 3, 1, 1 };
                //B
                case 0xB0: return new int[] { 2, 4, 4, 4, 2, 2 };
                case 0xB1: return new int[] { 2, 3, 4, 4, 2, 2 };
                case 0xB2: return new int[] { 2, 0, 2, 1, 2, 0 };
                case 0xB3: return new int[] { 2, 4, 3, 4, 2, 2 };
                case 0xB4: return new int[] { 2, 3, 2, 4, 2, 2 };
                case 0xB5: return new int[] { 1, 0, 1, 0, 0, 0 };
                case 0xB6: return new int[] { 0, 3, 2, 3, 2, 0 };
                case 0xB7: return new int[] { 3, 4, 4, 5, 0, 0 };
                case 0xB8: return new int[] { 2, 5, 4, 4, 2, 2 };
                case 0xB9: return new int[] { 2, 5, 2, 4, 2, 2 };
                case 0xBA: return new int[] { 4, 4, 4, 4, 2, 0 };
                case 0xBB: return new int[] { 2, 2, 4, 4, 2, 2 };
                default: return new int[] { 0, 0, 0, 0, 0, 0 };
            }
        }

    }
}
