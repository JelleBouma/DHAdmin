using System.Collections.Generic;
using InfinityScript;
using System.IO;

namespace LambAdmin
{
    public partial class DGAdmin
    {

        public class Achievement
        {
            public string Name;
            public string Icon;
            public string AwardOn;
            public string Objective;
            public string Parameter;
            public string Description;
            public string Message;

            public Achievement(string line)
            {
                string[] parts = line.Split('|');
                Name = parts[0];
                Icon = parts[1];
                AwardOn = parts[2];
                Objective = parts[3];
                Parameter = parts[4];
                Description = parts[5];
                Message = parts[6];
            }
        }

        string AchievementsFile = ConfigValues.ConfigPath + @"Achievements\achievements.txt";
        List<Achievement> Achievements;
        List<Achievement> Tracking;
        List<Achievement> TrackShots;
        List<Achievement> CheckOnWin;


        public void ACHIEVEMENTS_Load()
        {
            foreach (string line in File.ReadAllLines(AchievementsFile))
            {
                Achievements.Add(new Achievement(line));
            }
        }

        public void ACHIEVEMENTS_OnServerStart()
        {
            ACHIEVEMENTS_Load();
            foreach (Achievement a in Achievements)
            {
                GSCFunctions.PreCacheShader(a.Icon);
            }
        }

        public void ACHIEVEMENTS_Setup()
        {
            string[] trackThese = ConfigValues.settings_track_achievements.Split(',');
            foreach (string trackName in trackThese)
            {
                foreach (Achievement a in Achievements)
                {
                    if (trackName == a.Name)
                    {
                        Tracking.Add(a);
                    }
                }
            }
        }

        public void ACHIEVEMENTS_Track(Achievement t)
        {
            Tracking.Add(t);
        }

        public void ACHIEVEMENTS_OnSpawn(Entity player)
        {
            ACHIEVEMENTS_Hide(player);
        }

        public void ACHIEVEMENTS_OnKill(Entity deadguy, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            if (attacker != null && attacker.IsPlayer)
            {
                ACHIEVEMENTS_Show(attacker, deadguy);
            }
            else
            {
                ACHIEVEMENTS_Show(deadguy, deadguy);
            }
        }

        public void ACHIEVEMENTS_TrackShots(Entity player)
        {
            foreach (Achievement t in TrackShots)
            {
                player.SetField(t.Name + "_" + t.Objective, true);
            }
            player.OnNotify("weapon_fired", trackWeapon);
            void trackWeapon(Entity shooter, Parameter weapon)
            {
                foreach (Achievement t in TrackShots)
                {
                    WriteLog.Debug((string)weapon);
                    if ((string)weapon == t.Parameter || (string)weapon == "")
                    {
                        shooter.SetField(t.Name + "_" + t.Objective, false);
                        WriteLog.Debug("shot illegal gun");
                    }
                }
            }
        }

        public void ACHIEVEMENTS_OnGameEnded()
        {
            if (CheckOnWin.Count != 0)
            {
                Entity winner = null;
                foreach (Entity player in Players)
                {
                    if (winner == null || player.Score > winner.Score)
                        winner = player;
                }
                foreach (Achievement c in CheckOnWin)
                {
                    ACHIEVEMENTS_Check(winner, c);
                }
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
                {
                    player.SetField(line, true);
                }
            }
        }

        public int ACHIEVEMENTS_X = -120;
        public int ACHIEVEMENTS_Y = 180;
        public void ACHIEVEMENTS_Show(Entity achiever, Entity viewer)
        {
            foreach (Achievement a in Achievements)
            {
                if (achiever.HasField(a.Name))
                {
                    HudElem icon = HudElem.CreateIcon(viewer, a.Icon, 32, 32);
                    icon.SetPoint("CENTER", "CENTER", ACHIEVEMENTS_X, ACHIEVEMENTS_Y);
                    icon.HideWhenInMenu = false;
                    icon.HideWhenDead = false;
                    icon.Alpha = 1;
                    icon.Archived = true;
                    icon.Sort = 20;
                    viewer.SetField(a.Icon, icon);
                }
            }
        }

        public void ACHIEVEMENTS_Hide(Entity viewer)
        {
            foreach (Achievement a in Achievements)
            {
                if (viewer.HasField(a.Icon))
                {
                    viewer.GetField<HudElem>(a.Icon).Destroy();
                }
            }
        }

        public void ACHIEVEMENTS_Check(Entity player, Achievement a)
        {
            if (!player.HasField(a.Name) && player.HasField(a.Name + "_" + a.Objective))
            {
                switch (a.Objective)
                {
                    case "dont_shoot":
                        if (player.GetField<bool>(a.Name + "_" + a.Objective))
                            ACHIEVEMENTS_Award(player, a);
                        return;
                }
            }
        }

        public void ACHIEVEMENTS_Award(Entity achiever, Achievement a)
        {
            achiever.SetField(a.Name, true);
            string file = ACHIEVEMENTS_File(achiever);
            if (!File.Exists(file))
            {
                var fs = new FileStream(file, FileMode.Create);
                fs.Dispose();
            }
            string[] achievements = { a.Name };
            File.AppendAllLines(file, achievements);
            if (a.Message != "")
            {
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
                    msg.SetText(a.Message.Format(new Dictionary<string, string>()
                    {
                        {"<name>", achiever.Name}
                    }));
                }
            }
        }
    }
}
