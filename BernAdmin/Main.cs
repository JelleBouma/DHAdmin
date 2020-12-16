﻿using System;
using System.Collections.Generic;
using InfinityScript;
using System.IO;

namespace LambAdmin
{
    public partial class DHAdmin : BaseScript
    {

        public static partial class ConfigValues
        {
            public static string sv_current_dsr = "";
        }

        event Action<Entity> PlayerActuallySpawned = ent => { };
        static event Action<Entity, Entity, Entity, int, int, string, string, Vector3, Vector3, string> OnPlayerDamageEvent = (t1, t2, t3, t4, t5, t6, t7, t8, t9, t10) => { };
        static event Action<Entity, Entity, Entity, int, string, string, Vector3, string> OnPlayerKilledEvent = (t1, t2, t3, t4, t5, t6, t7, t8) => { };
        event Action OnGameEnded = () => { };

        public DHAdmin() : base()
        {
            WriteLog.Info("DHAdmin is starting...");
            if (!Directory.Exists(ConfigValues.ConfigPath))
            {
                if (Directory.Exists(ConfigValues.DGAdminConfigPath))
                {
                    WriteLog.Info("Renaming " + ConfigValues.DGAdminConfigPath);
                    Directory.Move(ConfigValues.DGAdminConfigPath, ConfigValues.ConfigPath);
                }
                else
                {
                    WriteLog.Info("Creating " + ConfigValues.ConfigPath);
                    Directory.CreateDirectory(ConfigValues.ConfigPath);
                }
            }

            ME_OnServerStart(); // do this first because some map stuff has to be spawned before the game activates them 
            HUD_PrecacheShaders(); // do this first because some icons have to be loaded
            
            #region MODULE LOADING
            MAIN_OnServerStart();
            CFG_OnServerStart();
            groups_OnServerStart();

            UTILS_OnServerStart();
            CMDS_OnServerStart();

            MainLog.WriteInfo("DHAdmin starting...");

            SetupKnife();

            WriteLog.Debug("Initializing PersonalPlayerDvars...");
            PersonalPlayerDvars = UTILS_PersonalPlayerDvars_load();

            if (ConfigValues.settings_dspl != "") // using DHAdmin map rotation
                MR_Setup();

            if (ConfigValues.settings_dynamic_properties)
            {
                if (ConfigValues.settings_dspl == "") // not using DHAdmin map rotation
                {
                    WriteLog.Debug(string.Format("Sleep({0})", ConfigValues.settings_dynamic_properties_delay.ToString()));
                    ConfigValues.sv_current_dsr = UTILS_GetDSRName();
                    Delay(ConfigValues.settings_dynamic_properties_delay, () =>
                    {
                        CFG_Dynprop_Apply();
                    });
                }
                else
                {
                    CFG_Dynprop_Apply();
                }
            }
            else
            {
                if (ConfigValues.ANTIWEAPONHACK)
                    WriteLog.Info("You have to enable \"settings_dynamic_properties\" if you wish to use antiweaponhack");

                if (ConfigValues.settings_servertitle)
                    WriteLog.Info("You have to enable \"settings_dynamic_properties\" if you wish to use \"Server Title\"");

                if (ConfigValues.ISNIPE_MODE)
                {
                    WriteLog.Debug("Initializing iSnipe mode...");
                    SNIPE_OnServerStart();
                }

                if (ConfigValues.settings_enable_xlrstats)
                {
                    WriteLog.Debug("Initializing XLRStats...");
                    XLR_OnServerStart();
                    XLR_InitCommands();
                }

                if (ConfigValues.settings_enable_alive_counter)
                    PlayerConnected += hud_alive_players;

                if (ConfigValues.settings_enable_chat_alias)
                {
                    WriteLog.Debug("Initializing Chat aliases...");
                    InitChatAlias();
                }

                if (ConfigValues.ISNIPE_MODE && ConfigValues.ISNIPE_SETTINGS.ANTIKNIFE)
                    DisableKnife();
                else
                    EnableKnife();

                GSCFunctions.SetDvarIfUninitialized("unlimited_ammo", "2");
                GSCFunctions.SetDvarIfUninitialized("unlimited_stock", "2");
                GSCFunctions.SetDvarIfUninitialized("unlimited_grenades", "2");

                if (ConfigValues.settings_unlimited_ammo || UTILS_GetDvar("unlimited_ammo") == "1" ||
                    ConfigValues.settings_unlimited_stock || UTILS_GetDvar("unlimited_stock") == "1" ||
                    ConfigValues.settings_unlimited_grenades || UTILS_GetDvar("unlimited_grenades") == "1")
                {
                    WriteLog.Debug("Initializing Unlimited Ammo...");
                    UTILS_UnlimitedAmmo();
                }

                CMD_JUMP(ConfigValues.settings_jump_height);

                timed_messages_init();
            }
            #endregion

        }

        public override EventEat OnSay3(Entity player, ChatType type, string name, ref string message)
        {
            if (!message.StartsWith("!") || type != ChatType.All)
            {
                MainLog.WriteInfo("[CHAT:" + type + "] " + player.Name + ": " + message);
                
                CHAT_WriteChat(player, type, message);
                return EventEat.EatGame;
            }

            if (message.ToLowerInvariant().StartsWith("!login"))
            {
                string line = "[SPY] " + player.Name + " : !login ****";
                WriteLog.Info(line);
                MainLog.WriteInfo(line);
                CommandsLog.WriteInfo(line);
            }
            else
            {
                string line = "[SPY] " + player.Name + " : " + message;
                WriteLog.Info(line);
                MainLog.WriteInfo(line);
                CommandsLog.WriteInfo(line);
            }
            ProcessCommand(player, name, message);
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
                if (ConfigValues.settings_enable_xlrstats)
                {
                    WriteLog.Info("Saving xlrstats...");
                    xlr_database.Save(this);
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

            OnNotify("game_ended", level =>
            {
                OnGameEnded();
            });

            PlayerConnected += MAIN_OnPlayerConnect;
            PlayerDisconnected += MAIN_OnPlayerDisconnect;
            PlayerConnecting += MAIN_OnPlayerConnecting;
            PlayerActuallySpawned += MAIN_OnPlayerSpawn;
            OnPlayerDamageEvent += ANTIWEAPONHACK;

            // CUSTOM EVENTS

            MAIN_ResetSpawnAction();

            WriteLog.Info("Done doing internal stuff.");
        }

        public void MAIN_OnPlayerConnecting(Entity player)
        {
            player.SetField("isConnecting", 1);
            WriteLog.Info("# Player " + player.Name + " is trying to connect now");
        }

        public void MAIN_OnPlayerConnect(Entity player)
        {
            if (ConfigValues.settings_didyouknow != "")
                player.SetClientDvars("didyouknow", ConfigValues.settings_didyouknow, "motd", ConfigValues.settings_didyouknow, "g_motd", ConfigValues.settings_didyouknow);
            if (ConfigValues.settings_objective != "")
                player.SetClientDvar("cg_objectiveText", ConfigValues.settings_objective);
            player.OnNotify("menuresponse", (p, menu, selection) =>
            {
                if ((string)menu == "changeclass")
                {
                    string classSelection = (string)selection;
                    player.SetField("currentlySelectedClass", classSelection);
                }
            });
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
            UTILS_SetCliDefDvars(player);
            if (ConfigValues.settings_skullmund)
            {
                player.SpawnedPlayer += player.DisableWeaponPickup;
                //trackGunForPlayer(player, mund);
            }
            if (ConfigValues.settings_snd)
            {
                player.SetField("score", 0);
                trackObjectivesForPlayer(player);
            }
            if (ConfigValues.settings_player_team != "")
            {
                string team = player.GetTeam();
                if (team != ConfigValues.settings_player_team) CMD_changeteam(player, ConfigValues.settings_player_team);
                    player.Suicide();
            }
            if (ConfigValues.settings_killionaire)
            {
                player.Score = 1000;
                player.SetField("score", 1000);
            }
            if (ConfigValues.settings_movement_speed != 1 || ConfigValues.settings_reward.Contains("speed"))
                player.SetSpeed(ConfigValues.settings_movement_speed);
            if (bool.Parse(Sett_GetString("settings_enable_connectmessage")) == true)
            {
                WriteChatToAll(Sett_GetString("format_connectmessage").Format(new Dictionary<string, string>()
                {
                    { "<player>", player.Name },
                    { "<playerf>", player.GetFormattedName(database) },
                    { "<clientnumber>", player.GetEntityNumber().ToString() },
                    { "<hour>", DateTime.Now.Hour.ToString() },
                    { "<min>", DateTime.Now.Minute.ToString() },
                    { "<rank>",  player.GetGroup(database).group_name.ToString() }
                }));
            }

            string line = "[CONNECT] " + string.Format("{0} : {1}, {2}, {3}, {4}, {5}", player.Name.ToString(), player.GetEntityNumber().ToString(), player.GUID, player.IP.Address.ToString(), player.GetHWID().Value, player.GetXNADDR().ToString());
            line.LogTo(PlayersLog, MainLog);
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
                player.SetField("currentClass", player.GetField<string>("currentlySelectedClass"));
        }

        public void MAIN_ResetSpawnAction()
        {
            foreach (Entity player in Players)
                player.SetField("spawnevent", 0);
        }

    }

    public static partial class Extensions
    {
        public static bool isConnecting(this Entity player)
        {
            return player.HasField("isConnecting") && player.GetField<int>("isConnecting") == 1;
        }
    }
}