using System.Collections.Generic;
using InfinityScript;
using System.IO;
using System;

namespace LambAdmin
{
    public partial class DHAdmin
    {

        class Achievement
        {
            public string Name;
            public string Icon;
            public List<Objective> Objectives = new List<Objective>();
            public int X;
            public int Y;

            public Achievement(string line, string location)
            {
                string[] parts = line.Split('|');
                Name = parts[0];
                Icon = parts[1];
                Objectives.Add(new Objective(Name + "|0", int.Parse(parts[2]), parts[3], parts[4]));
                string[] coordinates = location.Split(',');
                X = int.Parse(coordinates[0]);
                Y = int.Parse(coordinates[1]);
            }

            public void AddObjective(string line)
            {
                string[] parts = line.Split('|');
                Objectives.Add(new Objective(Name + "|" + Objectives.Count, int.Parse(parts[1]), parts[2], parts[3]));
            }
        }

        /// <summary>
        /// An Achievement Objective.
        /// </summary>
        class Objective
        {
            public string Name;
            public int Goal;
            public string Description;
            public string Message;

            /// <summary>
            /// Constructor for an Achievement Objective.
            /// </summary>
            /// <param name="name">Unique identifier of objective. Should be of form: "achievementName|objectiveNumber"</param>
            /// <param name="goal">The integer goal at which the objective will be completed. Progress can be added or removed using rewards.</param>
            /// <param name="description">Description of the objective, shown in chat when a player uses the command !achievements</param>
            /// <param name="message">Optional message, shown on HUD when a player completes the objective.</param>
            public Objective(string name, int goal, string description, string message = "")
            {
                Name = name;
                Goal = goal;
                Description = description;
                Message = message;
            }

        }

        string AchievementsFile = ConfigValues.ConfigPath + @"Achievements\achievements.txt";
        string LocationsFile = ConfigValues.ConfigPath + @"Hud\achievementlocations.txt";
        static List<Achievement> Achievements = new List<Achievement>();


        public void ACHIEVEMENTS_Load()
        {
            string[] lines = File.ReadAllLines(AchievementsFile);
            string[] locations = File.ReadAllLines(LocationsFile);
            int ll = 0;
            for (int ii = 0; ii < lines.Length; ii++)
                if (lines[ii].StartsWith("+"))
                    Achievements[Achievements.Count - 1].AddObjective(lines[ii]);
                else
                    Achievements.Add(new Achievement(lines[ii], locations[ll++]));
        }

        public void ACHIEVEMENTS_Setup()
        {
            ACHIEVEMENTS_Load();
            HUD_CreateAchievementMessage();
            PlayerConnected += ACHIEVEMENTS_OnPlayerConnect;
            PlayerActuallySpawned += ACHIEVEMENTS_OnSpawn;
            OnPlayerKilledEvent += ACHIEVEMENTS_OnKill;
        }

        public void ACHIEVEMENTS_OnPlayerConnect(Entity player)
        {
            ACHIEVEMENTS_Read(player);
            HUD_CreateAchievementIcons(player);
            ACHIEVEMENTS_SetProgressFields(player);
        }

        void ACHIEVEMENTS_SetProgressFields(Entity player)
        {
            foreach (Achievement a in Achievements)
                foreach (Objective o in a.Objectives)
                    player.SetField(o.Name + "p", 0);
        }

        public static void ACHIEVEMENTS_ForAllObjectives(Entity player, string achievementName, Action<Entity, string, int> objectiveAction)
        {
            int amountOfObjectives = ACHIEVEMENTS_GetAchievement(achievementName).Objectives.Count;
            for (int ii = 0; ii < amountOfObjectives; ii++)
                objectiveAction(player, achievementName, ii);
        }

        public static void ACHIEVEMENTS_DisableProgress(Entity player, string achievementName, int objectiveIndex)
        {
            player.ClearField(achievementName + "|" + objectiveIndex + "p");
        }

        public static void ACHIEVEMENTS_ResetProgress(Entity player, string achievementName, int objectiveIndex)
        {
            player.SetField(achievementName + "|" + objectiveIndex + "p", 0);
        }

        public static void ACHIEVEMENTS_Progress(Entity player, string achievementName, int objectiveIndex, int progress)
        {
            if (!player.HasField(achievementName + "|" + objectiveIndex) && player.HasField(achievementName + "|" + objectiveIndex + "p"))
            {
                Objective objective = ACHIEVEMENTS_GetObjective(achievementName, objectiveIndex);
                int updatedProgress = player.GetField<int>(objective.Name + "p") + progress;
                WriteLog.Debug("progressed " + objective.Name + " for " + player.Name + " to " + updatedProgress);
                player.SetField(objective.Name + "p", updatedProgress);
                if (updatedProgress >= objective.Goal)
                    ACHIEVEMENTS_Award(player, objective);
            }
        }

        static Achievement ACHIEVEMENTS_GetAchievement(string achievementName)
        {
            return Achievements.Find(a => a.Name == achievementName);
        }

        static Objective ACHIEVEMENTS_GetObjective(string achievementName, int objectiveIndex)
        {
            return ACHIEVEMENTS_GetAchievement(achievementName).Objectives[objectiveIndex];
        }

        public void ACHIEVEMENTS_OnSpawn(Entity player)
        {
            HUD_HideAchievements(player);
        }

        public void ACHIEVEMENTS_OnKill(Entity deadguy, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            if (attacker != null && attacker.IsPlayer)
                HUD_ShowAchievements(attacker, deadguy);
            else
                HUD_ShowAchievements(deadguy, deadguy);
        }

        private static string ACHIEVEMENTS_File(Entity player)
        {
            return ConfigValues.ConfigPath + @"Achievements\players\" + player.GUID + ".txt";
        }

        public void ACHIEVEMENTS_Read(Entity player)
        {
            string file = ACHIEVEMENTS_File(player);
            if (File.Exists(file))
            {
                foreach (string line in File.ReadAllLines(file))
                    player.SetField(line, true);
                foreach (Achievement a in Achievements)
                {
                    bool completedAllObjectives = true;
                    foreach (Objective o in a.Objectives)
                        completedAllObjectives &= player.HasField(o.Name);
                    if (completedAllObjectives)
                        player.SetField(a.Name, true);
                }
            }
        }

        public List<string> ACHIEVEMENTS_List(Entity viewer)
        {
            List<string> res = new List<string>();
            foreach (Achievement a in Achievements)
            {
                foreach (Objective o in a.Objectives)
                    res.Add(o.Description.Format(new Dictionary<string, string>()
                    {
                        {"<>", viewer.HasField(o.Name) ? "^2" : "^1"}
                    }));
            }
            return res;
        }

        static void ACHIEVEMENTS_Award(Entity achiever, Objective o)
        {
            achiever.SetField(o.Name, true);
            string file = ACHIEVEMENTS_File(achiever);
            if (!File.Exists(file))
                File.Create(file).Close();
            File.AppendAllText(file, o.Name + "\r\n");
            if (o.Message != "")
                HUD_ShowAchievementMessage(achiever.Name, o.Message);
        }

    }
}
