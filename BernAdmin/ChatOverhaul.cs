using InfinityScript;

namespace LambAdmin
{
    public partial class DHAdmin
    {

        public void CHAT_WriteChat(Entity sender, ChatType type, string message)
        {
            if (sender.IsMuted())
                return;
            ChatLog(sender, message, type);
            if (sender.HasField("nootnoot") && sender.GetField<int>("nootnoot") == 1)
                message = "noot noot";
            if(type == ChatType.All)
                if (sender.IsAlive)
                    Utilities.RawSayAll(string.Format(ConfigValues.Format_message, "", sender.GetFormattedName(database), message));
                else if (sender.IsSpectating())
                    Utilities.RawSayAll(string.Format(ConfigValues.Format_message, ConfigValues.Format_prefix_spectator, sender.GetFormattedName(database), message));
                else
                    Utilities.RawSayAll(string.Format(ConfigValues.Format_message, ConfigValues.Format_prefix_dead, sender.GetFormattedName(database), message));
            else
            {
                string team = sender.GetTeam();
                if (sender.IsAlive)
                    CHAT_WriteToAllFromTeam(team, string.Format(ConfigValues.Format_message, ConfigValues.Format_prefix_team, sender.GetFormattedName(database), message));
                else if (sender.IsSpectating())
                    CHAT_WriteToAllFromTeam(team, string.Format(ConfigValues.Format_message, ConfigValues.Format_prefix_team + ConfigValues.Format_prefix_spectator, sender.GetFormattedName(database), message));
                else
                    CHAT_WriteToAllFromTeam(team, string.Format(ConfigValues.Format_message, ConfigValues.Format_prefix_team + ConfigValues.Format_prefix_dead, sender.GetFormattedName(database), message));
            }
        }

        public void ChatLog(Entity sender, string message, ChatType chattype)
        {
            Log.Write(LogLevel.Info, "[" + chattype + "] " + sender.Name + ": " + message);
        }

        public void CHAT_WriteToAllFromTeam(string team, string message)
        {
            foreach(Entity player in Players)
                if(player.GetTeam() == team)
                    Utilities.RawSayTo(player, message);
        }
    }
}
