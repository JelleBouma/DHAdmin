using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using InfinityScript;
using System.IO;
using System.Reflection;

namespace LambAdmin
{
    public partial class DHAdmin
    {

        public volatile List<Command> CommandList;

        public volatile List<string> BanList = new List<string>();

        public volatile List<string> XBanList = new List<string>();

        public volatile List<long> LockServer_Whitelist = new List<long>();

        public volatile Dictionary<string, string> CommandAliases = new Dictionary<string, string>();

        public volatile SerializableDictionary<long, List<Dvar>> PersonalPlayerDvars = new SerializableDictionary<long, List<Dvar>>();

        public volatile Voting voting = new Voting();

        public class Command
        {
            [Flags] public enum Behaviour
            {
                Function = 1,
                HasOptionalArguments = 2,
                OptionalIsRequired = 4,
                MustBeConfirmed = 8,
                ArrayOutput = 16,
                IsAbusive = 32,
                IsUnsafe = 64
            }

            private readonly Action<Entity, string[], string> action;
            private readonly Func<Entity, string[], string, string> func;
            private readonly Func<Entity, string[], string, string[]> arrayFunc;
            private readonly Action<Entity, string[], bool> arrayWriter;
            private readonly int parametercount;
            public string name;
            private readonly Behaviour behaviour;

            private Command(string commandname, int paramcount)
            {
                name = commandname;
                parametercount = paramcount;
            }

            public Command(string commandname, Action<Entity, string[], string> actionToBeDone, int paramcount = 0, Behaviour commandBehaviour = 0) : this(commandname, paramcount)
            {
                action = actionToBeDone;
                behaviour = commandBehaviour;
            }

            public Command(string commandname, Func<Entity, string[], string, string> funcToBeDone, int paramcount = 0, Behaviour commandBehaviour = 0) : this(commandname, paramcount)
            {
                func = funcToBeDone;
                behaviour = commandBehaviour | Behaviour.Function;
            }

            public Command(string commandname, Func<Entity, string[], string, string[]> funcToBeDone, Action<Entity, string[], bool> arrayWriter, int paramcount = 0, Behaviour commandBehaviour = 0) : this(commandname, paramcount)
            {
                arrayFunc = funcToBeDone;
                this.arrayWriter = arrayWriter;
                behaviour = commandBehaviour | Behaviour.Function | Behaviour.ArrayOutput;
            }

            public void Run(Entity sender, string message, bool broadcast)
            {
                if (!CMDS_ParseCommand(message, parametercount, out string[] args, out string optargs) || (!behaviour.HasFlag(Behaviour.HasOptionalArguments) && !string.IsNullOrWhiteSpace(optargs)) || (behaviour.HasFlag(Behaviour.OptionalIsRequired) && string.IsNullOrWhiteSpace(optargs)))
                    WriteErrorTo(sender, name, "usage");
                else if (behaviour.HasFlag(Behaviour.MustBeConfirmed))
                    if (sender.GetField<string>("CurrentCommand") != message)
                    {
                        WriteChatToPlayerMultiline(sender, new string[] {
                            "^5-> You're trying ^1UNSAFE ^5command",
                            "^5-> ^3" + message,
                            "^5-> Confirm (^1!yes ^5/ ^2!no^5)"
                        }, 50);
                        sender.SetField("CurrentCommand", message);
                    }
                    else
                        sender.SetField("CurrentCommand", "");
                else
                    try
                    {
                        if (behaviour.HasFlag(Behaviour.Function))
                            if (behaviour.HasFlag(Behaviour.ArrayOutput))
                                arrayWriter(sender, arrayFunc(sender, args, optargs), broadcast);
                            else if (broadcast)
                                WriteChatToAll(func(sender, args, optargs));
                            else
                                WriteChatToPlayer(sender, func(sender, args, optargs));
                        else
                            action(sender, args, optargs);
                    }
                    catch (Exception ex)
                    {
                        WriteChatToPlayer(sender, GetMessage("DefaultError"));
                        MainLog.WriteError(ex.Message);
                        MainLog.WriteError(ex.StackTrace);
                        WriteLog.Error(ex.Message);
                        WriteLog.Error(ex.StackTrace);
                    }
            }

            public static string GetString(string name, string key)
            {
                return CmdLang_GetString(string.Join("_", "command", name, key));
            }

            public static string GetMessage(string name)
            {
                return CmdLang_GetString(string.Join("_", "Message", name));
            }

            /// <summary> Write an error message to the player using the command lang. </summary>
            /// <returns>an empty string</returns>
            public static string WriteErrorTo(Entity player, string name, string key = "")
            {
                WriteChatToPlayer(player, key == "" ? GetMessage(name) : GetString(name, key));
                return "";
            }
        }

        public class BanEntry
        {
            public int BanId
            {
                get; private set;
            }
            public PlayerInfo playerinfo
            {
                get; private set;
            }
            public string Playername
            {
                get; private set;
            }
            public DateTime Until
            {
                get; private set;
            }

            public BanEntry(int pbanid, PlayerInfo pplayerinfo, string pplayername, DateTime puntil)
            {
                BanId = pbanid;
                playerinfo = pplayerinfo;
                Playername = pplayername;
                Until = puntil;
            }
        }

        public class Voting
        {
            int MaxTime;
            float Threshold;
            int PosVotes, NegVotes;
            private bool Active;
            public Entity issuer, target;
            string reason;
            List<long> VotedPlayers;
            public string hudText = "";
            HudElem VoteStatsHUD;

            public bool isActive() {
                return Active;
            }
            public void Start(Entity issuer, Entity target, string reason)
            {
                if (Active)
                    return;
                Active = true;
                WriteLog.Info("Voting started");
                this.issuer = issuer; //cmd owner
                this.target = target; // player to be kicked
                this.reason = reason;
                MaxTime = ConfigValues.Commands_vote_time;
                Threshold = ConfigValues.Commands_vote_threshold;
                PosVotes = 0;
                NegVotes = 0;
                VotedPlayers = new List<long>();
                int time = MaxTime;

                VoteStatsHUD = HudElem.CreateServerFontString(HudElem.Fonts.HudSmall, 0.7f);
                VoteStatsHUD.SetPoint("TOPLEFT", "TOPLEFT", 10, 290);
                VoteStatsHUD.Foreground = true;
                VoteStatsHUD.HideWhenInMenu = true;
                VoteStatsHUD.Archived = false;

                OnInterval(1000, () =>
                {
                    try
                    {
                        if ((target == null) || (issuer == null))
                        {
                            VoteStatsHUD.Destroy();
                            return false;
                        }
                        if (!Active)
                        {
                            VoteStatsHUD.Destroy();
                            return false;
                        }
                        if (time <= 0)
                        {
                            VoteStatsHUD.Destroy();
                            End();
                            return false;
                        }

                        time--;
                        UpdateHUD(time);
                        VoteStatsHUD.SetText(string.IsNullOrEmpty(hudText) ? " " : hudText);
                        if (time % 5 == 0)
                            WriteLog.Info("Votekick: " + time.ToString() + "s remain");
                    }
                    catch
                    {
                        WriteLog.Error("Exception at DHAdmin.Voting::TimerEvent");
                    }
                    return true;
                });
            }
            private void End()
            {
                if (!Active)
                    return;
                Active = false;
                if (PosVotes - NegVotes >= Threshold)
                {
                    if (target.IsImmune(database))
                        WriteChatToAll(Command.GetMessage("TargetIsImmune"));
                    else
                    {
                        WriteChatToAll(Command.GetString("kick", "message").Format(new Dictionary<string, string>()
                        {
                            {"<target>", target.Name },
                            {"<issuer>", issuer.Name },
                            {"<reason>", reason },
                        }));
                        AfterDelay(100, () =>
                        {
                            ExecuteCommand("dropclient " + target.GetEntityNumber() + " \"" + reason + "\"");
                            WriteLog.Info("Voting passed successfully.");
                        });
                    }
                }
                else
                {
                    WriteChatToAll(Command.GetString("votekick", "error2"));
                    WriteLog.Info("Voting failed.");
                }
            }
            public void Abort() {
                Active = false;
                WriteLog.Info("Voting aborted.");
            }
            public void OnPlayerDisconnect(Entity player)
            {
                if (player == target)
                {
                    WriteChatToAll(Command.GetString("votekick", "error1"));
                    Abort();
                }
                else if (player == issuer)
                {
                    WriteChatToAll(Command.GetString("votekick", "error3"));
                    Abort();
                }
            }
            private void UpdateHUD(int time)
            {
                List<string> lines = Command.GetString("votekick", "HUD").Format(
                        new Dictionary<string, string>(){
                            {"<player>", string.IsNullOrEmpty(target.Name)?" ": target.Name},
                            {"<reason>", string.IsNullOrEmpty(reason)?"nothing":reason},
                            {"<time>", (time + 1).ToString()},
                            {"<posvotes>", PosVotes.ToString()},
                            {"<negvotes>", NegVotes.ToString()},
                    }).Split(new string[] { @"\n" }, StringSplitOptions.None).ToList();
                int maxlen = 0;
                lines.ForEach((s) => { maxlen = Math.Max(maxlen, s.Length); });
                for (int i = 0; i < lines.Count; i++)
                    lines[i] = "^3| " + lines[i];
                lines.Insert(0, "^3" + new string('-', maxlen));
                lines.Add("^3" + new string('-', maxlen));
                hudText = "";
                lines.ForEach((s) => { hudText += s + "\n"; });
            }

            public bool PositiveVote(Entity player)
            {
                if ((player == issuer) || (player == target))
                    return false;
                if (VotedPlayers.Contains(player.GUID))
                    return false; //whatever the vote is accepted, or not
                VotedPlayers.Add(player.GUID);
                PosVotes++;
                return true;
            }
            public bool NegativeVote(Entity player)
            {
                if ((player == issuer) || (player == target))
                    return false;
                if (VotedPlayers.Contains(player.GUID))
                    return false;
                VotedPlayers.Add(player.GUID);
                NegVotes++;
                return true;
            }
        }

        public void ProcessCommand(Entity sender, Entity target, string message)
        {
            string commandName = message.Substring(1).Split()[0].ToLowerInvariant();
            bool broadcast = commandName.StartsWith("@");
            commandName = broadcast ? commandName.Substring(1) : commandName;
            WriteLog.Debug(sender.Name + " attempted " + commandName);
            foreach (Entity player in Players)
                try
                {
                    if (player.IsSpying())
                        if (commandName == "login")
                            WriteChatSpyToPlayer(player, sender.Name + ": ^6" + "!login ****");
                        else
                            WriteChatSpyToPlayer(player, sender.Name + ": ^6" + message);
                }
                catch (Exception ex)
                {
                    HaxLog.WriteInfo(string.Join(" ", "----STARTREPORT", ex.Message));
                    try
                    {
                        HaxLog.WriteInfo("BAD PLAYER:");
                        HaxLog.WriteInfo(ex.StackTrace);
                        HaxLog.WriteInfo(player.Name);
                        HaxLog.WriteInfo(player.GUID.ToString());
                        HaxLog.WriteInfo(player.IP.Address.ToString());
                        HaxLog.WriteInfo(player.GetEntityNumber().ToString());
                    }
                    finally
                    {
                        HaxLog.WriteInfo("----ENDREPORT");
                    }
                }
            if (CommandAliases.TryGetValue(commandName, out string newcommandname))
                commandName = newcommandname;
            Command CommandToBeRun = FindCommand(commandName);
            if (CommandToBeRun == null)
                Command.WriteErrorTo(sender, "CommandNotFound");
            else
            {
                GroupsDatabase.Group playergroup = sender.GetGroup(database);
                GroupsDatabase.Group targetgroup = sender == target ? playergroup : target.GetGroup(database);
                if ((commandName == "login" && targetgroup.CanDo("login")) || sender.HasPermission(commandName, database))
                    CommandToBeRun.Run(target, message, broadcast);
                else if (playergroup.CanDo(commandName))
                    Command.WriteErrorTo(sender, "NotLoggedIn");
                else if (ConfigValues.Settings_disabled_commands.Contains(commandName))
                    Command.WriteErrorTo(sender, "CmdDisabled");
                else
                    Command.WriteErrorTo(sender, "NoPermission");
            }
        }

        public static bool CMDS_ParseCommand(string CommandToBeParsed, int ArgumentAmount, out string[] arguments, out string optionalarguments)
        {
            IEnumerable<string> allArguments = CommandToBeParsed.Trim().Split().Skip(1);
            arguments = allArguments.Take(ArgumentAmount).ToArray();
            optionalarguments = string.Join(" ", allArguments.Skip(ArgumentAmount));
            return arguments.Length == ArgumentAmount;
        }

        public void CMDS_OnServerStart()
        {

            PlayerConnected += CMDS_OnConnect;
            PlayerConnecting += CMDS_OnConnecting;
            PlayerDisconnected += CMDS_OnDisconnect;
            PlayerActuallySpawned += CMDS_OnPlayerSpawned;
            OnPlayerKilledEvent += CMDS_OnPlayerKilled;

            // lockserver init
            if (File.Exists(ConfigValues.ConfigPath + @"Utils\internal\LOCKSERVER") && File.Exists(ConfigValues.ConfigPath + @"Utils\internal\lockserver_whitelist.txt"))
            {
                WriteLog.Warning("Warning! Found \"Utils\\internal\\LOCKSERVER\"");
                WriteLog.Warning("All clients that do not match the whitelist will be dropped!");
                ConfigValues.LockServer = true;
                LockServer_Whitelist = File.ReadAllLines(ConfigValues.ConfigPath + @"Utils\internal\lockserver_whitelist.txt").ToList().ConvertAll(s => long.Parse(s));
            }

            CMDS_InitCommands();
            InitCommandAliases();
            InitCDVars();
            BanList = File.ReadAllLines(ConfigValues.ConfigPath + @"Commands\bannedplayers.txt").ToList();
            XBanList = File.ReadAllLines(ConfigValues.ConfigPath + @"Commands\xbans.txt").ToList();
        }

        #region COMMANDS
        private string CMD_3rdperson(Entity sender)
        {
            ConfigValues._3rdPerson = !ConfigValues._3rdPerson;
            if (ConfigValues._3rdPerson)
            {
                foreach (Entity player in Players)
                    foreach (KeyValuePair<string, string> dvar in new Dictionary<string, string> {
                        { "cg_thirdPerson", "1" },
                        { "cg_thirdPersonMode", "1" },
                        { "cg_thirdPersonSpectator", "1" },
                        { "scr_thirdPerson", "1" },
                        { "camera_thirdPerson", "1" },
                        { "camera_thirdPersonOffsetAds", "10 8 0" },
                        { "camera_thirdPersonOffset", "-70 -30 14" }
                    })
                        player.SetClientDvar(dvar.Key, dvar.Value);
                return Command.GetString("3rdperson", "message").Format(new Dictionary<string, string>() {
                    {"<issuer>", sender.Name },
                    {"<issuerf>", sender.GetFormattedName(database) }
                });
            }
            foreach (Entity player in Players)
                foreach (KeyValuePair<string, string> dvar in new Dictionary<string, string> {
                    { "cg_thirdPerson", "0" },
                    { "cg_thirdPersonMode", "0" },
                    { "cg_thirdPersonSpectator", "0" },
                    { "scr_thirdPerson", "0" },
                    { "camera_thirdPerson", "0" }
                })
                    player.SetClientDvar(dvar.Key, dvar.Value);
            return Command.GetString("3rdperson", "disabled").Format(new Dictionary<string, string>() {
                {"<issuer>", sender.Name },
                {"<issuerf>", sender.GetFormattedName(database) }
            });
        }
        private void CMD_Ac130(Entity sender, string players, string optarg)
        {
            if (players == "*all*")
            {
                foreach (Entity player in Players)
                    CMD_AC130(player, optarg == "-p");
                WriteChatToAll(Command.GetString("ac130", "all").Format(new Dictionary<string, string>()
                {
                    { "<issuer>", sender.Name },
                    { "<issuerf>", sender.GetFormattedName(database) }
                }));
            }
            else
                foreach (Entity target in FindSinglePlayerXFilter(players, sender))
                {
                    CMD_AC130(target, optarg == "-p");
                    WriteChatToAll(Command.GetString("ac130", "message").Format(new Dictionary<string, string>()
                    {
                        { "<issuer>", sender.Name },
                        { "<issuerf>", sender.GetFormattedName(database) },
                        { "<target>", target.Name },
                        { "<targetf>", target.GetFormattedName(database) }
                    }));
                }
        }
        private void CMD_Achievements(Entity sender) => WriteChatToPlayerMultiline(sender, ACHIEVEMENTS_List(sender).ToArray());
        private string CMD_Addimmune(Entity sender, string targetString)
        {
            Entity target = FindSinglePlayer(targetString);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            target.SetImmune(true, database);
            if (ConfigValues.Settings_groups_autosave)
                database.SaveGroups();
            return Command.GetString("addimmune", "message").Format(new Dictionary<string, string>()
            {
                {"<target>", target.Name },
                {"<targetf>", target.GetFormattedName(database) },
                {"<issuer>", sender.Name },
                {"<issuerf>", sender.GetFormattedName(database) },
            });
        }
        private void CMD_Admins(Entity sender)
        {
            WriteChatToPlayer(sender, Command.GetString("admins", "firstline"));
            WriteChatToPlayerCondensed(sender, database.GetAdminsString(Players), 1000, 40, Command.GetString("admins", "separator"));
        }
        private void CMD_Afk(Entity sender) => CMD_changeteam(sender, "spectator");
        private void CMD_Alias(Entity sender, string alias, string optarg)
        {
            if (ConfigValues.Settings_enable_chat_alias)
                UTILS_SetChatAlias(sender, alias, optarg);
            else
                Command.WriteErrorTo(sender, "alias", "disabled");
        }
        private void CMD_Amsg(Entity sender, string vararg)
        {
            CMD_sendadminmsg(Command.GetString("amsg", "message").Format(new Dictionary<string, string>()
            {
                { "<sender>", sender.Name },
                { "<senderf>", sender.GetFormattedName(database) },
                { "<message>", vararg }
            }));
            if (!sender.HasPermission("receiveadminmsg", database))
                WriteChatToPlayer(sender, Command.GetString("amsg", "confirmation"));
        }
        private void CMD_Apply(Entity sender) => WriteChatToPlayerMultiline(sender, File.ReadAllLines(ConfigValues.ConfigPath + @"Commands\apply.txt"));
        private string CMD_Balance(Entity sender)
        {
            List<Entity> axis = new List<Entity>();
            List<Entity> allies = new List<Entity>();
            foreach (Entity player in Players)
                if (player.GetTeam() == "axis")
                    axis.Add(player);
                else if (player.GetTeam() == "allies")
                    allies.Add(player);
            if (Math.Abs(axis.Count - allies.Count) < 2)
                return Command.WriteErrorTo(sender, "balance", "teamsalreadybalanced");

            axis = axis.OrderBy(player => player.IsAlive).ToList();
            allies = allies.OrderBy(player => player.IsAlive).ToList();

            while (axis.Count > allies.Count && Math.Abs(axis.Count - allies.Count) > 1)
            {
                Entity chosenplayer = axis[axis.Count - 1];
                CMD_changeteam(chosenplayer, "allies");
                axis.Remove(chosenplayer);
                allies.Add(chosenplayer);
            }

            while (allies.Count > axis.Count && Math.Abs(axis.Count - allies.Count) > 1)
            {
                Entity chosenplayer = allies[allies.Count - 1];
                CMD_changeteam(chosenplayer, "axis");
                allies.Remove(chosenplayer);
                axis.Add(chosenplayer);
            }

            return Command.GetString("balance", "message").Format(new Dictionary<string, string>()
            {
                {"<issuer>", sender.Name },
                {"<issuerf>", sender.GetFormattedName(database) }
            });
        }
        private string CMD_Ban(Entity sender, string player, string optarg)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            if (target.IsImmune(database))
                return Command.WriteErrorTo(sender, "TargetIsImmune");
            if (string.IsNullOrEmpty(optarg))
                CMD_ban(target);
            else
                CMD_ban(target, optarg);
            return Command.GetString("ban", "message").Format(new Dictionary<string, string>()
            {
                { "<target>", target.Name },
                { "<targetf>", target.GetFormattedName(database) },
                { "<issuer>", sender.Name },
                { "<issuerf>", sender.GetFormattedName(database) },
                { "<reason>", optarg }
            });
        }
        private string CMD_Betterbalance(Entity sender, string toggle){
            bool state = UTILS_ParseBool(toggle);
            GSCFunctions.SetDvar("betterbalance", state);
            return Command.GetString("betterbalance", "message_" + (state ? "on" : "off"));
        }
        private string CMD_Cdvar(Entity sender, string cdvar, string optarg)
        {
            string[] optargs = new string[] { };
            if (!string.IsNullOrEmpty(optarg))
                optargs = optarg.Split(new char[] { ' ' }, 2);
            if (optargs.Length == 2 && !string.IsNullOrEmpty(optargs[0]) && !string.IsNullOrEmpty(optargs[1]))
            {
                optargs[0] = optargs[0].ToLowerInvariant();
                bool success = false;
                switch (cdvar.ToLowerInvariant())
                {
                    case "-i":
                    case "--int":
                        sender.SetClientDvar(optargs[0], int.Parse(optargs[1]));
                        success = true;
                        break;
                    case "-f":
                    case "--float":
                        sender.SetClientDvar(optargs[0], float.Parse(optargs[1]));
                        success = true;
                        break;
                    case "-d":
                    case "--direct":
                        sender.SetClientDvar(optargs[0], optargs[1]);
                        success = true;
                        break;
                    case "-s":
                    case "--save":
                        sender.SetClientDvar(optargs[0], optargs[1]);
                        List<Dvar> dvar = new List<Dvar>() { new Dvar { key = optargs[0], value = optargs[1] } };
                        if (PersonalPlayerDvars.ContainsKey(sender.GUID))
                            PersonalPlayerDvars[sender.GUID] = UTILS_DvarListUnion(PersonalPlayerDvars[sender.GUID], dvar);
                        else
                            PersonalPlayerDvars.Add(sender.GUID, dvar);
                        success = true;
                        break;
                }
                if (success)
                    switch (cdvar.ToLowerInvariant())
                    {
                        case "-i":
                        case "--int":
                        case "-f":
                        case "--float":
                        case "-d":
                        case "--direct":
                            return Command.GetString("cdvar", "message").Format(new Dictionary<string, string>()
                            {
                                { "<key>", optargs[0] },
                                { "<value>", optargs[1] }
                            });
                        case "-s":
                        case "--save":
                            return Command.GetString("cdvar", "message1").Format(new Dictionary<string, string>()
                            {
                                { "<key>", optargs[0] },
                                { "<value>", optargs[1] }
                            });
                    }
            }

            switch (cdvar.ToLowerInvariant())
            {
                case "-r":
                case "--reset":
                    if (!PersonalPlayerDvars.ContainsKey(sender.GUID))
                        return Command.WriteErrorTo(sender, "cdvar", "error1");
                    if (optargs.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(optargs[0]))
                        {
                            optargs[0] = optargs[0].ToLowerInvariant();
                            if (UTILS_DvarListRelativeComplement(new List<Dvar>() { new Dvar { key = optargs[0], value = "0" } }, PersonalPlayerDvars[sender.GUID].ConvertAll((s) => { return s.key; })).Count != 0)
                            {
                                WriteChatToPlayer(sender, Command.GetString("cdvar", "error2").Replace("<key>", optargs[0]));
                                return "";
                            }
                            PersonalPlayerDvars[sender.GUID] = UTILS_DvarListRelativeComplement(PersonalPlayerDvars[sender.GUID], new List<string>() { optargs[0] });
                            if (optargs.Length == 2 && !string.IsNullOrEmpty(optargs[1]))
                                sender.SetClientDvar(optargs[0], optargs[1]);
                            return Command.GetString("cdvar", "message2").Replace("<key>", optargs[0]);
                        }
                    }
                    else
                    {
                        PersonalPlayerDvars.Remove(sender.GUID);
                        return Command.GetString("cdvar", "message3");
                    }
                    break;
            }
            return Command.WriteErrorTo(sender, "cdvar", "usage");
        }
        private string CMD_Changeteam(Entity sender, string player)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            switch (target.GetTeam())
            {
                case "spectator":
                    return Command.WriteErrorTo(sender, "PlayerIsSpectating");
                case "axis":
                    CMD_changeteam(target, "allies");
                    break;
                case "allies":
                    CMD_changeteam(target, "axis");
                    break;
            }
            return Command.GetString("setteam", "message").Format(new Dictionary<string, string>()
            {
                {"<issuer>", sender.Name },
                {"<issuerf>", sender.GetFormattedName(database) },
                {"<target>", target.Name },
                {"<targetf>", target.GetFormattedName(database) },
            });
        }
        private string CMD_Clankick(Entity sender, string player)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            target.SetGroup("default", database);
            if (ConfigValues.Settings_groups_autosave)
                database.SaveGroups();
            CMD_kick(target, Command.GetString("clankick", "kickmessage"));
            return Command.GetString("clankick", "message").Format(new Dictionary<string, string>()
            {
                {"<issuer>", sender.Name },
                {"<issuerf>", sender.GetFormattedName(database) },
                {"<target>", target.Name },
                {"<targetf>", target.GetFormattedName(database) },
            });
        }
        private string CMD_Clantag(Entity sender, string player, string tag)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            bool hasTag = Forced_clantags.Keys.Contains(target.GUID);
            string res;
            if (string.IsNullOrEmpty(tag))
            {
                if (hasTag)
                    Forced_clantags.Remove(target.GUID);
                target.ClanTag = "";
                res = Command.GetString("clantag", "reset").Replace("<player>", target.Name);
            }
            else
            {
                if (tag.Length > 7)
                    return Command.WriteErrorTo(sender, "clantag", "error");
                if (hasTag)
                    Forced_clantags[target.GUID] = tag;
                else
                    Forced_clantags.Add(target.GUID, tag);
                res = Command.GetString("clantag", "message").Format(new Dictionary<string, string>()
                {
                    {"<player>", target.Name },
                    {"<tag>", tag}
                });
            }
            //save settings
            List<string> tags = new List<string>();
            foreach (KeyValuePair<long, string> entry in Forced_clantags)
                tags.Add(entry.Key.ToString() + "=" + entry.Value);
            File.WriteAllLines(ConfigValues.ConfigPath + @"Utils\forced_clantags.txt", tags.ToArray());
            return res;
        }
        private string CMD_Clanvsall(Entity sender, string vararg)
        {
            CMD_clanvsall(vararg.Split(' ').ToList());
            return Command.GetString("clanvsall", "message").Format(new Dictionary<string, string>()
            {
                { "<issuer>", sender.Name },
                { "<issuerf>", sender.GetFormattedName(database) },
                { "<identifiers>", vararg }
            });
        }
        private string CMD_Clanvsallspectate(Entity sender, string vararg)
        {
            CMD_clanvsall(vararg.Split(' ').ToList(), true);
            return Command.GetString("clanvsall", "message").Format(new Dictionary<string, string>()
            {
                { "<issuer>", sender.Name },
                { "<issuerf>", sender.GetFormattedName(database) },
                { "<identifiers>", vararg }
            });
        }
        private void CMD_Cleartmpbanlist()
        {
            List<string> linescopy = BanList.Clone();
            foreach (string line in linescopy)
            {
                string[] parts = line.Split(';');
                if (parts.Length < 3)
                    continue;
                DateTime until = DateTime.ParseExact(parts[0], "yyyy MMM d HH:mm", Culture);
                if (until < DateTime.Now)
                    BanList.Remove(line);
            }
        }
        private string[] CMD_Credits() => new string[]
        {
            "^1DHAdmin ^3" + ConfigValues.Version,
            "^1Credits:",
            "^3Developed by ^2Jelle Bouma",
            "^3Based on DGAdmin v3.5.1",
            "^3Thanks to:",
            "^Frederica Bernkastel, for coding DGAdmin."
        };
        private string CMD_Debug(Entity sender, string feature)
        {
            switch (feature)
            {
                case "restrictedweapons":
                    WriteLog.Info("Restricted weapons: " + RestrictedWeapons);
                    return "debug.restrictedweapons::console out";
                case "weapon":
                    return sender.CurrentWeapon;
                case "svtitle":
                    UTILS_ServerTitle("123456789012345678901234567890", "123456789012345678901234567890");
                    return "debug.mem::callback";
                case "sky":
                    sender.SetClientDvar("r_lightTweakSunColor", "0 0 0");
                    sender.SetClientDvar("r_lighttweaksunlight", "-10");
                    sender.SetClientDvar("r_brightness", "-0.5");
                    sender.SetClientDvar("r_fog", "1");
                    sender.SetClientDvars("r_sun", "0", new Parameter[] { "r_lighttweaksunlight", "0.3 0.3 0.3" });
                    sender.SetClientDvar("r_lightTweakSunColor", "0 0 0");
                    sender.SetClientDvar("r_lighttweaksunlight", "0.991101 0.947308 0.760525");
                    sender.SetClientDvar("r_heroLightScale", "1 1 1");
                    sender.SetClientDvar("r_skyColorTemp", "6500");
                    return "";
                case "dsr":
                    string dsr = ConfigValues.Current_DSR;
                    WriteLog.Debug("Dvar DSR:" + GSCFunctions.GetDvar("sv_current_dsr"));
                    return "DHAdmin DSR: " + dsr + " " + (CFG_FindServerFile(dsr, out _) ? "(exists)" : "(not exists)");
                case "cmdcnt":
                    return "debug.cmdcnt:: Total commands count: " + CommandList.Count.ToString();
            }
            return "There is no debugging set up for the string: " + feature;
        }
        private string CMD_Drunk(Entity sender)
        {
            OnInterval(2, () =>
            {
                sender.ShellShock("default", 4F);
                return sender.IsAlive;
            });
            return Command.GetString("drunk", "message").Replace("<player>", sender.Name);
        }
        private void CMD_Daytime(Entity sender, string daytime)
        {
            ConfigValues.Settings_daytime = daytime;
            switch (daytime)
            {
                case "day":
                    ConfigValues.Settings_sunlight = new float[3] { 1f, 1f, 1f };
                    break;
                case "night":
                    ConfigValues.Settings_sunlight = new float[3] { 0f, 0.7f, 1f };
                    break;
                case "morning":
                    ConfigValues.Settings_daytime = "day";
                    ConfigValues.Settings_sunlight = new float[3] { 1.5f, 0.65f, 0f };
                    break;
                case "cloudy":
                    ConfigValues.Settings_daytime = "day";
                    ConfigValues.Settings_sunlight = new float[3] { 0f, 0f, 0f };
                    break;
            }
            GSCFunctions.SetSunlight(new Vector3(ConfigValues.Settings_sunlight[0], ConfigValues.Settings_sunlight[1], ConfigValues.Settings_sunlight[2]));
            foreach (Entity player in Players)
                UTILS_SetCliDefDvars(player);
        }
        private void CMD_Dbsearch(Entity sender, string player)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                Command.WriteErrorTo(sender, "NotOnePlayerFound");
            else {
                PlayerInfo playerinfo = target.GetInfo();
                string[] foundplayerinfo = (from p in database.Players
                                            where playerinfo.MatchesOR(p.Key)
                                            select Command.GetString("dbsearch", "message_found").Replace("<playerinfo>", CMDS_CommonIdentifiers(p.Key, playerinfo) + " ^7= ^5" + p.Value)).ToArray();
                if (foundplayerinfo.Length == 0)
                    Command.WriteErrorTo(sender, "dbsearch", "message_notfound");
                else
                {
                    WriteChatToPlayer(sender, Command.GetString("dbsearch", "message_firstline").Replace("<nr>", foundplayerinfo.Length.ToString()));
                    WriteChatToPlayerMultiline(sender, foundplayerinfo, 2000);
                }
            }
        }
        private void CMD_Dsrnames(Entity sender)
        {
            WriteChatToPlayer(sender, Command.GetString("dsrnames", "firstline"));
            string[] players2 = Directory.GetFiles(@"players2\", "*.DSR");
            string[] admin = Directory.GetFiles(@"admin\", "*.DSR");
            string[] combined = new string[players2.Length + admin.Length];
            players2.CopyTo(combined, 0);
            admin.CopyTo(combined, players2.Length);
            WriteChatToPlayerCondensed(sender, combined, 2000);
        }
        private string CMD_End(Entity sender)
        {
            CMDS_EndRound();
            return Command.GetString("end", "message").Format(new Dictionary<string, string>()
            {
                {"<issuer>", sender.Name },
                {"<issuerf>", sender.GetFormattedName(database) },
            });
        }
        private void CMD_Fakesay(Entity sender, string player, string vararg)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                Command.WriteErrorTo(sender, "NotOnePlayerFound");
            else
                CHAT_WriteChat(target, ChatType.All, vararg);
        }
        private void CMD_Fc(Entity sender, string player, string vararg)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                Command.WriteErrorTo(sender, "NotOnePlayerFound");
            else
            {
                WriteChatSpyToPlayer(target, sender.Name + ": ^6!fc " + player + " " + vararg); //abuse proof ;)
                ProcessCommand(sender, target, "!" + vararg);
            }
        }
        private string CMD_Fire(Entity sender)
        {
            int addr = GSCFunctions.LoadFX("misc/flares_cobra");
            OnInterval(200, () =>
            {
                GSCFunctions.PlayFX(addr, sender.GetEye());
                return true;
            });
            return "^1FIREEEEEEEEEEEE";
        }
        private string CMD_Fixplayergroup(Entity sender, string player)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            bool success = target.FixPlayerIdentifiers(database);
            database.SaveGroups();
            if (success)
                return Command.GetString("fixplayergroup", "message");
            else
                return Command.WriteErrorTo(sender, "fixplayergroup", "notfound");
        }
        private string CMD_Fly(Entity sender, string toggle, string optarg)
        {
            const int DISABLED = 0;
            const int EVENTHANDLERS_SET = 1;
            const int EVENTHANDLERS_ACTIVE = 2;
            const int EFFECTS_ACTIVE = 3;
            if (toggle != "on" && toggle != "off")
                return Command.WriteErrorTo(sender, "fly", "usage");
            if (!sender.HasField("CMD_FLY"))
                sender.SetField("CMD_FLY", DISABLED);
            string key = string.IsNullOrEmpty(optarg) ? "activate" : optarg;
            if (UTILS_GetFieldSafe<int>(sender, "CMD_FLY") == DISABLED)
            {
                sender.OnNotify("fly_on", new Action<Entity>(_player =>
                {
                    if (UTILS_GetFieldSafe<int>(_player, "CMD_FLY") == EVENTHANDLERS_ACTIVE)
                    {
                        sender.SetField("CMD_FLY", EFFECTS_ACTIVE);
                        _player.AllowSpectateTeam("freelook", true);
                        _player.SetField("sessionstate", "spectator");
                        _player.SetContents(0);
                        _player.ThermalVisionOn();
                        UTILS_SetClientDvarsPacked(_player, UTILS_SetClientInShadowFX());
                        int iter = 0;
                        OnInterval(100, () =>
                        {
                            if (iter % 10 == 0)
                                _player.PlayLocalSound("ui_mp_nukebomb_timer");
                            if (iter % 30 == 0)
                                _player.PlayLocalSound("breathing_hurt");
                            iter += 1;
                            return UTILS_GetFieldSafe<int>(_player, "CMD_FLY") == EFFECTS_ACTIVE;
                        });
                    }
                }));
                sender.OnNotify("fly_off", new Action<Entity>(_player =>
                {
                    if (UTILS_GetFieldSafe<int>(_player, "CMD_FLY") == EFFECTS_ACTIVE)
                    {
                        _player.SetField("CMD_FLY", EVENTHANDLERS_ACTIVE);
                        _player.AllowSpectateTeam("freelook", false);
                        _player.SetField("sessionstate", "playing");
                        _player.SetContents(100);
                        _player.ThermalVisionOff();
                        UTILS_SetCliDefDvars(_player);
                    }
                }));
            }

            if (UTILS_ParseBool(toggle))
            {
                int CMD_FLY = UTILS_GetFieldSafe<int>(sender, "CMD_FLY");
                if (CMD_FLY == DISABLED)
                {
                    sender.NotifyOnPlayerCommand("fly_on", "+" + key);
                    sender.NotifyOnPlayerCommand("fly_off", "-" + key);
                }
                sender.SetField("CMD_FLY", EVENTHANDLERS_ACTIVE);
                return Command.GetString("fly", "enabled").Replace("<key>", CMD_FLY == EVENTHANDLERS_SET ? "" : @"[[{" + key + @"}]]");
            }
            else
            {
                sender.SetField("CMD_FLY", EVENTHANDLERS_SET);
                return Command.GetString("fly", "disabled").Replace("<state>", "enabled");
            }
        }
        private void CMD_Foreach(Entity sender, string playerfilter, string vararg)
        {
            vararg = "!" + vararg;
            //we'll use global flag to gather all possible hacks with !foreach and !fc
            if (ConfigValues.Cmd_foreachContext)
                WriteChatToPlayer(sender, "I like the way you're thinking, but nope.");
            else
            {
                ConfigValues.Cmd_foreachContext = true;
                List<Entity> players = FindPlayersFilter(playerfilter, sender);

                foreach (Entity player in players)
                    ProcessCommand(sender, sender, vararg
                        .Replace("<player>", "#" + player.GetEntityNumber().ToString())
                        .Replace("<p>", "#" + player.GetEntityNumber().ToString()));
                ConfigValues.Cmd_foreachContext = false;
                if (players.Count > 0)
                    WriteChatToPlayer(sender, Command.GetMessage("Filters_message").Replace("<count>", players.Count.ToString()));
            }
        }
        private void CMD_Freeze(Entity sender, string players)
        {
            List<Entity> targets = FindSinglePlayerXFilter(players, sender);
            foreach (Entity target in targets)
                if (target.IsImmune(database))
                    Command.WriteErrorTo(sender, "TargetIsImmune");
                else
                {
                    target.FreezeControls(true);
                    target.SetField("frozenbycommand", true);
                    WriteChatToAll(Command.GetString("freeze", "message").Format(new Dictionary<string, string>() {
                        {"<issuer>", sender.Name },
                        {"<issuerf>", sender.GetFormattedName(database) },
                        {"<target>", target.Name },
                        {"<targetf>", target.GetFormattedName(database) },
                    }));
                }
        }
        private void CMD_Frfc(Entity sender, string playerfilter, string vararg)
        {
            //we'll use global flag to gather all possible hacks with !foreach and !fc
            if (ConfigValues.Cmd_foreachContext)
                WriteChatToPlayer(sender, "I like the way you're thinking, but nope.");
            else
            {
                ConfigValues.Cmd_foreachContext = true;
                List<Entity> players = FindPlayersFilter(playerfilter, sender);
                foreach (Entity player in players)
                {
                    WriteChatSpyToPlayer(player, sender.Name + ": ^6!frfc " + playerfilter + " " + vararg); //abuse proof ;)
                    ProcessCommand(sender, player, "!" + vararg);
                }
                ConfigValues.Cmd_foreachContext = false;
                if (players.Count > 0)
                    WriteChatToPlayer(sender, Command.GetMessage("Filters_message").Replace("<count>", players.Count.ToString()));
            }
        }
        private string CMD_Ft(Entity sender, string ft)
        {
            if (ConfigValues.Settings_daytime == "night")
                return Command.WriteErrorTo(sender, "blockedByNightMode");
            CMD_applyfilmtweak(sender, ft);
            return Command.GetString("ft", "message").Replace("<ft>", ft);
        }
        private string CMD_Fx(Entity sender, string toggle)
        {
            List<Dvar> dvars;
            bool enable = UTILS_ParseBool(toggle);
            dvars = new List<Dvar>()
            {
                new Dvar { key = "fx_draw", value = enable ? "1" : "0" },
                new Dvar { key = "r_fog", value = enable ? "1" : "0" }
            };
            if (PersonalPlayerDvars.ContainsKey(sender.GUID))
                PersonalPlayerDvars[sender.GUID] = UTILS_DvarListUnion(PersonalPlayerDvars[sender.GUID], dvars);
            else
                PersonalPlayerDvars.Add(sender.GUID, dvars);
            UTILS_SetClientDvarsPacked(sender, dvars);
            return Command.GetString("fx", enable ? "message_on" : "message_off");
        }
        private string CMD_Ga(Entity sender)
        {
            if (GSCFunctions.GetDvar("g_gametype") == "infect" && sender.GetTeam() == "axis")
                return "";
            CMD_GiveMaxAmmo(sender);
            return Command.GetString("ga", "message");
        }
        private string CMD_Gametype(Entity sender, string mode, string map)
        {
            if (!CFG_FindServerFile(mode + ".dsr", out _))
                return Command.WriteErrorTo(sender,"DSRNotFound");
            string newmap = FindSingleMap(map);
            if (string.IsNullOrWhiteSpace(newmap))
                return Command.WriteErrorTo(sender, "NotOneMapFound");
            MR_SwitchModeImmediately(mode, newmap);
            return Command.GetString("gametype", "message").Format(new Dictionary<string, string>()
            {
                {"<issuer>", sender.Name },
                {"<issuerf>", sender.GetFormattedName(database) },
                {"<dsr>", mode },
                {"<mapname>", newmap }
            });
        }
        private string CMD_Getplayerinfo(Entity sender, string player)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            return Command.GetString("getplayerinfo", "message").Format(new Dictionary<string, string>()
            {
                {"<target>", target.Name },
                {"<targetf>", target.GetFormattedName(database) },
                {"<id>", target.GetEntityNumber().ToString() },
                {"<guid>", target.GUID.ToString() },
                {"<ip>", target.IP.Address.ToString() },
                {"<hwid>", target.GetHWID().ToString() },
            });
        }
        private string CMD_Getwarns(Entity sender, string player, string optarg)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            return Command.GetString("getwarns", "message").Format(new Dictionary<string, string>()
            {
                { "<target>", target.Name },
                { "<targetf>", target.GetFormattedName(database) },
                { "<warncount>", CMD_getwarns(target).ToString() },
                { "<maxwarns>", ConfigValues.Settings_warn_maxwarns.ToString() }
            });
        }
        private string CMD_Guid(Entity sender) => Command.GetString("guid", "message").Replace("<guid>", sender.GUID.ToString());
        private string CMD_Gravity(Entity sender, string gravityString)
        {
            if (!float.TryParse(gravityString, out float gravity))
                gravity = 9.8f;
            CMD_GRAVITY((int)Math.Round(gravity / 9.8 * 800));
            return Command.GetString("gravity", "message").Format(new Dictionary<string, string>()
            {
                { "<g>", gravity.ToString() },
                { "<issuer>", sender.Name },
                { "<issuerf>", sender.GetFormattedName(database) }
            });
        }
        private void CMD_Help(Entity sender, string optarg)
        {
            if (string.IsNullOrEmpty(optarg))
            { // get list of commands
                GroupsDatabase.Group playergroup = sender.GetGroup(database);
                GroupsDatabase.Group defaultgroup = database.GetGroup("default");
                List<string> availablecommands;
                if ((playergroup.group_name == "sucker") || (playergroup.group_name == "banned"))
                    availablecommands = CommandList.FindAll(cmd => playergroup.CanDo(cmd.name)).ConvertAll(cmd => cmd.name);
                else
                    availablecommands = CommandList.FindAll(cmd => playergroup.CanDo(cmd.name) || defaultgroup.CanDo(cmd.name)).ConvertAll(cmd => cmd.name);
                WriteChatToPlayer(sender, Command.GetString("help", "firstline"));
                WriteChatToPlayerMultiline(sender, availablecommands.ToArray().Condense(), 2000);
            }
            else
            { // get help with a specific command
                if (CommandAliases.TryGetValue(optarg, out string actualcommand))
                    optarg = actualcommand;
                if (DefaultCmdLang.ContainsKey("command_" + optarg + "_usage"))
                    WriteChatToPlayer(sender, Command.GetString(optarg, "usage"));
                else
                    Command.WriteErrorTo(sender, "CommandNotFound");
            }
        }
        private string CMD_Hidebombicon(Entity sender)
        {
            CMD_HideBombIcon(sender);
            return Command.GetString("hidebombicon", "message");
        }
        private string CMD_Hwid(Entity sender) => Command.GetString("hwid", "message").Replace("<hwid>", sender.GetHWID().ToString());
        private string CMD_Jump(Entity sender, string heightString)
        {
            if (!float.TryParse(heightString, out float height))
                height = 39;
            CMD_JUMP(height);
            return Command.GetString("jump", "message").Format(new Dictionary<string, string>()
            {
                {"<height>", heightString.StartsWith("def") ? "default" : height.ToString() },
                {"<issuer>", sender.Name },
                {"<issuerf>", sender.GetFormattedName(database) }
            });
        }
        private string CMD_Kd(Entity sender, string player, string k, string d)
        {
            if (!int.TryParse(k, out int kills) || !int.TryParse(d, out int deaths))
                return Command.WriteErrorTo(sender, "kd", "usage");
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            target.SetField("kills", kills);
            target.SetField("Kills", kills);
            target.SetField("Kill", kills);
            target.SetField("deaths", deaths);
            target.SetField("Deaths", deaths);
            target.SetField("death", deaths);
            return Command.GetString("kd", "message").Format(new Dictionary<string, string>()
            {
                {"<player>", target.Name },
                {"<kills>", kills.ToString() },
                {"<deaths>", deaths.ToString() }
            });
        }
        private string CMD_Kick(Entity sender, string player, string optarg)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            if (target.IsImmune(database))
                return Command.WriteErrorTo(sender, "TargetIsImmune");
            if (string.IsNullOrEmpty(optarg))
                CMD_kick(target);
            else
                CMD_kick(target, optarg);
            return Command.GetString("kick", "message").Format(new Dictionary<string, string>()
            {
                { "<target>", target.Name },
                { "<targetf>", target.GetFormattedName(database) },
                { "<issuer>", sender.Name },
                { "<issuerf>", sender.GetFormattedName(database) },
                { "<reason>", optarg }
            });
        }
        private void CMD_Kickhacker(Entity sender, string vararg)
        {
            foreach (Entity entity in GetEntities())
                if (entity.Name == vararg)
                    ExecuteCommand("dropclient " + entity.GetEntityNumber().ToString());
        }
        private void CMD_Kill(Entity sender, string victims)
        {
            List<Entity> targets = FindSinglePlayerXFilter(victims, sender);
            foreach (Entity target in targets)
                if (target.IsImmune(database))
                    Command.WriteErrorTo(sender, "TargetIsImmune");
                else
                    AfterDelay(100, () => target.Suicide());
            if (targets.Count > 1)
                WriteChatToPlayer(sender, Command.GetMessage("Filters_message").Replace("<count>", targets.Count.ToString()));
        }
        private string CMD_Knife(Entity sender, string toggle)
        {
            bool enabled = UTILS_ParseBool(toggle);
            CMD_knife(enabled);
            return Command.GetString("knife", "message_" + (enabled ? "on" : "off"));
        }
        private void CMD_Lastbans(Entity sender, string optarg)
        {
            if (!int.TryParse(optarg, out int count))
                count = 4;
            List<BanEntry> banlist = CMD_GetLastBanEntries(count);
            List<string> messages = new List<string>();
            foreach (BanEntry banentry in banlist)
                messages.Add(Command.GetString("lastbans", "message").Format(new Dictionary<string, string>()
                {
                    { "<banid>", banentry.BanId.ToString() },
                    { "<name>", banentry.Playername },
                    { "<guid>", banentry.playerinfo.GetGUIDString() },
                    { "<ip>", banentry.playerinfo.GetIPString() },
                    { "<hwid>", banentry.playerinfo.GetHWIDString() },
                    { "<time>", banentry.Until.Year == 9999 ? "^6PERMANENT" : banentry.Until.ToString("yyyy MMM d HH:mm") }
                }));
            WriteChatToPlayer(sender, Command.GetString("lastbans", "firstline").Replace("<nr>", count.ToString()));
            if (messages.Count < 1)
                Command.WriteErrorTo(sender, "NoEntriesFound");
            else
                WriteChatToPlayerMultiline(sender, messages.ToArray());
        }
        private string[] CMD_Lastreports(Entity sender, string optarg)
        {
            if (!int.TryParse(optarg, out int reportcnt))
                reportcnt = 4;
            string[] reports = File.ReadAllLines(ConfigValues.ConfigPath + @"Commands\internal\ChatReports.txt");
            return reports.Skip(reports.Length - reportcnt).ToArray();
        }
        private string CMD_Letmehardscope(Entity sender, string toggle)
        {
            bool state = UTILS_ParseBool(toggle);
            sender.SetField("letmehardscope", state ? 1 : 0);
            return Command.GetString("letmehardscope", "message_" + (state ? "on" : "off"));
        }
        private string CMD_Loadgroups()
        {
            foreach (Entity player in Players)
                player.SetLogged(false);
            database = new GroupsDatabase();
            return Command.GetString("loadgroups", "message");
        }
        private void CMD_Lockserver(Entity sender, string optarg)
        {
            optarg = string.IsNullOrEmpty(optarg) ? "" : optarg;
            if (ConfigValues.LockServer)
            {
                ConfigValues.LockServer = false;
                LockServer_Whitelist.Clear();
                if (File.Exists(ConfigValues.ConfigPath + @"Utils\internal\LOCKSERVER"))
                    File.Delete(ConfigValues.ConfigPath + @"Utils\internal\LOCKSERVER");
                if (File.Exists(ConfigValues.ConfigPath + @"Utils\internal\lockserver_whitelist.txt"))
                    File.Delete(ConfigValues.ConfigPath + @"Utils\internal\lockserver_whitelist.txt");
                WriteChatToAll(Command.GetString("lockserver", "message1"));
                if (ConfigValues.Settings_servertitle)
                    UTILS_ServerTitle_MapFormat();
            }
            else
            {
                WriteChatToAll(@" ^3" + sender.Name + " ^1executed ^3!lockserver " + optarg);

                AfterDelay(2000, () =>
                {
                    WriteChatToAllMultiline(new string[]
                    {
                        "^1Server will be locked in:",
                        "^35",
                        "^34",
                        "^33",
                        "^32",
                        "^31",
                        "^1DONE."
                    }, 1000);
                });
                AfterDelay(8000, () =>
                {
                    ConfigValues.LockServer = true;
                    LockServer_Whitelist = Players.ConvertAll<long>(s => s.GUID);
                    File.WriteAllText(ConfigValues.ConfigPath + @"Utils\internal\LOCKSERVER", optarg);
                    File.WriteAllLines(ConfigValues.ConfigPath + @"Utils\internal\lockserver_whitelist.txt",
                        LockServer_Whitelist.ConvertAll(s => s.ToString())
                    );
                    if (ConfigValues.Settings_servertitle)
                        UTILS_ServerTitle("^1::LOCKED", "^1" + optarg);
                });
            }
        }
        private string CMD_Login(Entity sender, string password)
        {
            if (sender.IsLogged())
                return Command.WriteErrorTo(sender, "login", "alreadylogged");
            GroupsDatabase.Group grp = sender.GetGroup(database);
            if (string.IsNullOrWhiteSpace(grp.login_password))
                return Command.WriteErrorTo(sender, "login", "notrequired");
            if (password != grp.login_password)
                return Command.WriteErrorTo(sender, "login", "wrongpassword");
            sender.SetLogged(true);
            if (bool.Parse(Sett_GetString("settings_enable_spy_onlogin")) && grp.CanDo("spy"))
            {
                sender.SetSpying(true);
                WriteChatToPlayer(sender, Command.GetString("spy", "message_on"));
            }
            return Command.GetString("login", "successful");
        }
        private string CMD_Map(Entity sender, string map)
        {
            string newmap = FindSingleMap(map);
            if (string.IsNullOrEmpty(newmap))
                return Command.WriteErrorTo(sender, "NotOneMapFound");
            CMD_changemap(newmap);
            return Command.GetString("map", "message").Format(new Dictionary<string, string>()
            {
                {"<player>", sender.Name },
                {"<playerf>", sender.GetFormattedName(database) },
                {"<mapname>", newmap },
            });
        }
        private void CMD_Maps(Entity sender)
        {
            WriteChatToPlayer(sender, Command.GetString("maps", "firstline"));
            WriteChatToPlayerCondensed(sender, ConfigValues.AvailableMaps.Keys.ToArray());
        }
        private string CMD_Mode(Entity sender, string mode, string optarg)
        {
            if (!CFG_FindServerFile(mode + ".dsr", out _))
                return Command.WriteErrorTo(sender, "DSRNotFound");
            if (optarg == null || optarg == "")
                MR_SwitchModeImmediately(mode);
            else
                MR_SwitchModeImmediately(mode, FindSingleMap(optarg));
            return Command.GetString("mode", "message").Format(new Dictionary<string, string>()
            {
                { "<issuer>", sender.Name },
                { "<issuerf>", sender.GetFormattedName(database) },
                { "<dsr>", mode }
            });
        }
        private void CMD_Mute(Entity sender, string players)
        {
            List<Entity> targets = FindSinglePlayerXFilter(players, sender);
            foreach (Entity target in targets)
                if (target.IsImmune(database))
                    Command.WriteErrorTo(sender, "TargetIsImmune");
                else
                {
                    target.SetMuted(true);
                    WriteChatToAll(Command.GetString("mute", "message").Format(new Dictionary<string, string>()
                    {
                        {"<issuer>", sender.Name },
                        {"<issuerf>", sender.GetFormattedName(database) },
                        {"<target>", target.Name },
                        {"<targetf>", target.GetFormattedName(database) },
                    }));
                }
        }
        private void CMD_Myalias(Entity sender, string optarg)
        {
            if (ConfigValues.Settings_enable_chat_alias)
                UTILS_SetChatAlias(sender, sender.Name, optarg);
            else
                WriteChatToPlayer(sender, Command.GetString("alias", "disabled"));
        }
        private string CMD_Night(Entity sender, string toggle)
        {
            if (ConfigValues.Settings_daytime == "night")
                return Command.WriteErrorTo(sender, "blockedByNightMode");
            else
            {
                bool enable = UTILS_ParseBool(toggle);
                List<Dvar> dvars = UTILS_SetClientNightVision();
                if (enable)
                {
                    UTILS_SetClientDvarsPacked(sender, dvars);
                    if (PersonalPlayerDvars.ContainsKey(sender.GUID))
                        PersonalPlayerDvars[sender.GUID] = UTILS_DvarListUnion(PersonalPlayerDvars[sender.GUID], dvars);
                    else
                        PersonalPlayerDvars.Add(sender.GUID, dvars);
                    return "^4NightMod ^2Activated";
                }
                if (PersonalPlayerDvars.ContainsKey(sender.GUID))
                    PersonalPlayerDvars[sender.GUID] = UTILS_DvarListRelativeComplement(PersonalPlayerDvars[sender.GUID], dvars.ConvertAll(s => s.key));
                UTILS_SetCliDefDvars(sender);
                return "^4NightMod ^1Deactivated";
            }
        }
        private void CMD_No(Entity sender)
        {
            string command = sender.GetField<string>("CurrentCommand");
            if (string.IsNullOrEmpty(command))
                if (voting.isActive())
                    if (voting.NegativeVote(sender))
                        WriteChatToAll(Command.GetString("no", "message").Format(new Dictionary<string, string>() { { "<player>", sender.Name } }));
                    else
                    {
                        if ((sender == voting.issuer) || (sender == voting.target))
                            WriteChatToPlayer(sender, Command.GetString("votekick", "error6"));
                        else
                            WriteChatToPlayer(sender, Command.GetString("votekick", "error5"));
                    }
                else
                    WriteChatToPlayer(sender, "^3Warning: Command buffer is empty.");
            else
            {
                sender.SetField("CurrentCommand", "");
                WriteChatToPlayer(sender, "^3Command execution aborted (^1" + command + "^3)");
            }
        }
        private string CMD_Nootnoot(Entity sender, string victim)
        {
            Entity target = FindSinglePlayer(victim);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            if (target.IsImmune(database))
                return Command.WriteErrorTo(sender, "TargetIsImmune");
            bool newState = !target.HasField("nootnoot") || !target.GetField<bool>("nootnoot");
            target.SetField("nootnoot", newState);
            return Command.GetString("nootnoot", "message_" + (newState ? "on" : "off")).Format(new Dictionary<string, string>()
            {
                {"<target>", target.Name },
                {"<targetf>", target.GetFormattedName(database) },
            });
        }
        private void CMD_Playfx(Entity sender, string fx) => GSCFunctions.PlayFX(GSCFunctions.LoadFX(fx), sender.Origin);
        private string CMD_Playfxontag(Entity sender, string fx, string optarg)
        {
            if (!UTILS_ValidateFX(fx))
                return Command.WriteErrorTo(sender, "FX_not_found");
            string tag = string.IsNullOrEmpty(optarg) ? "j_head" : optarg;
            GSCFunctions.PlayFXOnTag(GSCFunctions.LoadFX(fx), sender, tag);
            return Command.GetString("playfxontag", "message").Format(new Dictionary<string, string>()
            {
                { "<fx>", fx },
                { "<tag>", tag }
            });
        }
        private string CMD_Pm(Entity sender, string recipient, string vararg)
        {
            Entity target = FindSinglePlayer(recipient);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            CMD_sendprivatemessage(target, sender.Name, vararg);
            return Command.GetString("pm", "confirmation").Replace("<receiver>", target.Name);
        }
        private void CMD_Rage(Entity sender)
        {
            if (!CMDS_IsRekt(sender))
            {
                CMD_kick(sender, Command.GetString("rage", "kickmessage"));
                string message = Command.GetString("rage", "message");
                foreach (string name in Command.GetString("rage", "custommessagenames").Split(','))
                    if (sender.Name.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                        message = Command.GetString("rage", "message_" + name);
                WriteChatToAll(message.Format(new Dictionary<string, string>()
                {
                    { "<issuer>", sender.Name },
                    { "<issuerf>", sender.GetFormattedName(database) }
                }));
            }
        }
        private string CMD_Register(Entity sender) => xlr_database.CMD_Register(sender.GUID) ? Command.GetString("register", "message") : Command.WriteErrorTo(sender, "register", "error");
        private string CMD_Rek(Entity sender, string victim)
        {
            Entity target = FindSinglePlayer(victim);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            if (target.IsImmune(database))
                return Command.WriteErrorTo(sender, "TargetIsImmune");
            if (CMDS_IsRekt(target))
            {
                if (CMDS_GetBanTime(target) == DateTime.MinValue)
                    CMDS_AddToBanList(target, DateTime.MaxValue);
                return "";
            }
            CMDS_Rek(target);
            return Command.GetString("rek", "message").Format(new Dictionary<string, string>()
            {
                { "<issuer>", sender.Name },
                { "<issuerf>", sender.GetFormattedName(database) },
                { "<target>", target.Name },
                { "<targetf>", target.GetFormattedName(database) }
            });
        }
        private string CMD_Rektroll(Entity sender, string victim)
        {
            Entity target = FindSinglePlayer(victim);
            if (target == null)
                Command.WriteErrorTo(sender, "NotOnePlayerFound");
            if (target.IsImmune(database))
                return Command.WriteErrorTo(sender, "TargetIsImmune");
            if (CMDS_IsRekt(target))
                return "";
            CMDS_RekEffects(target);
            return Command.GetString("rek", "message").Format(new Dictionary<string, string>()
            {
                {"<issuer>", sender.Name },
                {"<issuerf>", sender.GetFormattedName(database) },
                {"<target>", target.Name },
                {"<targetf>", target.GetFormattedName(database) }
            });
        }
        private string CMD_Report(Entity sender, string vararg)
        {
            Dictionary<string, string> reportFormat = new Dictionary<string, string>()
            {
                { "<sender>", sender.Name },
                { "<senderf>", sender.GetFormattedName(database) },
                { "<message>", vararg }
            };
            using (StreamWriter w = File.AppendText(ConfigValues.ConfigPath + @"Commands\internal\ChatReports.txt"))
                w.WriteLine(Command.GetString("lastreports", "message").Format(reportFormat));
            CMD_sendadminmsg(Command.GetString("report", "message").Format(reportFormat));
            return "Reported.";
        }
        private void CMD_Res(Entity sender)
        {
            OnExitLevel();
            ExecuteCommand("fast_restart");
        }
        private string CMD_Resetwarns(Entity sender, string player, string optarg)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            CMD_resetwarns(target);
            return Command.GetString("resetwarns", "message").Format(new Dictionary<string, string>()
            {
                { "<target>", target.Name },
                { "<targetf>", target.GetFormattedName(database) },
                { "<issuer>", sender.Name },
                { "<issuerf>", sender.GetFormattedName(database) },
                { "<reason>", optarg }
            });
        }
        private void CMD_Rotate(Entity sender, string x, string y, string z, string optarg)
        {
            Vector3 rotationVector = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
            lastSpawnedParts[2] = lastSpawnedParts[2].ToVector3() + rotationVector + "";
            if (string.IsNullOrWhiteSpace(optarg))
                lastSpawned.Delete();
            WriteLog.Info("short line: " + string.Join("|", lastSpawnedParts));
            lastSpawned = ME_Spawn(lastSpawnedParts);
            WriteLog.Info("full line: " + string.Join("|", MapEditDefaults["+"]));
        }
        private string CMD_Rotatescreen(Entity sender, string players, string angle)
        {
            List<Entity> targets = FindSinglePlayerXFilter(players, sender);
            foreach (Entity target in targets)
            {
                Vector3 angles = target.GetPlayerAngles();
                if (!float.TryParse(angle, out angles.Z))
                    return Command.WriteErrorTo(sender, "rotatescreen", "usage");
                if (targets.Count == 1)
                    return Command.GetString("rotatescreen", "message").Format(new Dictionary<string, string>()
                    {
                        { "<player>", target.Name },
                        { "<roll>", angles.Z.ToString() }
                    });
            }
            return Command.GetMessage("Filters_message").Replace("<count>", targets.Count.ToString());
        }
        private string[] CMD_Rules(Entity sender) => ConfigValues.Cmd_rules.ToArray();
        private void CMD_Savegroups(Entity sender)
        {
            database.SaveGroups();
            WriteChatToAll(Command.GetString("savegroups", "message").Format(new Dictionary<string, string>()
            {
                { "<issuer>", sender.Name },
                { "<issuerf>", sender.GetFormattedName(database) }
            }));
            if (ConfigValues.Settings_enable_xlrstats)
            {
                xlr_database.Save();
                WriteChatToAll(Command.GetString("savegroups", "message_xlr").Format(new Dictionary<string, string>()
                {
                    { "<issuer>", sender.Name },
                    { "<issuerf>", sender.GetFormattedName(database) }
                }));
            }
        }
        private void CMD_Say(string vararg) => WriteChatToAll(vararg);
        private void CMD_Sayto(Entity sender, string player, string vararg)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                Command.WriteErrorTo(sender, "NotOnePlayerFound");
            else
                WriteChatToPlayer(target, vararg);
        }
        private void CMD_Scream(string vararg) => CMD_spammessagerainbow(vararg);
        private string CMD_Sdvar(Entity sender, string dvar, string optarg)
        {
            GSCFunctions.SetDvar(dvar, optarg);
            return Command.GetString("sdvar", "message").Format(new Dictionary<string, string>()
            {
                { "<key>", dvar },
                { "<value>", string.IsNullOrEmpty(optarg) ? "NULL" : optarg }
            });
        }
        private void CMD_Searchbans(Entity sender, string vararg)
        {
            PlayerInfo playerinfo = PlayerInfo.Parse(vararg);
            List<BanEntry> banlist;
            if (playerinfo.IsNull())
                banlist = CMD_SearchBanEntries(vararg);
            else
                banlist = CMD_SearchBanEntries(playerinfo);
            List<string> messages = new List<string>();
            foreach (BanEntry banentry in banlist)
                messages.Add(Command.GetString("searchbans", "message").Format(new Dictionary<string, string>()
                {
                    { "<banid>", banentry.BanId.ToString() },
                    { "<name>", banentry.Playername },
                    { "<guid>", banentry.playerinfo.GetGUIDString() },
                    { "<ip>", banentry.playerinfo.GetIPString() },
                    { "<hwid>", banentry.playerinfo.GetHWIDString() },
                    { "<time>", banentry.Until.Year == 9999 ? "^6PERMANENT" : banentry.Until.ToString("yyyy MMM d HH:mm") }
                }));
            WriteChatToPlayer(sender, Command.GetString("searchbans", "firstline"));
            if (messages.Count < 1)
                WriteChatToPlayer(sender, Command.GetMessage("NoEntriesFound"));
            else
                WriteChatToPlayerMultiline(sender, messages.ToArray());
        }
        private string CMD_Server(string vararg)
        {
            ExecuteCommand(vararg);
            return Command.GetString("server", "message").Replace("<command>", vararg);
        }
        private void CMD_Setafk(Entity sender, string player)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                Command.WriteErrorTo(sender, "NotOnePlayerFound");
            else if (target.IsSpectating())
                Command.WriteErrorTo(sender, "PlayerIsSpectating");
            else
                CMD_changeteam(target, "spectator");
        }
        private string CMD_Setfx(Entity sender, string fxlist, string optarg)
        {
            bool fx_valid = true;
            foreach (string s in fxlist.Split(','))
                fx_valid &= UTILS_ValidateFX(s);
            if (!fx_valid)
                return Command.WriteErrorTo(sender, "FX_not_found");
            if (!sender.HasField("CMD_SETFX"))
            {
                sender.SetField("CMD_SETFX", fxlist);
                string key = string.IsNullOrEmpty(optarg) ? "+activate" : optarg;
                sender.NotifyOnPlayerCommand("spawnfx", key);
                sender.OnNotify("spawnfx", ent =>
                {
                    using (StreamWriter file = File.AppendText(ConfigValues.ConfigPath + @"Commands\internal\setfx.txt"))
                    {
                        foreach (string s in sender.GetField<string>("CMD_SETFX").Split(','))
                        {
                            GSCFunctions.TriggerFX(GSCFunctions.SpawnFX(GSCFunctions.LoadFX(s), sender.Origin, new Vector3(0, 0, 1), new Vector3(0, 0, 0)));
                            WriteChatToPlayer(sender, Command.GetString("setfx", "spawned").Format(new Dictionary<string, string>()
                            {
                                { "<fx>", s },
                                { "<origin>", sender.Origin.ToString() }
                            }));
                            file.WriteLine("<fx> <x> <y> <z>".Format(new Dictionary<string, string>()
                            {
                                { "<fx>", s },
                                { "<x>", sender.Origin.X.ToString() },
                                { "<y>", sender.Origin.Y.ToString() },
                                { "<z>", sender.Origin.Z.ToString() }
                            }));
                        }
                    };
                });
                return Command.GetString("setfx", "enabled").Replace("<key>", @"[[{" + key + @"}]]");
            }
            else
            {
                sender.SetField("CMD_SETFX", fxlist);
                return Command.GetString("setfx", "changed").Replace("<fx>", fxlist);
            }
        }
        private string CMD_Setgroup(Entity sender, string player, string group)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            if (!target.SetGroup(group, database))
                return Command.WriteErrorTo(sender, "GroupNotFound");
            if (ConfigValues.Settings_groups_autosave)
                database.SaveGroups();
            return Command.GetString("setgroup", "message").Format(new Dictionary<string, string>()
            {
                {"<target>", target.Name },
                {"<targetf>", target.GetFormattedName(database) },
                {"<issuer>", sender.Name },
                {"<issuerf>", sender.GetFormattedName(database) },
                {"<rankname>", group.ToLowerInvariant() }
            });
        }
        private string CMD_Setteam(Entity sender, string player, string team)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            if (!Data.TeamNames.Contains(team))
                return Command.WriteErrorTo(sender, "InvalidTeamName");
            CMD_changeteam(target, team);
            return Command.GetString("setteam", "message").Format(new Dictionary<string, string>()
            {
                { "<issuer>", sender.Name },
                { "<issuerf>", sender.GetFormattedName(database) },
                { "<target>", target.Name },
                { "<targetf>", target.GetFormattedName(database) }
            });
        }
        private string CMD_Silentban(Entity sender, string player)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            if (target.IsImmune(database))
                return Command.WriteErrorTo(sender, "TargetIsImmune");
            CMDS_AddToBanList(target, DateTime.MaxValue);
            OnInterval(50, () =>
            {
                target.SetClientDvar("g_scriptMainMenu", "");
                return true;
            });
            return Command.GetString("silentban", "message").Format(new Dictionary<string, string>()
            {
                {"<issuer>", sender.Name },
                {"<issuerf>", sender.GetFormattedName(database) },
                {"<target>", target.Name },
                {"<targetf>", target.GetFormattedName(database) },
            });
        }
        private void CMD_Sound(Entity sender, string sound) => sender.PlaySound(sound);
        private void CMD_Spawn(Entity sender, string vararg)
        {
            lastSpawnedParts = vararg.Split('|').ToList();
            if (lastSpawnedParts.Count < 2)
                lastSpawnedParts.Add(sender.Origin + "");
            if (lastSpawnedParts[1] == "")
                lastSpawnedParts[1] = sender.Origin + "";
            WriteLog.Info("short line: " + string.Join("|", lastSpawnedParts));
            lastSpawned = ME_Spawn(lastSpawnedParts);
            WriteLog.Info("full line: " + string.Join("|", MapEditDefaults["+"]));
        }
        private string CMD_Speed(Entity sender, string speedString)
        {
            if (!float.TryParse(speedString, out float speed))
                speed = 1;
            foreach (Entity player in Players)
                CMD_SPEED(player, speed);
            return Command.GetString("speed", "message").Format(new Dictionary<string, string>()
            {
                { "<speed>", speedString.StartsWith("def") ? "default" : speed.ToString() },
                { "<issuer>", sender.Name },
                { "<issuerf>", sender.GetFormattedName(database) }
            });
        }
        private string CMD_Spy(Entity sender, string toggle)
        {
            bool enabled = UTILS_ParseBool(toggle);
            sender.SetSpying(enabled);
            return Command.GetString("spy", "message_" + (enabled ? "on" : "off"));
        }
        private void CMD_Suicide(Entity sender) => AfterDelay(100, () => sender.Suicide());
        private void CMD_Sunlight(Entity sender, string red, string green, string blue)
        {
            try
            {
                float r = Convert.ToSingle(red), g = Convert.ToSingle(green), b = Convert.ToSingle(blue);
                ConfigValues.Settings_sunlight = new float[3] { r, g, b };
                GSCFunctions.SetSunlight(new Vector3(r, g, b));
            }
            catch
            {
                Command.WriteErrorTo(sender, "sunlight", "usage");
            }
        }
        private void CMD_Svpassword(Entity sender, string optarg)
        {
            optarg = string.IsNullOrEmpty(optarg) ? "" : optarg;
            if (optarg.IndexOf('"') != -1)
            {
                WriteChatToPlayer(sender, "^1Error: Password has forbidden characters. Try another.");
                return;
            }
            if (!CFG_FindServerFile("server.cfg", out string path))
            {
                WriteChatToPlayer(sender, "^1Error: ^3" + path + "^1 not found.");
                return;
            }
            WriteChatToAll(@"^3" + sender.Name + " ^1executed ^3!svpassword");
            AfterDelay(2000, () =>
            {
                WriteChatToAllMultiline(new string[]
                {
                    "^1Server will be killed in:",
                    "^35",
                    "^34",
                    "^33",
                    "^32",
                    "^31",
                    "^30"
                }, 1000);
            });
            AfterDelay(8000, () =>
            {
                string password = "seta g_password \"" + optarg + "\"";
                List<string> lines = File.ReadAllLines(path).ToList();
                Regex regex = new Regex(@"seta g_password ""[^""]*""");

                bool found = false;
                for (int i = 0; i < lines.Count; i++)
                    if (regex.Matches(lines[i]).Count == 1)
                    {
                        found = true;
                        lines[i] = password;
                        break;
                    }
                if (!found)
                    lines.Add(password);
                File.WriteAllLines(path, lines.ToArray());
                foreach (Entity player in Players)
                    CMD_kick(player, "^3Server killed");
                AfterDelay(1000, () => Environment.Exit(-1));
            });
        }
        private void CMD_Teleport(Entity sender, string sources, string target)
        {
            Entity player2 = FindSinglePlayer(target);
            if (player2 == null)
                Command.WriteErrorTo(sender, "NotOnePlayerFound");
            else
                foreach (Entity player in FindSinglePlayerXFilter(sources, sender))
                {
                    player.SetOrigin(player2.Origin);
                    WriteChatToPlayer(sender, Command.GetString("teleport", "message").Format(new Dictionary<string, string>()
                    {
                        {"<player1>", player.Name },
                        {"<player2>", player2.Name }
                    }));
                }
        }
        private string CMD_Time() => string.Format(Command.GetString("time", "message"), DateTime.Now);
        private string CMD_Tmpban(Entity sender, string player, string optarg)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            if (target.IsImmune(database))
                return Command.WriteErrorTo(sender, "TargetIsImmune");
            if (string.IsNullOrEmpty(optarg))
                CMD_tmpban(target);
            else
                CMD_tmpban(target, optarg);
            return Command.GetString("tmpban", "message").Format(new Dictionary<string, string>()
            {
                { "<target>", target.Name },
                { "<targetf>", target.GetFormattedName(database) },
                { "<issuer>", sender.Name },
                { "<issuerf>", sender.GetFormattedName(database) },
                { "<reason>", optarg }
            });
        }
        private string CMD_Tmpbantime(Entity sender, string minutesString, string player, string optarg)
        {
            if (!int.TryParse(minutesString, out int minutes))
                return Command.WriteErrorTo(sender, "InvalidTimeSpan");
            TimeSpan duration = new TimeSpan(0, minutes, 0);
            if (duration.TotalHours > 24)
                duration = new TimeSpan(24, 0, 0);
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            if (target.IsImmune(database))
                return Command.WriteErrorTo(sender, "TargetIsImmune");
            if (string.IsNullOrEmpty(optarg))
                CMD_tmpbantime(target, DateTime.Now.Add(duration));
            else
                CMD_tmpbantime(target, DateTime.Now.Add(duration), optarg);
            return Command.GetString("tmpbantime", "message").Format(new Dictionary<string, string>()
            {
                { "<target>", target.Name },
                { "<targetf>", target.GetFormattedName(database) },
                { "<issuer>", sender.Name },
                { "<issuerf>", sender.GetFormattedName(database) },
                { "<reason>", optarg },
                { "<timespan>", duration.ToString() }
            });
        }
        private void CMD_Translate(Entity sender, string x, string y, string z, string optarg)
        {
            Vector3 translationVector = new Vector3(float.Parse(x), float.Parse(y), float.Parse(z));
            lastSpawnedParts[1] = lastSpawnedParts[1].ToVector3() + translationVector + "";
            if (optarg == "")
                lastSpawned.Delete();
            WriteLog.Info("short line: " + string.Join("|", lastSpawnedParts));
            lastSpawned = ME_Spawn(lastSpawnedParts);
            WriteLog.Info("full line: " + string.Join("|", MapEditDefaults["+"]));
        }
        private string CMD_Unban(Entity sender, string name)
        {
            List<BanEntry> entries = CMD_SearchBanEntries(name);
            if (entries.Count == 0)
                return Command.WriteErrorTo(sender, "NoEntriesFound");
            if (entries.Count > 1)
                return Command.WriteErrorTo(sender, "unban", "multiple_entries_found");
            CMD_unban(entries[0].BanId);
            return Command.GetString("unban", "message").Format(new Dictionary<string, string>()
            {
                {"<banid>", entries[0].BanId.ToString() },
                {"<name>",  entries[0].Playername },
                {"<guid>",  entries[0].playerinfo.GetGUIDString() },
                {"<hwid>",  entries[0].playerinfo.GetHWIDString() },
                {"<time>",  entries[0].Until.Year == 9999 ? "^6PERMANENT" : entries[0].Until.ToString("yyyy MMM d HH:mm") }
            });
        }
        private string CMD_UnbanId(Entity sender, string id)
        {
            BanEntry entry;
            if (!int.TryParse(id, out int bannumber))
                return Command.WriteErrorTo(sender, "InvalidNumber");
            if ((entry = CMD_unban(bannumber)) == null)
                return Command.WriteErrorTo(sender, "DefaultError");
            return Command.GetString("unban-id", "message").Format(new Dictionary<string, string>()
            {
                {"<banid>", entry.BanId.ToString() },
                {"<name>",  entry.Playername },
                {"<guid>",  entry.playerinfo.GetGUIDString() },
                {"<hwid>",  entry.playerinfo.GetHWIDString() },
                {"<time>",  entry.Until.Year == 9999 ? "^6PERMANENT" : entry.Until.ToString("yyyy MMM d HH:mm") }
            });
        }
        private void CMD_Unfreeze(Entity sender, string playerXFilter)
        {
            foreach (Entity target in FindSinglePlayerXFilter(playerXFilter, sender))
            {
                target.SetField("frozenbycommand", false);
                target.FreezeControls(false);
                WriteChatToAll(Command.GetString("unfreeze", "message").Format(new Dictionary<string, string>() {
                    {"<issuer>", sender.Name },
                    {"<issuerf>", sender.GetFormattedName(database) },
                    {"<target>", target.Name },
                    {"<targetf>", target.GetFormattedName(database) },
                }));
            }
        }
        private string CMD_Unimmune(Entity sender, string player)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            target.SetImmune(false, database);
            if (ConfigValues.Settings_groups_autosave)
                database.SaveGroups();
            return Command.GetString("unimmune", "message").Format(new Dictionary<string, string>()
            {
                {"<target>", target.Name },
                {"<targetf>", target.GetFormattedName(database) },
                {"<issuer>", sender.Name },
                {"<issuerf>", sender.GetFormattedName(database) },
            });
        }
        private string CMD_Unlimitedammo(Entity sender, string toggle)
        {
            if (toggle == "auto")
            {
                GSCFunctions.SetDvar("unlimited_ammo", "0");
                AfterDelay(200, () =>
                {
                    GSCFunctions.SetDvar("unlimited_ammo", "2");
                    UTILS_UnlimitedAmmo();
                });
            }
            else if (UTILS_ParseBool(toggle))
            {
                GSCFunctions.SetDvar("unlimited_ammo", "1");
                UTILS_UnlimitedAmmo(true);
            }
            else
                GSCFunctions.SetDvar("unlimited_ammo", "0");
            return Command.GetString("unlimitedammo", toggle == "auto" ? "message_auto" : UTILS_ParseBool(toggle) ? "message_on" : "message_off").Format(new Dictionary<string, string>()
            {
                {"<issuer>", sender.Name},
                {"<issuerf>", sender.GetFormattedName(database)}
            });
        }
        private void CMD_Unmute(Entity sender, string playerXFilter)
        {
            foreach (Entity target in FindSinglePlayerXFilter(playerXFilter, sender))
            {
                target.SetMuted(false);
                WriteChatToAll(Command.GetString("unmute", "message").Format(new Dictionary<string, string>()
                {
                    {"<issuer>", sender.Name },
                    {"<issuerf>", sender.GetFormattedName(database) },
                    {"<target>", target.Name },
                    {"<targetf>", target.GetFormattedName(database) },
                }));
            }
        }
        private string CMD_Unwarn(Entity sender, string player, string optarg)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            return Command.GetString("unwarn", "message").Format(new Dictionary<string, string>()
            {
                {"<target>", target.Name },
                {"<targetf>", target.GetFormattedName(database) },
                {"<issuer>", sender.Name },
                {"<issuerf>", sender.GetFormattedName(database) },
                {"<reason>", optarg },
                {"<warncount>", CMD_unwarn(target).ToString() },
                {"<maxwarns>", ConfigValues.Settings_warn_maxwarns.ToString() },
            });
        }
        private string CMD_Version() => "^3DHAdmin ^1" + ConfigValues.Version + "^1. ^3Do !credits for detailed info.";
        private string CMD_Votecancel(Entity sender)
        {
            if (voting.isActive())
            {
                voting.Abort();
                return Command.GetString("votecancel", "message").Format(new Dictionary<string, string>()
                {
                    {"<issuer>", sender.Name},
                    {"<issuerf>", sender.GetFormattedName(database)}
                });
            }
            else
                return Command.WriteErrorTo(sender, "votecancel", "error");
        }
        private void CMD_Votekick(Entity sender, string player, string optarg)
        {
            if (voting.isActive())
                Command.WriteErrorTo(sender, "votekick", "error4");
            else
            {
                Entity target = FindSinglePlayer(player);
                if (target == null)
                    Command.WriteErrorTo(sender, "NotOnePlayerFound");
                else if (target.IsImmune(database))
                    Command.WriteErrorTo(sender, "TargetIsImmune");
                else
                {
                    WriteChatToAll(Command.GetString("votekick", "message1").Format(new Dictionary<string, string>()
                    {
                        { "<issuer>", sender.Name },
                        { "<player>", target.Name },
                        { "<reason>", optarg }
                    }));
                    string message2 = Command.GetString("votekick", "message2");
                    if (!string.IsNullOrEmpty(message2))
                        WriteChatToAll(message2);
                    voting.Start(sender, target, optarg);
                }
            }
        }
        private string CMD_Warn(Entity sender, string player, string optarg)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            if (target.IsImmune(database))
                return Command.WriteErrorTo(sender, "TargetIsImmune");
            int warns = CMD_addwarn(target);
            target.IPrintLnBold(Command.GetMessage("YouHaveBeenWarned"));
            if (warns >= ConfigValues.Settings_warn_maxwarns)
            {
                CMD_resetwarns(target);
                if (string.IsNullOrEmpty(optarg))
                    CMD_tmpban(target);
                else
                    CMD_tmpban(target, optarg);
            }
            return Command.GetString("warn", "message").Format(new Dictionary<string, string>()
            {
                { "<target>", target.Name },
                { "<targetf>", target.GetFormattedName(database) },
                { "<issuer>", sender.Name },
                { "<issuerf>", sender.GetFormattedName(database) },
                { "<reason>", optarg },
                { "<warncount>", warns.ToString() },
                { "<maxwarns>", ConfigValues.Settings_warn_maxwarns.ToString() }
            });
        }
        private void CMD_Weapon(Entity sender, string playerxfilter, string weapon, string optarg)
        {
            List<Entity> targets = FindSinglePlayerXFilter(playerxfilter, sender);
            bool takeWeapons = false;
            if (!string.IsNullOrWhiteSpace(optarg))
                if (optarg == "-t")
                    takeWeapons = true;
                else
                {
                    Command.WriteErrorTo(sender, "weapon", "usage");
                    return;
                }
            foreach (Entity target in targets)
                if (target.IsAlive)
                {
                    string orig_weapon = target.CurrentWeapon;
                    if (takeWeapons)
                        target.TakeAllWeapons();
                    AfterDelay(50, () =>
                    {
                        target.GiveWeapon(weapon);
                        AfterDelay(50, () =>
                        {
                            target.SwitchToWeaponImmediate(weapon);
                            AfterDelay(1000, () =>
                            {
                                if ((target.CurrentWeapon == "none" && takeWeapons) || (target.CurrentWeapon == orig_weapon && !takeWeapons))
                                {
                                    if (takeWeapons)
                                    {
                                        target.GiveWeapon(orig_weapon);
                                        AfterDelay(50, () => target.SwitchToWeaponImmediate(orig_weapon));
                                    }
                                    if (targets.Count == 1)
                                        WriteChatToPlayer(sender, Command.GetString("weapon", "error").Replace("<weapon>", weapon));
                                }
                                else
                                {
                                    new Weapon(target.CurrentWeapon).Allow();
                                    CMD_GiveMaxAmmo(sender);
                                    if (targets.Count == 1)
                                        WriteChatToPlayer(sender, Command.GetString("weapon", "message").Format(new Dictionary<string, string>()
                                        {
                                            { "<weapon>", target.CurrentWeapon },
                                            { "<player>", target.Name }
                                        }));
                                }
                            });
                        });
                    });
                }
                else
                    WriteChatToPlayer(sender, Command.GetString("weapon", "error1").Replace("<player>", target.Name));
            if (targets.Count > 1)
                WriteChatToPlayer(sender, Command.GetMessage("Filters_message").Replace("<count>", targets.Count.ToString()));
        }
        private void CMD_Whois(Entity sender, string player)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                Command.WriteErrorTo(sender, "NotOnePlayerFound");
            else
            {
                WriteChatToPlayer(sender, Command.GetString("whois", "firstline").Format(new Dictionary<string, string>()
                {
                    {"<target>", target.Name },
                    {"<targetf>", target.GetFormattedName(database) },
                }));
                WriteChatToPlayerCondensed(sender, CMD_getallknownnames(target), 500, 50, Command.GetString("whois", "separator"));
            }
        }
        private string CMD_Xban(Entity sender, string player, string optarg)
        {
            Entity target = FindSinglePlayer(player);
            if (target == null)
                return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            if (target.IsImmune(database))
                return Command.WriteErrorTo(sender, "TargetIsImmune");
            CMDS_AddToXBanList(target);
            CMD_kick(target, optarg);
            return Command.GetString("xban", "message").Format(new Dictionary<string, string>()
            {
                { "<issuer>", sender.Name },
                { "<issuerf>", sender.GetFormattedName(database) },
                { "<target>", target.Name },
                { "<targetf>", target.GetFormattedName(database) },
                { "<reason>", optarg }
            });
        }
        private string CMD_Xlrstats(Entity sender, string optarg)
        {
            Entity target;
            if (string.IsNullOrEmpty(optarg))
                target = sender;
            else
            {
                target = FindSinglePlayer(optarg);
                if (target == null)
                    return Command.WriteErrorTo(sender, "NotOnePlayerFound");
            }
            if (xlr_database.xlr_players.ContainsKey(target.GUID))
            {
                XLR_database.XLREntry xlr_entry = xlr_database.xlr_players[target.GUID];
                return Command.GetString("xlrstats", "message").Format(new Dictionary<string, string>()
                {
                    { "<score>", xlr_entry.score.ToString() },
                    { "<kills>", xlr_entry.kills.ToString() },
                    { "<deaths>", xlr_entry.deaths.ToString() },
                    { "<kd>", xlr_database.Math_kd(xlr_entry).ToString() },
                    { "<headshots>", xlr_entry.headshots.ToString() },
                    { "<tk_kills>", xlr_entry.tk_kills.ToString() },
                    { "<precision>", (xlr_database.Math_precision(xlr_entry) * 100).ToString() },
                });
            }
            else
                return Command.WriteErrorTo(sender, "xlrstats", "error");
        }
        private string[] CMD_Xlrtop(Entity sender, string optarg)
        {
            int amount = 4;
            if (!string.IsNullOrEmpty(optarg) && !int.TryParse(optarg, out amount))
            {
                Command.WriteErrorTo(sender, "xlrtop", "usage");
                return new string[0];
            }
            List<KeyValuePair<long, XLR_database.XLREntry>> topscores = xlr_database.CMD_XLRTOP(amount);
            if (topscores.Count == 0)
            {
                Command.WriteErrorTo(sender, "xlrtop", "error");
                return new string[0];
            }
            List<string> output = new List<string>();
            for (int i = 0; i < topscores.Count; i++)
            {
                XLR_database.XLREntry entry = topscores[i].Value;
                output.Add(Command.GetString("xlrtop", "message").Format(new Dictionary<string, string>()
                {
                            {"<place>",(topscores.Count - i).ToString()},
                            {"<player>", UTILS_ResolveGUID(topscores[i].Key)},
                            {"<score>",entry.score.ToString()},
                            {"<kills>",entry.kills.ToString()},
                            {"<kd>", xlr_database.Math_kd(entry).ToString()},
                            {"<precision>",(xlr_database.Math_precision(entry)*100).ToString()}
                }));
            }
            return output.ToArray();
        }
        private void CMD_Yell(Entity sender, string playerxfilter, string vararg)
        {
            List<Entity> targets = FindSinglePlayerXFilter(playerxfilter, sender);
            foreach (Entity target in targets)
                target.IPrintLnBold(vararg);
            if (targets.Count > 1)
                WriteChatToPlayer(sender, Command.GetMessage("Filters_message").Replace("<count>", targets.Count.ToString()));
        }
        private string CMD_Yes(Entity sender)
        {
            string command = sender.GetField<string>("CurrentCommand");
            if (string.IsNullOrEmpty(command))
                if (voting.isActive())
                    if (voting.PositiveVote(sender))
                        return Command.GetString("yes", "message").Replace("<player>", sender.Name);
                    else if (sender == voting.issuer || sender == voting.target)
                        Command.WriteErrorTo(sender, "votekick", "error6");
                    else
                        Command.WriteErrorTo(sender, "votekick", "error5");
                else
                    WriteChatToPlayer(sender, "^3Warning: Command buffer is empty.");
            else
                ProcessCommand(sender, sender, command);
            return "";
        }
        private void CMD_Status(Entity sender, string optarg)
        {
            List<Entity> players = string.IsNullOrEmpty(optarg) ? Players : FindPlayers(optarg, sender);
            string[] statusstrings = (from player in players
                                     select Command.GetString("status", "formatting").Format(new Dictionary<string, string>()
                                     {
                                        { "<namef>", player.GetFormattedName(database) },
                                        { "<name>", player.Name },
                                        { "<rankname>", player.GetGroup(database).group_name },
                                        { "<shortrank>", player.GetGroup(database).short_name },
                                        { "<id>", player.GetEntityNumber().ToString() }
                                     })).ToArray();
            WriteChatToPlayer(sender, Command.GetString("status", "firstline"));
            WriteChatToPlayerMultiline(sender, statusstrings);
        }
#if DEBUG
        private void CMD_Aloc(Entity sender, string iconString, string x, string y)
        {
            HudElem icon = HudElem.CreateIcon(sender, iconString, 32, 32);
            icon.SetPoint("CENTER", "CENTER", int.Parse(x), int.Parse(y));
            icon.HideWhenInMenu = false;
            icon.HideWhenDead = false;
            icon.Alpha = 1;
            icon.Archived = true;
            icon.Sort = 20;
        }
        private void CMD_Entityinfo()
        {
            Log.Info("[EntityInfo]:: Check Data...");
            if (!Directory.Exists("scripts\\EntityInfo"))
            {
                Directory.CreateDirectory("scripts\\EntityInfo");
                Log.Info("[EntityInfo]:: Creating Data...");
                EntityInit();
            }
            else
            {
                Log.Info("[EntityInfo]:: Data Found..."); Log.Info("[EntityInfo]:: Loading Data...");
                EntityInit();
            }
            void EntityInit()
            {
                StreamWriter entinfo = new StreamWriter("scripts\\EntityInfo\\" + GSCFunctions.GetDvar("mapname") + ".txt", true);
                int modelcount = 0; int collisioncount = 0; int entitycount = 0;

                entinfo.WriteLine("-----------------------------------------------");
                entinfo.WriteLine("-                Entity Info                  -");
                entinfo.WriteLine("-----------------------------------------------");
                entinfo.WriteLine("");
                entinfo.WriteLine("-----------------------------------------------");
                entinfo.WriteLine("//#" + GSCFunctions.GetDvar("mapname"));
                entinfo.WriteLine("-----------------------------------------------");
                for (int i = 0; i < 2048; i++)
                {
                    Entity entity = Entity.GetEntity(i);
                    if (entity != null && !entity.IsPlayer)
                    {
                        entinfo.WriteLine("Entnum: " + GSCFunctions.GetEntityNumber(entity).ToString());
                        entinfo.WriteLine("Enttargetname: " + entity.TargetName);
                        entinfo.WriteLine("Enttarget: " + entity.Target);
                        entinfo.WriteLine("Entmodel: " + entity.Model);
                        entinfo.WriteLine("Entorigin: " + entity.Origin);
                        entinfo.WriteLine("Entangles: " + entity.Angles);
                        entinfo.WriteLine("Enttarget: " + entity.Classname);
                        entinfo.WriteLine("Enttarget: " + entity.Code_Classname);
                        entinfo.WriteLine("");
                        entitycount++;
                        if (entity.Classname == "script_brushmodel")
                            collisioncount++;
                        if (entity.Model != null && entity.Model != "")
                            modelcount++;
                    }
                }
                entinfo.WriteLine("-----------------------------------------------");
                entinfo.WriteLine("Collision_Count:: " + collisioncount.ToString());
                entinfo.WriteLine("Model_Count:: " + modelcount.ToString());
                entinfo.WriteLine("Entity_Count:: " + entitycount.ToString());
                entinfo.WriteLine("-----------------------------------------------");
                entinfo.Close();
            }
        }
        private void CMD_Openmenu(Entity sender, string menu) => sender.OpenMenu(menu);
        private void CMD_Setviewmodel(Entity sender, string viewmodel) => sender.SetViewModel(viewmodel);
        private void CMD_Shellshock(Entity sender, string shell, string shocks)
        {
            GSCFunctions.PreCacheShellShock(shell);
            sender.ShellShock(shell, float.Parse(shocks));
        }
        private void CMD_Vision(Entity sender, string vision) => sender.VisionSetNakedForPlayer(vision);
#endif
        private void CMDS_InitCommands()
        {
            WriteLog.Info("Initialising commands...");
            CommandList = new List<Command>()
            {
                new Command("3rdperson", (s, a, o) => CMD_3rdperson(s)),
                new Command("ac130", (s, a, o) => CMD_Ac130(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("achievements", (s, a, o) => CMD_Achievements(s)),
                new Command("addimmune", (s, a, o) => CMD_Addimmune(s, a[0]), 1),
                new Command("admins", (s, a, o) => CMD_Admins(s)),
                new Command("afk", (s, a, o) => CMD_Afk(s)),
                new Command("alias", (s, a, o) => CMD_Alias(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("amsg", (s, a, o) => CMD_Amsg(s, o), 0, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("apply", (s, a, o) => CMD_Apply(s)),
                new Command("balance", (s, a, o) => CMD_Balance(s)),
                new Command("ban", (s, a, o) => CMD_Ban(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("betterbalance", (s, a, o) => CMD_Betterbalance(s, a[0]), 1),
                new Command("cdvar", (s, a, o) => CMD_Cdvar(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("changeteam", (s, a, o) => CMD_Changeteam(s, a[0]), 1),
                new Command("clankick", (s, a, o) => CMD_Clankick(s, a[0]), 1),
                new Command("clantag", (s, a, o) => CMD_Clantag(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("clanvsall", (s, a, o) => CMD_Clanvsall(s, o), 0, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("clanvsallspectate", (s, a, o) => CMD_Clanvsallspectate(s, o), 0, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("cleartmpbanlist", (s, a, o) => CMD_Cleartmpbanlist()),
                new Command("credits", (s, a, o) => CMD_Credits(), (s, ms, b) => WriteChatMultiline(s, ms, b, 1500)),
                new Command("daytime", (s, a, o) => CMD_Daytime(s, a[0]), 1),
                new Command("dbsearch", (s, a, o) => CMD_Dbsearch(s, a[0]), 1),
                new Command("drunk", (s, a, o) => CMD_Drunk(s)),
                new Command("dsrnames", (s, a, o) => CMD_Dsrnames(s)),
                new Command("end", (s, a, o) => CMD_End(s)),
                new Command("fakesay", (s, a, o) => CMD_Fakesay(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("fc", (s, a, o) => CMD_Fc(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("fire", (s, a, o) => CMD_Fire(s)),
                new Command("fixplayergroup", (s, a, o) => CMD_Fixplayergroup(s, a[0]), 1),
                new Command("fly", (s, a, o) => CMD_Fly(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("foreach", (s, a, o) => CMD_Foreach(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("freeze", (s, a, o) => CMD_Freeze(s, a[0]), 1),
                new Command("frfc", (s, a, o) => CMD_Frfc(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("ft", (s, a, o) => CMD_Ft(s, a[0]), 1),
                new Command("fx", (s, a, o) => CMD_Fx(s, a[0]), 1),
                new Command("ga", (s, a, o) => CMD_Ga(s)),
                new Command("gametype", (s, a, o) => CMD_Gametype(s, a[0], a[1]), 2),
                new Command("getplayerinfo", (s, a, o) => CMD_Getplayerinfo(s, a[0]), 1),
                new Command("getwarns", (s, a, o) => CMD_Getwarns(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("guid", (s, a, o) => CMD_Guid(s)),
                new Command("gravity", (s, a, o) => CMD_Gravity(s, a[0]), 1),
                new Command("help", (s, a, o) => CMD_Help(s, o), 0, Command.Behaviour.HasOptionalArguments),
                new Command("hidebombicon", (s, a, o) => CMD_Hidebombicon(s)),
                new Command("hwid", (s, a, o) => CMD_Hwid(s)),
                new Command("jump", (s, a, o) => CMD_Jump(s, a[0]), 1),
                new Command("kd", (s, a, o) => CMD_Kd(s, a[0], a[1], a[2]), 3),
                new Command("kick", (s, a, o) => CMD_Kick(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("kickhacker", (s, a, o) => CMD_Kickhacker(s, o), 0, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("kill", (s, a, o) => CMD_Kill(s, a[0]), 1),
                new Command("knife", (s, a, o) => CMD_Knife(s, a[0]), 1),
                new Command("lastbans", (s, a, o) => CMD_Lastbans(s, o), 0, Command.Behaviour.HasOptionalArguments),
                new Command("lastreports", (s, a, o) => CMD_Lastreports(s, o), (s, ms, b) => WriteChatMultiline(s, ms, b), 0, Command.Behaviour.HasOptionalArguments),
                new Command("letmehardscope", (s, a, o) => CMD_Letmehardscope(s, a[0]), 1),
                new Command("loadgroups", (s, a, o) => CMD_Loadgroups()),
                new Command("lockserver", (s, a, o) => CMD_Lockserver(s, o), 0, Command.Behaviour.HasOptionalArguments | Command.Behaviour.MustBeConfirmed),
                new Command("login", (s, a, o) => CMD_Login(s, a[0]), 1),
                new Command("map", (s, a, o) => CMD_Map(s, a[0]), 1),
                new Command("maps", (s, a, o) => CMD_Maps(s)),
                new Command("mode", (s, a, o) => CMD_Mode(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("mute", (s, a, o) => CMD_Mute(s, a[0]), 1),
                new Command("myalias", (s, a, o) => CMD_Myalias(s, o), 0, Command.Behaviour.HasOptionalArguments),
                new Command("night", (s, a, o) => CMD_Night(s, a[0]), 1),
                new Command("no", (s, a, o) => CMD_No(s)),
                new Command("nootnoot", (s, a, o) => CMD_Nootnoot(s, a[0]), 1),
                new Command("playfx", (s, a, o) => CMD_Playfx(s, a[0]), 1),
                new Command("playfxontag", (s, a, o) => CMD_Playfxontag(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("pm", (s, a, o) => CMD_Pm(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("rage", (s, a, o) => CMD_Rage(s)),
                new Command("register", (s, a, o) => CMD_Register(s)),
                new Command("rek", (s, a, o) => CMD_Rek(s, a[0]), 1),
                new Command("rektroll", (s, a, o) => CMD_Rektroll(s, a[0]), 1),
                new Command("report", (s, a, o) => CMD_Report(s, o), 0, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("res", (s, a, o) => CMD_Res(s)),
                new Command("resetwarns", (s, a, o) => CMD_Resetwarns(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("rotate", (s, a, o) => CMD_Rotate(s, a[0], a[1], a[2], o), 3, Command.Behaviour.HasOptionalArguments),
                new Command("rotatescreen", (s, a, o) => CMD_Rotatescreen(s, a[0], a[1]), 2),
                new Command("rules", (s, a, o) => CMD_Rules(s), (s, ms, b) => WriteChatMultiline(s, ms, b, 1000)),
                new Command("savegroups", (s, a, o) => CMD_Savegroups(s)),
                new Command("say", (s, a, o) => CMD_Say(o), 0, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("sayto", (s, a, o) => CMD_Sayto(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("scream", (s, a, o) => CMD_Scream(o), 0, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("sdvar", (s, a, o) => CMD_Sdvar(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("searchbans", (s, a, o) => CMD_Searchbans(s, o), 0, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("server", (s, a, o) => CMD_Server(o), 0, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("setafk", (s, a, o) => CMD_Setafk(s, a[0]), 1),
                new Command("setfx", (s, a, o) => CMD_Setfx(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("setgroup", (s, a, o) => CMD_Setgroup(s, a[0], a[1]), 2),
                new Command("setteam", (s, a, o) => CMD_Setteam(s, a[0], a[1]), 2),
                new Command("silentban", (s, a, o) => CMD_Silentban(s, a[0]), 1),
                new Command("sound", (s, a, o) => CMD_Sound(s, a[0]), 1),
                new Command("spawn", (s, a, o) => CMD_Spawn(s, o), 0, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("speed", (s, a, o) => CMD_Speed(s, a[0]), 1),
                new Command("spy", (s, a, o) => CMD_Spy(s, a[0]), 1),
                new Command("status", (s, a, o) => CMD_Status(s, o), 0, Command.Behaviour.HasOptionalArguments),
                new Command("suicide", (s, a, o) => CMD_Suicide(s)),
                new Command("sunlight", (s, a, o) => CMD_Sunlight(s, a[0], a[1], a[2]), 3),
                new Command("svpassword", (s, a, o) => CMD_Svpassword(s, o), 0, Command.Behaviour.HasOptionalArguments | Command.Behaviour.MustBeConfirmed),
                new Command("teleport", (s, a, o) => CMD_Teleport(s, a[0], a[1]), 2),
                new Command("time", (s, a, o) => CMD_Time()),
                new Command("tmpban", (s, a, o) => CMD_Tmpban(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("tmpbantime", (s, a, o) => CMD_Tmpbantime(s, a[0], a[1], o), 2, Command.Behaviour.HasOptionalArguments),
                new Command("translate", (s, a, o) => CMD_Translate(s, a[0], a[1], a[2], o), 3, Command.Behaviour.HasOptionalArguments),
                new Command("unban", (s, a, o) => CMD_Unban(s, a[0]), 1),
                new Command("unban-id", (s, a, o) => CMD_UnbanId(s, a[0]), 1),
                new Command("unfreeze", (s, a, o) => CMD_Unfreeze(s, a[0]), 1),
                new Command("unimmune", (s, a, o) => CMD_Unimmune(s, a[0]), 1),
                new Command("unlimitedammo", (s, a, o) => CMD_Unlimitedammo(s, a[0]), 1),
                new Command("unmute", (s, a, o) => CMD_Unmute(s, a[0]), 1),
                new Command("unwarn", (s, a, o) => CMD_Unwarn(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("version", (s, a, o) => CMD_Version()),
                new Command("votecancel", (s, a, o) => CMD_Votecancel(s)),
                new Command("votekick", (s, a, o) => CMD_Votekick(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("warn", (s, a, o) => CMD_Warn(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("weapon", (s, a, o) => CMD_Weapon(s, a[0], a[1], o), 2, Command.Behaviour.HasOptionalArguments),
                new Command("whois", (s, a, o) => CMD_Whois(s, a[0]), 1),
                new Command("xban", (s, a, o) => CMD_Xban(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments),
                new Command("yell", (s, a, o) => CMD_Yell(s, a[0], o), 1, Command.Behaviour.HasOptionalArguments | Command.Behaviour.OptionalIsRequired),
                new Command("yes", (s, a, o) => CMD_Yes(s)),
#if DEBUG
                new Command("aloc", (s, a, o) => CMD_Aloc(s, a[0], a[1], a[2]), 3),
                new Command("debug", (s, a, o) => CMD_Debug(s, a[0]), 1),
                new Command("entityinfo", (s, a, o) => CMD_Entityinfo()),
                new Command("openmenu", (s, a, o) => CMD_Openmenu(s, a[0]), 1),
                new Command("setviewmodel", (s, a, o) => CMD_Setviewmodel(s, a[0]), 1),
                new Command("shellshock", (s, a, o) => CMD_Shellshock(s, a[0], a[1]), 2),
                new Command("vision", (s, a, o) => CMD_Vision(s, a[0]), 1)
#endif
            };
            if (ConfigValues.Settings_enable_xlrstats)
            {
                CommandList.Add(new Command("xlrstats", (s, a, o) => CMD_Xlrstats(s, o), 0, Command.Behaviour.HasOptionalArguments));
                CommandList.Add(new Command("xlrtop", (s, a, o) => CMD_Xlrtop(s, o), (s, ms, b) => WriteChatMultiline(s, ms, b, 1500), 0, Command.Behaviour.HasOptionalArguments));
            }
            #endregion

            WriteLog.Info("Initialised commands.");
        }

        public void InitCommandAliases()
        {
            foreach (string line in File.ReadAllLines(ConfigValues.ConfigPath + @"Commands\commandaliases.txt"))
            {
                string[] parts = line.Split('=');
                CommandAliases.Add(parts[0], parts[1]);
            }
            WriteLog.Info("Initialised command aliases");
        }

        public void InitCDVars()
        {
            foreach (string line in File.ReadAllLines(ConfigValues.ConfigPath + @"Utils\cdvars.txt"))
            {
                string[] parts = line.Split('=');
                parts[0] = parts[0].ToLowerInvariant();
                CDvars.Add(parts[0], parts[1]);
            }
            if (File.Exists(ConfigValues.ConfigPath + @"Commands\internal\daytime.txt"))
            {
                try
                {
                    foreach (string line in File.ReadAllLines(ConfigValues.ConfigPath + @"Commands\internal\daytime.txt"))
                    {
                        string[] _line = line.Split('=');
                        switch (_line[0])
                        {
                            case "daytime":
                                ConfigValues.Settings_daytime = _line[1];
                                break;
                            case "sunlight":
                                string[] _sunlight = _line[1].Split(',');
                                ConfigValues.Settings_sunlight = new float[3] { Convert.ToSingle(_sunlight[0]), Convert.ToSingle(_sunlight[1]), Convert.ToSingle(_sunlight[2]) };
                                break;
                            default:
                                break;
                        }
                        GSCFunctions.SetSunlight(new Vector3(ConfigValues.Settings_sunlight[0], ConfigValues.Settings_sunlight[1], ConfigValues.Settings_sunlight[2]));
                    }
                }
                catch
                {
                    WriteLog.Error("Error loading \"Commands\\internal\\daytime.txt\"");
                }
            }
        }

        public void InitChatAlias()
        {
            foreach (string line in File.ReadAllLines(ConfigValues.ConfigPath + @"Utils\chatalias.txt"))
            {
                string[] parts = line.Split('=');
                for (int i = 2; i < parts.Length; i++)
                    parts[1] += "=" + parts[i];
                try
                {
                    ChatAlias.Add(Convert.ToInt64(parts[0]), parts[1]);
                }
                catch
                {
                    WriteLog.Error("Error reading chat alias entry: " + line);
                }
            }
            if (File.Exists(ConfigValues.ConfigPath + @"Utils\forced_clantags.txt"))
            {
                foreach (string line in File.ReadAllLines(ConfigValues.ConfigPath + @"Utils\forced_clantags.txt"))
                {
                    string[] parts = line.Split('=');
                    for (int i = 2; i < parts.Length; i++)
                        parts[1] += "=" + parts[i];
                    try
                    {
                        Forced_clantags.Add(Convert.ToInt64(parts[0]), parts[1]);
                    }
                    catch
                    {
                        WriteLog.Error("Error reading forced clantag entry: " + line);
                    }
                }
            }

            // clantag worker
            OnInterval(1000, () => {
                //iterate over connected players
                foreach(Entity player in Players)
                {
                    if (Forced_clantags.Keys.Contains(player.GUID))
                        player.ClanTag = Forced_clantags[player.GUID];
                }
                return true;
            });
        }
        #region ACTUAL COMMANDS

        public void CMD_kick(Entity target, string reason = "You have been kicked") => AfterDelay(100, () => ExecuteCommand("dropclient " + target.GetEntityNumber() + " \"" + reason + "\""));

        public void CMD_tmpban(Entity target, string reason = "You have been tmpbanned") => AfterDelay(100, () => ExecuteCommand("tempbanclient " + target.GetEntityNumber() + " \"" + reason + "\""));

        public void CMD_ban(Entity target, string reason = "You have been banned")
        {
            CMDS_AddToBanList(target, DateTime.MaxValue);
            CMD_kick(target, reason);
        }

        public void CMD_tmpbantime(Entity target, DateTime until, string reason = "You have been tmpbanned")
        {
            CMDS_AddToBanList(target, until);
            CMD_kick(target, reason);
        }

        public void CMD_sendprivatemessage(Entity target, string sendername, string message)
        {
            AfterDelay(100, () =>
            {
                WriteChatToPlayer(target, Command.GetString("pm", "message").Format(new Dictionary<string, string>()
                    {
                        {"<sender>", sendername },
                        {"<message>", message },
                    }));
            });
        }

        public void CMD_changemap(string devmapname)
        {
            OnExitLevel();
            ChangeMap(devmapname);
        }

        public int CMD_getwarns(Entity player)
        {
            List<string> lines = File.ReadAllLines(ConfigValues.ConfigPath + @"Commands\internal\warns.txt").ToList();
            string identifiers = player.GetInfo().GetIdentifiers();
            foreach (string line in lines)
            {
                string[] parts = line.Split(':');
                if (parts[0] == identifiers)
                    return int.Parse(parts[1]);
            }
            return 0;
        }

        public int CMD_addwarn(Entity player)
        {
            List<string> lines = File.ReadAllLines(ConfigValues.ConfigPath + @"Commands\internal\warns.txt").ToList();
            string identifiers = player.GetInfo().GetIdentifiers();
            for (int i = 0; i < lines.Count; i++)
            {
                string[] parts = lines[i].Split(':');
                if (parts[0] == identifiers)
                {
                    int warns = (int.Parse(parts[1]) + 1);
                    lines[i] = string.Format("{0}:{1}", parts[0], warns.ToString());
                    File.WriteAllLines(ConfigValues.ConfigPath + @"Commands\internal\warns.txt", lines);
                    return warns;
                }
            }
            lines.Add(string.Format("{0}:1", player.GetInfo().GetIdentifiers()));
            File.WriteAllLines(ConfigValues.ConfigPath + @"Commands\internal\warns.txt", lines);
            return 1;
        }

        public int CMD_unwarn(Entity player)
        {
            List<string> lines = File.ReadAllLines(ConfigValues.ConfigPath + @"Commands\internal\warns.txt").ToList();
            string identifiers = player.GetInfo().GetIdentifiers();
            for (int i = 0; i < lines.Count; i++)
            {
                string[] parts = lines[i].Split(':');
                if (parts[0] == identifiers)
                {
                    int warns = (int.Parse(parts[1]) - 1);
                    if (warns < 0)
                        warns = 0;
                    lines[i] = parts[0] + warns.ToString();
                    File.WriteAllLines(ConfigValues.ConfigPath + @"Commands\internal\warns.txt", lines);
                    return warns;
                }
            }
            return 0;

        }

        public void CMD_resetwarns(Entity player)
        {
            List<string> lines = File.ReadAllLines(ConfigValues.ConfigPath + @"Commands\internal\warns.txt").ToList();
            string identifiers = player.GetInfo().GetIdentifiers();
            for (int i = 0; i < lines.Count; i++)
            {
                string[] parts = lines[i].Split(':');
                if (parts[0] == identifiers)
                {
                    lines.Remove(lines[i]);
                    File.WriteAllLines(ConfigValues.ConfigPath + @"Commands\internal\warns.txt", lines);
                    return;
                }
            }
        }

        public void CMD_changeteam(Entity player, string team)
        {
            player.SetField("sessionteam", team);
            player.Notify("menuresponse", "team_marinesopfor", team);
        }

        public void CMD_clanvsall(List<string> identifiers, bool changespectators = false)
        {
            foreach (Entity player in Players)
                if (!player.IsSpectating() || changespectators)
                    foreach (string identifier in identifiers)
                    {
                        string tolowidentifier = identifier.ToLowerInvariant();
                        if (player.Name.ToLowerInvariant().Contains(tolowidentifier) || player.ClanTag.ToLowerInvariant().Contains(tolowidentifier))
                        {
                            if (player.GetTeam() == "axis")
                                CMD_changeteam(player, "allies");
                        }
                        else
                            CMD_changeteam(player, "axis");
                    }
        }

        public BanEntry CMD_unban(int id)
        {
            BanEntry entry = null;
            try
            {
                if (id < BanList.Count && id >= 0)
                {
                    string[] parts = BanList[id].Split(';');
                    string playername = string.Join(";", parts.Skip(2));
                    entry = new BanEntry(id, PlayerInfo.Parse(parts[1]), playername, DateTime.ParseExact(parts[0], "yyyy MMM d HH:mm", Culture));
                    BanList.Remove(BanList[id]);
                }
                CMDS_SaveBanList();
                return entry;
            }
            catch (Exception ex)
            {
                WriteLog.Error("Error while running unban command");
                WriteLog.Error(ex.Message);
                return null;
            }
        }

        public List<BanEntry> CMD_SearchBanEntries(string name)
        {
            List<BanEntry> foundentries = new List<BanEntry>();
            try
            {
                for (int i = 0; i < BanList.Count; i++)
                {
                    string[] parts = BanList[i].Split(';');
                    string playername = string.Join(";", parts.Skip(2));
                    if (playername.ToLowerInvariant().Contains(name.ToLowerInvariant()))
                        foundentries.Add(new BanEntry(i, PlayerInfo.Parse(parts[1]), playername, DateTime.ParseExact(parts[0], "yyyy MMM d HH:mm", Culture)));
                }
                return foundentries;
            }
            catch (Exception)
            {
                return new List<BanEntry>();
            }
        }

        public List<BanEntry> CMD_SearchBanEntries(PlayerInfo playerinfo)
        {
            List<BanEntry> foundentries = new List<BanEntry>();
            try
            {
                for (int i = 0; i < BanList.Count; i++)
                {
                    string[] parts = BanList[i].Split(';');
                    string playername = string.Join(";", parts.Skip(2));
                    if (PlayerInfo.Parse(parts[1]).MatchesOR(playerinfo))
                        foundentries.Add(new BanEntry(i, PlayerInfo.Parse(parts[1]), playername, DateTime.ParseExact(parts[0], "yyyy MMM d HH:mm", Culture)));
                }
                return foundentries;
            }
            catch (Exception)
            {
                return new List<BanEntry>();
            }
        }

        public List<BanEntry> CMD_GetLastBanEntries(int count)
        {
            List<BanEntry> foundentries = new List<BanEntry>();
            try
            {
                if (count > BanList.Count)
                    count = BanList.Count;
                if (count < 1)
                    count = 1;
                for (int i = BanList.Count - 1; i >= BanList.Count - count; i--)
                {
                    string[] parts = BanList[i].Split(';');
                    string playername = string.Join(";", parts.Skip(2));
                    foundentries.Add(new BanEntry(i, PlayerInfo.Parse(parts[1]), playername, DateTime.ParseExact(parts[0], "yyyy MMM d HH:mm", Culture)));
                }
                return foundentries;
            }
            catch (Exception)
            {
                return new List<BanEntry>();
            }
        }

        public string[] CMD_getallknownnames(Entity player)
        {
            if (File.Exists(string.Format(ConfigValues.ConfigPath + @"Utils\playerlogs\{0}.txt", player.GUID)))
                return File.ReadAllLines(string.Format(ConfigValues.ConfigPath + @"Utils\playerlogs\{0}.txt", player.GUID));
            return new string[0];
        }

        public static void CMDS_EndRound()
        {
            foreach (Entity player in Players)
                player.Notify("menuresponse", "menu", "endround");
        }

        public void CMD_sendadminmsg(string message)
        {
            foreach (Entity player in Players)
                if (player.HasPermission("receiveadminmsg", database))
                    WriteChatAdmToPlayer(player, message);
        }

        public void CMD_applyfilmtweak(Entity sender, string ft)
        {
            List<Dvar> dvars = new List<Dvar>();
            switch (ft)
            {
                case "0":
                    dvars.Add(new Dvar { key = "r_filmusetweaks",           value = "0" });
                    dvars.Add(new Dvar { key = "r_filmusetweaks",           value = "0" });
                    dvars.Add(new Dvar { key = "r_filmtweakenable",         value = "0" });
                    dvars.Add(new Dvar { key = "r_colorMap",                value = "1" });
                    dvars.Add(new Dvar { key = "r_specularMap",             value = "1" });
                    dvars.Add(new Dvar { key = "r_normalMap",               value = "1" });
                    break;
                case "1":
                    dvars.Add(new Dvar { key = "r_filmtweakdarktint",       value = "0.65 0.7 0.8"});
                    dvars.Add(new Dvar { key = "r_filmtweakcontrast",       value = "1.3"});
                    dvars.Add(new Dvar { key = "r_filmtweakbrightness",     value = "0.15"});
                    dvars.Add(new Dvar { key = "r_filmtweakdesaturation",   value = "0"});
                    dvars.Add(new Dvar { key = "r_filmusetweaks",           value = "1"});
                    dvars.Add(new Dvar { key = "r_filmtweaklighttint",      value = "1.8 1.8 1.8"});
                    dvars.Add(new Dvar { key = "r_filmtweakenable",         value = "1" });
                    break;
                case "2":
                    dvars.Add(new Dvar { key = "r_filmtweakdarktint",       value = "1.15 1.1 1.3"});
                    dvars.Add(new Dvar { key = "r_filmtweakcontrast",       value = "1.6"});
                    dvars.Add(new Dvar { key = "r_filmtweakbrightness",     value = "0.2"});
                    dvars.Add(new Dvar { key = "r_filmtweakdesaturation",   value = "0"});
                    dvars.Add(new Dvar { key = "r_filmusetweaks",           value = "1"});
                    dvars.Add(new Dvar { key = "r_filmtweaklighttint",      value = "1.35 1.3 1.25"});
                    dvars.Add(new Dvar { key = "r_filmtweakenable",         value = "1" });
                    break;
                case "3":
                    dvars.Add(new Dvar { key = "r_filmtweakdarktint",       value = "0.8 0.8 1.1"});
                    dvars.Add(new Dvar { key = "r_filmtweakcontrast",       value = "1.3"});
                    dvars.Add(new Dvar { key = "r_filmtweakbrightness",     value = "0.48"});
                    dvars.Add(new Dvar { key = "r_filmtweakdesaturation",   value = "0"});
                    dvars.Add(new Dvar { key = "r_filmusetweaks",           value = "1"});
                    dvars.Add(new Dvar { key = "r_filmtweaklighttint",      value = "1 1 1.4"});
                    dvars.Add(new Dvar { key = "r_filmtweakenable",         value = "1" });
                    break;
                case "4":
                    dvars.Add(new Dvar { key = "r_filmtweakdarktint",       value = "1.8 1.8 2"});
                    dvars.Add(new Dvar { key = "r_filmtweakcontrast",       value = "1.25"});
                    dvars.Add(new Dvar { key = "r_filmtweakbrightness",     value = "0.02"});
                    dvars.Add(new Dvar { key = "r_filmtweakdesaturation",   value = "0"});
                    dvars.Add(new Dvar { key = "r_filmusetweaks",           value = "1"});
                    dvars.Add(new Dvar { key = "r_filmtweaklighttint",      value = "0.8 0.8 1"});
                    dvars.Add(new Dvar { key = "r_filmtweakenable",         value = "1"});
                    break;
                case "5":
                    dvars.Add(new Dvar { key = "r_filmtweakdarktint",       value = "1 1 2"});
                    dvars.Add(new Dvar { key = "r_filmtweakcontrast",       value = "1.5"});
                    dvars.Add(new Dvar { key = "r_filmtweakbrightness",     value = "0.07"});
                    dvars.Add(new Dvar { key = "r_filmtweakdesaturation",   value = "0"});
                    dvars.Add(new Dvar { key = "r_filmusetweaks",           value = "1"});
                    dvars.Add(new Dvar { key = "r_filmtweaklighttint",      value = "1 1.2 1"});
                    dvars.Add(new Dvar { key = "r_filmtweakenable",         value = "1"});
                    break;
                case "6":
                    dvars.Add(new Dvar { key = "r_filmtweakdarktint",       value = "1.5 1.5 2"});
                    dvars.Add(new Dvar { key = "r_filmtweakcontrast",       value = "1"});
                    dvars.Add(new Dvar { key = "r_filmtweakbrightness",     value = "0.0.4"});
                    dvars.Add(new Dvar { key = "r_filmtweakdesaturation",   value = "0"});
                    dvars.Add(new Dvar { key = "r_filmusetweaks",           value = "1"});
                    dvars.Add(new Dvar { key = "r_filmtweaklighttint",      value = "1.5 1.5 1"});
                    dvars.Add(new Dvar { key = "r_filmtweakenable",         value = "1"});
                    break;
                case "7":
                    dvars.Add(new Dvar { key = "r_normalMap",               value = "0"});
                    break;
                case "8":
                    dvars.Add(new Dvar { key = "cg_drawFPS",                value = "1"});
                    dvars.Add(new Dvar { key = "cg_fovScale",               value = "1.5"});
                    break;
                case "9":
                    dvars.Add(new Dvar { key = "r_debugShader",             value = "1"});
                    break;
                case "10":
                    dvars.Add(new Dvar { key = "r_colorMap",                value = "3"});
                    break;
                case "11":
                    dvars.Add(new Dvar { key = "com_maxfps",                value = "0"});
                    dvars.Add(new Dvar { key = "con_maxfps",                value = "0"});
                    break;
                case "default":
                    dvars.Add(new Dvar { key = "r_filmtweakdarktint",       value = "0.7 0.85 1"});
                    dvars.Add(new Dvar { key = "r_filmtweakcontrast",       value = "1.4"});
                    dvars.Add(new Dvar { key = "r_filmtweakdesaturation",   value = "0.2"});
                    dvars.Add(new Dvar { key = "r_filmusetweaks",           value = "0"});
                    dvars.Add(new Dvar { key = "r_filmtweaklighttint",      value = "1.1 1.05 0.85"});
                    dvars.Add(new Dvar { key = "cg_scoreboardpingtext",     value = "1"});
                    dvars.Add(new Dvar { key = "waypointIconHeight",        value = "13"});
                    dvars.Add(new Dvar { key = "waypointIconWidth",         value = "13"});
                    dvars.Add(new Dvar { key = "cl_maxpackets",             value = "100"});
                    dvars.Add(new Dvar { key = "r_fog",                     value = "0"});
                    dvars.Add(new Dvar { key = "fx_drawclouds",             value = "0"});
                    dvars.Add(new Dvar { key = "r_distortion",              value = "0"});
                    dvars.Add(new Dvar { key = "r_dlightlimit",             value = "0"});
                    dvars.Add(new Dvar { key = "cg_brass",                  value = "0"});
                    dvars.Add(new Dvar { key = "snaps",                     value = "30"});
                    dvars.Add(new Dvar { key = "com_maxfps",                value = "100"});
                    dvars.Add(new Dvar { key = "clientsideeffects",         value = "0"});
                    dvars.Add(new Dvar { key = "r_filmTweakBrightness",     value = "0.2" });
                    dvars.Add(new Dvar { key = "cg_fovScale",               value = "1" });
                    break;
            }
            try
            {
                if (PersonalPlayerDvars.ContainsKey(sender.GUID))
                    if (ft == "0")
                        PersonalPlayerDvars[sender.GUID] = dvars;
                    else
                        PersonalPlayerDvars[sender.GUID] = UTILS_DvarListUnion(PersonalPlayerDvars[sender.GUID], dvars);
                else
                    PersonalPlayerDvars.Add(sender.GUID, dvars);
                UTILS_SetClientDvarsPacked(sender, dvars);
                if ((ft == "0") && PersonalPlayerDvars.ContainsKey(sender.GUID))
                    PersonalPlayerDvars.Remove(sender.GUID);
            }
            catch
            {
                WriteLog.Error("Exception at DHAdmin::CMD_applyfilmtweak");
            }
            
        }

        public void CMD_spammessagerainbow(string message, int times = 8, int delay = 500)
        {
            List<string> messages = new List<string>();
            string[] colors = Data.Colors.Keys.ToArray();
            for (int i = 0; i < times; i++)
                messages.Add(colors[i % Data.Colors.Keys.Count] + message);
            WriteChatToAllMultiline(messages.ToArray(), delay);
        }

        public unsafe void CMD_JUMP(float height)
        {
            *(float*)new IntPtr(7186184) = height;
        }

        public void CMD_SPEED(Entity player, float speed)
        {
            player.SetMoveSpeedScale(speed);
        }

        public unsafe void CMD_GRAVITY(int g)
        {
            *(int*)new IntPtr(4679878) = g;
        }

        public void CMD_AC130(Entity player, bool permanent)
        {
            AfterDelay(500, () => {
                player.TakeAllWeapons();
                player.GiveWeapon("ac130_105mm_mp");
                player.GiveWeapon("ac130_40mm_mp");
                player.GiveWeapon("ac130_25mm_mp");
                player.SwitchToWeaponImmediate("ac130_25mm_mp");           
            });

            if(permanent)
                player.SetField("CMD_AC130", new Parameter(1));
        }

        #endregion

        #region other useful crap

        public void CMDS_OnDisconnect(Entity player)
        {
            player.SetSpying(false);
            player.SetMuted(false);
            player.SetField("rekt", 0);
            if (voting.isActive())
                voting.OnPlayerDisconnect(player);
        }

        public void CMDS_OnPlayerSpawned(Entity player)
        {
            if (UTILS_GetFieldSafe<int>(player, "CMD_AC130") == 1)
                CMD_AC130(player, false);
        }

        public void CMDS_AddToBanList(Entity player, DateTime until)
        {
            BanList.Add
                    (
                    string.Format
                        (
                        "{0};{1};{2}",
                        until.ToString("yyyy MMM d HH:mm"),
                        player.GetInfo().GetIdentifiers(),
                        player.Name
                        )
                    );
            CMDS_SaveBanList();
        }

        public DateTime CMDS_GetBanTime(Entity player)
        {
            List<string> linescopy = BanList.Clone();
            foreach (string line in linescopy)
            {
                string[] parts = line.Split(';');
                string playername = string.Join(";", parts.Skip(2));
                if (parts.Length < 3)
                {
                    continue;
                }
                if (player.GetInfo().MatchesOR(PlayerInfo.Parse(parts[1])) || player.Name == playername)
                {
                    DateTime until = DateTime.ParseExact(parts[0], "yyyy MMM d HH:mm", Culture);
                    if (until < DateTime.Now)
                    {
                        BanList.Remove(line);
                    }
                    return until;
                }
            }
            return DateTime.MinValue;
        }

        public void CMDS_Rek(Entity player)
        {
            CMDS_AddToBanList(player, DateTime.MaxValue);
            CMDS_RekEffects(player);
        }

        public void CMDS_RekEffects(Entity player)
        {
            player.SetField("rekt", 1);
            OnInterval(50, () =>
            {
                player.SetClientDvar("g_scriptMainMenu", "");
                player.FreezeControls(true);
                try
                {
                    player.TakeAllWeapons();
                    player.DisableWeapons();
                    player.DisableOffhandWeapons();
                    player.DisableWeaponSwitch();
                }
                catch (Exception ex)
                {
                    try
                    {
                        HaxLog.WriteInfo("----STARTREPORT----");
                        HaxLog.WriteInfo("Failed Method call while reking");
                        HaxLog.WriteInfo(ex.Message);
                        HaxLog.WriteInfo(ex.StackTrace);
                    }
                    finally
                    {
                        HaxLog.WriteInfo("----ENDREPORT---");
                    }
                }
                player.IPrintLnBold("^1YOU'RE REKT");
                player.IPrintLn("^1YOU'RE REKT");
                Utilities.RawSayTo(player, "^1YOU'RE REKT");
                player.SetClientDvar("r_colorMap", "3");
                return true;
            });
        }

        public bool CMDS_IsRekt(Entity player)
        {
            return player.HasField("rekt") && player.GetField<int>("rekt") == 1;
        }

        public void CMDS_OnConnect(Entity player)
        {
            WriteLog.Debug("CMDS_OnConnect");
            DateTime until = CMDS_GetBanTime(player);
            if (until > DateTime.Now)
            {
                AfterDelay(1000, () =>
                {
                    if (until.Year != 9999)
                    {
                        TimeSpan forhowlong = until - DateTime.Now;
                        ExecuteCommand(string.Format("dropclient {0} \"^1You are banned from this server for ^3{1}d {2}h {3}m\"", player.GetEntityNumber(), forhowlong.Days, forhowlong.Hours, forhowlong.Minutes));
                    }
                    else
                        ExecuteCommand(string.Format("dropclient {0} \"^1You are banned from this server ^3permanently.\"", player.GetEntityNumber()));
                });
            }

            if (ConfigValues.LockServer && !LockServer_Whitelist.Contains(player.GUID))
            {
                AfterDelay(1000, () =>
                {
                    string reason = File.ReadAllText(ConfigValues.ConfigPath + @"Utils\internal\LOCKSERVER");
                    ExecuteCommand(string.Format("dropclient {0} \"^3Server is protected!{1}\"", player.GetEntityNumber(), string.IsNullOrEmpty(reason) ? "" : " ^7Reason: ^1" + reason));
                });
            }

            if (!player.HasField("CurrentCommand"))
                player.SetField("CurrentCommand", new Parameter(""));

            MainLog.WriteInfo("CMDS_OnConnect done");
            WriteLog.Debug("CMDS_OnConnect done");
        }

        public void CMDS_OnConnecting(Entity player)
        {
            WriteLog.Debug("CMDS_OnConnecting");
            foreach (string xnaddr in XBanList)
            {
                if (player.GetXNADDR().ToString().Contains(xnaddr))
                {
                    ExecuteCommand(string.Format("dropclient {0} \"^1You are banned from this server ^3permanently.\"", player.GetEntityNumber()));
                    return;
                }
            }
            WriteLog.Debug("CMDS_OnConnecting complete");
        }

        public void CMDS_OnPlayerKilled(Entity player, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            if (ConfigValues.Settings_enable_spree_messages)
            {
                int attacker_killstreak = UTILS_GetFieldSafe<int>(attacker, "killstreak") + 1;
                attacker.SetField("killstreak", attacker_killstreak);
                int victim_killstreak = UTILS_GetFieldSafe<int>(player,"killstreak");
                player.SetField("killstreak", 0);

                if (mod == "MOD_HEAD_SHOT")
                    WriteChatToAll(Lang_GetString("Spree_Headshot").Format(new Dictionary<string, string>()
                    {
                        {"<attacker>", attacker.Name},
                        {"<victim>", player.Name}
                    }));
                if (mod == "MOD_MELEE" && weapon != "riotshield_mp")
                    WriteChatToAll(Lang_GetString("Spree_KnifeKill").Format(new Dictionary<string, string>()
                    {
                        {"<attacker>", attacker.Name},
                        {"<victim>", player.Name}
                    }));

                if (attacker_killstreak == 5)
                    WriteChatToAll(Lang_GetString("Spree_Kills_5").Replace("<attacker>", attacker.Name));

                if (attacker_killstreak == 10)
                    WriteChatToAll(Lang_GetString("Spree_Kills_10").Replace("<attacker>", attacker.Name));

                switch (weapon)
                {
                    case "moab":
                    case "briefcase_bomb_mp": 
                    case "destructible_car":
                    case "barrel_mp":
                    case "destructible_toy":
                        WriteChatToAll(Lang_GetString("Spree_Explosivekill").Replace("<victim>", player.Name));
                        break;
                    case "trophy_mp":
                        WriteChatToAll(Lang_GetString("Spree_Trophykill").Format(new Dictionary<string, string>()
                        {
                            {"<attacker>", attacker.Name},
                            {"<victim>", player.Name}
                        }));
                        break;
                }

                if(victim_killstreak >= 5)
                    WriteChatToAll(Lang_GetString("Spree_Ended").Format(new Dictionary<string, string>()
                    {
                        {"<attacker>", attacker.Name},
                        {"<victim>", player.Name},
                        {"<killstreak>", victim_killstreak.ToString()}
                    }));
            }

            string line = "[DEATH] " + string.Format("{0} : {1}, {2}, {3}", player.Name, attacker.Name, mod, weapon);
            line.LogTo(PlayersLog, MainLog);
        }

        public string CMDS_CommonIdentifiers(PlayerInfo A, PlayerInfo B)
        {
            if (B.IsNull() || A.IsNull())
                return null;
            List<string> identifiers = new List<string>();
            CMDS_AddCommonIdentifier(identifiers, A.GetIPString(), B.GetIPString());
            CMDS_AddCommonIdentifier(identifiers, A.GetGUIDString(), B.GetGUIDString());
            CMDS_AddCommonIdentifier(identifiers, A.GetHWIDString(), B.GetHWIDString());
            return string.Join("^7, ", identifiers.ToArray());
        }

        private void CMDS_AddCommonIdentifier(List<string> identifiers, string a, string b)
        {
            if (!string.IsNullOrWhiteSpace(a))
                if (a == b)
                    identifiers.Add("^2" + a);
                else
                    identifiers.Add("^1" + a);
        }

        public void CMDS_SaveBanList()
        {
            File.WriteAllLines(ConfigValues.ConfigPath + @"Commands\bannedplayers.txt", BanList.ToArray());
        }

        public void CMDS_SaveXBanList()
        {
            File.WriteAllLines(ConfigValues.ConfigPath + @"Commands\xbans.txt", XBanList.ToArray());
        }

        public void CMDS_AddToXBanList(Entity player)
        {
            XBanList.Add(player.GetXNADDR().ToString());
            CMDS_SaveXBanList();
        }

        public Command FindCommand(string cmdname)
        {
            foreach (Command cmd in CommandList)
                if (cmd.name == cmdname)
                    return cmd;
            return null;
        }

        #endregion
    }
}
