using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfinityScript;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace LambAdmin
{
    public partial class DHAdmin
    {
        System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.InvariantCulture;

        static SLOG MainLog;
        static SLOG PlayersLog;
        static SLOG CommandsLog;
        static SLOG HaxLog;

        HudElem RGAdminMessage;
        HudElem OnlineAdmins;

        //typedef
        public struct Dvar
        {
            [XmlAttribute]
            public string key;
            [XmlAttribute]
            public string value;
        }
        public class Dvars : List<Dvar> { };
        //-------

        public static class Data
        {
            public static int HWIDOffset = 0x4A30335;
            public static int HWIDDataSize = 0x78688;

            public static int XNADDROffset = 0x049EBD00;
            public static int XNADDRDataSize = 0x78688;

            public static int ClantagOffset = 0x01AC5564;
            public static int ClantagPlayerDataSize = 0x38A4;

            public static List<char> HexChars = new List<char>()
            {
                'a', 'b', 'c', 'd', 'e', 'f', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            };

            public static Dictionary<string, string> Colors = new Dictionary<string, string>()
            {
                {"^1", "red"},
                {"^2", "green"},
                {"^3", "yellow"},
                {"^4", "blue"},
                {"^5", "lightblue"},
                {"^6", "purple"},
                {"^7", "white"},
                {"^8", "defmapcolor"},
                {"^9", "grey"},
                {"^0", "black"},
                {"^;", "yaleblue"},
                {"^:", "orange"}
            };
            public static Dictionary<string, string> StandardMapNames = new Dictionary<string, string>()
            {
                {"dome", "mp_dome"},
                {"mission", "mp_bravo"},
                {"lockdown", "mp_alpha"},
                {"bootleg", "mp_bootleg"},
                {"hardhat", "mp_hardhat"},
                {"bakaara", "mp_mogadishu"},
                {"arkaden", "mp_plaza2"},
                {"carbon", "mp_carbon"},
                {"fallen", "mp_lambeth"},
                {"outpost", "mp_radar"},
                {"downturn", "mp_exchange"},
                {"interchange", "mp_interchange"},
                {"resistance", "mp_paris"},
                {"seatown", "mp_seatown"},
                {"village", "mp_village"},
                {"underground", "mp_underground"}
            };

            public static Dictionary<string, string> DLCMapNames = new Dictionary<string, string>()
            {
                {"piazza", "mp_italy"},
                {"liberation", "mp_park"},
                {"blackbox", "mp_morningwood"},
                {"overwatch", "mp_overwatch"},
                {"aground", "mp_aground_ss"},
                {"erosion", "mp_courtyard_ss"},
                {"foundation", "mp_cement"},
                {"getaway", "mp_hillside_ss"},
                {"sanctuary", "mp_meteora"},
                {"oasis", "mp_qadeem"},
                {"lookout", "mp_restrepo_ss"},
                {"terminal", "mp_terminal_cls"},
                {"intersection", "mp_crosswalk_ss"},
                {"u-turn", "mp_burn_ss"},
                {"vortex", "mp_six_ss"},
                {"gulch", "mp_moab"},
                {"boardwalk", "mp_boardwalk"},
                {"parish", "mp_nola"},
                {"offshore", "mp_roughneck"},
                {"decommision", "mp_shipbreaker"}
            };

            public static Dictionary<string, string> Pistols = new Dictionary<string, string>()
            {
                {"iw5_mp412_mp", "weapon_mp412"},
                {"iw5_deserteagle_mp", "weapon_desert_eagle_iw5"},
                {"iw5_p99_mp", "weapon_walther_p99_iw5"},
                {"iw5_fnfiveseven_mp", "weapon_fn_fiveseven_iw5"},
                {"iw5_44magnum_mp", "weapon_44_magnum_iw5"},
                {"iw5_g18_mp", "weapon_g18_iw5"},
                {"iw5_usp45_mp", "weapon_usp45_iw5"}
            };

            public static Dictionary<string, string> AllMapNames = StandardMapNames.Plus(DLCMapNames);
            public static List<string> TeamNames = new List<string>()
            {
                "axis", "allies", "spectator"
            };
        }

        public static class WriteLog
        {
            public static void Info(string message)
            {
                Log.Write(LogLevel.Info, message);
            }

            public static void Error(string message)
            {
                Log.Write(LogLevel.Error, message);
            }

            public static void Warning(string message)
            {
                Log.Write(LogLevel.Warning, message);
            }

            public static void Debug(string message)
            {
                if (ConfigValues.DEBUG)
                    Log.Write(LogLevel.Debug, message);
            }
        }

        public static class Mem
        {
            public static unsafe string ReadString(int address, int maxlen = 0)
            {
                string ret = "";
                maxlen = (maxlen == 0) ? int.MaxValue : maxlen;
                for (; address < address + maxlen && *(byte*)address != 0; address++)
                    ret += Encoding.ASCII.GetString(new byte[] { *(byte*)address });
                return ret;
            }

            public static unsafe void WriteString(int address, string str)
            {
                byte[] strarr = Encoding.ASCII.GetBytes(str);
                foreach (byte ch in strarr)
                {
                    *(byte*)address = ch;
                    address++;
                }
                *(byte*)address = 0;
            }
        }

        public class SLOG
        {
            string path = ConfigValues.ConfigPath + @"Logs\";
            string filepath;
            bool notify;

            public SLOG(string filename, bool NotifyIfFileExists = false)
            {
                path += filename;
                notify = NotifyIfFileExists;
            }

            private void CheckFile()
            {
                filepath = path + " " + DateTime.Now.ToString("yyyy MM dd") + ".log";
                if (!File.Exists(filepath))
                    File.WriteAllLines(filepath, new string[]
                    {
                        "---- LOG FILE CREATED ----",
                    });
                if (notify)
                    File.AppendAllLines(filepath, new string[]
                    {
                        "---- INSTANCE CREATED ----",
                    });
            }

            public void WriteInfo(string message) => WriteMsg("INFO", message);
            public void WriteError(string message) => WriteMsg("ERROR", message);
            public void WriteWarning(string message) => WriteMsg("WARNING", message);
            public void WriteMsg(string prefix, string message)
            {
                CheckFile();
                using (StreamWriter file = File.AppendText(filepath))
                    file.WriteLine(DateTime.Now.TimeOfDay.ToString() + " [" + prefix + "] " + message);
            }
        }

        public class Announcer
        {
            List<string> message_list;
            public int message_interval;
            string name;

            public Announcer(string announcername, List<string> messages, int interval = 40000)
            {
                message_interval = interval;
                message_list = messages;
                name = announcername;
            }

            public string SpitMessage()
            {
                int currentmsg = GetStep();
                string messagetobespit = message_list[currentmsg];
                if (++currentmsg >= message_list.Count)
                    currentmsg = 0;
                SetStep(currentmsg);
                return messagetobespit;
            }

            public int GetStep()
            {
                string path = ConfigValues.ConfigPath + @"Utils\internal\announcers\" + name + ".txt";
                if (File.Exists(path))
                    try
                    {
                        int step = int.Parse(File.ReadAllText(path));
                        if (step > message_list.Count - 1)
                        {
                            File.Delete(path);
                            return 0;
                        }
                        else
                            return step;
                    }
                    catch
                    {
                        File.Delete(path);
                    }
                return 0;
            }

            public void SetStep(int step) => File.WriteAllLines(ConfigValues.ConfigPath + @"Utils\internal\announcers\" + name + ".txt", new string[] { step.ToString() });
        }

        public class HWID
        {
            public string Value
            {
                get; private set;
            }

            public HWID(Entity player)
            {
                if (player == null || !player.IsPlayer)
                {
                    Value = null;
                    return;
                }
                int address = Data.HWIDDataSize * player.GetEntityNumber() + Data.HWIDOffset;
                string formattedhwid = "";
                unsafe
                {
                    for (int i = 0; i < 12; i++)
                    {
                        if (i % 4 == 0 && i != 0)
                            formattedhwid += "-";
                        formattedhwid += (*(byte*)(address + i)).ToString("x2");
                    }
                }
                Value = formattedhwid;
            }

            private HWID(string value) => Value = value;

            public bool IsBadHWID() => string.IsNullOrWhiteSpace(Value) || Value == "00000000-00000000-00000000";
            public override string ToString() => Value;
            public static bool TryParse(string str, out HWID parsedhwid)
            {
                str = str.ToLowerInvariant();
                parsedhwid = new HWID((string)null);
                if (str.Length != 26)
                    return false;
                for (int i = 0; i < 26; i++)
                    if (((i == 8 || i == 17) && str[i] != '-') || !str[i].IsHex())
                        return false;
                parsedhwid = new HWID(str);
                return true;
            }
        }

        public class XNADDR
        {
            public string Value
            {
                get; private set;
            }

            public XNADDR(Entity player)
            {
                if (player == null || !player.IsPlayer)
                {
                    Value = null;
                    return;
                }
                string connectionstring = Mem.ReadString(Data.XNADDRDataSize * player.GetEntityNumber() + Data.XNADDROffset, Data.XNADDRDataSize);
                string[] parts = connectionstring.Split('\\');
                for (int i = 1; i < parts.Length; i++)
                    if (parts[i - 1] == "xnaddr")
                    {
                        Value = parts[i].Substring(0, 12);
                        return;
                    }
                Value = null;
            }

            public override string ToString() => Value;
        }

        public class PlayerInfo
        {
            private string player_ip = null;
            private long? player_guid = null;
            private HWID player_hwid = null;

            public PlayerInfo(Entity player)
            {
                player_ip = player.IP.Address.ToString();
                player_guid = player.GUID;
                player_hwid = player.GetHWID();
            }

            private PlayerInfo() {}

            /// <returns>Whether IP, GUID and HWID are all either the same as in B or nulled in B. But returns false if either PlayerInfo is null.</returns>
            public bool MatchesAND(PlayerInfo B)
            {
                if (B.IsNull() || IsNull())
                    return false;
                return
                    (B.player_ip == null || player_ip == B.player_ip) &&
                    (B.player_guid == null || player_guid.Value == B.player_guid.Value) &&
                    (B.player_hwid == null || player_hwid.Value == B.player_hwid.Value);
            }

            public bool MatchesOR(PlayerInfo B)
            {
                if (player_ip != null && B.player_ip != null && player_ip == B.player_ip)
                    return true;
                if (player_guid != null && B.player_guid != null && player_guid.Value == B.player_guid.Value)
                    return true;
                if (player_hwid != null && B.player_hwid != null && player_hwid.Value == B.player_hwid.Value)
                    return true;
                return false;
            }

            public void AddIdentifier(string identifier)
            {
                if (long.TryParse(identifier, out long result))
                    player_guid = result;
                else if (IPAddress.TryParse(identifier, out IPAddress address))
                    player_ip = address.ToString();
                else if (HWID.TryParse(identifier, out HWID possibleHWID))
                    player_hwid = possibleHWID;
            }

            public static PlayerInfo Parse(string str)
            {
                PlayerInfo pi = new PlayerInfo();
                string[] parts = str.Split(',');
                foreach (string part in parts)
                    pi.AddIdentifier(part);
                return pi;
            }

            public string GetIdentifiers()
            {
                List<string> identifiers = new List<string>();
                if (player_guid != null)
                    identifiers.Add(player_guid.ToString());
                if (player_hwid != null)
                    identifiers.Add(player_hwid.ToString());
                return string.Join(",", identifiers);
            }

            public bool IsNull() => player_ip == null && player_guid == null && player_hwid == null;

            public override string ToString() => GetIdentifiers();

            public string GetGUIDString()
            {
                if (player_guid.HasValue)
                    return player_guid.Value.ToString();
                return null;
            }

            public string GetIPString() => player_ip;

            //CHANGE
            public string GetHWIDString() => player_hwid?.Value;

            //CHANGE
            public static PlayerInfo CommonIdentifiers(PlayerInfo A, PlayerInfo B)
            {
                PlayerInfo commoninfo = new PlayerInfo();
                if (B.IsNull() || A.IsNull())
                    return null;
                if (!string.IsNullOrWhiteSpace(A.GetIPString()) && A.GetIPString() == B.GetIPString())
                    commoninfo.player_ip = A.player_ip;
                if (!string.IsNullOrWhiteSpace(A.GetGUIDString()) && A.GetGUIDString() == B.GetGUIDString())
                    commoninfo.player_guid = A.player_guid;
                if (!string.IsNullOrWhiteSpace(A.GetHWIDString()) && A.GetHWIDString() == B.GetHWIDString())
                    commoninfo.player_hwid = A.player_hwid;
                return commoninfo;
            }
        }

        public static void WriteChat(Entity player, string message, bool broadcast)
        {
            if (broadcast)
                WriteChatToAll(message);
            else
                WriteChatToPlayer(player, message);
        }

        public static void WriteChatToAll(string message)
        {
            if (!string.IsNullOrWhiteSpace(message))
                Utilities.RawSayAll(ConfigValues.ChatPrefix + " " + message);
        }

        public static void WriteChatToPlayer(Entity player, string message) => Utilities.RawSayTo(player, ConfigValues.ChatPrefixPM + " " + message);

        public static void WriteChatMultiline(Entity player, string[] messages, bool broadcast, int delay = 500)
        {
            if (broadcast)
                WriteChatToAllMultiline(messages, delay);
            else
                WriteChatToPlayerMultiline(player, messages, delay);
        }

        public static void WriteChatToAllMultiline(string[] messages, int delay = 500)
        {
            int num = 0;
            foreach (string str in messages)
            {
                string message = str;
                AfterDelay(num * delay, (() => WriteChatToAll(message)));
                ++num;
            }
        }
        public static void WriteChatToPlayerMultiline(Entity player, string[] messages, int delay = 500)
        {
            int num = 0;
            foreach (string message in messages)
            {
                AfterDelay(num * delay, () => WriteChatToPlayer(player, message));
                num++;
            }
        }

        public static void WriteChatCondensed(Entity player, string[] messages, bool broadcast, int delay = 1000, int condenselevel = 40, string separator = ", ")
        {
            if (broadcast)
                WriteChatToAllCondensed(messages, delay, condenselevel, separator);
            else
                WriteChatToPlayerCondensed(player, messages, delay, condenselevel, separator);
        }

        public static void WriteChatToAllCondensed(string[] messages, int delay = 1000, int condenselevel = 40, string separator = ", ") => WriteChatToAllMultiline(messages.Condense(condenselevel, separator), delay);

        public static void WriteChatToPlayerCondensed(Entity player, string[] messages, int delay = 1000, int condenselevel = 40, string separator = ", ") => WriteChatToPlayerMultiline(player, messages.Condense(condenselevel, separator), delay);

        public static void WriteChatSpyToPlayer(Entity player, string message) => Utilities.RawSayTo(player, ConfigValues.ChatPrefixSPY + " " + message);

        public static void WriteChatAdmToPlayer(Entity player, string message) => Utilities.RawSayTo(player, ConfigValues.ChatPrefixAdminMSG + message);

        public void ChangeMap(string devmapname) => ExecuteCommand("map " + devmapname);

        public List<Entity> FindPlayers(string identifier, Entity sender = null)
        {
            if (identifier.StartsWith("#"))
            {
                int number = int.Parse(identifier.Substring(1));
                Entity ent = Entity.GetEntity(number);
                if (number >= 0 && number < 18)
                    foreach (Entity player in Players)
                        if (player.GetEntityNumber() == number)
                            return new List<Entity>() { ent };
                return new List<Entity>();
            }
            if (identifier.StartsWith("*") && identifier.EndsWith("*") && (identifier.Length > 1) && (sender != null))
            {
                identifier = identifier.Substring(1, identifier.Length - 2);
                return (new PlayersFilter(sender)).Filter(identifier);
            }
            identifier = identifier.ToLowerInvariant();
            return (from player in Players
                    where player.Name.ToLowerInvariant().Contains(identifier)
                    select player).ToList();
        }

        public Entity FindSinglePlayer(string identifier)
        {
            List<Entity> players = FindPlayers(identifier);
            if (players.Count != 1)
                return null;
            return players[0];
        }

        // Find single player,
        // or multiple players (if Filter given)
        public List<Entity> FindSinglePlayerXFilter(string identifier, Entity sender = null)
        {
            if (identifier.StartsWith("*") && identifier.EndsWith("*") && (identifier.Length > 1) && (sender != null))
            {
                identifier = identifier.Substring(1, identifier.Length - 2);
                List<Entity> players = new PlayersFilter(sender).Filter(identifier);
                if (players.Count == 0)
                    WriteChatToPlayer(sender, Command.GetMessage("NotOnePlayerFound"));
                return players;
            }
            else
            {
                List<Entity> players = FindPlayers(identifier);
                if (players.Count != 1)
                {
                    WriteChatToPlayer(sender, Command.GetMessage("NotOnePlayerFound"));
                    return new List<Entity>();
                }
                return players;
            }
        }

        public List<Entity> FindPlayersFilter(string identifier, Entity sender)
        {
            if (identifier.StartsWith("*") && identifier.EndsWith("*") && (identifier.Length > 1)) {
                identifier = identifier.Substring(1, identifier.Length - 2);
                List<Entity> players = new PlayersFilter(sender).Filter(identifier);
                if (players.Count == 0)
                    WriteChatToPlayer(sender, Command.GetMessage("NotOnePlayerFound"));
                return players;
            }
            else
            {
                WriteChatToPlayer(sender, Command.GetMessage("Filters_error1"));
                return new List<Entity>();
            }
        }

        public List<string> FindMaps(string identifier)
        {
            return (from map in ConfigValues.AvailableMaps
                    where map.Key.Contains(identifier) || map.Value.Contains(identifier)
                    select map.Value).ToList();
        }

        public string FindSingleMap(string identifier)
        {
            List<string> maps = FindMaps(identifier);
            if (maps.Count != 1)
                return null;
            return maps[0];
        }

        public string DevMapName2Mapname(string devMapname)
        {
            List<string> maps =
                (from map in ConfigValues.AvailableMaps
                 where map.Value.Contains(devMapname)
                 select map.Key).ToList();
            if (maps.Count != 1)
                return null;
            return maps[0];
        }

        public IEnumerable<Entity> GetEntities()
        {
            for (int i = 0; i < 2048; i++)
                yield return Entity.GetEntity(i);
        }

        public void UTILS_OnPlayerConnect(Entity player)
        {
            MainLog.WriteInfo("UTILS_OnPlayerConnect");
            //check if bad name
            foreach (string identifier in File.ReadAllLines(ConfigValues.ConfigPath + @"Utils\badnames.txt"))
                if (player.Name == identifier)
                {
                    CMD_tmpban(player, "^1Piss off, hacker.");
                    WriteChatToAll(Command.GetString("tmpban", "message").Format(new Dictionary<string, string>()
                    {
                        {"<target>", "^:" + player.Name },
                        {"<targetf>", "^:" + player.GetFormattedName(database) },
                        {"<issuer>", ConfigValues.ChatPrefix },
                        {"<issuerf>", ConfigValues.ChatPrefix },
                        {"<reason>", "Piss off, hacker." },
                    }));
                    return;
                }
            if (!Forced_clantags.Keys.Contains(player.GUID))
            {
                //check if bad clantag
                foreach (string identifier in File.ReadAllLines(ConfigValues.ConfigPath + @"Utils\badclantags.txt"))
                if (player.ClanTag == identifier)
                {
                        CMD_tmpban(player, "^1Piss off, hacker.");
                        WriteChatToAll(Command.GetString("tmpban", "message").Format(new Dictionary<string, string>()
                        {
                            {"<target>", "^:" + player.Name },
                            {"<targetf>", "^:" + player.GetFormattedName(database) },
                            {"<issuer>", ConfigValues.ChatPrefix },
                            {"<issuerf>", ConfigValues.ChatPrefix },
                            {"<reason>", "Piss off, hacker." },
                        }));
                        return;
                    }
            }

            //check if bad xnaddr
            if (player.GetXNADDR() == null || string.IsNullOrEmpty(player.GetXNADDR().ToString()))
            {
                ExecuteCommand("dropclient " + player.EntRef + " \"Cool story bro.\"");
                return;
            }

            //check if EO Unbanner
            string rawplayerhwid = player.GetHWIDRaw();
            if (System.Text.RegularExpressions.Regex.Matches(rawplayerhwid, "adde").Count >= 3)
            {
                WriteChatToAll(Command.GetString("ban", "message").Format(new Dictionary<string, string>()
                    {
                        {"<target>", "^:" + player.Name },
                        {"<targetf>", "^:" + player.GetFormattedName(database) },
                        {"<issuer>", ConfigValues.ChatPrefix },
                        {"<issuerf>", ConfigValues.ChatPrefix },
                        {"<reason>", "Piss off, hacker." },
                    }));
                ExecuteCommand("dropclient " + player.EntRef + " \"^3BAI SCRUB :D\"");
                return;
            }

            //log player name
            if (File.Exists(string.Format(ConfigValues.ConfigPath + @"Utils\playerlogs\{0}.txt", player.GUID)))
            {
                List<string> lines = File.ReadAllLines(string.Format(ConfigValues.ConfigPath + @"Utils\playerlogs\{0}.txt", player.GUID)).ToList();
                if (!lines.Contains(player.Name))
                    lines.Add(player.Name);
                File.WriteAllLines(string.Format(ConfigValues.ConfigPath + @"Utils\playerlogs\{0}.txt", player.GUID), lines);
            }
            else
                File.WriteAllLines(string.Format(ConfigValues.ConfigPath + @"Utils\playerlogs\{0}.txt", player.GUID), new string[] { player.Name });

            //check for names ingame
            if (player.Name.Length < 3)
            {
                CMD_kick(player, "Name must be at least 3 characters long.");
                return;
            }

            if (!player.HasField("killstreak"))
                player.SetField("killstreak", 0);
            MainLog.WriteInfo("UTILS_OnPlayerConnect done");
        }

        public void UTILS_ForceClass(Entity player, string teamAndNumber) {
            string team = int.TryParse(teamAndNumber.Last() + "", out int classNumber) ? teamAndNumber.Substring(0, teamAndNumber.Length - 1) : teamAndNumber;
            player.CloseInGameMenu();
            player.Notify("menuresponse", "team_marinesopfor", team);
            if (classNumber != 0)
            {
                player.OnNotify("joined_team", ent => AfterDelay(100, () => ent.Notify("menuresponse", "changeclass", team + "_recipe" + classNumber)));
                player.OnNotify("menuresponse", (player2, menu, response) =>
                {
                    if (menu.ToString().Equals("class") && response.ToString().Equals("changeclass_marines"))
                        AfterDelay(100, () => player.Notify("menuresponse", "changeclass", "back"));
                });
            }
        }

        public void UTILS_OnServerStart()
        {

            MainLog = new SLOG("main");
            PlayersLog = new SLOG("players");
            CommandsLog = new SLOG("commands");
            HaxLog = new SLOG("haxor");

            PlayerConnected += UTILS_OnPlayerConnect;
            PlayerConnecting += UTILS_OnPlayerConnecting;
            OnPlayerKilledEvent += UTILS_BetterBalance;

            // RGADMIN HUDELEM
            if (bool.Parse(Sett_GetString("settings_showversion")))
            {
                RGAdminMessage = HudElem.CreateServerFontString(HudElem.Fonts.Big, 0.6f);
                RGAdminMessage.SetPoint("BOTTOMRIGHT", "BOTTOMRIGHT",0,-30);
                RGAdminMessage.SetText(" ^0[^1DH^0]\n^:Admin\n^0" + ConfigValues.Version);
                RGAdminMessage.Color = new Vector3(1f, 0.75f, 0f);
                RGAdminMessage.GlowAlpha = 1f;
                RGAdminMessage.GlowColor = new Vector3(0.349f, 0f, 0f);
                RGAdminMessage.Foreground = true;
                RGAdminMessage.HideWhenInMenu = true;
            }

            // ADMINS HUDELEM
            if (bool.Parse(Sett_GetString("settings_adminshudelem")))
            {
                OnlineAdmins = HudElem.CreateServerFontString(HudElem.Fonts.HudSmall, 0.5f);
                OnlineAdmins.SetPoint("top", "top", 0, 5);
                OnlineAdmins.Foreground = true;
                OnlineAdmins.Archived = false;
                OnlineAdmins.HideWhenInMenu = true;
                OnInterval(5000, () =>
                {
                    OnlineAdmins.SetText("^1Online Admins:\n" + string.Join("\n", database.GetAdminsString(Players).Condense(100, "^7, ")));
                    return true;
                });
            }

            OnGameEnded += UTILS_OnGameEnded;
        }

        private void hud_alive_players(Entity player)
        {
            HudElem fontString1 = HudElem.CreateFontString(player, HudElem.Fonts.Big, 0.6f);
            fontString1.SetPoint("DOWNRIGHT", "DOWNRIGHT", -19, 60);
            fontString1.SetText("^3Allies^7:");
            fontString1.HideWhenInMenu = true;
            HudElem fontString2 = HudElem.CreateFontString(player, HudElem.Fonts.Big, 0.6f);
            fontString2.SetPoint("DOWNRIGHT", "DOWNRIGHT", -19, 80);
            fontString2.SetText("^1Enemy^7:");
            fontString2.HideWhenInMenu = true;
            HudElem hudElem2 = HudElem.CreateFontString(player, HudElem.Fonts.Big, 0.6f);
            hudElem2.SetPoint("DOWNRIGHT", "DOWNRIGHT", -8, 60);
            hudElem2.HideWhenInMenu = true;
            HudElem hudElem3 = HudElem.CreateFontString(player, HudElem.Fonts.Big, 0.6f);
            hudElem3.SetPoint("DOWNRIGHT", "DOWNRIGHT", -8, 80);
            hudElem3.HideWhenInMenu = true;
            OnInterval(50, () =>
            {
                string str1 = player.GetField<string>("sessionteam");
                string str2 = GSCFunctions.GetTeamPlayersAlive("axis").ToString();
                string str3 = GSCFunctions.GetTeamPlayersAlive("allies").ToString();
                hudElem2.SetText(str1.Equals("allies") ? str3 : str2);
                hudElem3.SetText(str1.Equals("allies") ? str2 : str3);
                return true;
            });
        }
        private void Timed_messages_init()
        {
            if (ConfigValues.Settings_timed_messages)
            {
                Announcer announcer = new Announcer(
                    "default",
                    File.ReadAllLines(ConfigValues.ConfigPath + @"Utils\announcer.txt").ToList(),
                    ConfigValues.Settings_timed_messages_interval
                );
                OnInterval(announcer.message_interval, () =>
                {
                    WriteChatToAll(announcer.SpitMessage());
                    return true;
                });
            }
        }

        public void UTILS_BetterBalance(Entity player, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            if (!ConfigValues.Settings_betterbalance_enable || GSCFunctions.GetDvar("g_gametype") == "infect")
                return;
            if (GSCFunctions.GetDvar("betterbalance") == "false")
                return;
            UTILS_GetTeamPlayers(out int axis, out int allies);
            switch (player.GetTeam())
            {
                case "axis":
                    if (axis - allies > 1)
                    {
                        player.SetField("sessionteam", "allies");
                        player.Notify("menuresponse", "team_marinesopfor", "allies");
                        WriteChatToAll(Sett_GetString("settings_betterbalance_message").Format(new Dictionary<string, string>()
                        {
                            {"<player>", player.Name},
                            {"<playerf>", player.GetFormattedName(database)},
                        }));
                    }
                    return;
                case "allies":
                    if (allies - axis > 1)
                    {
                        player.SetField("sessionteam", "axis");
                        player.Notify("menuresponse", "team_marinesopfor", "axis");
                        WriteChatToAll(Sett_GetString("settings_betterbalance_message").Format(new Dictionary<string, string>()
                        {
                            {"<player>", player.Name},
                            {"<playerf>", player.GetFormattedName(database)},
                        }));
                    }
                    return;
            }
        }

        public void UTILS_UnlimitedAmmo(bool force = false)
        {
            string state = GSCFunctions.GetDvar("unlimited_ammo") + GSCFunctions.GetDvar("unlimited_stock") + GSCFunctions.GetDvar("unlimited_grenades");
            switch (state)
            {
                case "000":
                    return;
                case "222":
                    if (((!ConfigValues.Settings_unlimited_ammo && !ConfigValues.Settings_unlimited_stock && !ConfigValues.Settings_unlimited_grenades) || GSCFunctions.GetDvar("g_gametype") == "infect") && !force)
                        return;
                    break;
            }
            if (ConfigValues.Unlimited_ammo_active)
                return;
            ConfigValues.Unlimited_ammo_active = true;
            WriteLog.Debug("Initializing Unlimited Ammo...");
        }

        public static void UTILS_Maintain()
        {
            OnInterval(50, () =>
            {
                if (ConfigValues.Unlimited_ammo_active || ConfigValues.Score_maintenance_active || ConfigValues.Speed_maintenance_active) {
                    foreach (Entity player in from player in Players where player.IsAlive select player)
                    {
                        if ((ConfigValues.Settings_unlimited_grenades && GSCFunctions.GetDvar("unlimited_grenades") != "0") || GSCFunctions.GetDvar("unlimited_grenades") == "1")
                        {
                            var offhandAmmo = player.GetCurrentOffhand();
                            player.SetWeaponAmmoClip(offhandAmmo, 99);
                            player.GiveMaxAmmo(offhandAmmo);
                        }
                        var currentWeapon = player.CurrentWeapon;
                        if (!ETC.ContainsName(currentWeapon))
                        {
                            if ((ConfigValues.Settings_unlimited_stock && GSCFunctions.GetDvar("unlimited_stock") != "0") || GSCFunctions.GetDvar("unlimited_stock") == "1")
                                player.SetWeaponAmmoStock(currentWeapon, 99);
                            if ((ConfigValues.Settings_unlimited_ammo && GSCFunctions.GetDvar("unlimited_ammo") != "0") || GSCFunctions.GetDvar("unlimited_ammo") == "1")
                                player.SetWeaponAmmoClip(currentWeapon, 99);
                        }
                        if (ConfigValues.Score_maintenance_active)
                            player.MaintainScore();
                        if (ConfigValues.Speed_maintenance_active)
                            player.MaintainSpeed();
                        if (ConfigValues.Lovecraftian_active)
                            CheckForHorrors(player);
                    }
                }
                return true;
            });
        }

        private static void CheckForHorrors(Entity player)
        {
            foreach (Entity potentialHorror in Players)
                if (potentialHorror != player && potentialHorror.IsAlive && potentialHorror.HasField("horror") && player.WorldPointInReticle_Circle(potentialHorror.GetTagOrigin("J_Spine4"), 80, 80) && player.Origin.DistanceTo(potentialHorror.Origin) < 400)
                {
                    player.FinishPlayerDamage(potentialHorror, potentialHorror, potentialHorror.GetField<int>("horror"), 0, "MOD_HEADSHOT", "none", player.GetTagOrigin("tag_eye"), potentialHorror.Origin, "none", 0);
                }
        }

        public void UTILS_OnGameEnded()
        {
            GameEnded = true;
            AfterDelay(1100, () =>
            {            
                // UNFREEZE PLAYERS ON GAME END
                if (bool.Parse(Sett_GetString("settings_unfreezeongameend")))
                    foreach (Entity player in Players)
                        if (!CMDS_IsRekt(player))
                            player.FreezeControls(false);

                // Save xlr stats
                if (ConfigValues.Settings_enable_xlrstats)
                {
                    WriteLog.Info("Saving xlrstats...");
                    xlr_database.Save();
                }

                WriteLog.Info("Saving PersonalPlayerDvars...");
                // Save FilmTweak settings
                UTILS_PersonalPlayerDvars_save(PersonalPlayerDvars);

                ConfigValues.SettingsMutex = true;
            });
        }

        public void UTILS_OnPlayerConnecting(Entity player)
        {
            WriteLog.Debug("UTILS_OnPlayerConnecting");
            MainLog.WriteInfo("UTILS_OnPlayerConnecting" + player.ClanTag);
            if (player.ClanTag.Contains(Encoding.ASCII.GetString(new byte[] { 0x5E, 0x02 })))
                ExecuteCommand("dropclient " + player.GetEntityNumber() + " \"Get out.\"");
            MainLog.WriteInfo("UTILS_OnPlayerConnecting done");
            WriteLog.Debug("UTILS_OnPlayerConnecting complete");
        }

        public bool UTILS_ParseBool(string message)
        {
            message = message.ToLowerInvariant().Trim();
            return message == "y" || message == "ye" || message == "yes" || message == "on" || message == "true" || message == "1";
        }
        
        /// <summary>
        /// Tick reward timers.
        /// </summary>
        void UTILS_TickTimers()
        {
            OnInterval(1000, () =>
            {
                foreach (Entity player in Players)
                    if (player.HasField("ticks_left"))
                    {
                        int ticksLeft = player.GetField<int>("ticks_left") - 1;
                        if (ticksLeft <= 0)
                        {
                            OnTimerExpireEvent(player);
                            player.ClearField("ticks_left");
                        }
                        else
                            player.SetField("ticks_left", ticksLeft);
                    }
                return true;
            });
        }

        public void UTILS_SetCliDefDvars(Entity player)
        {
            List<Dvar> dvars = new List<Dvar>();

            //DefaultCDvars
            foreach (KeyValuePair<string, string> dvar in CDvars)
                dvars.Add(new Dvar { key = dvar.Key, value = dvar.Value });

            dvars = UTILS_DvarListUnion(dvars, new List<Dvar>() { new Dvar { key = "fx_draw", value = "1" } });

            if (ConfigValues.Settings_daytime == "night") //night mode
                dvars = UTILS_DvarListUnion(dvars, UTILS_SetClientNightVision());
            else if (PersonalPlayerDvars.ContainsKey(player.GUID)) //personal dvars
                dvars = UTILS_DvarListUnion(dvars, PersonalPlayerDvars[player.GUID]);

            //team names
            List<Dvar> teamNames = new List<Dvar>();
            if (!string.IsNullOrWhiteSpace(ConfigValues.Settings_teamnames_allies))
                teamNames.Add(new Dvar { key = "g_TeamName_Allies", value = ConfigValues.Settings_teamnames_allies });
            if (!string.IsNullOrWhiteSpace(ConfigValues.Settings_teamnames_axis))
                teamNames.Add(new Dvar { key = "g_TeamName_Axis", value = ConfigValues.Settings_teamnames_axis });
            if (!string.IsNullOrWhiteSpace(ConfigValues.Settings_teamicons_allies))
                teamNames.Add(new Dvar { key = "g_TeamIcon_Allies", value = ConfigValues.Settings_teamicons_allies });
            if (!string.IsNullOrWhiteSpace(ConfigValues.Settings_teamicons_axis))
                teamNames.Add(new Dvar { key = "g_TeamIcon_Axis", value = ConfigValues.Settings_teamicons_axis });

            dvars = UTILS_DvarListUnion(dvars, teamNames);

            UTILS_SetClientDvarsPacked(player, dvars);
        }

        public void UTILS_SetClientDvarsPacked(Entity player, List<Dvar> dvars)
        { // do some dumb shit in this method because SetClientDvars takes the head and rest of array separate
            if (dvars.Count > 0)
            {
                var fuckingKey = dvars[0].key;
                var fuckingValue = dvars[0].value;
                dvars.RemoveAt(0);
                if (dvars.Count > 0)
                {
                    var fuckingArray = dvars.ConvertAll(v => { return new Parameter[] { v.key, v.value }; }).SelectMany(v => v).ToArray();
                    player.SetClientDvars(fuckingKey, fuckingValue, fuckingArray);
                }
                else
                    player.SetClientDvar(fuckingKey, fuckingValue);
            }
        }

        public static void ExecuteCommand(string command)
        {
            Utilities.ExecuteCommand(command);
        }

        public List<Dvar> UTILS_SetClientNightVision()
        {
            return
                new List<Dvar>(){
                    new Dvar {key = "r_filmUseTweaks",              value =  "1" },
                    new Dvar {key = "r_filmTweakEnable",            value =  "1" },
                    new Dvar {key = "r_filmTweakLightTint",         value =  "0 0.2 1" },
                    new Dvar {key = "r_filmTweakDarkTint",          value =  "0 0.125 1" },
                    new Dvar {key = "r_filmtweakbrightness",        value =  "0" },
                    new Dvar {key = "r_glowTweakEnable",            value =  "1"},
                    new Dvar {key = "r_glowUseTweaks",              value =  "1"},
                    new Dvar {key = "r_glowTweakRadius0",           value =  "5"},
                    new Dvar {key = "r_glowTweakBloomIntensity0",   value =  "0.5"},
                    new Dvar {key = "r_fog",                        value =  "0"}
                };
        }

        public List<Dvar> UTILS_SetClientInShadowFX()
        {
            return
                new List<Dvar>(){
                    new Dvar {key = "r_filmUseTweaks",              value =  "1" },
                    new Dvar {key = "r_filmTweakEnable",            value =  "1" },
                    new Dvar {key = "r_filmTweakDesaturation",      value =  "1" },
                    new Dvar {key = "r_filmTweakDesaturationDark",  value =  "1" },
                    new Dvar {key = "r_filmTweakInvert",            value =  "1" },
                    new Dvar {key = "r_glowTweakEnable",            value =  "1"},
                    new Dvar {key = "r_glowUseTweaks",              value =  "1"},
                    new Dvar {key = "r_glowTweakRadius0",           value =  "10"},
                    new Dvar {key = "r_filmTweakContrast",          value =  "3"},
                    new Dvar {key = "r_filmTweakBrightness",        value =  "1"},
                    new Dvar {key = "r_filmTweakLightTint",         value =  "1 0.125 0"},
                    new Dvar {key = "r_filmTweakDarkTint",          value =  "0 0 0"}
                };
        }

        public bool UTILS_ValidateFX(string fx)
        {
            string[] precached_fx = new string[] { };
            switch (GSCFunctions.GetDvar("mapname"))
            {
                case "mp_alpha": 
                    precached_fx = new string[]{
                        "dust/falling_dirt_frequent_runner",
                        "dust/dust_wind_fast_paper",
                        "dust/dust_wind_slow_paper",
                        "misc/trash_spiral_runner",
                        "dust/room_dust_200_blend_mp_vacant",
                        "misc/insects_carcass_flies",
                        "explosions/spark_fall_runner_mp",
                        "weather/ash_prague",
                        "weather/embers_prague_light",
                        "fire/firelp_med_pm",
                        "fire/firelp_small",
                        "smoke/thin_black_smoke_s_fast",
                        "dust/falling_dirt_frequent_runner",
                        "smoke/steam_manhole",
                        "smoke/battlefield_smokebank_S_warm_dense",
                        "smoke/bg_smoke_plume",
                        "fire/firelp_med_pm_cheap",
                        "fire/banner_fire",
                        "misc/light_glow_white_lamp",
                        "fire/fire_wall_50",
                        "smoke/white_battle_smoke",
                        "fire/after_math_embers",
                        "fire/wall_fire_mp",
                        "fire/car_fire_mp",
                        "fire/firelp_small_cheap_mp",
                        "misc/falling_ash_mp"
                    };
                    break;
                case "mp_bootleg":
                    precached_fx = new string[]{
                        "dust/falling_dirt_frequent_runner",
                        "dust/dust_wind_fast_paper",
                        "dust/dust_wind_slow_paper",
                        "misc/trash_spiral_runner",
                        "dust/room_dust_200_blend_mp_vacant_light",
                        "misc/insects_carcass_flies",
                        "explosions/spark_fall_runner_mp",
                        "weather/embers_prague_light",
                        "fire/firelp_med_pm",
                        "fire/firelp_small",
                        "smoke/thin_black_smoke_s_fast",
                        "dust/falling_dirt_frequent_runner",
                        "smoke/steam_manhole",
                        "smoke/battlefield_smokebank_S_warm_dense",
                        "smoke/bg_smoke_plume",
                        "fire/firelp_med_pm_cheap",
                        "fire/car_fire_mp",
                        "fire/firelp_small_cheap_mp",
                        "misc/falling_ash_mp",   
                        "misc/flocking_birds_mp",
                        "misc/insects_light_hunted_a_mp",
                        "weather/rain_mp_bootleg",
                        "weather/rain_noise_splashes",
                        "weather/rain_splash_lite_64x64",
                        "water/water_drips_fat_fast_speed",
                        "smoke/bg_smoke_plume",
                        "smoke/bootleg_alley_steam",
                        "misc/palm_leaves",
                        "misc/light_glow_white_lamp",
                        "water/waterfall_drainage_short_london",
                        "water/waterfall_splash_medium_london",
                        "water/drainpipe_mp_bootleg"
                    };
                    break;
                case "mp_bravo":
                    precached_fx = new string[]{
                        "dust/dust_wind_fast_paper",
                        "dust/dust_wind_fast_no_paper_bravo",
                        "misc/moth_runner",
                        "misc/trash_spiral_runner_bravo",
                        "smoke/battlefield_smokebank_s_cheap",
                        "smoke/hallway_smoke_light",
                        "misc/falling_brick_runner_line_100",
                        "misc/falling_brick_runner_line_200",
                        "misc/falling_brick_runner_line_300",
                        "misc/falling_brick_runner_line_400",
                        "misc/falling_brick_runner_line_600",
                        "misc/falling_brick_runner_line_75",
                        "smoke/room_smoke_200",
                        "misc/insects_carcass_runner",
                        "smoke/smoke_plume_grey_01",
                        "smoke/smoke_plume_grey_02",
                        "smoke/thick_black_smoke_mp",
                        "dust/falling_dirt_light_1_runner_bravo",
                        "misc/flocking_birds_mp",
                        "fire/car_fire_mp",
                        "fire/firelp_small_cheap_mp",
                        "misc/insects_light_invasion",
                        "misc/insect_trail_runner_icbm"
                    };
                    break;
                case "mp_carbon":
                    precached_fx = new string[]{
                        "dust/dust_wind_fast_paper",
                        "dust/dust_wind_fast_no_paper",
                        "dust/dust_wind_slow_paper",
                        "misc/trash_spiral_runner",
                        "smoke/battlefield_smokebank_s_cheap_mp_carbon",
                        "smoke/hallway_smoke_light",
                        "smoke/room_smoke_200",
                        "smoke/room_smoke_400",
                        "explosions/electrical_transformer_falling_fire_runner",
                        "fire/car_fire_mp",
                        "fire/firelp_small_cheap_mp",
                        "smoke/smoke_plume_grey_01",
                        "smoke/smoke_plume_grey_02",
                        "smoke/smoke_plume_white_01",
                        "smoke/smoke_plume_white_02",
                        "smoke/steam_large_vent_rooftop",
                        "smoke/steam_manhole",
                        "smoke/steam_roof_ac",
                        "fire/flame_refinery_far",
                        "fire/flame_refinery_small_far",
                        "fire/flame_refinery_small_far_2",
                        "fire/flame_refinery_small_far_3",
                        "smoke/steam_cs_mp_carbon",
                        "smoke/steam_jet_loop_cheap_mp_carbon",
                        "dust/dust_wind_fast_no_paper_airiel",
                        "water/water_drips_fat_fast_singlestream_mp_carbon",
                        "smoke/bootleg_alley_steam",
                        "misc/moth_runner"
                    };
                    break;
                case "mp_dome":
                    precached_fx = new string[]{
                        "weather/sand_storm_mp_dome_exterior",
                        "weather/sand_storm_mp_dome_interior",
                        "weather/sand_storm_mp_dome_interior_outdoor_only",
                        "dust/sand_spray_detail_runner_0x400",
                        "dust/sand_spray_detail_runner_400x400",
                        "dust/sand_spray_detail_oriented_runner_mp_dome",
                        "dust/sand_spray_cliff_oriented_runner",
                        "smoke/hallway_smoke_light",
                        "dust/light_shaft_dust_large",
                        "dust/room_dust_200_blend",
                        "dust/room_dust_100_blend",
                        "smoke/battlefield_smokebank_S",
                        "dust/dust_ceiling_ash_large",
                        "dust/ash_spiral_runner",
                        "dust/dust_wind_fast_paper",
                        "dust/dust_wind_slow_paper",
                        "misc/trash_spiral_runner",
                        "misc/leaves_spiral_runner",
                        "dust/dust_ceiling_ash_large_mp_vacant",
                        "dust/room_dust_200_blend_mp_vacant",
                        "dust/light_shaft_dust_large_mp_vacant", 
                        "dust/light_shaft_dust_large_mp_vacant_sidewall",  
                        "misc/falling_brick_runner",
                        "misc/falling_brick_runner_line_400",
                        "fire/firelp_med_pm_nodistort",
                        "fire/firelp_small_pm"
                    };
                    break;
                case "mp_exchange":
                    precached_fx = new string[]{
                        "misc/floating_room_dust",
                        "dust/falling_dirt_frequent_runner",
                        "dust/dust_wind_fast_paper",
                        "dust/dust_wind_slow_paper",
                        "misc/trash_spiral_runner",
                        "dust/room_dust_200_blend_mp_vacant",
                        "misc/insects_carcass_flies",
                        "explosions/spark_fall_runner_mp",
                        "weather/embers_prague_light",
                        "smoke/thin_black_smoke_s_fast",
                        "dust/falling_dirt_frequent_runner_exchange",
                        "smoke/building_hole_smoke_mp",
                        "fire/car_fire_mp",
                        "fire/firelp_small_cheap_mp",
                        "misc/falling_ash_mp",
                        "misc/insects_light_hunted_a_mp",
                        "misc/building_hole_paper_fall_mp",
                        "weather/ground_fog_mp",
                        "misc/oil_drip_puddle",
                        "misc/antiair_runner_cloudy",
                        "fire/fire_falling_runner_point",
                        "weather/ceiling_smoke_exchange",
                        "smoke/large_battle_smoke_mp",
                        "misc/light_glow_white",
                        "fire/building_hole_embers_mp",
                        "dust/room_dust_200_z150_mp",
                        "explosions/building_hole_elec_short_runner",
                        "smoke/thick_black_smoke_mp",
                        "fire/burned_vehicle_sparks",
                        "water/water_drips_fat_fast_singlestream"
                    };
                    break;
                case "mp_hardhat":
                    precached_fx = new string[]{
                        "explosions/electrical_transformer_spark_runner_loop",
                        "explosions/large_vehicle_explosion_ir",
                        "explosions/vehicle_explosion_btr80",
                        "explosions/generator_spark_runner_loop_interchange",
                        "explosions/spark_fall_runner_mp",
                        "dust/falling_dirt_light_1_runner_bravo",
                        "weather/ash_prague",
                        "weather/embers_prague_light",
                        "smoke/bg_smoke_plume_mp",
                        "smoke/white_battle_smoke",
                        "smoke/hallway_smoke_light",
                        "smoke/room_smoke_400",
                        "smoke/smoke_plume_grey_01",
                        "smoke/smoke_plume_grey_02",
                        "misc/light_glow_white_lamp",
                        "misc/falling_ash_mp",
                        "misc/trash_spiral_runner",
                        "misc/antiair_runner_cloudy",
                        "misc/insects_carcass_runner",
                        "misc/moth_runner",
                        "fire/firelp_small_cheap_mp",
                        "fire/car_fire_mp_far",
                        "dust/dust_cloud_mp_hardhat",
                        "dust/sand_spray_detail_oriented_runner_hardhat",
                        "dust/dust_spiral_runner_small",
                        "misc/jet_flyby_runner",
                        "explosions/building_missilehit_runner"
                    };
                    break;
                case "mp_interchange":
                    precached_fx = new string[]{
                        "dust/falling_dirt_frequent_runner",
                        "dust/dust_wind_fast_paper",
                        "dust/dust_wind_slow_paper",
                        "misc/trash_spiral_runner",
                        "dust/room_dust_200_blend_mp_vacant_light",
                        "misc/insects_carcass_flies",
                        "explosions/spark_fall_runner_mp",
                        "weather/embers_prague_light",
                        "fire/firelp_med_pm",
                        "fire/firelp_small",
                        "smoke/thin_black_smoke_s_fast",
                        "dust/falling_dirt_frequent_runner",
                        "smoke/steam_manhole",
                        "smoke/battlefield_smokebank_S_warm_dense",
                        "smoke/bg_smoke_plume",
                        "fire/firelp_med_pm_cheap",
                        "fire/car_fire_mp",
                        "fire/firelp_small_cheap_mp",
                        "misc/falling_ash_mp",
                        "misc/flocking_birds_mp",
                        "misc/insects_light_hunted_a_mp"
                    };
                    break;
                case "mp_lambeth":
                    precached_fx = new string[]{
                        "dust/falling_dirt_infrequent_runner",
                        "fire/car_fire_mp",
                        "fire/firelp_small_cheap_mp",
                        "fire/firelp_small_cheap_dist_mp",
                        "fire/firelp_med_cheap_dist_mp",
                        "fire/fire_falling_runner_point_infrequent_mp",     
                        "misc/trash_spiral_runner",
                        "misc/falling_brick_runner_line_100",
                        "misc/falling_brick_runner_line_200",
                        "misc/falling_brick_runner_line_300",
                        "misc/falling_brick_runner_line_400",
                        "misc/falling_brick_runner_line_600",
                        "misc/insects_carcass_runner",
                        "misc/insect_trail_runner_icbm",
                        "misc/insects_dragonfly_runner_a",
                        "misc/insects_light_complex",
                        "misc/leaves_fall_gentlewind_lambeth",
                        "misc/falling_ash_mp",
                        "misc/leaves_spiral_runner",
                        "smoke/battlefield_smokebank_s_cheap_mp_carbon",
                        "smoke/battlefield_smokebank_s_cheap_heavy_mp",
                        "smoke/hallway_smoke_light",
                        "smoke/room_smoke_200",
                        "smoke/room_smoke_400",
                        "smoke/steam_roof_ac",
                        "smoke/steam_jet_loop_cheap_mp_carbon",
                        "smoke/thin_black_smoke_m_mp",
                        "smoke/mist_drifting_groundfog_lambeth",
                        "smoke/mist_drifting_lambeth",
                        "water/water_drips_fat_fast_singlestream_mp_carbon",
                        "weather/ceiling_smoke"
                    };
                    break;
                case "mp_mogadishu":
                    precached_fx = new string[]{
                        "fire/firelp_med_pm_cheap",
                        "fire/firelp_small_pm_a_cheap",
                        "dust/dust_wind_fast_paper",
                        "dust/dust_wind_fast_no_paper",
                        "dust/dust_wind_slow_paper",
                        "misc/trash_spiral_runner",
                        "smoke/battlefield_smokebank_s_cheap",
                        "smoke/hallway_smoke_light",
                        "misc/falling_brick_runner_line_400",
                        "smoke/room_smoke_200",
                        "smoke/room_smoke_400",
                        "misc/insects_carcass_runner",
                        "explosions/electrical_transformer_spark_runner_loop",
                        "smoke/smoke_plume_grey_01",
                        "smoke/smoke_plume_grey_02",
                        "smoke/thick_black_smoke_mp",
                        "dust/falling_dirt_light_1_runner",
                        "dust/falling_dirt_light_2_runner"
                    };
                    break;
                case "mp_paris":
                    precached_fx = new string[]{
                        "dust/falling_dirt_light_1_runner_bravo",
                        "dust/falling_dirt_frequent_runner",
                        "dust/room_dust_200_z150_mp",
                        "weather/ash_prague",
                        "weather/embers_prague_light",
                        "weather/ceiling_smoke_seatown",
                        "smoke/chimney_smoke_mp",
                        "smoke/chimney_smoke_large_mp",
                        "misc/falling_ash_mp",
                        "misc/trash_spiral_runner",
                        "misc/leaves_fall_gentlewind_lambeth",
                        "misc/leaves_spiral_runner",
                        "misc/antiair_runner_cloudy",
                        "misc/insects_carcass_runner",
                        "misc/insects_light_hunted_a_mp",
                        "misc/insect_trail_runner_icbm",
                        "fire/firelp_med_pm",
                        "fire/firelp_small",
                        "fire/firelp_small_cheap_mp",
                        "fire/firelp_med_pm_cheap",
                        "fire/building_hole_embers_mp"
                    };
                    break;
                case "mp_plaza2":
                    precached_fx = new string[]{
                        "misc/floating_room_dust",
                        "dust/falling_dirt_frequent_runner",
                        "dust/dust_wind_slow_paper",
                        "misc/trash_spiral_runner",
                        "dust/room_dust_200_blend_mp_vacant",
                        "misc/insects_carcass_flies",
                        "explosions/spark_fall_runner_mp",
                        "weather/embers_prague_light",
                        "smoke/thin_black_smoke_s_fast",
                        "dust/falling_dirt_frequent_runner",
                        "smoke/building_hole_smoke_mp",
                        "fire/car_fire_mp",
                        "fire/firelp_small_cheap_mp",
                        "misc/falling_ash_mp",
                        "misc/insects_light_hunted_a_mp",
                        "smoke/bootleg_alley_steam",
                        "misc/building_hole_paper_fall_mp",
                        "weather/ground_fog_mp",
                        "misc/oil_drip_puddle",
                        "misc/antiair_runner_cloudy",
                        "misc/leaves_spiral_runner",
                        "fire/fire_falling_runner_point",
                        "weather/ceiling_smoke_seatown",
                        "smoke/large_battle_smoke_mp",
                        "misc/light_glow_white",
                        "fire/building_hole_embers_mp",
                        "dust/room_dust_200_z150_mp",
                        "explosions/building_hole_elec_short_runner",
                        "smoke/thick_black_smoke_mp",
                        "misc/leaves_fall_gentlewind_green",
                        "smoke/smoke_plume_grey_02",
                        "fire/burned_vehicle_sparks"
                    };
                    break;
                case "mp_radar":
                    precached_fx = new string[]{
                        "snow/tree_snow_dump_fast",
                        "snow/tree_snow_dump_fast_small",
                        "snow/tree_snow_fallen_heavy",
                        "snow/tree_snow_fallen",
                        "snow/tree_snow_fallen_small",
                        "snow/tree_snow_dump_runner",
                        "snow/snow_spray_detail_contingency_runner_0x400",
                        "snow/snow_spray_detail_oriented_runner_0x400",
                        "snow/snow_spray_detail_oriented_runner_400x400",
                        "snow/snow_spray_detail_oriented_runner",
                        "snow/snow_spray_detail_oriented_large_runner",
                        "snow/snow_spray_large_oriented_radar_od_runner",
                        "snow/snow_spray_large_oriented_runner",
                        "snow/snow_vortex_runner_cheap",
                        "smoke/room_smoke_200",
                        "snow/snow_spiral_runner",
                        "snow/snow_blowoff_ledge_runner",
                        "snow/snow_clifftop_runner",
                        "snow/radar_windy_snow",
                        "snow/blowing_ground_snow",
                        "snow/tree_snow_dump_radar_runner",
                        "water/water_drips_fat_slow_speed",
                        "snow/snow_blizzard_radar",
                        "misc/light_glow_white_lamp",
                        "snow/snow_gust_runner_radar",
                        "fire/heat_lamp_distortion",
                        "fire/car_fire_mp",
                        "fire/firelp_cheap_mp",
                        "snow/radar_windy_snow_no_mist",
                        "snow/radar_windy_snow_small_area",
                        "misc/moth_runner"
                    };
                    break;
                case "mp_seatown":
                    precached_fx = new string[]{
                        "dust/sand_spray_detail_oriented_runner_mp_dome",
                        "dust/room_dust_100_blend",
                        "smoke/battlefield_smokebank_S_warm",
                        "dust/dust_wind_fast_paper",
                        "misc/trash_spiral_runner",
                        "dust/room_dust_200_blend_mp_seatown",
                        "weather/ceiling_smoke_seatown",
                        "misc/insects_carcass_flies",
                        "explosions/spark_fall_runner_mp",
                        "water/seatown_lookout_splash_runner",
                        "water/seatown_pillar_mist",
                        "misc/palm_leaves",
                        "dust/falling_dirt_frequent_runner",
                        "misc/flocking_birds_mp",
                        "misc/insects_light_hunted_a_mp",
                        "fire/firelp_small_cheap_mp",
                        "fire/car_fire_mp",
                        "smoke/bg_smoke_plume",
                        "dust/room_dust_200_blend_seatown_wind_fast" 
                    };
                    break;
                case "mp_underground":
                    precached_fx = new string[]{
                        "dust/falling_dirt_frequent_runner",
                        "dust/dust_wind_fast_paper",
                        "dust/dust_wind_slow_paper",
                        "misc/trash_spiral_runner",
                        "dust/room_dust_200_blend_mp_vacant",
                        "misc/insects_carcass_flies",
                        "explosions/spark_fall_runner_mp",
                        "weather/embers_prague_light",
                        "smoke/thin_black_smoke_s_fast",
                        "dust/falling_dirt_frequent_runner",
                        "smoke/steam_manhole",
                        "smoke/battlefield_smokebank_S_warm_dense",
                        "smoke/building_hole_smoke_mp",
                        "fire/car_fire_mp",
                        "fire/firelp_small_cheap_mp",
                        "misc/falling_ash_mp",
                        "misc/insects_light_hunted_a_mp",
                        "smoke/bootleg_alley_steam",
                        "misc/building_hole_paper_fall_mp",
                        "weather/ground_fog_mp",
                        "misc/leaves_fall_gentlewind_green",
                        "smoke/chimney_smoke_mp",
                        "misc/oil_drip_puddle",
                        "misc/antiair_runner_cloudy",
                        "misc/leaves_spiral_runner",
                        "fire/fire_falling_runner_point",
                        "weather/ceiling_smoke_seatown",
                        "smoke/large_battle_smoke_mp",
                        "misc/light_glow_white",
                        "dust/room_dust_200_z150_mp",
                        "smoke/chimney_smoke_large_mp",
                        "fire/building_hole_embers_mp"
                    };
                    break;
                case "mp_village":
                    precached_fx = new string[]{
                        "distortion/heat_haze_mirage",
                        "dust/falling_dirt_area_runner",
                        "dust/sand_spray_detail_oriented_runner_hardhat",
                        "dust/sand_spray_cliff_oriented_runner_hardhat",
                        "dust/dust_spiral_runner_small",
                        "fire/car_fire_mp",
                        "fire/car_fire_mp_far",
                        "misc/trash_spiral_runner",
                        "misc/birds_takeoff_infrequent_runner",
                        "misc/leaves_fall_gentlewind_mp_village",
                        "misc/leaves_fall_gentlewind_mp_village_far",
                        "misc/insects_carcass_runner",
                        "misc/insects_light_hunted_a_mp",
                        "misc/insect_trail_runner_icbm",
                        "misc/insects_dragonfly_runner_a",
                        "smoke/hallway_smoke_light",
                        "smoke/room_smoke_400",
                        "water/waterfall_mist_mp_village",
                        "water/waterfall_mist_ground",
                        "water/waterfall_village_1",
                        "water/waterfall_village_2",
                        "water/waterfall_drainage_splash",
                        "water/waterfall_drainage_splash_mp",
                        "water/waterfall_drainage_splash_large"
                    };
                    break;
                case "mp_terminal_cls":
                    precached_fx = new string[]{
                        "smoke/ground_smoke1200x1200",
                        "smoke/hallway_smoke_light",
                        "smoke/room_smoke_200",
                        "smoke/room_smoke_400",
                        "dust/light_shaft_motes_airport",
                        "misc/moth_runner"
                    };
                    break;
                case "mp_aground_ss":
                    precached_fx = new string[]{
                        "misc/birds_circle_main",
                        "water/water_wave_splash2_runner",
                        "water/water_shore_splash_xlg_r",
                        "water/water_wave_splash_xsm_runner",
                        "water/water_shore_splash_r",
                        "water/mist_light",
                        "weather/fog_aground",
                        "misc/drips_slow",
                        "maps/mp_crosswalk_ss/mp_cw_insects",
                        "weather/fog_bog_c",
                        "dust/falling_dirt_infrequent_runner_mp",
                        "lights/godrays_aground",
                        "lights/godrays_aground_b",
                        "lights/tinhat_beam",
                        "lights/bulb_single_orange",
                        "maps/mp_overwatch/light_dust_motes_fog",
                        "misc/paper_blowing_Trash_r",
                        "animals/penguin"
                    };
                    break;
                case "mp_courtyard_ss":
                    precached_fx = new string[]{
                        "maps/mp_courtyard_ss/mp_ct_volcano",
                        "maps/mp_courtyard_ss/mp_ct_godrays_a",
                        "maps/mp_courtyard_ss/mp_ct_godrays_b",
                        "maps/mp_courtyard_ss/mp_ct_godrays_c",
                        "maps/mp_courtyard_ss/mp_ct_ash",
                        "maps/mp_courtyard_ss/mp_ct_ambdust",
                        "maps/mp_courtyard_ss/mp_ct_insects",
                        "misc/insects_dragonfly_runner_a"
                    };
                    break;
            }
            return precached_fx.Contains(fx);
        }

        public string UTILS_GetDefCDvar(string key)
        {
            return GSCFunctions.GetDvar(key);
        }

        public void UTILS_SetChatAlias(Entity sender, string player, string alias)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
            {
                WriteChatToPlayer(sender, Command.GetMessage("NotOnePlayerFound"));
                return;
            }

            bool hasAlias = ChatAlias.Keys.Contains(target.GUID);

            if (string.IsNullOrEmpty(alias))
            {
                if (hasAlias)
                    ChatAlias.Remove(target.GUID);
                WriteChatToAll(Command.GetString("alias", "reset").Format(new Dictionary<string, string>()
                {
                    {"<player>", target.Name }
                }));
            }
            else
            {
                if (hasAlias)
                    ChatAlias[target.GUID] = alias;
                else
                    ChatAlias.Add(target.GUID, alias);
                WriteChatToAll(Command.GetString("alias", "message").Format(new Dictionary<string, string>()
                {
                    {"<player>", target.Name },
                    {"<alias>", alias}
                }));
            }
            //save settings
            List<string> aliases = new List<string>();
            foreach (KeyValuePair<long, string> entry in ChatAlias)
                aliases.Add(entry.Key.ToString() + "=" + entry.Value);
            File.WriteAllLines(ConfigValues.ConfigPath + @"Utils\chatalias.txt", aliases.ToArray());
        }

        public void UTILS_GetTeamPlayers(out int axis, out int allies)
        {
            axis = 0;
            allies = 0;
            foreach (Entity player in Players)
            {
                string team = player.GetTeam();
                switch (team)
                {
                    case "axis":
                        axis++;
                        break;
                    case "allies":
                        allies++;
                        break;
                }
            }
        }

        public T UTILS_GetFieldSafe<T>(Entity player, string field)
        {
            if (player.HasField(field))
                return player.GetField<T>(field);
            return default;
        }

        public string UTILS_ResolveGUID(long GUID)
        {
            if (File.Exists(string.Format(ConfigValues.ConfigPath + @"Utils\playerlogs\{0}.txt", GUID)))
                return File.ReadAllLines(string.Format(ConfigValues.ConfigPath + @"Utils\playerlogs\{0}.txt", GUID)).Last();
            return "unknown";
        }

        public SerializableDictionary<long, List<Dvar>> UTILS_PersonalPlayerDvars_load()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(SerializableDictionary<long, List<Dvar>>));
            using (FileStream fs = new FileStream(ConfigValues.ConfigPath + @"Utils\internal\PersonalPlayerDvars.xml", FileMode.Open))
                return (SerializableDictionary<long, List<Dvar>>)xmlSerializer.Deserialize(fs);
        }

        public void UTILS_PersonalPlayerDvars_save(SerializableDictionary<long, List<Dvar>> db)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(SerializableDictionary<long, List<Dvar>>));
            using (FileStream fs = new FileStream(ConfigValues.ConfigPath + @"Utils\internal\PersonalPlayerDvars.xml", FileMode.Create))
                xmlSerializer.Serialize(fs, db);
        }

        public void UTILS_ServerTitle_MapFormat()
        {
            string mapname = DevMapName2Mapname(GSCFunctions.GetDvar("mapname"));

            // ToTitleCase
            if (!string.IsNullOrEmpty(mapname))
            {
                char[] ca = mapname.ToCharArray();
                ca[0] = char.ToUpperInvariant(ca[0]);
                mapname = new string(ca);
            }

            UTILS_ServerTitle(ConfigValues.Servertitle_map.Format(new Dictionary<string, string>()
                {
                    {"<map>", mapname }
                }), ConfigValues.Servertitle_mode);
        }

        public List<Dvar> UTILS_DvarListUnion(List<Dvar> set1, List<Dvar> set2)
        {
            Dictionary<string, string> _dvars = set1.ToDictionary(x => x.key.ToLowerInvariant(), x => x.value);
            foreach (Dvar dvar in set2)
            {
                string key = dvar.key.ToLowerInvariant();
                if (_dvars.ContainsKey(key))
                    _dvars[key] = dvar.value;
                else
                    _dvars.Add(key, dvar.value);
            }
            set1.Clear();
            foreach (KeyValuePair<string, string> dvar in _dvars)
                set1.Add(new Dvar { key = dvar.Key, value = dvar.Value });
            return set1;
        }

        public List<Dvar> UTILS_DvarListRelativeComplement(List<Dvar> set1, List<string> set2)
        {
            Dictionary<string, string> _dvars = set1.ToDictionary(x => x.key.ToLowerInvariant(), x => x.value);
            foreach (string dvar in set2)
                if (_dvars.ContainsKey(dvar.ToLowerInvariant()))
                    _dvars.Remove(dvar.ToLowerInvariant());
            set1.Clear();
            foreach (KeyValuePair<string, string> dvar in _dvars)
                set1.Add(new Dvar { key = dvar.Key, value = dvar.Value });
            return set1;
        }

    }
}
