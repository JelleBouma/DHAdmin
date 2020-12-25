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

        HudElem HUD_GetObjectives(Entity player)
        {
            if (!player.HasField("hud_objectives"))
            {
                HudElem objectives = HudElem.CreateFontString(player, HudElem.Fonts.Default, 1f);
                objectives.SetPoint("TOPLEFT", "TOPLEFT", 110, 3);
                objectives.HideWhenInMenu = true;
                objectives.HideWhenDead = false;
                objectives.Alpha = 0.85f;
                player.SetField("hud_objectives", objectives);
            }
            return player.GetField<HudElem>("hud_objectives");
        }

        void HUD_UpdateObjectives()
        {
            string text = "";
            foreach (Entity player in Players)
            {
                HudElem hudObjectives = HUD_GetObjectives(player);
                foreach (Entity objective in Objectives)
                {
                    string colour = "^7";
                    if (objective.HasField("bomb"))
                        colour = objective.GetField<Entity>("bomb") == player ? "^2" : "^1";
                    string ticks_left = "";
                    if (objective.GetField<bool>("usable"))
                        ticks_left += objective.GetField<int>("ticks_left");
                    else
                        ticks_left = "destroyed by " + objective.GetField<Entity>("destroyer").Name;
                    text += colour + objective.GetField("name") + ": " + ticks_left + "\n";
                }
                hudObjectives.SetText(text);
            }
        }

    }
}
