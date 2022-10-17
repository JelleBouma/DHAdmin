using System;
using System.Collections.Generic;
using InfinityScript;

namespace LambAdmin
{
    public partial class DHAdmin : BaseScript
    {

        public static event Action<Entity> PlayerActuallySpawned = ent => { };
        public static event Action<Entity, Entity, Entity, int, int, string, string, Vector3, Vector3, string> OnPlayerDamageEvent = (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10) => { };
        public static event Action<Entity, Entity, Entity, int, string, string, Vector3, string> OnPlayerKilledEvent = (t1, t2, t3, t4, t5, t6, t7, t8) => { };
        public static event Action<Entity, string, string> OnClassChangeEvent = (player, oldClass, newClass) => { };
        public static event Action OnGameEnded = () => { };
        public static bool GameEnded = false;

        public DHAdmin() : base()
        {
            WriteLog.Info("DHAdmin is starting...");
            if (!CFG_VerifyFiles())
                return;
            ME_OnServerStart(); // do this first because some map stuff has to be spawned before the game activates them 
            HUD_PrecacheShaders(); // do this first because some icons have to be loaded

            #region MODULE LOADING
            CFG_ReadConfig();

            MAIN_OnServerStart();
            groups_OnServerStart();

            SetupKnife();

            WriteLog.Debug("Initializing PersonalPlayerDvars...");
            PersonalPlayerDvars = UTILS_PersonalPlayerDvars_load();

            MR_Setup();

            if (ConfigValues.Settings_dynamic_properties)
                CFG_Dynprop_Init();
            CFG_Apply();
            UTILS_OnServerStart();
            CMDS_OnServerStart();
            MainLog.WriteInfo("DHAdmin has started...");
            #endregion
        }

        /// <summary>
        /// This method is automatically called when a player sends a chat message.
        /// Any commands in the chat message will be processed.
        /// </summary>
        public override EventEat OnSay3(Entity player, ChatType type, string name, ref string message)
        {
            if (message.Trim().StartsWith("!") && type == ChatType.All)
            {
                string spy = "[SPY] ";
                if (message.ToLowerInvariant().StartsWith("!login"))
                    spy += player.Name + " : !login ****";
                else
                    spy += player.Name + " : " + message;
                WriteLog.Info(spy);
                MainLog.WriteInfo(spy);
                CommandsLog.WriteInfo(spy);
                ProcessCommand(player, player, message.Trim());
            }
            else
            {
                MainLog.WriteInfo("[CHAT:" + type + "] " + player.Name + ": " + message);
                CHAT_WriteChat(player, type, message);
            }
            return EventEat.EatGame;
        }

        public override void OnStartGameType()
        {
            MAIN_ResetSpawnAction();
            base.OnStartGameType();
        }

        public override void OnExitLevel()
        {
            WriteLog.Info("Saving groups...");
            database.SaveGroups();

            if (!ConfigValues.SettingsMutex)
            {
                // Save xlr stats
                if (ConfigValues.Settings_enable_xlrstats)
                {
                    WriteLog.Info("Saving xlrstats...");
                    xlr_database.Save();
                }

                WriteLog.Info("Saving PersonalPlayerDvars...");
                // Save FilmTweak settings
                UTILS_PersonalPlayerDvars_save(PersonalPlayerDvars);

                ConfigValues.SettingsMutex = false;
            }

            MAIN_ResetSpawnAction();
            base.OnExitLevel();
        }

        public override void OnPlayerDamage(Entity player, Entity inflictor, Entity attacker, int damage, int dFlags, string mod, string weapon, Vector3 point, Vector3 dir, string hitLoc)
        {
            OnPlayerDamageEvent(player, inflictor, attacker, damage, dFlags, mod, weapon, point, dir, hitLoc);
            base.OnPlayerDamage(player, inflictor, attacker, damage, dFlags, mod, weapon, point, dir, hitLoc);
        }

        public override void OnPlayerKilled(Entity player, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            OnPlayerKilledEvent(player, inflictor, attacker, damage, mod, weapon, dir, hitLoc);
            base.OnPlayerKilled(player, inflictor, attacker, damage, mod, weapon, dir, hitLoc);
        }

        public void MAIN_OnServerStart()
        {
            WriteLog.Info("Setting up internal stuff...");

            PlayerConnected += e =>
            {
                WriteLog.Debug("player connected, setting up spawning");
                e.SetField("spawnevent", 0);
                OnInterval(100, () => {
                    if (e.IsAlive)
                    {
                        if (!e.HasField("spawnevent") || e.GetField<int>("spawnevent") == 0)
                        {
                            PlayerActuallySpawned(e);
                            e.SetField("spawnevent", 1);
                        }
                    }
                    else
                        e.SetField("spawnevent", 0);
                    return true;
                });
            };

            OnNotify("game_ended", _ => OnGameEnded());

            PlayerConnected += MAIN_OnPlayerConnect;
            PlayerDisconnected += MAIN_OnPlayerDisconnect;
            PlayerConnecting += MAIN_OnPlayerConnecting;
            PlayerActuallySpawned += MAIN_OnPlayerSpawn;

            MAIN_ResetSpawnAction();

            WriteLog.Info("Done doing internal stuff.");
        }

        public void MAIN_OnPlayerConnecting(Entity player)
        {
            WriteLog.Debug("MAIN_OnPlayerConnecting");
            player.SetField("isConnecting", 1);
            WriteLog.Info("# Player " + player.Name + " is trying to connect now");
            try
            {
                WriteLog.Info("# GUID: " + player.GUID.ToString() + " IP: " + player.IP.ToString());
                WriteLog.Debug("checking " + player.Name);
                WriteLog.Debug("checking " + player.Name);
                if (string.IsNullOrEmpty(player.GetXNADDR().Value))
                    throw new Exception("Bad xnaddr");
                WriteLog.Info("# XNADDR(12): " + player.GetXNADDR().ToString());
                if (!player.IsPlayer)
                    throw new Exception("Invalid entref");
                WriteLog.Debug("finished checking " + player.Name);
            }
            catch (Exception)
            {
                WriteLog.Info("# Haxor connected. Could not retrieve/set player info. Kicking...");
                try
                {
                    HaxLog.WriteInfo("----STARTREPORT----");
                    HaxLog.WriteInfo("BAD PLAYER");
                    HaxLog.WriteInfo(player.ToString());
                }
                catch (Exception ex)
                {
                    HaxLog.WriteInfo("ERROR ON TOSTRING");
                    HaxLog.WriteInfo(ex.ToString());
                }
                finally
                {
                    HaxLog.WriteInfo("----ENDREPORT----");
                }
                AfterDelay(100, () =>
                {
                    ExecuteCommand("dropclient " + player.GetEntityNumber() + " \"Something went wrong. Please restart TeknoMW3 and try again.\"");
                });
            }
        }

        public void MAIN_OnPlayerConnect(Entity player)
        {
            WriteLog.Debug("MAIN_OnPlayerConnect");
            if (ConfigValues.Settings_didyouknow != "")
                player.SetClientDvars("didyouknow", ConfigValues.Settings_didyouknow, "motd", ConfigValues.Settings_didyouknow, "g_motd", ConfigValues.Settings_didyouknow);
            if (ConfigValues.Settings_objective != "")
                player.SetClientDvar("cg_objectiveText", ConfigValues.Settings_objective);
            WriteLog.Debug("initiating class check for " + player.Name);
            player.OnNotify("menuresponse", (p, menu, selection) =>
            {
                if ((string)menu == "changeclass" && (string)selection != "back" && (string)selection != "allies" && (string)selection != "axis")
                    player.SetField("currentlySelectedClass", (string)selection);
            });
            WriteLog.Debug("initiating weapon check for " + player.Name);
            OnInterval(100, () =>
            {
                string currentWeapon = player.GetCurrentPrimaryWeapon();
                if (currentWeapon != "none")
                    player.SetField("currentweapon", currentWeapon);
                return true;
            });
            if ((ConfigValues.Settings_player_team == "allies" || ConfigValues.Settings_player_team == "axis") && (ConfigValues.G_gametype == "oic" || ConfigValues.G_gametype == "gun"))
            {
                WriteLog.Debug("forcing team for " + player.Name);
                if (player.GetTeam() != ConfigValues.Settings_player_team)
                {
                    CMD_changeteam(player, ConfigValues.Settings_player_team);
                    if (player.IsAlive)
                    {
                        WriteLog.Debug("suiciding " + player.Name);
                        player.Suicide();
                    }
                }
            }
            else if (ConfigValues.Settings_player_team != "")
            {
                WriteLog.Debug("forcing class for " + player.Name);
                UTILS_ForceClass(player, ConfigValues.Settings_player_team);
            }
            WriteLog.Debug("try to do some stuff for " + player.Name);
            try
            {
                player.SetField("spawnevent", 0);
                player.SetField("isConnecting", 0);
                GroupsDatabase.Group playergroup = player.GetGroup(database);
                WriteLog.Info("# Player " + player.Name + " from group \"" + playergroup.group_name + "\" connected.");
                WriteLog.Info("# GUID: " + player.GUID.ToString() + " IP: " + player.IP.ToString());
                WriteLog.Info("# HWID: " + player.GetHWID() + " ENTREF: " + player.GetEntityNumber());
                if (string.IsNullOrEmpty(player.GetXNADDR().Value))
                    throw new Exception("Bad xnaddr");
                WriteLog.Info("# XNADDR(12): " + player.GetXNADDR().ToString());
                if (!player.IsPlayer || player.GetHWID().IsBadHWID())
                    throw new Exception("Invalid entref/hwid");
            }
            catch (Exception)
            {
                WriteLog.Info("# Haxor connected. Could not retrieve/set player info. Kicking...");
                try
                {
                    HaxLog.WriteInfo("----STARTREPORT----");
                    HaxLog.WriteInfo("BAD PLAYER");
                    HaxLog.WriteInfo(player.ToString());
                }
                catch (Exception ex)
                {
                    HaxLog.WriteInfo("ERROR ON TOSTRING");
                    HaxLog.WriteInfo(ex.ToString());
                }
                finally
                {
                    HaxLog.WriteInfo("----ENDREPORT----");
                }
                AfterDelay(100, () =>
                {
                    ExecuteCommand("dropclient " + player.GetEntityNumber() + " \"Something went wrong. Please restart TeknoMW3 and try again.\"");
                });
            }
            WriteLog.Debug("UTILS_SetCliDefDvars for " + player.Name);
            UTILS_SetCliDefDvars(player);
            if (!ConfigValues.Settings_dropped_weapon_pickup)
                player.SpawnedPlayer += player.DisableWeaponPickup;
            if (ConfigValues.Settings_score_start > 0)
                player.AddScore(ConfigValues.Settings_score_start);
            if (ConfigValues.Settings_movement_speed != 1)
                player.SetSpeed(ConfigValues.Settings_movement_speed);
            if (WeaponRewardList.Count != 0)
            {
                player.SetField("weapon_index", 0);
                HUD_UpdateTopLeftInformation(player);
            }
            if (ConfigValues.Settings_enable_connectmessage)
                WriteChatToAll(Sett_GetString("format_connectmessage").Format(new Dictionary<string, string>()
                {
                    { "<player>", player.Name },
                    { "<playerf>", player.GetFormattedName(database) },
                    { "<clientnumber>", player.GetEntityNumber().ToString() },
                    { "<hour>", DateTime.Now.Hour.ToString() },
                    { "<min>", DateTime.Now.Minute.ToString() },
                    { "<rank>",  player.GetGroup(database).group_name.ToString() }
                }));
            string line = "[CONNECT] " + string.Format("{0} : {1}, {2}, {3}, {4}, {5}", player.Name.ToString(), player.GetEntityNumber().ToString(), player.GUID, player.IP.Address.ToString(), player.GetHWID().Value, player.GetXNADDR().ToString());
            WriteLog.Debug("logging " + player.Name);
            line.LogTo(PlayersLog, MainLog);
            if (ConfigValues.Settings_objective != "")
                AfterDelay(500, () => player.SetClientDvar("cg_objectiveText", ConfigValues.Settings_objective));
            WriteLog.Debug("MAIN_OnPlayerConnect done for " + player.Name);
        }

        public void MAIN_OnPlayerDisconnect(Entity player)
        {
            player.SetField("spawnevent", 0);
            string line = "[DISCONNECT] " + string.Format("{0} : {1}, {2}, {3}, {4}, {5}", player.Name.ToString(), player.GetEntityNumber().ToString(), player.GUID, player.IP.Address.ToString(), player.GetHWID().Value, player.GetXNADDR().ToString());
            line.LogTo(PlayersLog, MainLog);
        }

        public void MAIN_OnPlayerSpawn(Entity player)
        {
            if (player.HasField("currentlySelectedClass"))
            {
                string oldClass = player.HasField("currentClass") ? player.GetField<string>("currentClass") : null;
                string newClass = player.GetField<string>("currentlySelectedClass");
                if (oldClass != newClass)
                    OnClassChangeEvent(player, oldClass, newClass);
                player.SetField("currentClass", newClass);
            }
            
        }

        public void MAIN_ResetSpawnAction()
        {
            foreach (Entity player in Players)
                player.SetField("spawnevent", 0);
        }

    }
}