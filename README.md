# 🎵 BDX to MIDI Converter

A Windows desktop application that converts **BDX files** into **standard MIDI** format for editing, playback, or digital preservation. Intended for use with songs from Jam with the Band / Daigasso Band Bros. DX.

---

## Features

- Load `.bdx` files
- Convert song data into proper MIDI tracks:
  - Instruments
  - Drum tracks
  - Chords
- Save to `.mid` files
- Optional dark mode

---

## 🛠 Requirements

- Windows 10 or later
- [.NET Framework 4.7.2+](https://dotnet.microsoft.com/en-us/download/dotnet-framework)
- Visual Studio 2019+ (for building)

---

## How to Use

1. **Launch the app**
2. Click **Browse BDX** to select a `.bdx` file
3. Review metadata (title, instruments, tempo, etc.)
4. Click **Save MIDI** to choose an output location
5. Hit **Convert**
6. Done!

---

## Building from Source

1. Clone the repository:

   ```bash
   git clone https://github.com/madafue/BDXToolbox.git
   cd BDXToolbox
