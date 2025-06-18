using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Speech.Synthesis;
using System.Windows.Threading;

namespace DeutscheVerbenSpiel
{
    public partial class MainWindow : Window
    {
        private List<VerbEntry> verbs = new List<VerbEntry>();
        private int[] order;
        private int currentWord = 0;
        private int[] optionOrder = new int[4];
        private int correctOption = 0;
        private int correctCount = 0, incorrectCount = 0;
        private int attemptCount = 0;
        private bool isAnswered = false;
        private Random rnd = new Random();
        private SpeechSynthesizer speechSynth;
        private DispatcherTimer questionTimer;
        private int timerSeconds = 5;
        private List<ReviewItem> reviewList = new List<ReviewItem>();

        private Button[] btns;

        public MainWindow()
        {
            InitializeComponent();
            btns = new[] { Option1, Option2, Option3, Option4 };
            speechSynth = new SpeechSynthesizer();
            LoadAllVerbs("Verben_Most_Used");
            if (verbs.Count == 0)
            {
                MessageBox.Show("Keine Verben gefunden!");
                Close();
                return;
            }
            ShuffleOrder();
            currentWord = 0;
            SetupTimer();
            ShowCurrentVerb();
            UpdateScoreLabel();
        }

        private void LoadAllVerbs(string tableName)
        {
            verbs.Clear();
            string dbPath = "Database\\verben.db";
            if (!System.IO.File.Exists(dbPath))
            {
                MessageBox.Show("DB-Datei nicht gefunden! (" + dbPath + ")");
                Application.Current.Shutdown();
                return;
            }
            using (var conn = new SQLiteConnection("Data Source=" + dbPath))
            {
                conn.Open();
                string sql = $"SELECT id, german, english, beispiel, example, hint FROM {tableName}";
                using (var cmd = new SQLiteCommand(sql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        verbs.Add(new VerbEntry
                        {
                            Id = reader.GetInt32(0),
                            German = reader.GetString(1),
                            English = reader.GetString(2),
                            Beispiel = reader.GetString(3),
                            Example = reader.GetString(4),
                            Hint = reader.GetString(5)
                        });
                    }
                }
            }
        }

        private void ShuffleOrder()
        {
            int n = verbs.Count;
            order = Enumerable.Range(0, n).ToArray();
            for (int i = n - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                int temp = order[i];
                order[i] = order[j];
                order[j] = temp;
            }
        }

        private void SetupTimer()
        {
            questionTimer = new DispatcherTimer();
            questionTimer.Interval = TimeSpan.FromSeconds(1);
            questionTimer.Tick += QuestionTimer_Tick;
        }

        private void StartTimer()
        {
            timerSeconds = 15;
            TimerLabel.Text = timerSeconds + "s";
            questionTimer.Start();
        }

        private void StopTimer()
        {
            questionTimer.Stop();
            TimerLabel.Text = "";
        }

        private void QuestionTimer_Tick(object sender, EventArgs e)
        {
            timerSeconds--;
            TimerLabel.Text = timerSeconds + "s";
            if (timerSeconds <= 0)
            {
                questionTimer.Stop();
                TimerLabel.Text = "";
                if (!isAnswered)
                {
                    isAnswered = true;
                    attemptCount++;
                    incorrectCount++;
                    MarkCorrectAndDisable();
                    AddReview(verbs[order[currentWord]].German, verbs[order[currentWord]].English, "", verbs[order[currentWord]].Hint, verbs[order[currentWord]].Beispiel, verbs[order[currentWord]].Example);
                    UpdateScoreLabel();
                }
            }
        }

        private void ShowCurrentVerb()
        {
            if (currentWord >= order.Length)
            {
                StopTimer();
                ShowReview();
                Close();
                return;
            }
            int thisIndex = order[currentWord];
            GermanVerbenLabel.Text = verbs[thisIndex].German;
            HintLabel.Text = "";
            BeispielLabel.Text = "";
            ExampleLabel.Text = "";
            isAnswered = false;

            // Reset option buttons
            foreach (var btn in btns)
            {
                btn.IsEnabled = true;
                btn.Background = new SolidColorBrush(Colors.White);
                btn.Foreground = Brushes.Black;
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(240, 240, 240));
            }

            // Prepare answer options
            var pool = new List<int>();
            for (int i = 0; i < verbs.Count; i++)
                if (i != thisIndex) pool.Add(i);
            pool = pool.OrderBy(x => rnd.Next()).ToList();
            correctOption = rnd.Next(4);
            int idx = 0;
            for (int i = 0; i < 4; i++)
            {
                if (i == correctOption)
                    optionOrder[i] = thisIndex;
                else
                    optionOrder[i] = pool[idx++];
            }
            Option1.Content = verbs[optionOrder[0]].English;
            Option2.Content = verbs[optionOrder[1]].English;
            Option3.Content = verbs[optionOrder[2]].English;
            Option4.Content = verbs[optionOrder[3]].English;

            SpeakGerman(verbs[thisIndex].German);

            StartTimer();
        }

        private void Option_Click(object sender, RoutedEventArgs e)
        {
            if (isAnswered) return;
            isAnswered = true;
            StopTimer();
            attemptCount++;
            var selectedBtn = sender as Button;
            int selectedIndex = Array.IndexOf(btns, selectedBtn);
            int thisIndex = order[currentWord];

            // Reset all backgrounds before feedback
            foreach (var btn in btns)
            {
                btn.ClearValue(Button.BackgroundProperty);
                btn.ClearValue(Button.BorderBrushProperty);
                btn.Foreground = Brushes.Black;
            }

            if (selectedIndex == correctOption)
            {
                correctCount++;
                btns[correctOption].Background = new SolidColorBrush(Color.FromRgb(168, 255, 175)); // green
                btns[correctOption].BorderBrush = Brushes.LimeGreen;
            }
            else
            {
                incorrectCount++;
                btns[selectedIndex].Background = new SolidColorBrush(Color.FromRgb(255, 183, 183)); // red
                btns[selectedIndex].BorderBrush = Brushes.Red;
                btns[correctOption].Background = new SolidColorBrush(Color.FromRgb(168, 255, 175)); // green
                btns[correctOption].BorderBrush = Brushes.LimeGreen;
                AddReview(verbs[thisIndex].German, verbs[thisIndex].English, (string)selectedBtn.Content, verbs[thisIndex].Hint, verbs[thisIndex].Beispiel, verbs[thisIndex].Example);
            }

            foreach (var btn in btns)
                btn.IsEnabled = false;

            UpdateScoreLabel();
        }

        private void HinweisButton_Click(object sender, RoutedEventArgs e)
        {
            int thisIndex = order[currentWord];
            if (!isAnswered)
            {
                // Before answer: only show the Beispiel (German)
                HintLabel.Text = !string.IsNullOrWhiteSpace(verbs[thisIndex].Hint)
                    ? "Hinweis: " + verbs[thisIndex].Hint
                    : "";
                BeispielLabel.Text = "";
                ExampleLabel.Text = "";

                // Speak the Beispiel (German sentence)
                SpeakGerman(verbs[thisIndex].Hint);
            }
            else
            {
                // After answer: show Hint, Beispiel (German), Example (English)
                HintLabel.Text = !string.IsNullOrWhiteSpace(verbs[thisIndex].Hint)
                    ? "Hinweis: " + verbs[thisIndex].Hint
                    : "";
                BeispielLabel.Text = !string.IsNullOrWhiteSpace(verbs[thisIndex].Beispiel)
                    ? "Beispiel: " + verbs[thisIndex].Beispiel
                    : "";
                ExampleLabel.Text = !string.IsNullOrWhiteSpace(verbs[thisIndex].Example)
                    ? "Englisch: " + verbs[thisIndex].Example
                    : "";

                // Speak the Beispiel (German sentence)
                SpeakGerman(verbs[thisIndex].Beispiel);
            }
        }

        private void NaechsteButton_Click(object sender, RoutedEventArgs e)
        {
            StopTimer();
            currentWord++;
            ShowCurrentVerb();
        }

        private void PlayAudioButton_Click(object sender, RoutedEventArgs e)
        {
            int thisIndex = order[currentWord];
            SpeakGerman(verbs[thisIndex].German);
        }

        private void StoppButton_Click(object sender, RoutedEventArgs e)
        {
            StopTimer();
            ShowReview();
            Close();
        }

        private void UpdateScoreLabel()
        {
            ScoreLabel.Text = $"Richtig: {correctCount}  |  Falsch: {incorrectCount}";
        }

        private void MarkCorrectAndDisable()
        {
            btns[correctOption].Background = new SolidColorBrush(Color.FromRgb(168, 255, 175));
            btns[correctOption].BorderBrush = Brushes.LimeGreen;
            foreach (var btn in btns)
                btn.IsEnabled = false;
        }

        private void SpeakGerman(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            try
            {
                // Pick a German voice if not already selected
                if (speechSynth.Voice == null || !speechSynth.Voice.Culture.TwoLetterISOLanguageName.Equals("de"))
                {
                    foreach (var voice in speechSynth.GetInstalledVoices())
                    {
                        if (voice.VoiceInfo.Culture.TwoLetterISOLanguageName.Equals("de"))
                        {
                            speechSynth.SelectVoice(voice.VoiceInfo.Name);
                            break;
                        }
                    }
                }
                speechSynth.SpeakAsyncCancelAll();
                speechSynth.SpeakAsync(text);
            }
            catch { }
        }

        private void AddReview(string german, string correctEnglish, string yourAnswer, string hint, string beispiel, string example)
        {
            if (!reviewList.Any(r => r.German == german))
                reviewList.Add(new ReviewItem
                {
                    German = german,
                    CorrectEnglish = correctEnglish,
                    YourAnswer = yourAnswer,
                    Hint = hint,
                    Beispiel = beispiel,
                    Beispiel_EN = example
                });
        }

        private void ShowReview()
        {
            if (reviewList.Count > 0)
            {
                var reviewWin = new ReviewWindow(reviewList);
                reviewWin.Owner = this;
                reviewWin.ShowDialog();
            }
            else
            {
                MessageBox.Show("Gratuliere! Keine Fehler zum Überprüfen.");
            }
        }
    }

    public class VerbEntry
    {
        public int Id { get; set; }
        public string German { get; set; }
        public string English { get; set; }
        public string Beispiel { get; set; }
        public string Example { get; set; }
        public string Hint { get; set; }
    }

    public class ReviewItem
    {
        public string German { get; set; }
        public string CorrectEnglish { get; set; }
        public string YourAnswer { get; set; }
        public string Hint { get; set; }
        public string Beispiel { get; set; }
        public string Beispiel_EN { get; set; }
    }
}
