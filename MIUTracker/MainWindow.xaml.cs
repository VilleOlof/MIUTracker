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
using System.IO;
using System.Threading;
using System.Windows.Threading;
using System.Diagnostics;

namespace MIUTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            File.WriteAllText(@".\ErrorLog.txt", "");
            AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) => {
                File.AppendAllText(@".\ErrorLog.txt", $"" +
                    $"TIMESTAMP: {DateTime.Now} - " +
                    $"EXCEPTION: {eventArgs.Exception.ToString()}" +
                    $"\n");
            };

            InitializeComponent();

            string miupath = File.ReadAllText(@".\miuPath.txt");
            if (Process.GetProcessesByName("Marble It Up").Length == 0) {
                ProcessStartInfo info = new ProcessStartInfo($@"{miupath}Marble It Up.exe");
                info.WorkingDirectory = miupath; Process.Start(info);
            }
            
            Thread logThread = new Thread(Log.LogMain) {
                IsBackground = true
            }; logThread.Start();

            Task.Run(() => {
                while (true) {
                    float currentTime = Log.time; string fixedTime = "";
                    string currentLevel = Log.publiclevelName;
                    
                    if (currentTime.ToString().Contains(",")) { fixedTime = currentTime.ToString().Replace(",", "."); }

                    if (Application.Current == null) { continue; }
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        LatestScore.Content = $"Latest Score:\n{currentLevel}\nTime: {(fixedTime == "" ? currentTime.ToString() : fixedTime)}\nNew PB: {Log.newreplay}";
                        currentLevelLabel.Text = $"Current: {Log.currentLevel}\nAttempts: {Log.currentRespawnAmount}";
                        fullGame.Content = $"FullGame: ({Log.recentUserLevels.Count()}/{Log.levelOrder.Count()})";
                    }), DispatcherPriority.Render);
                    Thread.Sleep(50);
                }
            });
            bool doreplayThing = true;
            if (doreplayThing) { ReplaySetup(); }
        }
        public void OpenDir(object sender, RoutedEventArgs e) {
            Process.Start("explorer.exe",@".\");

        }

        static readonly string MIUPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow"), @"Bad Habit\MarbleItUp\");
        static readonly string MIULog = $@"{MIUPath}\Player.log";
        static readonly string MIUHighscore = $@"{MIUPath}\highscores.mp";
        public static readonly string replayDir = $@"{MIUPath}\Replays\"; public static string replayDes = @".\Replays\";
        static readonly List<string> weeklyLevel = new List<string>() {
            "A0", "A1", "A2", "A3", "A4",
            "B0", "B1", "B2", "B3", "B4",
        };
        public static Dictionary<string,DateTime> lastModifiedList = new Dictionary<string, DateTime>();

        public static void ReplaySetup()
        {
            foreach (var file in Directory.GetFiles(replayDir)) {
                bool skipCycle = false;
                string onlyName = System.IO.Path.GetFileName(file); string nameNoEx = System.IO.Path.GetFileNameWithoutExtension(file);

                foreach (var weekly in weeklyLevel) {
                    if (onlyName.Contains(weekly)) {skipCycle = true; break; }
                }
                if (skipCycle) { continue; }
                string desReplay = $@"{replayDes}{nameNoEx}\{onlyName}";
                DateTime lastModified = File.GetLastWriteTime(file);
                lastModifiedList[nameNoEx] = lastModified;
                try {
                    if (Directory.Exists($@"{replayDes}{nameNoEx}")) { continue; }
                    Directory.CreateDirectory($@"{replayDes}{nameNoEx}");
                    File.Copy(file, desReplay);
                }
                catch {
                    throw new Exception($"Couldn't Copy Replay: {onlyName}");
                }
                File.Move(desReplay, $@"{replayDes}{nameNoEx}\{nameNoEx}_{$"{lastModified.Year.ToString()}-{lastModified.Month.ToString()}-{lastModified.Day.ToString()};{lastModified.Hour}-{lastModified.Minute}-{lastModified.Second}"}_Original.mp");
            }
        }

        public static void CopyReplay(string levelName)
        {
            string fullPath = $"{replayDir}{levelName}.mp";
            string fullDes = $@".\Replays\{levelName}\{levelName}.mp";
            try {
                if (File.Exists(fullDes)) { File.Delete(fullDes); }
                File.Copy(fullPath, fullDes);
                File.Move(fullDes, $@".\Replays\{levelName}\{levelName}_{Log.lastAccess.Year}-{Log.lastAccess.Month}-{Log.lastAccess.Day};{Log.lastAccess.Hour}-{Log.lastAccess.Minute}-{Log.lastAccess.Second}_{Log.time}.mp");
                Log.newreplay = true;
            }
            catch {
                if (File.Exists(fullDes)) { File.Delete(fullDes); }
                //throw new Exception($"Couldn't Copy New Replay: {fullPath}|{fullDes}");
            }
        }
    }
}
