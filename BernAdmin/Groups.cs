using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using InfinityScript;

namespace LambAdmin
{
    public partial class DHAdmin
    {

        private static volatile GroupsDatabase database;

        /// <summary>
        /// Singleton class that contains all permissions.
        /// </summary>
        public class GroupsDatabase
        {
            public volatile List<PlayerInfo> ImmunePlayers = new List<PlayerInfo>();
            public volatile List<Group> Groups = new List<Group>();
            public volatile Dictionary<PlayerInfo, string> Players = new Dictionary<PlayerInfo, string>();

            /// <summary>
            /// A player group that can hold permissions and may require login.
            /// </summary>
            public class Group
            {
                public List<string> permissions = new List<string>();
                public string group_name;
                public string login_password;
                public string short_name;

                /// <summary>
                /// Create a group from a name, password (can be empty for no password), permission strings and optionally a short name.
                /// </summary>
                public Group(string name, string password, List<string> perms, string sh_name = "")
                {
                    group_name = name.ToLowerInvariant();
                    login_password = password;
                    short_name = sh_name;
                    permissions = perms;
                }

                /// <returns>Whether the group is allowed the specified permission.</returns>
                public bool CanDo(string permission)
                {
                    
                    if (permissions.Contains("-" + permission))
                        return false;
                    if (ConfigValues.Settings_disabled_commands.Contains(permission))
                        return false;
                    if (permissions.Contains(permission))
                        return true;
                    if (permissions.Contains("-*abusive*") && AbusiveCommandList.Any(c => c.name == permission))
                        return false;
                    if (permissions.Contains("-*unsafe*") && UnsafeCommandList.Any(c => c.name == permission))
                        return false;
                    return permissions.Contains("*all*");
                }
            }

            /// <summary>
            /// Build a GroupsDatabase out of Groups\groups.txt, Groups\players.txt and Groups\immuneplayers.txt
            /// </summary>
            public GroupsDatabase()
            {
                WriteLog.Info("Reading groups...");
                if (!File.Exists(ConfigValues.ConfigPath + @"Groups\groups.txt"))
                {
                    WriteLog.Warning("Groups file not found, creating new one...");
                    File.WriteAllLines(ConfigValues.ConfigPath + @"Groups\groups.txt", new string[]
                    {
                        "default::"
                                + "pm,admins,guid,version,rules,afk,credits,hidebombicon,help,rage,maps,time,"
                                + "amsg,ft,hwid,apply,night,ga,report,suicide,yes,no,register,xlrstats,xlrtop,votekick,drunk,fx",
                        "moderator::"
                                + "login,warn,unwarn,kick,mode,map,setafk,kick,tmpban,changeteam,lastreports,dsrnames,votecancel,"
                                + "@admins,@rules,@apply,@time,@xlrstats,@xlrtop"
                                    + ":^0[^5Moderator^0]^7",
                        "family::"
                                + "kickhacker,kill,mute,unmute,end,tmpbantime,cdvar,getplayerinfo,say,sayto,resetwarns,setgroup,"
                                + "scream,whois,changeteam,yell,gametype,mode,login,map,status,kick,tmpban,ban,warn,unwarn,getwarns,"
                                + "res,setafk,setteam,balance,clanvsall,clanvsallspectate,sunlight,alias,lastreports,fire,"
                                + "dsrnames,votecancel,@admins,@rules,@apply,@time,@xlrstats,@xlrtop"
                                    + ":^0[^3F^0]^7",
                        "elder:"
                            + "password:"
                                + "-*unsafe*,*all*"
                                    + ":^0[^4Elder^0]^7",
                        "developer:"
                            + "password:"
                                + "*all*"
                                    + ":^0[^6neko neko ^1=^_^=^0]^5",
                        "owner:"
                            + "password:"
                                + "*all*"
                                    + ":^0[^1O^2w^3n^4e^5r^0]^3",
                        "admin::"
                                + "scream,whois,changeteam,yell,gametype,mode,login,map,status,unban,unban-id,kick,tmpban,ban,warn,"
                                + "unwarn,getwarns,res,setafk,setteam,balance,clanvsall,clanvsallspectate,login,lastreports,"
                                + "dsrnames,votecancel,@admins,@rules,@apply,@time,@xlrstats,@xlrtop"
                                    + ":^0[^1Admin^0]^7",
                        "leader:"
                            + "password:"
                                + "*all*"
                                    + ":^0[^1L^2e^3a^4d^5e^7r^0]^2",
                        "trial::"
                                + "login,warn,unwarn,kick"
                                    + ":^0[^5Trial^0]^7",
                        "member::"
                                + "login,warn,unwarn,kick,mode,map,setafk,kick,tmpban,lastreports,dsrnames,votecancel,"
                                + "@admins,@rules,@apply,@time,@xlrstats,@xlrtop"
                                    + ":^0[^5Member^0]^7",
                        "friend::"
                                + "login,warn,unwarn,kick,mode,map,setafk,kick,tmpban,map,mode,tmpban,lastreports,"
                                + "dsrnames,votecancel,@admins,@rules,@apply,@time,@xlrstats,@xlrtop"
                                    + ":^0[^6Friend^0]^7",
                        "vip::"
                                + "ban,kick,tmpban,warn,unwarn,map,balance,mode,whois,status,login,setafk,changeteam,scream,fakesay,"
                                + "myalias,fire,dsrnames,votecancel,@admins,@rules,@apply,@time,@xlrstats,@xlrtop"
                                    + ":^0[^3V.I.P.^0]^7",
                        "founder:"
                            + "password:"
                                + "*all*"
                                    + ":^0[^1F^2o^3u^4n^5d^6e^8r^0]^6",
                        "donator::"
                                + "kick,warn,tmpban,unwarn,mute,unmute,login,balance,setafk,changeteam,myalias,lastreports,fire,"
                                + "votecancel,@admins,@rules,@apply,@time,@xlrstats,@xlrtop"
                                    + ":^0[^2Donator^0]^7",
                        "coleader:"
                            + "password:"
                                + "-*abusive*,-*unsafe*,*all*"
                                    + ":^0[^3CoLeader^0]^7",
                        "banned::"
                                + "drunk,help"
                                    + ":^0[^1BANNED^0]^5"
                    });
                }

                try
                {
                    foreach (string line in File.ReadAllLines(ConfigValues.ConfigPath + @"Groups\groups.txt"))
                    {
                        string[] parts = line.Split(':');
                        string[] permissions = parts[2].ToLowerInvariant().Split(',');
                        if (parts.Length == 3)
                            Groups.Add(new Group(parts[0].ToLowerInvariant(), parts[1], permissions.ToList()));
                        else if (parts.Length == 4)
                            Groups.Add(new Group(parts[0].ToLowerInvariant(), parts[1], permissions.ToList(), parts[3]));
                        else
                            Groups.Add(new Group(parts[0].ToLowerInvariant(), parts[1], permissions.ToList(), string.Join(":", parts.Skip(3).ToList())));
                    }
                }
                catch (Exception ex)
                {
                    WriteLog.Error("Could not set up groups.");
                    WriteLog.Error(ex.Message);
                }

                try
                {
                    foreach (string line in File.ReadAllLines(ConfigValues.ConfigPath + @"Groups\players.txt"))
                    {
                        string[] parts = line.ToLowerInvariant().Split(':');
                        Players.Add(PlayerInfo.Parse(parts[0]), parts[1].ToLowerInvariant());
                    }
                }
                catch (Exception ex)
                {
                    WriteLog.Error("Could not set up playergroups.");
                    WriteLog.Error(ex.Message);
                }

                try
                {
                    foreach (string line in File.ReadAllLines(ConfigValues.ConfigPath + @"Groups\immuneplayers.txt"))
                        ImmunePlayers.Add(PlayerInfo.Parse(line));
                }
                catch (Exception ex)
                {
                    WriteLog.Error("Could not set up immuneplayers");
                    WriteLog.Error(ex.Message);
                }
                WriteLog.Info("Groups successfully set up.");
            }

            /// <summary>
            /// Save the changed permissions to the permissions files: Groups\groups.txt, Groups\players.txt and Groups\immuneplayers.txt
            /// </summary>
            public void SaveGroups()
            {
                using (StreamWriter groupsfile = new StreamWriter(ConfigValues.ConfigPath + @"Groups\groups.txt"))
                {
                    foreach (Group group in Groups)
                    {
                        if (group.short_name == "")
                            groupsfile.WriteLine(string.Join(":", group.group_name, group.login_password, string.Join(",", group.permissions.ToArray())));
                        else
                            groupsfile.WriteLine(string.Join(":", new string[] { group.group_name, group.login_password, string.Join(",", group.permissions.ToArray()), group.short_name }));
                    }
                }
                using (StreamWriter playersfile = new StreamWriter(ConfigValues.ConfigPath + "Groups\\players.txt"))
                {
                    foreach (KeyValuePair<DHAdmin.PlayerInfo, string> keyValuePair in Players)
                        playersfile.WriteLine(keyValuePair.Key.GetIdentifiers() + ":" + keyValuePair.Value);
                }
                using (StreamWriter immuneplayersfile = new StreamWriter(ConfigValues.ConfigPath + "Groups\\immuneplayers.txt"))
                {
                    foreach (PlayerInfo playerInfo in ImmunePlayers)
                        immuneplayersfile.WriteLine(playerInfo.GetIdentifiers());
                }
            }

            /// <returns>The group with the specified name. null if the group does not exist.</returns>
            public Group GetGroup(string name) => Groups.Find(g => g.group_name == name.ToLowerInvariant());

            /// <returns>The PlayerInfo entry with the same IP, GUID and HWID. null if the entry does not exist.</returns>
            public KeyValuePair<PlayerInfo, string>? FindEntryFromPlayersAND(PlayerInfo playerinfo) => Players.Find(p => playerinfo.MatchesAND(p.Key));

            /// <returns>The PlayerInfo entry with the same IP, GUID or HWID. null if the entry does not exist.</returns>
            public KeyValuePair<PlayerInfo, string>? FindEntryFromPlayersOR(PlayerInfo playerinfo) => Players.Find(p => playerinfo.MatchesOR(p.Key));

            /// <returns>The immune player with the same IP, GUID and HWID. null if the immune player does not exist.</returns>
            public PlayerInfo FindMatchingPlayerFromImmunes(PlayerInfo playerinfo) => ImmunePlayers.Find(playerinfo.MatchesAND);

            /// <returns>Whether the player has the specified permission.</returns>
            public bool GetEntityPermission(Entity player, string permission_string)
            {
                WriteLog.Debug("Getting Entity permission for " + player.Name.ToString() + " permission string = " + permission_string);

                Group group = player.GetGroup(this);
                WriteLog.Debug("playergroup acquired");

                if (ConfigValues.Settings_disabled_commands.Contains(permission_string))
                    return false;

                //right groups for right ppl
                if (group.group_name != "sucker" && group.group_name != "banned" && GetGroup("default").permissions.Contains(permission_string))
                {
                    WriteLog.Debug("Default contained...");
                    return true;
                }

                if (!player.IsLogged() && !string.IsNullOrWhiteSpace(group.login_password))
                {
                    WriteLog.Debug("Player not logged");
                    return false;
                }

                return group.CanDo(permission_string);
            }

            /// <returns>The formatted string listing the admins.</returns>
            public string[] GetAdminsString(List<Entity> Players)
            {
                return (from player in Players
                        let grp = player.GetGroup(this)
                        where !string.IsNullOrWhiteSpace(grp.short_name) && grp.group_name != "sucker" && grp.group_name != "banned"
                        select Command.GetString("admins", "formatting").Format(new Dictionary<string, string>()
                        {
                            {"<name>", player.Name },
                            {"<formattedname>", player.GetFormattedName(this) },
                            {"<rankname>", grp.group_name },
                            {"<shortrank>", grp.short_name },
                        })).ToArray();
            }
        }

        /// <summary>
        /// When a player disconnects, log the player out
        /// </summary>
        public void groups_OnDisconnect(Entity player) => player.SetLogged(false);

        /// <summary>
        /// Initial setup for groups on server start.
        /// </summary>
        public void groups_OnServerStart()
        {
            PlayerDisconnected += groups_OnDisconnect;
            database = new GroupsDatabase();
        }
    }
}
