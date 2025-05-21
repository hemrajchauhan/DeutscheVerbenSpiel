using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Speech.Synthesis;

namespace DeutscheVerbenSpiel
{
    public partial class ReviewWindow : Window
    {
        private readonly SpeechSynthesizer speechSynth = new SpeechSynthesizer();

        public ReviewWindow(List<ReviewItem> reviewList)
        {
            InitializeComponent();
            ReviewListPanel.ItemsSource = reviewList;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PlayAudio_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            string german = btn?.Tag as string;
            if (!string.IsNullOrWhiteSpace(german))
            {
                try
                {
                    foreach (var voice in speechSynth.GetInstalledVoices())
                    {
                        if (voice.VoiceInfo.Culture.TwoLetterISOLanguageName.Equals("de"))
                        {
                            speechSynth.SelectVoice(voice.VoiceInfo.Name);
                            break;
                        }
                    }
                    speechSynth.SpeakAsyncCancelAll();
                    speechSynth.SpeakAsync(german);
                }
                catch { }
            }
        }
    }
}