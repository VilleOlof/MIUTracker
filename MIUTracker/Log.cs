using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace MIUTracker
{
    class Log
    {
        public static bool newreplay = false;
        public static DateTime lastAccess;
        public static float time;
        public static string publiclevelName = "None";
        public static string currentLevel = "";
        public static int currentRespawnAmount;

        public static bool watchLog = true;
        public static void LogMain()
        {

            string logPath = @".\Player.log"; string miuLog = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow"), @"Bad Habit\MarbleItUp\Player.log");
            CopyLog(miuLog, logPath);
            int latestScoreIndex = File.ReadAllText(logPath).Length - 1;
            string logContent = File.ReadAllText(logPath);
            int oldRespawnI = 0; string oldLevel = "";

            string trueCurrent = "";
            List<string> levelindex = new List<string>();

            while (watchLog) {
                Thread.Sleep(500);
                CopyLog(miuLog, logPath);
                logContent = File.ReadAllText(logPath);
                //Log things and checking goes here
                //levelName
                int nameLevel = logContent.LastIndexOf("Level Complete ");
                if (nameLevel != -1) {
                    string realLevelName = logContent.Substring(nameLevel);
                    int nameLevelI = realLevelName.IndexOf("Time: ");
                    realLevelName = logContent.Substring(nameLevel + 16, nameLevelI - 20);
                    if (classisDict.ContainsKey(realLevelName)) { realLevelName = classisDict[realLevelName]; } //fixes classic levels
                }
                //####
                int recentRespawn = logContent.LastIndexOf("Respawning Marble");
                if (recentRespawn != oldRespawnI) {
                    currentRespawnAmount++;
                    if (trueCurrent != "") {
                        string path = $@".\Attempts\{trueCurrent}.txt";
                        if (!File.Exists(path)) { using (FileStream fs = File.Create(path)) { fs.Close(); }; }
                        int lineAmount = File.ReadAllLines(path).Length+1;
                        File.AppendAllText(path, $"Attempts: {lineAmount} | Date: {DateTime.Now}\n");
                    }
                    
                    oldRespawnI = recentRespawn;
                }
                // [mapload] Finish loading 'onward_and_upward'...
                int maploadI = logContent.LastIndexOf("[mapload] Finish loading ");
                trueCurrent = logContent.Substring(maploadI + 1);
                int maploadNewL = trueCurrent.IndexOf("\n");
                trueCurrent = logContent.Substring(maploadI + 26, maploadNewL -30);
                if (trueCurrent.Contains("steamapps") || trueCurrent.Contains("appdata")) { trueCurrent = Path.GetFileNameWithoutExtension(trueCurrent); }
                if (trueCurrent != oldLevel) {
                    oldLevel = trueCurrent;
                    currentRespawnAmount = 0;
                }
                if (trueCurrent.Contains("_Data")) { trueCurrent = "Starting..."; }
                currentLevel = trueCurrent;
                //####

                try {
                    logContent = logContent.Substring(latestScoreIndex);
                }
                catch {
                    latestScoreIndex = 0;
                    logContent = File.ReadAllText(logPath);
                }
                //level completed>Time
                int logIndex = logContent.IndexOf("Level Complete ");
                if (logIndex != -1) {
                    latestScoreIndex = logIndex + latestScoreIndex + 15;

                    int newLineIndex = logContent.Substring(logIndex).IndexOf("\n");
                    string completeLine = "";

                    if (newLineIndex != -1) {
                        completeLine = logContent.Substring(logIndex, newLineIndex);
                    }
                    else {
                        completeLine = logContent.Substring(logIndex);
                    }

                    int timeIndex = completeLine.IndexOf("Time: ");
                    if (timeIndex == -1) {
                        throw new Exception("Time not found in complete line");
                    }
                    string levelName = logContent.Substring(logIndex+16, timeIndex);
                    int A = levelName.IndexOf("]");
                    levelName = logContent.Substring(logIndex+16,A);

                    timeIndex += 6;
                    time = -1f;
                    bool success = float.TryParse(completeLine.Substring(timeIndex), out time);
                    if (!success) {
                        throw new Exception("Time was not a valid float");
                    }
                    if (classisDict.ContainsKey(levelName)) { levelName = classisDict[levelName]; } //fixes classic levels
                    File.AppendAllText($@".\Scores\{levelName}.txt",$"Level:{levelName} - Time:{time} - Date:{DateTime.Now}\n");
                    newreplay = false;
                    publiclevelName = levelName;
                    lastAccess = File.GetLastWriteTime($"{MainWindow.replayDir}{levelName}.mp");



                    if (levelOrder.Contains(levelName)) {
                        recentUserLevels.Add(levelName); recentUserTimes.Add(time);
                        int countU = recentUserLevels.Count() - 1;
                        if (recentUserLevels[0] != levelOrder[0] && recentUserLevels.Count()<2) {
                            recentUserLevels.Clear(); recentUserTimes.Clear(); completedFullGame = false;
                        }
                        else if (recentUserLevels[countU] != levelName || recentUserLevels.Count() > levelOrder.Count() || recentUserLevels[countU] != levelOrder[countU])
                        {
                            recentUserLevels.Clear(); recentUserTimes.Clear(); completedFullGame = false;
                        }

                        if (recentUserLevels.Count() == levelOrder.Count()) {
                            for (int i = 0; i < recentUserLevels.Count(); i++) {
                                if (recentUserLevels[i] != levelOrder[i]) {
                                    recentUserLevels.Clear(); recentUserTimes.Clear();
                                    completedFullGame = false;
                                    continue;
                                }
                                completedFullGame = true;
                            }
                        }
                    }

                    if (completedFullGame) {
                        int levelI = 0;
                        int currentFAttempt = Directory.GetFiles(@".\Scores\FullGame\").Count();
                        foreach (var level in recentUserLevels) {
                            File.AppendAllText($@".\Scores\FullGame\Attempt {currentFAttempt}.txt", $"Level: {level}| Time: {recentUserTimes[levelI]}\n");
                            levelI++;
                        }
                        completedFullGame = false;
                        recentUserLevels.Clear(); recentUserTimes.Clear();
                    }


                    if (!MainWindow.lastModifiedList.ContainsKey(levelName)) {
                        MainWindow.lastModifiedList[levelName] = lastAccess;
                        MainWindow.CopyReplay(levelName);
                        //newreplay = true;
                        continue;
                    }
                    if (lastAccess != MainWindow.lastModifiedList[levelName]) {
                        MainWindow.lastModifiedList[levelName] = lastAccess;
                        MainWindow.CopyReplay(levelName);
                        //newreplay = true;
                        continue;
                    }
                    
                }
            }
        }

        static Dictionary<string, string> classisDict = new Dictionary<string, string>() {
            {"Learning To Roll","rollTutorial" },
            {"Learning To Turn","turnTutorial" },
            {"Learning To Jump","jumpTutorial" },
            {"Precious Gems","gemTutorial" },
            {"Up the Wall","gravityTutorial" },
            {"Super Jump","superjumpTutorial" },
            {"Full Speed Ahead","superspeedTutorial" },
            {"Stay Frosty","tutorialIce" },
            {"Onward and Upward","tutorialFinal" },

            {"Duality","duality" },
            {"Transit","transit" },
            {"Great Wall","greatWall" },
            {"Bump in the Night","bumperTutorial" },
            {"Over the Garden Wall","otgw" },
            {"Wave Pool","wavepool" },
            {"Big Easy","bigeasy" },
            {"Archipelago","archipelago" },
            {"Triple Divide","3divide" },
            {"Thread the Needle","threadNeedle" },

            {"Sugar Rush","sugarRush" },
            {"Elevator Action","elevatoraction" },
            {"Speedball","speedball" },
            {"Icy Ascent","icyascent" },
            {"River Vantage","rivervantage" },
            {"Off Kilter","offkilter" },
            {"Four Stairs","4stairs" },
            {"Totally Tubular","tubular" },
            {"Time Capsule","timecapsule" },
            {"Cog Valley","cogValley" },

            {"Bumper Invasion","bumperinvasion" },
            {"Braid","braid" },
            {"Sun Spire","sunspire" },
            {"Epoch","epoch" },
            {"Retrograde Rally","retro" },
            {"Gearheart","gearheart" },
            {"Acrophobia","Acrophobia" },
            {"Dire Straits","direstraits" },
            {"Ex Machina","exmachina" },
            {"Diamond in the Sky","diamond" },

            {"Newton's Cradle","newtonscradle" },
            {"Archiarchy","archiarchy" },
            {"Stayin' Alive","stayinalive" },
            {"Gordian","gordian" },
            {"Crystalline Matrix","crystalmatrix" },
            {"Contraption","contraption" },
            {"Uphill Both Ways","uphill" },
            {"Flip the Table","flippity" },
            {"Vertigo","vertigo" },
            {"Warp Core","warpcore" },
            {"The Pit of Despair","pitofdespair" },

            {"Danger Zone","dangerzone" },
            {"Platinum Playground","platinumplayground" },
            {"Radius","radius" },
            {"Head in the Clouds","headintheclouds" },
            {"Centripetal Force","centripetalforce" },
            {"Escalation","escalation" },
            {"Confluence","confluence" },
            {"Olympus","olympus" },
            {"Tangle","tangle" },
            {"Stratosphere","stratosphere" },
        };

        public static bool completedFullGame = false;
        public static List<string> recentUserLevels = new List<string>();
        public static List<float> recentUserTimes = new List<float>();
        public static readonly List<string> levelOrder = new List<string>() {
                "rollTutorial",
                "turnTutorial",
                "jumpTutorial",
                "gemTutorial",
                "gravityTutorial",
                "superjumpTutorial",
                "superspeedTutorial",
                "tutorialIce",
                "tutorialFinal",

                "duality",
                "transit",
                "greatWall",
                "bumperTutorial",
                "otgw",
                "wavepool",
                "bigeasy",
                "archipelago",
                "3divide",
                "threadNeedle",

                "sugarRush",
                "elevatoraction",
                "speedball",
                "icyascent",
                "rivervantage",
                "offkilter",
                "4stairs",
                "tubular",
                "timecapsule",
                "cogValley",

                "bumperinvasion",
                "braid",
                "sunspire",
                "epoch",
                "retro",
                "gearheart",
                "Acrophobia",
                "direstraits",
                "exmachina",
                "diamond",

                "newtonscradle",
                "archiarchy",
                "stayinalive",
                "gordian",
                "crystalmatrix",
                "contraption",
                "uphill",
                "flippity",
                "vertigo",
                "warpcore",
                "pitofdespair",

                "dangerzone",
                "platinumplayground",
                "radius",
                "headintheclouds",
                "centripitalforce",
                "escalation",
                "confluence",
                "olympus",
                "tangle",
                "stratosphere",
        };

        static bool CopyLog(string src, string des) {
            try {
                File.Delete(des);
            }
            catch {
                throw new Exception($"Couldn't Delete File {des}");
            }
            try {
                File.Copy(src, des);
                return true;
            }
            catch {
                return false;
                throw new Exception($"Couldn't Copy File(source:{src}|destination:{des}");
            }

        }
    }
}