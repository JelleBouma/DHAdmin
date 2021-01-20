using System.Collections.Generic;
using InfinityScript;
using System.IO;

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
                Objectives.Add(new Objective(Name + "|0", parts[2], parts[3], parts[4], parts[5], parts[6]));
                string[] coordinates = location.Split(',');
                X = int.Parse(coordinates[0]);
                Y = int.Parse(coordinates[1]);
            }

            public void AddObjective(string line)
            {
                string[] parts = line.Split('|');
                Objectives.Add(new Objective(Name + "|" + Objectives.Count, parts[1], parts[2], parts[3], parts[4], parts[5]));
            }
        }

        /// <summary>
        /// An Achievement Objective.
        /// </summary>
        class Objective
        {
            public string Name;
            public string AwardOn;
            public string Track;
            public object Parameters;
            public string Description;
            public string Message;

            /// <summary>
            /// Constructor for an Achievement Objective.
            /// </summary>
            /// <param name="name">Unique identifier of objective. Should be of form: "achievementName|objectiveNumber"</param>
            /// <param name="awardOn">When to check if the objective has been completed. Possible values: "win"</param>
            /// <param name="track">The type of objective. Possible values: "", "shoot", "dont_shoot"</param>
            /// <param name="parameters">Parameters for the objective. For example: a list of weapons.</param>
            /// <param name="description">Description of the objective, shown in chat when a player uses the command !achievements</param>
            /// <param name="message">Optional message, shown on HUD when a player completes the objective.</param>
            public Objective(string name, string awardOn, string track, string parameters, string description, string message = "")
            {
                Name = name;
                AwardOn = awardOn;
                Track = track;
                Description = description;
                Message = message;
                LoadParameters(parameters);
            }

            private void LoadParameters(string parameters)
            {
                switch (Track)
                {
                    case "shoot":
                    case "dont_shoot":
                        Parameters = new Weapons(parameters);
                        break;
                }
            }

        }

        string AchievementsFile = ConfigValues.ConfigPath + @"Achievements\achievements.txt";
        string LocationsFile = ConfigValues.ConfigPath + @"Hud\achievementlocations.txt";
        List<Achievement> Achievements = new List<Achievement>();
        Dictionary<string, List<Objective>> Tracking = new Dictionary<string, List<Objective>>()
        {
            // Track keys
            { "", new List<Objective>() },
            { "shoot", new List<Objective>() },
            { "dont_shoot", new List<Objective>() },

            // AwardOn keys
            { "win", new List<Objective>() }
        };


        public void ACHIEVEMENTS_Load()
        {
            string[] lines = File.ReadAllLines(AchievementsFile);
            string[] locations = File.ReadAllLines(LocationsFile);
            for (int ii = 0; ii < lines.Length; ii++)
                if (lines[ii].StartsWith("+"))
                    Achievements[Achievements.Count - 1].AddObjective(lines[ii]);
                else
                    Achievements.Add(new Achievement(lines[ii], locations[ii]));
        }

        public void ACHIEVEMENTS_Setup()
        {
            ACHIEVEMENTS_Load();
            string[] trackThese = ConfigValues.Settings_track_achievements.Split(',');
            foreach (string trackName in trackThese)
                foreach (Achievement a in Achievements)
                    if (trackName == a.Name)
                        foreach (Objective o in a.Objectives)
                        {
                            Tracking.GetValue(o.AwardOn).Add(o);
                            Tracking.GetValue(o.Track).Add(o);
                        }
            PlayerConnected += ACHIEVEMENTS_OnPlayerConnect;
            PlayerActuallySpawned += ACHIEVEMENTS_OnSpawn;
            OnPlayerKilledEvent += ACHIEVEMENTS_OnKill;
            if (Tracking.GetValue("win").Count != 0)
                OnGameEnded += ACHIEVEMENTS_OnGameEnded;
        }

        public void ACHIEVEMENTS_OnPlayerConnect(Entity player)
        {
            ACHIEVEMENTS_Read(player);
            ACHIEVEMENTS_CreateHud(player);
            List<Objective> trackShots = Tracking.GetValue("shoot").Plus(Tracking.GetValue("dont_shoot"));
            if (ACHIEVEMENTS_FilterCompleted(player, trackShots).Count > 0)
                ACHIEVEMENTS_TrackShots(player, trackShots);
        }

        public void ACHIEVEMENTS_OnSpawn(Entity player)
        {
            ACHIEVEMENTS_Hide(player);
        }

        public void ACHIEVEMENTS_OnKill(Entity deadguy, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            if (attacker != null && attacker.IsPlayer)
                ACHIEVEMENTS_Show(attacker, deadguy);
            else
                ACHIEVEMENTS_Show(deadguy, deadguy);
        }

        public void ACHIEVEMENTS_OnGameEnded()
        {
            Entity winner = null;
            foreach (Entity player in Players)
                if (winner == null || player.Score > winner.Score)
                    winner = player;
            foreach (Objective o in Tracking.GetValue("win"))
                ACHIEVEMENTS_Check(winner, o);
        }

        void ACHIEVEMENTS_TrackShots(Entity player, List<Objective> tracking)
        {
            foreach (Objective t in tracking)
                player.SetField(t.Name + "_shoot", false);
            player.OnNotify("weapon_fired", trackWeapon);
            void trackWeapon(Entity shooter, Parameter weapon)
            {
                foreach (Objective t in tracking)
                    if (((Weapons)t.Parameters).ContainsName((string)weapon))
                        shooter.SetField(t.Name + "_shoot", true);
            }
        }

        private string ACHIEVEMENTS_File(Entity player)
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

        public void ACHIEVEMENTS_CreateHud(Entity player)
        {
            foreach (Achievement a in Achievements)
            {
                HudElem icon = HudElem.CreateIcon(player, a.Icon, 32, 32);
                icon.SetPoint("CENTER", "CENTER", a.X, a.Y);
                icon.HideWhenInMenu = true;
                icon.HideWhenDead = false;
                icon.Alpha = 0;
                icon.Archived = false;
                icon.Sort = 20;
                player.SetField(a.Icon, icon);
            }
        }

        public void ACHIEVEMENTS_Show(Entity achiever, Entity viewer)
        {
            foreach (Achievement a in Achievements)
                if (achiever.HasField(a.Name))
                    viewer.GetField<HudElem>(a.Icon).Alpha = 1;
        }

        public void ACHIEVEMENTS_Hide(Entity viewer)
        {
            foreach (Achievement a in Achievements)
                viewer.GetField<HudElem>(a.Icon).Alpha = 0;
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

        void ACHIEVEMENTS_Check(Entity player, Objective o)
        {
            if (!player.HasField(o.Name) && (o.Track == "" || player.HasField(o.Name + "_" + o.Track)))
                switch (o.Track)
                {
                    case "":
                        ACHIEVEMENTS_Award(player, o);
                        return;
                    case "shoot":
                        if (player.GetField<bool>(o.Name + "_shoot"))
                            ACHIEVEMENTS_Award(player, o);
                        return;
                    case "dont_shoot":
                        if (!player.GetField<bool>(o.Name + "_shoot"))
                            ACHIEVEMENTS_Award(player, o);
                        return;
                }
        }

        void ACHIEVEMENTS_Award(Entity achiever, Objective o)
        {
            achiever.SetField(o.Name, true);
            string file = ACHIEVEMENTS_File(achiever);
            if (!File.Exists(file))
                File.Create(file).Close();
            File.AppendAllText(file, o.Name + "\r\n");
            if (o.Message != "")
                foreach (Entity player in Players)
                {
                    HudElem msg = HudElem.CreateFontString(player, HudElem.Fonts.Big, 2.2f);
                    msg.SetPoint("CENTER", "CENTER", 0, -230);
                    msg.HideWhenInMenu = false;
                    msg.HideWhenDead = false;
                    msg.Alpha = 1;
                    msg.Archived = true;
                    msg.Sort = 20;
                    player.SetField("award_msg", msg);
                    msg.SetText(o.Message.Format(new Dictionary<string, string>()
                    {
                        {"<name>", achiever.Name}
                    }));
                }
        }

        List<Objective> ACHIEVEMENTS_FilterCompleted(Entity player, List<Objective> list)
        {
            return list.FindAll(delegate(Objective o)
            {
                return !player.HasField(o.Name);
            });
        }

    }
}
