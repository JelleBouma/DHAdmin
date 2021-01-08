using System.IO;
using InfinityScript;

namespace LambAdmin
{
    public partial class DHAdmin
    {

        void HUD_PrecacheShaders()
        {
            string file = ConfigValues.ConfigPath + @"Hud\precacheshaders.txt";
            if (File.Exists(file))
                foreach (string line in File.ReadAllLines(file))
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
            foreach (Entity objective in Objectives)
            {
                string colour = "^7";
                if (objective.HasField("bomb"))
                    colour = objective.GetField<Entity>("bomb") == player ? "^2" : "^1";
                string ticks_left = "";
                //if (objective.GetField<bool>("usable"))
                //    ticks_left += objective.GetField<int>("ticks_left");
                //else
                //    ticks_left = "destroyed by " + objective.GetField<Entity>("destroyer").Name;
                text += colour + objective.GetField("name") + "" + ticks_left + "\n";
            }
            topLeftInformation.SetText(text);
        }

    }
}
