using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;
using System.IO;
using System.Runtime.InteropServices;

namespace BDX_TO_MIDI
{
    public partial class MainWindow : Window
    {
        //[DllImport("kernel32.dll")]
        //static extern bool AllocConsole();
        public MainWindow()
        {
            //AllocConsole();
            Console.WriteLine("Console initialized.");
            InitializeComponent();
            ApplyTheme("Light");
        }

        private void BrowseBdx_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog { Filter = "BDX files (*.bdx)|*.bdx" };
            if (openFileDialog.ShowDialog() == true)
            {
                bdxPathTextBox.Text = openFileDialog.FileName;
                try
                {
                    var info = new FileInfo(openFileDialog.FileName);
                    logTextBox.Text += $"Loaded: {info.Name} ({info.Length} bytes)\n";

                    var bdxBytes = File.ReadAllBytes(openFileDialog.FileName);
                    var wrapper = new BDX2MIDIWrapper(bdxBytes);
                    var meta = wrapper.Analyze(bdxBytes);

                    logTextBox.Text += $"Title: {meta.Title}\n";
                    logTextBox.Text += $"Instruments: {string.Join(", ", meta.Instruments)}\n";
                    logTextBox.Text += $"Tracks: {meta.TrackCount}\n";
                    logTextBox.Text += $"Tempo Changes: {meta.TempoChanges}\n";
                }
                catch (Exception ex)
                {
                    logTextBox.Text += $"Error reading file: {ex.Message}\n";
                }
            }
        }

        private void SaveMidi_Click(object sender, RoutedEventArgs e)
        {
            var saveFileDialog = new SaveFileDialog { Filter = "MIDI files (*.mid)|*.mid" };
            if (saveFileDialog.ShowDialog() == true)
            {
                midiPathTextBox.Text = saveFileDialog.FileName;
            }
        }

        private void Convert_Click(object sender, RoutedEventArgs e)
        {
            var bdxPath = bdxPathTextBox.Text;
            var midiPath = midiPathTextBox.Text;

            if (string.IsNullOrEmpty(bdxPath) || string.IsNullOrEmpty(midiPath))
            {
                logTextBox.Text += "Please select both BDX input and MIDI output paths.\n";
                return;
            }

            try
            {
                BDXConverter.Convert(bdxPath, midiPath);
                logTextBox.Text += "Conversion successful!\n";
            }
            catch (Exception ex)
            {
                logTextBox.Text += $"Error: {ex.Message}\n";
            }
        }

        private void DarkMode_Checked(object sender, RoutedEventArgs e)
        {
            ApplyTheme("Dark");
        }

        private void DarkMode_Unchecked(object sender, RoutedEventArgs e)
        {
            ApplyTheme("Light");
        }

        private void ApplyTheme(string themeName)
        {
            var themeDictionary = new ResourceDictionary();
            themeDictionary.Source = new Uri($"/Themes/{themeName}Theme.xaml", UriKind.Relative);

            // Clear and replace existing theme dictionaries
            Application.Current.Resources.MergedDictionaries.Clear();
            Application.Current.Resources.MergedDictionaries.Add(themeDictionary);
        }




    }
}

