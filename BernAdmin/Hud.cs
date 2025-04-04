﻿using System.Collections.Generic;
using System.IO;
using InfinityScript;

namespace LambAdmin
{
    public partial class DHAdmin
    {

        static HudElem AwardMsg;


        /// <summary>
        /// Precache the shaders in accordance with the file Hud\precacheshaders.txt
        /// </summary>
        void HUD_PrecacheShaders()
        {
            foreach (string line in File.ReadAllLines(ConfigValues.ConfigPath + @"Hud\precacheshaders.txt"))
                GSCFunctions.PreCacheShader(line);
        }

        /// <returns>the usability message HUD element (bottom centre)</returns>
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

        /// <summary>
        /// Show a usability message (bottom centre).
        /// </summary>
        void HUD_ShowMessage(Entity player, string text)
        {
            HudElem message = HUD_GetMessage(player);
            message.Alpha = .85f;
            message.SetText(text);
        }

        /// <summary>
        /// Hide the usability message (bottom centre).
        /// </summary>
        void HUD_HideMessage(Entity player)
        {
            HudElem message = HUD_GetMessage(player);
            message.Alpha = 0;
            message.SetText("");
        }

        /// <returns>the top left information HUD element (left of minimap).</returns>
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

        /// <summary>
        /// Update the top left information HUD element (left of minimap) for all players.
        /// This HUD element shows the currect weapon out of the weapon reward list and it shows the timers of any search and destroy style mapedit objectives.
        /// </summary>
        public static void HUD_UpdateTopLeftInformation()
        {
            foreach (Entity player in Players)
                HUD_UpdateTopLeftInformation(player);
        }

        /// <summary>
        /// Update the top left information HUD element (left of minimap) for a single player.
        /// This HUD element shows the currect weapon out of the weapon reward list and it shows the timers of any search and destroy style mapedit objectives.
        /// </summary>
        public static void HUD_UpdateTopLeftInformation(Entity player)
        {
            HudElem topLeftInformation = HUD_GetTopLeftInformation(player);
            string text = "";
            if (player.HasField("weapon_index"))
                text += "Weapon " + (player.GetField<int>("weapon_index") + 1) + "/" + WeaponRewardLists[0].Count + "\n";
            foreach (Entity objective in MapObjectives)
            {
                string colour = "^7";
                if (objective.HasField("bomb"))
                    colour = objective.GetField<Entity>("bomb") == player ? "^2" : "^1";
                text += "           " + colour + objective.GetField("name") + "\n";
            }
            topLeftInformation.SetText(text);
        }

        /// <summary>
        /// Start counting the search and destroy mapedit objective HUD timers.
        /// </summary>
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

        /// <summary>
        /// Given a mapedit objective, start counting if it has a bomb or reset the timer if it does not.
        /// </summary>
        public static void HUD_UpdateTopLeftTimer(Entity objective)
        {
            if (objective.HasField("bomb"))
                objective.GetField<HudElem>("hud_timer").SetTimer(objective.GetField<int>("timer"));
            else
                objective.GetField<HudElem>("hud_timer").SetTimerStatic(objective.GetField<int>("timer"));
        }

        /// <returns>The top centre HUD element</returns>
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

        /// <summary>
        /// Update the top centre information HUD in accordance with settings_top_hud.
        /// </summary>
        public static void HUD_UpdateTopInformation(Entity player)
        {
            if (ConfigValues.Settings_hud_top != "")
                HUD_GetTopInformation(player).SetText(ConfigValues.Settings_hud_top.Format(new Dictionary<string, string>()
                {
                    { "<score>", player.GetField<int>("score") + "" },
                    { "<{0:n}score>", string.Format("{0:n}", player.GetField<int>("score")) }
                }));
        }
        
        /// <summary>
        /// Get/initialise the top centre HUD message shown on reward
        /// </summary>
        public static HudElem HUD_GetRewardMessage(Entity player)
        {
            if (!player.HasField("hud_reward_message"))
            {
                HudElem rewardMsg = HudElem.CreateFontString(player, HudElem.Fonts.Big, 2.2f);
                rewardMsg.SetPoint("CENTER", "CENTER", 0, -230);
                rewardMsg.HideWhenInMenu = false;
                rewardMsg.HideWhenDead = false;
                rewardMsg.Alpha = 0;
                rewardMsg.Archived = true;
                rewardMsg.Sort = 20;
                player.SetField("hud_reward_message", rewardMsg);
            }
            return player.GetField<HudElem>("hud_reward_message");
        }
        
        /// <summary>
        /// Get/initialise the top centre HUD timer shown on reward
        /// </summary>
        public static HudElem HUD_GetRewardTimer(Entity player)
        {
            if (!player.HasField("hud_reward_timer"))
            {
                HudElem timer = HudElem.CreateFontString(player, HudElem.Fonts.Big, 2.2f);
                timer.SetPoint("CENTER", "CENTER", 0, -200);
                timer.HideWhenInMenu = false;
                timer.HideWhenDead = false;
                timer.Alpha = 0;
                timer.Archived = true;
                timer.Sort = 20;
                player.SetField("hud_reward_timer", timer);
            }
            return player.GetField<HudElem>("hud_reward_timer");
        }
        
        /// <summary>
        /// Set the top centre HUD timer shown on reward
        /// </summary>
        public static void HUD_SetRewardTimer(Entity player)
        {
            HudElem timer = HUD_GetRewardTimer(player);
            if (player.HasField("ticks_left"))
            {
                int ticksLeft = player.GetField<int>("ticks_left");
                WriteLog.Debug($"setting timer {ticksLeft}");
                timer.SetTimer(ticksLeft);
                timer.Alpha = 0.85f;
            }
        }
        
        /// <summary>
        /// Reset the top centre HUD timer shown on reward
        /// </summary>
        public static void HUD_ResetRewardTimer(Entity player)
        {
            HudElem timer = HUD_GetRewardTimer(player);
            timer.Alpha = 0;
        }
        
        /// <summary>
        /// Show a top centre HUD message on reward
        /// </summary>
        public static void HUD_SetRewardMessage(Entity player, string text)
        {
            HudElem rewardMsg = HUD_GetRewardMessage(player);
            rewardMsg.SetText(text);
            rewardMsg.Alpha = 0.85f;
        }
        
        /// <summary>
        /// Reset the top centre HUD message on reward
        /// </summary>
        public static void HUD_ResetRewardMessage(Entity player)
        {
            HudElem rewardMsg = HUD_GetRewardMessage(player);
            rewardMsg.Alpha = 0;
        }

        /// <summary>
        /// Initialise the top centre HUD message shown when an achievement objective has been completed.
        /// </summary>
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

        /// <summary>
        /// Initialise the achievement icon HUD.
        /// </summary>
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

        /// <summary>
        /// Show the achievement icons of the achiever to the viewer.
        /// </summary>
        public void HUD_ShowAchievements(Entity achiever, Entity viewer)
        {
            foreach (Achievement a in Achievements)
                if (achiever.HasField(a.Name))
                    viewer.GetField<HudElem>(a.Icon).Alpha = 1;
        }

        /// <summary>
        /// Hide all achievement icons to the viewer.
        /// </summary>
        public void HUD_HideAchievements(Entity viewer)
        {
            foreach (Achievement a in Achievements)
                viewer.GetField<HudElem>(a.Icon).Alpha = 0;
        }

        /// <summary>
        /// Show the achievement objective completed message to all players.
        /// </summary>
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
