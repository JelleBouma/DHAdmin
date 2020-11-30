using System.Collections.Generic;
using InfinityScript;
using System.IO;

namespace LambAdmin
{
    public partial class DGAdmin
    {

        public static Dictionary<string, string> Achievements = new Dictionary<string, string>()
        {
            {"achievementNinjas", "iw5_cardicon_ninja"}
        };

        public void ACHIEVEMENTS_OnServerStart()
        {
            foreach (string icon in Achievements.Values)
            {
                GSCFunctions.PreCacheShader(icon);
            }
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

        public void ACHIEVEMENTS_OnGameEnded()
        {
            Entity winner = null;
            foreach (Entity player in Players)
            {
                if (winner == null || player.Score > winner.Score)
                    winner = player;
            }
            if (winner.GetField<bool>("honour"))
            {
                if (!winner.HasField("achievementNinjas"))
                    ACHIEVEMENTS_Award(winner, "achievementNinjas");
                foreach (Entity player in Players)
                {
                    HudElem msg = HudElem.CreateFontString(player, HudElem.Fonts.Big, 2.2f);
                    msg.SetPoint("CENTER", "CENTER", 0, -230);
                    msg.HideWhenInMenu = false;
                    msg.HideWhenDead = false;
                    msg.Alpha = 1;
                    msg.Archived = true;
                    msg.Sort = 20;
                    player.SetField("hud_message", msg);
                    msg.SetText("Honourable Ninja Win! ^3Glory to the dojo of " + winner.Name + "!");
                }
            }
            foreach (Entity player in Players)
            {
                ACHIEVEMENTS_Show(winner, player);
            }
        }

        private string ACHIEVEMENTS_File(Entity player)
        {
            return ConfigValues.ConfigPath + @"Achievements\" + player.GUID + ".txt";
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
            foreach (string achievement in Achievements.Keys)
            {
                if (achiever.HasField(achievement))
                {
                    string value = Achievements.GetValue(achievement);
                    HudElem icon = HudElem.CreateIcon(viewer, value, 32, 32);
                    icon.SetPoint("CENTER", "CENTER", ACHIEVEMENTS_X, ACHIEVEMENTS_Y);
                    icon.HideWhenInMenu = false;
                    icon.HideWhenDead = false;
                    icon.Alpha = 1;
                    icon.Archived = true;
                    icon.Sort = 20;
                    viewer.SetField(value, icon);
                }
            }
        }

        public void ACHIEVEMENTS_Hide(Entity viewer)
        {
            foreach (string icon in Achievements.Values)
            {
                if (viewer.HasField(icon))
                {
                    viewer.GetField<HudElem>(icon).Destroy();
                }
            }
        }

        public void ACHIEVEMENTS_Award(Entity player, string achievement)
        {
            player.SetField(achievement, true);
            string file = ACHIEVEMENTS_File(player);
            if (!File.Exists(file))
            {
                var fs = new FileStream(file, FileMode.Create);
                fs.Dispose();
            }
            string[] achievements = { achievement };
            File.AppendAllLines(file, achievements);
        }
    }
}
