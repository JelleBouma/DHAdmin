using System.Collections.Generic;
using System.IO;
using InfinityScript;

namespace LambAdmin
{
    public partial class DHAdmin
    {

        static HudElem AwardMsg;

        void HUD_PrecacheShaders()
        {
            foreach (string line in File.ReadAllLines(ConfigValues.ConfigPath + @"Hud\precacheshaders.txt"))
                GSCFunctions.PreCacheShader(line);
        }

        HudElem HUD_GetMessage(Entity player)
        {
            if (!player.HasField("hud_message"))
            {
                HudElem msg = HudElem.CreateFontString(player, HudElem.Fonts.Default, 1.6f);
                msg.SetPoint("CENTER", "CENTER", 0, 110);
                msg.HideWhenInMenu = true;
                msg.HideWhenDead = true;
                msg.Alpha = 0;
                player.SetField("hud_message", msg);
            }
            return player.GetField<HudElem>("hud_message");
        }

        void HUD_ShowMessage(Entity player, string text)
        {
            HudElem message = HUD_GetMessage(player);
            message.Alpha = .85f;
            message.SetText(text);
        }

        void HUD_HideMessage(Entity player)
        {
            HudElem message = HUD_GetMessage(player);
            message.Alpha = 0;
            message.SetText("");
        }

        static HudElem HUD_GetTopLeftInformation(Entity player)
        {
            if (!player.HasField("hud_topleft_information"))
            {
                HudElem topLeftInformation = HudElem.CreateFontString(player, HudElem.Fonts.Default, 1f);
                topLeftInformation.SetPoint("TOPLEFT", "TOPLEFT", 110, 3);
                topLeftInformation.HideWhenInMenu = true;
                topLeftInformation.HideWhenDead = false;
                topLeftInformation.Alpha = 0.85f;
                player.SetField("hud_topleft_information", topLeftInformation);
            }
            return player.GetField<HudElem>("hud_topleft_information");
        }

        public static void HUD_UpdateTopLeftInformation()
        {
            foreach (Entity player in Players)
                HUD_UpdateTopLeftInformation(player);
        }

        public static void HUD_UpdateTopLeftInformation(Entity player)
        {
            HudElem topLeftInformation = HUD_GetTopLeftInformation(player);
            string text = "";
            if (player.HasField("weapon_index"))
                text += "Weapon " + (player.GetField<int>("weapon_index") + 1) + "/" + WeaponRewardList.Count + "\n";
            foreach (Entity objective in MapObjectives)
            {
                string colour = "^7";
                if (objective.HasField("bomb"))
                    colour = objective.GetField<Entity>("bomb") == player ? "^2" : "^1";
                //if (objective.GetField<bool>("usable"))
                //    ticks_left += objective.GetField<int>("ticks_left");
                //else
                //    ticks_left = "destroyed by " + objective.GetField<Entity>("destroyer").Name;
                text += "           " + colour + objective.GetField("name") + "\n";
            }
            topLeftInformation.SetText(text);
        }

        public static void HUD_InitTopLeftTimers()
        {
            for (int ii = 0; ii < MapObjectives.Count; ii++)
            {
                HudElem timer = HudElem.CreateServerFontString(HudElem.Fonts.Default, 1f);
                timer.SetPoint("TOPLEFT", "TOPLEFT", 110, 3 + ii * 12);
                timer.HideWhenInMenu = true;
                timer.HideWhenDead = false;
                timer.Alpha = 0.85f;
                timer.SetTimerStatic(MapObjectives[ii].GetField<int>("timer"));
                MapObjectives[ii].SetField("hud_timer", timer);
            }
        }

        public static void HUD_UpdateTimer(Entity objective)
        {
            if (objective.HasField("bomb"))
                objective.GetField<HudElem>("hud_timer").SetTimer(objective.GetField<int>("timer"));
            else
                objective.GetField<HudElem>("hud_timer").SetTimerStatic(objective.GetField<int>("timer"));
        }

        public static HudElem HUD_GetTopInformation(Entity player)
        {
            if (!player.HasField("hud_top_information"))
            {
                HudElem hud = HudElem.CreateFontString(player, HudElem.Fonts.Big, 2f);
                hud.SetPoint("CENTER", "CENTER", 0, -220);
                hud.HideWhenInMenu = true;
                hud.HideWhenDead = false;
                hud.Alpha = .85f;
                player.SetField("hud_top_information", hud);
            }
            return player.GetField<HudElem>("hud_top_information");
        }

        public static void HUD_UpdateTopInformation(Entity player)
        {
            if (ConfigValues.Settings_hud_top != "")
                HUD_GetTopInformation(player).SetText(ConfigValues.Settings_hud_top.Format(new Dictionary<string, string>()
                {
                    { "<score>", player.GetField<int>("score") + "" },
                    { "<{0:n}score>", string.Format("{0:n}", player.GetField<int>("score")) }
                }));
        }

        public static void HUD_CreateAchievementMessage()
        {
            AwardMsg = HudElem.CreateServerFontString(HudElem.Fonts.Big, 2.2f);
            AwardMsg.SetPoint("CENTER", "CENTER", 0, -230);
            AwardMsg.HideWhenInMenu = false;
            AwardMsg.HideWhenDead = false;
            AwardMsg.Alpha = 0;
            AwardMsg.Archived = true;
            AwardMsg.Sort = 20;
        }

        public void HUD_CreateAchievementIcons(Entity player)
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

        public void HUD_ShowAchievements(Entity achiever, Entity viewer)
        {
            foreach (Achievement a in Achievements)
                if (achiever.HasField(a.Name))
                    viewer.GetField<HudElem>(a.Icon).Alpha = 1;
        }

        public void HUD_HideAchievements(Entity viewer)
        {
            foreach (Achievement a in Achievements)
                viewer.GetField<HudElem>(a.Icon).Alpha = 0;
        }

        public static void HUD_ShowAchievementMessage(string achiever, string message)
        {
            string formattedMessage = message.Format(new Dictionary<string, string>() {
                {"<name>", achiever}
            });
            AwardMsg.SetText(formattedMessage);
            AwardMsg.Alpha = 1;
        }

    }
}
