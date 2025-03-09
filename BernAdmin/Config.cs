using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using InfinityScript;
using System;

namespace LambAdmin
{
    public partial class DHAdmin
    {
        public static Dictionary<string, string> Lang = new Dictionary<string, string>();
        public static Dictionary<string, string> Settings = new Dictionary<string, string>();
        public static Dictionary<string, string> CmdLang = new Dictionary<string, string>();

        protected static readonly Dictionary<string, string> DefaultLang = new Dictionary<string, string>()
        {
            { "ChatPrefix", "^0[^1DG^0]^7" },
            { "ChatPrefixPM", "^0[^5PM^0]^7" },
            { "ChatPrefixSPY", "^0[^6SPY^0]^7" },
            { "ChatPrefixAdminMSG", "^0[^3ADM^0]^3" },
            { "FormattedNameRank", "<shortrank> <name>" },
            { "FormattedNameRankless", "<name>" },
            { "Message_HardscopingNotAllowed", "^1Hardscoping is not allowed!" },
            { "Message_PlantingNotAllowed", "^1Planting not allowed!" },
            { "Message_CRTK_NotAllowed", "^1CRTK not allowed!"},
            { "Message_BoltCancel_NotAllowed", "^1Boltcancel not allowed!"},
            { "Spree_Headshot", "^3<attacker> ^7killed ^3<victim> ^7by ^2 Headshot"},
            { "Spree_Kills_5", "Nice spree, ^2<attacker>! ^7got ^35 ^7kills in a row."},
            { "Spree_Kills_10", "Nice spree, ^2<attacker>! ^7got ^310 ^7kills in a row!"},
            { "Spree_Ended", "^2<victim>'s ^7killing spree ended (^3<killstreak> ^7kills). He was killed by ^3<attacker>!"},
            { "Spree_Explosivekill", "^3<victim> ^7has exploded!"},
            { "Spree_Trophykill", "^1L^2O^1L^9Z! ^3<attacker> ^7killed ^3<victim> ^7by ^2Trophy^1!"},
            { "Spree_KnifeKill", "^2<attacker> ^3humiliated ^5<victim>"}
        };

        public static Dictionary<string, string> CDvars = new Dictionary<string, string>();

        public static Dictionary<long, string> ChatAlias = new Dictionary<long, string>();

        public static Dictionary<long, string> Forced_clantags = new Dictionary<long, string>();

        protected static readonly Dictionary<string, string> DefaultSettings = new Dictionary<string, string>()
        {
            { "settings_isnipe", "true" },
            { "settings_isnipe_antiplant", "true" },
            { "settings_isnipe_antihardscope", "true" },
            { "settings_isnipe_anticrtk", "false"},
            { "settings_isnipe_antiboltcancel", "false"},
            { "settings_isnipe_antiknife", "true" },
            { "settings_isnipe_antifalldamage", "true" },
            { "settings_enable_xlrstats","true"},
            { "settings_teamnames_allies", "^0[^1DG^0] ^7Clan" },
            { "settings_teamnames_axis", "^7Noobs" },
            { "settings_teamicons_allies", "cardicon_weed" },
            { "settings_teamicons_axis", "cardicon_thebomb" },
            { "settings_enable_connectmessage", "false" },
            { "format_connectmessage", "^3#^1<hour>:<min> ^3#^1<clientnumber> ^3#^1<rank> ^3#^1<player> ^7Connected." },
            { "settings_disabled_commands", "svpassword,server,debug" },
            { "settings_maxwarns", "3" },
            { "settings_groups_autosave", "true" },
            { "settings_enable_spy_onlogin", "false" },
            { "settings_showversion", "true" },
            { "settings_adminshudelem", "true"},
            { "settings_enable_alive_counter", "true"},
            { "settings_unfreezeongameend", "true" },
            { "settings_betterbalance_enable", "true" },
            { "settings_betterbalance_message", "^3<player> ^2got teamchanged for balance." },
            { "settings_enable_dlcmaps", "true" },
            { "settings_enable_chat_alias", "true" },
            { "settings_enable_spree_messages", "true"},
            { "settings_dynamic_properties", "true" },
            { "settings_antiweaponhack", "true" },
            { "settings_servertitle", "true" },
            { "commands_vote_time", "20"},
            { "commands_vote_threshold", "2"},
            { "settings_timed_messages", "true" },
            { "settings_timed_messages_interval", "45" },
            { "settings_unlimited_ammo", "false" },
            { "settings_unlimited_stock", "false" },
            { "settings_unlimited_grenades", "false" },
            { "settings_jump_height", "39" },
            { "settings_movement_speed", "1" },
            { "settings_dspl", "default" },
            { "settings_dsr_repeat", "false" },
            { "settings_objective", "" },
            { "settings_didyouknow", "" },
            { "settings_dropped_weapon_pickup", "true" },
            { "settings_player_team", "" },
            { "settings_achievements", "false" },
            { "settings_score_start", "0" },
            { "settings_score_limit", "0" },
            { "settings_rewards", "" },
            { "settings_map_edit", "" },
            { "johnwoo_improved_reload", "false" },
            { "johnwoo_pistol_throw", "false" }
        };

        protected static readonly Dictionary<string, string> DefaultCmdLang = new Dictionary<string, string>()
        {
            #region MESSAGES

            {"Message_NotOnePlayerFound", "^1No or more players found under that criteria." },
            {"Message_TargetIsImmune", "^1Target is immune." },
            {"Message_NotOneMapFound", "^1No or more maps found under that criteria." },
            {"Message_GroupNotFound", "^1No group was found under that name." },
            {"Message_GroupsSaved", "^2Groups configuration saved." },
            {"Message_PlayerIsSpectating", "^1Player is spectating." },
            {"Message_InvalidTeamName", "^1Invalid team name." },
            {"Message_DSRNotFound", "^1DSR file not found." },
            {"Message_InvalidTimeSpan", "^1Invalid time span." },
            {"Message_InvalidSearch", "^1Invalid search term(s)" },
            {"Message_NoPermission", "^1You do not have permission to do that." },
            {"Message_CmdDisabled", "^1Command has been disabled for everyone." },
            {"Message_CommandNotFound", "^1Command not found." },
            {"Message_YouHaveBeenWarned", "^1You have been warned!" },
            {"Message_YouHaveBeenUnwarned", "^2You have been unwarned!" },
            {"Message_NotLoggedIn", "^1You need to log in first." },
            {"Message_InvalidNumber", "^1Invalid number." },
            {"Message_DefaultError", "^1Something went wrong. Check console for more details." },
            {"Message_NoEntriesFound", "^1No entries found." },
            {"Message_blockedByNightMode", "^1You cant use this command when night mode is active" },
            {"Message_FX_not_found", "^1Error: given FX not found."},
            {"Message_Filters_error1", "^1Error: ^3Wrong filter syntax"},
            {"Message_Filters_error2", "^1Error: ^3Unknown filter selector: ^2<selector>"},
            {"Message_Filters_message", "^3Applied to ^2<count> ^3players"},

            #endregion

            {"command_version_usage", "^1Usage: !version" },
            {"command_credits_usage", "^1Usage: !credits" },

            {"command_pm_usage", "^1Usage: !pm <player> <message>" },
            {"command_pm_message", "^1<sender>^0: ^2<message>" },
            {"command_pm_confirmation", "^2PM SENT." },

            {"command_admins_usage", "^1Usage: !admins" },
            {"command_admins_firstline", "^1Online Admins: ^7" },
            {"command_admins_formatting", "<formattedname>" },
            {"command_admins_separator", "^7, " },

            {"command_status_usage", "^1Usage: !status [*filter*]" },
            {"command_status_firstline", "^3Online players:" },
            {"command_status_formatting", "^1<id>^0 : ^7<namef>" },
            {"command_status_separator", "^7, " },

            {"command_login_usage", "^1Usage: !login <password>" },
            {"command_login_alreadylogged", "^1You are already logged in." },
            {"command_login_successful", "^2You have successfully logged in." },
            {"command_login_wrongpassword", "^1Wrong password." },
            {"command_login_notrequired", "^2Login is not required." },

            {"command_kick_usage", "^1Usage: !kick <player> [reason]" },
            {"command_kick_message", "^3<target>^7 was ^5kicked^7 by ^1<issuer>^7. Reason: ^6<reason>" },

            {"command_tmpban_usage", "^1Usage: !tmpban <player> [reason]" },
            {"command_tmpban_message", "^3<target>^7 was ^4tmpbanned^7 by ^1<issuer>^7. Reason: ^6<reason>" },

            {"command_ban_usage", "^1Usage: !ban <player> [reason]" },
            {"command_ban_message", "^3<target>^7 was ^1banned^7 by ^1<issuer>^7. Reason: ^6<reason>" },

            {"command_rules_usage", "^1Usage: !rules"},

            {"command_apply_usage", "^1Usage: !apply"},

            {"command_say_usage", "^1Usage: !say <message>" },

            {"command_sayto_usage", "^1Usage: !sayto <player>, <message>" },

            {"command_map_usage", "^1Usage: !map <mapname>" },
            {"command_map_message", "^5Map was changed by ^1<player>^5 to ^2<mapname>^5." },

            {"command_guid_usage", "^1Usage: !guid" },
            {"command_guid_message", "^1Your GUID: ^5<guid>" },

            {"command_warn_usage", "^1Usage: !warn <player> [reason]" },
            {"command_warn_message", "^3<target>^7 was ^3warned (<warncount>/<maxwarns>)^7 by ^1<issuer>^7. Reason: ^6<reason>"},

            {"command_unwarn_usage", "^1Usage: !unwarn <player> [reason]" },
            {"command_unwarn_message", "^3<target>^7 was ^2unwarned (<warncount>/<maxwarns>)^7 by ^1<issuer>^7. Reason: ^6<reason>" },

            {"command_resetwarns_usage", "^1Usage: !resetwarns <player> [reason]" },
            {"command_resetwarns_message","^3<target>^7 had his warnings ^2reset ^7by ^1<issuer>^7. Reason: ^6<reason>" },

            {"command_getwarns_usage", "^1Usage: !getwarns <player>" },
            {"command_getwarns_message", "^1<target>^7 has ^3(<warncount>/<maxwarns>) ^7warnings."},

            {"command_addimmune_usage", "^1Usage: !addimmune <player>" },
            {"command_addimmune_message", "^2<target>^5 has been added to the immune group by ^2<issuer>" },

            {"command_unimmune_usage", "^1Usage: !unimmune <player>" },
            {"command_unimmune_message", "^2<target>^5 has been ^1removed^5 from the immune group by ^2<issuer>" },

            {"command_setgroup_usage", "^1Usage: !setgroup <player> <groupname/default>" },
            {"command_setgroup_message", "^2<target> ^5has been added to group ^1<rankname> ^5by ^2<issuer>" },

            {"command_savegroups_usage", "^1Usage: !savegroups" },
            {"command_savegroups_message", "^2Groups have been saved." },
            {"command_savegroups_message_xlr", "^2XLRStats db have been saved." },

            {"command_res_usage", "^1Usage: !res" },

            {"command_getplayerinfo_usage", "^1Usage: !getplayerinfo <player>" },
            {"command_getplayerinfo_message", "^1<target>^7:^3<id>^7, ^5<guid>^7, ^2<ip>, ^5<hwid>" },

            {"command_balance_usage", "^1Usage: !balance" },
            {"command_balance_message", "^2Teams have been balanced." },
            {"command_balance_teamsalreadybalanced", "^1Teams are already balanced." },

            {"command_afk_usage", "^1Usage: !afk" },

            {"command_setafk_usage", "^1Usage: !setafk <player>" },

            {"command_setteam_usage", "^1Usage: !setteam <player> <axis/allies/spectator>" },
            {"command_setteam_message", "^2<target>^5's team has been changed by ^1<issuer>^5." },

            {"command_clanvsall_usage", "^1Usage: !clanvsall <matches...>" },
            {"command_clanvsallspectate_usage" , "^1Usage: !clanvsallspectate <matches...>" },
            {"command_clanvsall_message", "^1<issuer>^5 used ^2clanvsall ^5with terms ^3<identifiers>" },

            {"command_cdvar_usage", "^1Usage: !cdvar <<-ifds> <dvar> <value> | <-r> [dvar] [value]>" },
            {"command_cdvar_message", "^5Dvar ^3<key> ^5= ^3<value>" },
            {"command_cdvar_message1", "^5Dvar ^2saved permanently: ^3<key> ^5= ^3<value>" },
            {"command_cdvar_message2", "^5Dvar ^3<key> ^2was reset." },
            {"command_cdvar_message3", "^2All Dvars was reset." },
            {"command_cdvar_error1", "^1Error: ^7dvars already empty" },
            {"command_cdvar_error2", "^1Error: ^5dvar ^3<key> ^7not set!" },

            {"command_sdvar_usage", "^1Usage: !sdvar <key> [value]" },
            {"command_sdvar_message", "^5Server Dvar ^3<key> ^5= ^3<value>" },

            {"command_mode_usage", "^1Usage: !mode <DSR>" },
            {"command_mode_message", "^5DSR was changed by ^1<issuer>^5 to ^2<dsr>^5." },

            {"command_gametype_usage", "^1Usage: !gametype <DSR> <mapname>" },
            {"command_gametype_message", "^5Game changed to map ^3<mapname>^5, DSR ^3<dsr>" },

            {"command_server_usage", "^1Usage: !server <cmd>" },
            {"command_server_message", "^5Command executed: ^3<command>" },

            {"command_tmpbantime_usage", "^1Usage: !tmpbantime <minutes> <player> [reason]" },
            {"command_tmpbantime_message", "^3<target> ^7was tmpbanned by ^1<issuer> ^7for ^5<timespan>^7. Reason: ^6<reason>" },

            {"command_unban_usage", "^1Usage: !unban <name>"},
            {"command_unban_message", "^3Ban entry removed. ^1<banid>: <name>, <guid>, <hwid>, <time>"},
            {"command_unban_multiple_entries_found", "^1Error: Multiple entries found. Use ^3!searchbans > !unban-id"},

            {"command_unban-id_usage", "^1Usage: !unban-id <ban ID>" },
            {"command_unban-id_message", "^3Ban entry removed. ^1<banid>: <name>, <guid>, <hwid>, <time>" },

            {"command_lastbans_usage", "^1Usage: !lastbans [amount]" },
            {"command_lastbans_firstline", "^2Last <nr> bans:" },
            {"command_lastbans_message", "^1<banid>: <name>, <guid>, <hwid>, <time>" },

            {"command_searchbans_usage", "^1Usage: !searchbans <name/playerinfo>" },
            {"command_searchbans_firstline", "^2Search results:" },
            {"command_searchbans_message", "^1<banid>: <name>, <guid>, <hwid>, <time>" },

            {"command_help_usage", "^1Usage: !help [command]" },
            {"command_help_firstline", "^5Available commands:" },

            {"command_cleartmpbanlist_usage", "^1Usage: !cleartmpbanlist" },

            {"command_rage_usage", "^1Usage: !rage" },
            {"command_rage_message", "^3<issuer> ^5ragequit." },
            {"command_rage_kickmessage", "RAGEEEEEEEEE" },
            {"command_rage_custommessagenames", "lambder,juice,future,hotshot,destiny,peppah,moskvish,myst,bernkastel,ayoub" },
            {"command_rage_message_lambder", "^3<issuerf> ^5went back to fucking coding." },
            {"command_rage_message_juice", "^3<issuer> ^5squeezed outta here." },
            {"command_rage_message_future", "^3<issuer> ^5ragequit again." },
            {"command_rage_message_hotshot", "^3<issuer> ^5hit the shitty road." },
            {"command_rage_message_destiny", "^3<issuer> ^5resterino in pepperonis." },
            {"command_rage_message_peppah", "^3<issuer> ^5went back to the fap cave." },
            {"command_rage_message_moskvish", "^3<issuer> ^5is done with this fucking lag." },
            {"command_rage_message_myst", "^3<issuer> ^5will rek you scrubs later." },
            {"command_rage_message_bernkastel", "^3<issuer>^5: Sayonara, BAKEMI. ^6nipa~ ^1=^_^="},
            {"command_rage_message_ayoub", "^3<issuer>^5: ^1Da^2dd^1y ^3is ^5OuT"},

            {"command_loadgroups_usage", "^1Usage: !loadgroups" },
            {"command_loadgroups_message", "^2Groups configuration loaded." },

            {"command_maps_usage", "^1Usage: !maps" },
            {"command_maps_firstline", "^2Available maps:" },

            {"command_dsrnames_usage", "^1Usage: !dsrnames" },
            {"command_dsrnames_firstline", "^2Available DSRs:" },

            {"command_time_usage", "^1Usage: !time" },
            {"command_time_message", "^2Time: {0:HH:mm:ss}" },

            {"command_yell_usage", "^1Usage: !yell <player | *filter*> <message>" },

            {"command_changeteam_usage", "^1Usage: !changeteam <player>" },

            {"command_whois_usage", "^1Usage: !whois <player>" },
            {"command_whois_firstline", "^3All known names for player ^4<target>^3:" },
            {"command_whois_separator", "^1, ^7" },

            {"command_end_usage", "^1Usage: !end" },
            {"command_end_message", "^2Game ended by ^3<issuer>" },

            {"command_foreach_usage", "^1Usage: !foreach *filter* <command>" },

            {"command_frfc_usage", "^1Usage: !frfc *filter* <command>" },

            {"command_spy_usage", "^1Usage: !spy <on|off>" },
            {"command_spy_message_on", "^0Spy mode ^2enabled"},
            {"command_spy_message_off", "^0Spy mode ^1disabled" },

            {"command_amsg_usage", "^1Usage: !amsg <message>" },
            {"command_amsg_message", "^7<senderf>^7: ^3<message>" },
            {"command_amsg_confirmation", "^3Your message will be read by all online admins." },

            {"command_ga_usage", "^1Usage: !ga"},
            {"command_ga_message", "^5Ammo given." },

            {"command_hidebombicon_usage", "^1Usage: !hidebombicon" },
            {"command_hidebombicon_message", "^5Bomb icons hidden." },

            {"command_knife_usage", "^1Usage: !knife <on|off>" },
            {"command_knife_message_on", "^2Knife enabled." },
            {"command_knife_message_off", "^1Knife disabled." },

            {"command_letmehardscope_usage", "^1Usage: !letmehardscope <on/off>" },
            {"command_letmehardscope_message_on", "^5Hardscoping enabled for you. NEWB." },
            {"command_letmehardscope_message_off", "^5Hardscoping disabled for you." },

            {"command_freeze_usage", "^1Usage: !freeze <player | *filter*>" },
            {"command_freeze_message", "^3<target> ^7was frozen by ^1<issuer>" },

            {"command_unfreeze_usage", "^1Usage: !unfreeze <player | *filter*>" },
            {"command_unfreeze_message", "^3<target> ^7was ^0unfrozen ^7by ^1<issuer>" },

            {"command_mute_usage", "^1Usage: !mute <player | *filter*" },
            {"command_mute_message", "^3<target>^7 was ^:muted^7 by ^1<issuer>^7." },

            {"command_unmute_usage", "^1Usage: !unmute <player | *filter*" },
            {"command_unmute_message", "^3<target>^7 was ^;unmuted^7 by ^1<issuer>^7." },

            {"command_kill_usage", "^1Usage: !kill <player | *filter*>" },

            {"command_ft_usage", "^1Usage: !ft <0-10>" },
            {"command_ft_message", "^3FilmTweak ^2<ft> ^3applied." },

            {"command_scream_usage", "^1Usage: !scream <message>" },

            {"command_kickhacker_usage", "^1Usage: !kickhacker <full name>" },

            {"command_fakesay_usage", "^1Usage: !fakesay <player> <message>" },

            {"command_silentban_usage", "^1Usage: !silentban <player>" },
            {"command_silentban_message", "^3Player added to banlist. Will be kicked next game." },

            {"command_hwid_usage", "^1Usage: !hwid" },
            {"command_hwid_message", "^1Your HWID: ^5<hwid>" },

            {"command_rek_usage", "^1Usage: !rek <player>" },
            {"command_rek_message", "^3<target>^7 was ^:REKT^7 by ^1<issuer>^7." },

            {"command_rektroll_usage", "^1Usage: !rektroll <player>" },

            {"command_clankick_usage", "^1Usage: !clankick <player>" },
            {"command_clankick_kickmessage", "You have been kicked from the clan. You can remove clantag and reconnect." },
            {"command_clankick_message", "^2<target> ^7was ^5CLANKICKED ^7by ^3<issuerf>^7." },

            {"command_nootnoot_usage", "^1Usage: !nootnoot <player>" },
            {"command_nootnoot_message_on", "^3<target> ^5was nootnooted." },
            {"command_nootnoot_message_off", "^3<target> ^5was ^1unnootnooted." },

            {"command_betterbalance_usage", "^1Usage: !autobalance <off/on>" },
            {"command_betterbalance_message_on", "^3BetterBalance is now ^2enabled^3." },
            {"command_betterbalance_message_off", "^3BetterBalance is now ^1disabled^3." },

            {"command_xban_usage", "^1Usage: !xban <player> [reason]" },
            {"command_xban_message", "^5<target> ^7has been ^0xbanned ^7by ^2<issuer>^7. Reason: ^6" },

            {"command_dbsearch_usage", "^1Usage: !dbsearch <player>" },
            {"command_dbsearch_message_firstline", "^3<nr> entries found!" },
            {"command_dbsearch_message_found", "^3<playerinfo>" },
            {"command_dbsearch_message_notfound", "^1Player info not found in the database." },

            {"command_ac130_usage", "^1Usage: !ac130 <player | *filter*> [-p]" },
            {"command_ac130_message", "^1AC130 ^7Given to ^1<target>^7." },
            {"command_ac130_all", "^1AC130 ^7Given to ALL by ^1<issuerf>"},

            {"command_fixplayergroup_usage", "^1Usage: !fixplayergroup <player>" },
            {"command_fixplayergroup_message", "^2User group fixed." },
            {"command_fixplayergroup_notfound", "^1User IDs not found in the database." },

            {"command_sunlight_usage", "^1Usage: !sunlight <float R> <float G> <float B>"},

            {"command_night_usage", "^1Usage: !night <on|off>"},

            {"command_alias_usage", "^1Usage: !alias <player> [alias]"},
            {"command_alias_reset", "^2<player> ^7alias has been ^2reset"},
            {"command_alias_message", "^2<player> ^7alias has been set to «<alias>^7»"},
            {"command_alias_disabled", "^1Chat alias feature has been disabled in settings."},

            {"command_clantag_usage", "^1Usage: !clantag <player> [tag]"},
            {"command_clantag_reset", "^2<player> ^7clantag has been ^2reset"},
            {"command_clantag_message", "^2<player> ^7clantag has been set to «<tag>^7»"},
            {"command_clantag_error", "^1Error: 7 characters is max"},

            {"command_myalias_usage", "^1Usage: !myalias [alias]"},

            {"command_daytime_usage", "^1Usage: !daytime <day|night|morning|cloudy>"},

            {"command_kd_usage", "^Usage: !kd <player> <kills> <deaths>"},
            {"command_kd_message","^2<player>'s ^7KD has been set to ^1<kills>^7/^1<deaths>"},

            {"command_report_usage", "^1Usage: !report <message>" },
            {"command_report_message", "^7<senderf>^7 reported: ^3<message>" },


            {"command_lastreports_usage", "^1Usage: !lastreports [count = 4]^3; 1 <= Count <= 8" },
            {"command_lastreports_message", "^;<sender>: ^3<message>" },

            {"command_setfx_usage", "^Usage: !setfx <fx> [spawn key = activate]"},
            {"command_setfx_enabled", "FX spawner bound to ^3<key> ^7key"},
            {"command_setfx_changed", "FX spawner set to ^3<fx>"},
            {"command_setfx_spawned", "FX ^3<fx> ^2spawned ^7at ^3<origin>"},

            {"command_hell_message", "^1Hell ^0Mode ^3Enabled."},
            {"command_hell_usage", "^Usage: !hell"},
            {"command_hell_error1", "^1Error: Hell mode already active."},
            {"command_hell_error2", "^1Error: Hell mode avaliable only for ^3Seatown"},

            {"command_fire_usage", "^1Usage: !fire"},

            {"command_suicide_usage", "^1Usage: !suicide"},

            {"command_svpassword_usage", "^1Usage !svpassword [password]"},

            {"command_yes_usage", "^1Usage: !yes"},
            {"command_yes_message", "^3<player> voted ^2yes"},

            {"command_no_usage", "^1Usage: !no"},
            {"command_no_message", "^3<player> voted ^1no"},

            {"command_3rdperson_usage", "^1Usage: !3rdperson"},
            {"command_3rdperson_message", "^33RD person mode enabled by ^2<issuerf>"},
            {"command_3rdperson_disabled", "^33RD person mode ^1disabled ^3by ^2<issuerf>"},

            {"command_fly_usage", "^1Usage: !fly <on|off> [spawn key = activate]"},
            {"command_fly_enabled", "^3Fly mode ^2enabled. ^3Key: <key>"},
            {"command_fly_disabled", "^3Fly mode ^1disabled."},

            {"command_jump_usage", "^1Usage: !jump <<height> | default>"},
            {"command_jump_message", "^3Jump height has been set to: ^2<height>"},

            {"command_speed_usage", "^1Usage: !speed <<speed> | default>"},
            {"command_speed_message", "^3Speed has been set to: ^2<speed>"},

            {"command_gravity_usage", "^1Usage: !gravity <<g> | default>"},
            {"command_gravity_message", "^3Gravity has been set to: ^2<g> m / s ^ 2"},

            {"command_teleport_usage", "^1Usage: !teleport <player1 | *filter*> <player2>"},
            {"command_teleport_message", "^3<player1> ^2teleported to ^3<player2>"},

            {"command_register_usage", "^1Usage: !register"},
            {"command_register_message", "^3You ^2registered ^3to XLRStats"},
            {"command_register_error", "^1Error: ^3you already registered to XLRStats"},

            {"command_xlrstats_usage", "^1Usage: !xlrstats [player = self]"},
            {"command_xlrstats_message", "^3Score:^2<score> ^3kills:^2<kills> ^3deaths:^2<deaths>^3 k/d:^2<kd>^3 headshots:^2<headshots>^3 TK_kills:^2<tk_kills> ^3 accuracy:^2<precision>°/."},
            {"command_xlrstats_error", "^1Error: ^3Player not registered to XLRStats"},

            {"command_xlrtop_usage", "^1Usage: !xlrtop [amount = 4]^3; 1 <= amount <= 8"},
            {"command_xlrtop_error", "^1Error: ^2XLR db is empty."},
            {"command_xlrtop_message", "^1<place>) ^6<player>: ^3score:^2<score> ^3k:^2<kills> ^3kd:^2<kd> ^3acc:^2<precision>°/."},

            {"command_playfxontag_usage", "^1Usage: !playfxontag <fx> [tag = j_head]"},
            {"command_playfxontag_message", "^3FX ''^2<fx>^3'' spawned on ^2<tag>"},

            {"command_rotatescreen_usage", "^1Usage: !rotatescreen <player | *filter*> <degree>"},
            {"command_rotatescreen_message", "^2<player>'s ^3roll has been set to ^2<roll>°"},

            {"command_votekick_usage", "^1Usage: !votekick <player> [reason]"},
            {"command_votekick_message1", "^2<issuer> ^7wants to kick ^3<player>. ^7Reason: ^1<reason>"},
            {"command_votekick_message2", "^7Type ^3(^1!yes ^3/ ^1!no^3) ^7to vote."},
            {"command_votekick_HUD", @"^3Voting: ^1kick ^2<player> ^3for ^2<reason>\n^3Time remaining: ^2<time>s\n^3Votes: ^2+<posvotes>^3 / ^1-<negvotes>"},
            {"command_votekick_error1", "^1Voting failed: ^3player leaved the game."},
            {"command_votekick_error2", "^1Voting failed: ^3not enough votes"},
            {"command_votekick_error3", "^1Voting failed: ^3issuer leaved the game."},
            {"command_votekick_error4", "^1Error: ^3Voting already occur"},
            {"command_votekick_error5", "^1Error: ^3You already voted"},
            {"command_votekick_error6", "^1Error: ^3You are not allowed to vote"},

            {"command_votecancel_usage", "^1Usage: !votecancel"},
            {"command_votecancel_error", "^1Error: ^3There is no voting" },
            {"command_votecancel_message", "^3Voting cancelled by ^2<issuer>" },

            {"command_moab_usage", "^1Usage: !moab <<player | *filter*>"},
            {"command_moab_message", "^7A ^1MOAB ^3given to ^2<player>"},
            {"command_moab_message_all", "^7A ^1MOAB ^3given to ^1Everyone by ^2<issuer>"},

            {"command_drunk_usage", "^1Usage: !drunk"},
            {"command_drunk_message", "^3<player> ^2is ^1drunk"},

            {"command_weapon_usage", "^1Usage: !weapon <player | *filter*> <raw weapon string> [-t]"},
            {"command_weapon_message", "^3<player> ^7weapon set to ^2<weapon>"},
            {"command_weapon_error", "^1Error: ^7weapon ^2<weapon> ^7not exist! Switching back"},
            {"command_weapon_error1", "^1Error: ^3<player> ^7is dead"},

            {"command_fx_usage", "^1Usage: !fx <on/off>"},
            {"command_fx_message_on", "^3Fx ^2applied."},
            {"command_fx_message_off", "^3Fx ^1disabled."},

            {"command_unlimitedammo_usage", "^1Usage: !unlimitedammo <on/off/auto>"},
            {"command_unlimitedammo_message_on", "^3Unlimited ammo ^2enabled ^7by <issuerf>"},
            {"command_unlimitedammo_message_off", "^3Unlimited ammo ^1disabled ^7by <issuerf>"},
            {"command_unlimitedammo_message_auto", "^3Unlimited ammo set to ^5auto ^7by <issuerf>"},

            {"command_@admins_usage", "^1Usage: !@admins" },

            {"command_@rules_usage", "^1Usage: !@rules"},

            {"command_@apply_usage", "^1Usage: !@apply"},

            {"command_@time_usage", "^1Usage: !@time" },

            {"command_@xlrstats_usage", "^1Usage: !@xlrstats [player = self]"},
            {"command_@xlrstats_message", "^1<player>| ^3Score:^2<score> ^3kills:^2<kills> ^3deaths:^2<deaths>^3 k/d:^2<kd>^3 headshots:^2<headshots>^3 TK_kills:^2<tk_kills> ^3 accuracy:^2<precision>°/."},

            {"command_@xlrtop_usage", "^1Usage: !@xlrtop [amount = 4]^3; 1 <= amount <= 8"},

            {"command_fc_usage", "^1Usage: !fc <player> <command>" },

            {"command_lockserver_usage", "^1Usage: !lockserver [reason]" },
            {"command_lockserver_message1", "^2Server unlocked." }
        };

        protected static readonly Dictionary<string, string> DefaultCDVars = new Dictionary<string, string>()
        {
            { "cg_chatTime", "30000" },
            { "cg_chatHeight", "8" },
            { "cg_hudChatIntermissionPosition", "5 240" },
            { "cg_hudChatPosition", "5 240" },
            { "cg_hudSayPosition", "5 240" },
            { "r_filmUseTweaks", "0" },
            { "r_filmTweakEnable", "0" },
            { "r_filmTweakDesaturation", "0.2" },
            { "r_filmTweakDesaturationDark", "0.2" },
            { "r_filmTweakInvert", "0" },
            { "r_glowTweakEnable", "0" },
            { "r_glowUseTweaks", "0" },
            { "r_glowTweakRadius0", "5" },
            { "r_filmTweakContrast", "1.4" },
            { "r_filmTweakBrightness", "0" },
            { "r_filmTweakLightTint", "1.1 1.05 0.85" },
            { "r_filmTweakDarkTint", "0.7 0.85 1" }
        };

        protected static readonly Dictionary<string, string> DefaultCommandAliases = new Dictionary<string, string>()
        {
            { "protect", "addimmune" },
            { "unprotect", "unimmune" },
            { "hbi", "hidebombicon" },
            { "cvsa", "clanvsall" },
            { "tbt", "tmpbantime" },
            { "a", "amsg" },
            { "k", "kick" },
            { "w", "warn" },
            { "fr", "foreach" },
            { "s", "suicide" },
            { "tp", "teleport" },
            { "pft", "playfxontag" },
            { "vk", "votekick" },
            { "vc", "votecancel" },
            { "ua", "unlimitedammo" }
        };

        public static class ConfigValues
        {
#if DEBUG
            public static string Version = "v3.5.1.0d";
#else
            public static string Version = "v3.5.1.0";
#endif
            public static string DGAdminConfigPath = @"scripts\DGAdmin\";
            public static string ConfigPath = @"scripts\DHAdmin\";
            public static string Current_DSR = "";

            public static string ChatPrefix => Lang_GetString("ChatPrefix");
            public static string ChatPrefixPM => Lang_GetString("ChatPrefixPM");
            public static string ChatPrefixSPY => Lang_GetString("ChatPrefixSPY");
            public static string ChatPrefixAdminMSG => Lang_GetString("ChatPrefixAdminMSG");

            public static string Formatting_onlineadmins = "^1Online Admins: ^7";
            public static string Formatting_eachadmin = "{0} {1}";
            public static string Format_message = "{0}{1}^7: {2}";
            public static string Format_prefix_spectator = "(Spectator)";
            public static string Format_prefix_dead = "^7(Dead)^7";
            public static string Format_prefix_team = "^5[TEAM]^7";

            public static bool ISNIPE_MODE => Sett_GetBool("settings_isnipe");

            public static class ISNIPE_SETTINGS
            {
                public static bool ANTIHARDSCOPE => Sett_GetBool("settings_isnipe_antihardscope");
                public static bool ANTIBOLTCANCEL => Sett_GetBool("settings_isnipe_antiboltcancel");
                public static bool ANTICRTK => Sett_GetBool("settings_isnipe_anticrtk");
                public static bool ANTIKNIFE => Sett_GetBool("settings_isnipe_antiknife");
                public static bool ANTIPLANT => Sett_GetBool("settings_isnipe_antiplant");
                public static bool ANTIFALLDAMAGE => Sett_GetBool("settings_isnipe_antifalldamage");
            }

            private static string DayTime = "day";
            private static float[] SunLight = new float[3] { 1F, 1F, 1F };
            public static bool LockServer = false;
            public static bool SettingsMutex = false;
            public static bool _3rdPerson = false;
            public static List<string> Cmd_rules = new List<string>();
            public static bool Cmd_foreachContext = false;
            public static bool Unlimited_ammo_active = false;
            public static bool Score_maintenance_active = false;
            public static bool Speed_maintenance_active = false;
            public static bool Lovecraftian_active = false;

            public static int Settings_warn_maxwarns => Sett_GetInt("settings_maxwarns");
            public static bool Settings_groups_autosave => Sett_GetBool("settings_groups_autosave");
            public static List<string> Settings_disabled_commands => Sett_GetString("settings_disabled_commands").ToLowerInvariant().Split(',').ToList();
            public static bool Settings_enable_chat_alias => Sett_GetBool("settings_enable_chat_alias");
            public static bool Settings_enable_connectmessage => Sett_GetBool("settings_enable_connectmessage");
            public static bool Settings_enable_spree_messages => Sett_GetBool("settings_enable_spree_messages");
            public static bool Settings_enable_xlrstats => Sett_GetBool("settings_enable_xlrstats");
            public static bool Settings_enable_alive_counter => Sett_GetBool("settings_enable_alive_counter");
            public static bool Settings_dynamic_properties => Sett_GetBool("settings_dynamic_properties");
            public static bool Settings_antiweaponhack => Sett_GetBool("settings_antiweaponhack");
            public static bool Settings_servertitle => Sett_GetBool("settings_servertitle");

            public static string Settings_daytime
            {
                get => DayTime;
                set
                {
                    switch (value)
                    {
                        case "night":
                        case "day":
                        case "morning":
                        case "cloudy":
                            DayTime = value;
                            File.WriteAllLines(ConfigPath + @"Commands\internal\daytime.txt", new string[] {
                                "daytime=" + value,
                                "sunlight="+ SunLight[0]+","+SunLight[1]+","+SunLight[2]
                            });
                            break;
                    }
                }
            }
            public static float[] Settings_sunlight
            {
                get
                {
                    return SunLight;
                }
                set
                {
                    SunLight = value;
                    File.WriteAllLines(ConfigPath + @"Commands\internal\daytime.txt", new string[] {
                        "daytime=" + Settings_daytime,
                        "sunlight="+ SunLight[0]+","+SunLight[1]+","+SunLight[2]
                    });

                }
            }
            public static int Commands_vote_time => Sett_GetInt("commands_vote_time");
            public static float Commands_vote_threshold => Sett_GetFloat("commands_vote_threshold");

            public static string Servertitle_map = "";
            public static string Servertitle_mode = "";
            public static string Mapname = GSCFunctions.GetDvar("mapname");
            public static string G_gametype = GSCFunctions.GetDvar("g_gametype");

            public static string Settings_teamnames_allies => Sett_GetString("settings_teamnames_allies");
            public static string Settings_teamnames_axis => Sett_GetString("settings_teamnames_axis");
            public static string Settings_teamicons_allies => Sett_GetString("settings_teamicons_allies");
            public static string Settings_teamicons_axis => Sett_GetString("settings_teamicons_axis");
            public static bool Settings_timed_messages => Sett_GetBool("settings_timed_messages");
            public static bool Settings_betterbalance_enable => Sett_GetBool("settings_betterbalance_enable");
            public static bool Settings_enable_dlcmaps => Sett_GetBool("settings_enable_dlcmaps");
            public static int Settings_timed_messages_interval => Sett_GetInt("settings_timed_messages_interval");
            public static bool Settings_unlimited_ammo => Sett_GetBool("settings_unlimited_ammo");
            public static bool Settings_unlimited_stock => Sett_GetBool("settings_unlimited_stock");
            public static bool Settings_unlimited_grenades => Sett_GetBool("settings_unlimited_grenades");
            public static int Settings_jump_height => Sett_GetInt("settings_jump_height");
            public static float Settings_movement_speed => Sett_GetFloat("settings_movement_speed");
            public static string Settings_dspl => Sett_GetString("settings_dspl");
            public static bool Settings_dsr_repeat => Sett_GetBool("settings_dsr_repeat");
            public static string Settings_didyouknow => Sett_GetString("settings_didyouknow");
            public static string Settings_objective => Sett_GetString("settings_objective");
            public static string Settings_player_team => Sett_GetString("settings_player_team");
            public static bool Settings_dropped_weapon_pickup => Sett_GetBool("settings_dropped_weapon_pickup");
            public static bool Settings_achievements => Sett_GetBool("settings_achievements");
            public static int Settings_score_start => Sett_GetInt("settings_score_start");
            public static int Settings_score_limit => Sett_GetInt("settings_score_limit");
            public static string Settings_rewards => Sett_GetString("settings_rewards");
            public static string Settings_map_edit => Sett_GetString("settings_map_edit");
            public static string Settings_hud_top => Sett_GetString("settings_hud_top");
            public static bool Johnwoo_improved_reload => Sett_GetBool("johnwoo_improved_reload");
            public static bool Johnwoo_pistol_throw => Sett_GetBool("johnwoo_pistol_throw");
            public static bool Johnwoo_momentum => Sett_GetBool("johnwoo_momentum");


#if DEBUG
            public static readonly bool DEBUG = true;
#else
            public static readonly bool DEBUG = false;
#endif
            public static Dictionary<string, string> AvailableMaps = Data.StandardMapNames;
        }

        protected static readonly string[] Directories =
        {
            ConfigValues.ConfigPath,
            ConfigValues.ConfigPath + @"Achievements\",
            ConfigValues.ConfigPath + @"Achievements\players\",
            ConfigValues.ConfigPath + @"Commands\",
            ConfigValues.ConfigPath + @"Commands\internal\",
            ConfigValues.ConfigPath + @"Groups\",
            ConfigValues.ConfigPath + @"Groups\internal\",
            ConfigValues.ConfigPath + @"Hud\",
            ConfigValues.ConfigPath + @"Knife\",
            ConfigValues.ConfigPath + @"Logs\",
            ConfigValues.ConfigPath + @"MapEdit\",
            ConfigValues.ConfigPath + @"Rewards\",
            ConfigValues.ConfigPath + @"Utils\",
            ConfigValues.ConfigPath + @"Utils\internal\",
            ConfigValues.ConfigPath + @"Utils\internal\announcers\",
            ConfigValues.ConfigPath + @"Utils\playerlogs\"
        };

        protected static readonly string[] InitiallyEmptyFiles =
        {
            ConfigValues.ConfigPath + @"Achievements\achievements.txt",
            ConfigValues.ConfigPath + @"Commands\bannedplayers.txt",
            ConfigValues.ConfigPath + @"Commands\xbans.txt",
            ConfigValues.ConfigPath + @"Commands\internal\ChatReports.txt",
            ConfigValues.ConfigPath + @"Commands\internal\mutedplayers.txt",
            ConfigValues.ConfigPath + @"Commands\internal\setfx.txt",
            ConfigValues.ConfigPath + @"Commands\internal\spyingplayers.txt",
            ConfigValues.ConfigPath + @"Commands\internal\warns.txt",
            ConfigValues.ConfigPath + @"Groups\immuneplayers.txt",
            ConfigValues.ConfigPath + @"Groups\players.txt",
            ConfigValues.ConfigPath + @"Groups\internal\loggedinplayers.txt",
            ConfigValues.ConfigPath + @"Hud\precacheshaders.txt",
            ConfigValues.ConfigPath + @"MapEdit\OnServerStart.txt",
            ConfigValues.ConfigPath + @"Utils\chatalias.txt"
        };

        protected static readonly Dictionary<string, string> DefaultStringFiles = new Dictionary<string, string>()
        {
            { ConfigValues.ConfigPath + @"Commands\apply.txt", "Wanna join ^1DG^7? Apply at ^2DGClan.eu^3/apply" },
            { ConfigValues.ConfigPath + @"Commands\rules.txt", "Rule one: ^1No Rules!" },
            { ConfigValues.ConfigPath + @"Commands\internal\daytime.txt", "daytime=day\r\nsunlight=1,1,1" },
            { ConfigValues.ConfigPath + @"Hud\achievementlocations.txt", "-120,180\r\n155,180\r\n-120,215\r\n-79,215\r\n-39,215\r\n2,215\r\n42,215\r\n83,215\r\n124,215\r\n155,215" },
            { ConfigValues.ConfigPath + @"Utils\announcer.txt", "Set announcements in scripts\\DHAdmin\\Utils\\announcer.txt" },
            { ConfigValues.ConfigPath + @"Utils\badclantags.txt", "hkClan" },
            { ConfigValues.ConfigPath + @"Utils\badnames.txt", "thisguyhax.\r\nMW2Player" },
            { ConfigValues.ConfigPath + @"Utils\internal\PersonalPlayerDvars.xml", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<dictionary />" },
            { ConfigValues.ConfigPath + @"Utils\internal\XLRStats.xml", "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<dictionary />" }
        };

        protected static readonly Dictionary<string, Dictionary<string, string>> DefaultDictionaryFiles = new Dictionary<string, Dictionary<string, string>>()
        {
            { ConfigValues.ConfigPath + "settings.txt", DefaultSettings },
            { ConfigValues.ConfigPath + "lang.txt", DefaultLang },
            { ConfigValues.ConfigPath + "cmdlang.txt", DefaultCmdLang },
            { ConfigValues.ConfigPath + @"Commands\commandaliases.txt", DefaultCommandAliases },
            { ConfigValues.ConfigPath + @"Utils\cdvars.txt", DefaultCDVars }
        };

        /// <summary>
        /// Verify that all needed DHAdmin files are present. If they are not, create them.
        /// </summary>
        /// <returns>false if the first time setup has just been performed and the server should be restarted.</returns>
        public bool CFG_VerifyFiles()
        {
            if (Directory.Exists(ConfigValues.DGAdminConfigPath) && !Directory.Exists(ConfigValues.ConfigPath))
            {
                WriteLog.Warning("Renaming " + ConfigValues.DGAdminConfigPath + " to " + ConfigValues.ConfigPath);
                Directory.Move(ConfigValues.DGAdminConfigPath, ConfigValues.ConfigPath);
            }
            foreach (string directory in Directories)
                if (!Directory.Exists(directory))
                    WriteLog.Warning("Creating missing directory: " + Directory.CreateDirectory(directory).Name);
            CFG_CreateMissingFiles(InitiallyEmptyFiles, (f) => File.Create(f).Close());
            CFG_CreateMissingFiles(DefaultStringFiles.Keys, (f) => File.WriteAllText(f, DefaultStringFiles[f]));
            CFG_CreateMissingFiles(DefaultDictionaryFiles.Keys, (f) => CFG_CreateFile(f, DefaultDictionaryFiles[f]));
            if (!CFG_FindServerFile("DH.dspl", out _))
            {
                CFG_FirstTimeSetup();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Given a list of file paths, perform an action for each file that doesnt exist.
        /// </summary>
        public void CFG_CreateMissingFiles(IEnumerable<string> filepaths, Action<string> create)
        {
            foreach (string filepath in filepaths)
                if (!File.Exists(filepath))
                {
                    WriteLog.Warning(filepath + " not found. Creating new file...");
                    create(filepath);
                }
        }

        /// <summary>
        /// Create a file and write a dictionary to it.
        /// </summary>
        public void CFG_CreateFile(string filepath, Dictionary<string, string> defaultValues)
        {
            CFG_WriteDictionary(defaultValues, filepath);
        }

        /// <summary>
        /// First time setup, needed for the maprotation system.
        /// </summary>
        public void CFG_FirstTimeSetup()
        {
            WriteLog.Warning("DH.dspl missing, performing first time setup. Game will switch to Free For All on Village if succesful.");
            string servercfg = CFG_FindServerFile("server.cfg");
            string[] lines = File.ReadAllLines(servercfg);
            for (int ii = 0; ii < lines.Length; ii++)
                if (!lines[ii].Trim().StartsWith("//") && lines[ii].Contains("seta sv_maprotation"))
                    lines[ii] = "seta sv_maprotation \"DH\"";
            File.WriteAllLines(servercfg, lines);
            ExecuteCommand("sv_maprotation DH");
            using (StreamWriter DSPLStream = new StreamWriter(@"players2\DH.dspl"))
                DSPLStream.Write("mp_village,FFA_Default,1000");
            ExecuteCommand("map_rotate");
        }

        /// <summary>
        /// Read the general purpose config files.
        /// </summary>
        public static void CFG_ReadConfig()
        {
            WriteLog.Info("Reading config...");
            CFG_ReadDictionary(ConfigValues.ConfigPath + @"settings.txt", ref Settings);
            CFG_ReadDictionary(ConfigValues.ConfigPath + @"lang.txt", ref Lang);
            CFG_ReadDictionary(ConfigValues.ConfigPath + @"cmdlang.txt", ref CmdLang);
            ConfigValues.Cmd_rules = File.ReadAllLines(ConfigValues.ConfigPath + @"Commands\rules.txt").ToList();
            WriteLog.Info("Done reading config... gametype is " + ConfigValues.G_gametype);
        }

        /// <summary>
        /// Read dynamic properties from a dsr file.
        /// </summary>
        public void CFG_Dynprop_Init()
        {
            WriteLog.Info("Applying dynamic properties for DSR: " + ConfigValues.Current_DSR);
            List<string> DSRData = new List<string>();
            if (CFG_FindServerFile(ConfigValues.Current_DSR, out string DSR))
                DSRData = File.ReadAllLines(DSR).ToList();
            else
            {
                WriteLog.Error("Error loading dynamic_properties feature: DSR does not exist! \"" + DSR + "\"");
                return;
            }

            List<Dvar> dvars = new List<Dvar>();
            List<Dvar> teamNames = new List<Dvar>();

            // start of parsing

            int count = 0;

            foreach (string s in DSRData)
            {
                //#DGAdmin settings <setting> = <value> 
                Match match = new Regex(@"^[\s]{0,31}\/\/#D[G,H]Admin[\s]{1,31}settings[\s]{1,31}([a-z_]{0,63})[\s]{0,31}=[\s]{0,31}(.*)?$", RegexOptions.IgnoreCase)
                                .Match(s);

                if (match.Success)
                {
                    string prop = match.Groups[1].Value.ToLower();
                    if (Settings.Keys.Contains(prop))
                    {
                        count++;
                        switch (prop)
                        {
                            case "settings_showversion":
                            case "settings_adminshudelem":
                            case "settings_enable_dlcmaps":
                            case "settings_dynamic_properties":
                                WriteLog.Debug("dynamic_properties:: unable to override \"" + prop +"\"");
                                break;
                            default:
                                {
                                    Settings[prop] = match.Groups[2].Value;

                                    //team names
                                    switch (prop)
                                    {
                                        case "settings_teamnames_allies":
                                        case "settings_teamnames_axis":
                                        case "settings_teamicons_allies":
                                        case "settings_teamicons_axis":
                                            if (!string.IsNullOrWhiteSpace(ConfigValues.Settings_teamnames_allies))
                                                teamNames.Add(new Dvar { key = "g_TeamName_Allies", value = ConfigValues.Settings_teamnames_allies });
                                            if (!string.IsNullOrWhiteSpace(ConfigValues.Settings_teamnames_axis))
                                                teamNames.Add(new Dvar { key = "g_TeamName_Axis", value = ConfigValues.Settings_teamnames_axis });
                                            if (!string.IsNullOrWhiteSpace(ConfigValues.Settings_teamicons_allies))
                                                teamNames.Add(new Dvar { key = "g_TeamIcon_Allies", value = ConfigValues.Settings_teamicons_allies });
                                            if (!string.IsNullOrWhiteSpace(ConfigValues.Settings_teamicons_axis))
                                                teamNames.Add(new Dvar { key = "g_TeamIcon_Axis", value = ConfigValues.Settings_teamicons_axis });
                                            break;
                                    }
                                    break;
                                }
                        }
                    }
                    else
                    {
                        WriteLog.Warning("Unknown setting: " + prop);
                    }
                }

                //#DGAdmin cdvar <dvar name> = <value>
                match = new Regex(@"^[\s]{0,31}\/\/#D[G,H]Admin[\s]{1,31}cdvar[\s]{1,31}([a-z_]{0,63})[\s]{0,31}=[\s]{0,31}(.*)?$", RegexOptions.IgnoreCase)
                        .Match(s);

                if (match.Success)
                {
                    count++;
                    string prop = match.Groups[1].Value.ToLower();
                    string value = match.Groups[2].Value;
                    dvars.Add(new Dvar { key = prop, value = value });
                }

                //#DGAdmin rules "Rule1\nRule2\nRule3"
                match = new Regex(@"^[\s]{0,31}\/\/#D[G,H]Admin[\s]{1,31}rules[\s]{1,31}'([^']*?)'[\s]{0,31}$".Replace('\'', '"'), RegexOptions.IgnoreCase)
                        .Match(s);
                if (match.Success)
                {
                    count++;
                    ConfigValues.Cmd_rules = Regex.Split(match.Groups[1].Value, @"\\n").ToList();
                }

                if (ConfigValues.Settings_servertitle)
                {
                    /* 
                    *  //#DGAdmin servertitle map = <value> 
                    *  //#DGAdmin servertitle mode = <value> 
                    */
                    match = new Regex(@"^[\s]{0,31}\/\/#D[G,H]Admin[\s]{1,31}servertitle[\s]{1,31}([a-z_]{0,63})[\s]{0,31}=[\s]{0,31}(.*)?$", RegexOptions.IgnoreCase)
                                    .Match(s);
                    switch (match.Groups[1].Value.ToLowerInvariant())
                    {
                        case "map":
                            ConfigValues.Servertitle_map = match.Groups[2].Value;
                            count++;
                            break;
                        case "mode":
                            ConfigValues.Servertitle_mode = match.Groups[2].Value;
                            count++;
                            break;
                    }
                }

                dvars = UTILS_DvarListUnion(dvars, teamNames);

                if (dvars.Count > 0)
                {
                    foreach (Dvar dvar in dvars)
                        if (CDvars.ContainsKey(dvar.key))
                            CDvars[dvar.key] = dvar.value;
                        else
                            CDvars.Add(dvar.key, dvar.value);
                    foreach (Entity player in Players)
                        UTILS_SetClientDvarsPacked(player, dvars);
                }
            }

            /* 
            *  ############## ANTIWEAPONHACK ############### 
            *      get the list of restricted weapons
            */
            if (ConfigValues.Settings_antiweaponhack)
            {
                DSRData.ForEach(s => {

                    Regex rgx = new Regex(
                        @"^[\s]{0,31}gameOpt[\s]{1,31}commonOption\.weaponRestricted\.([a-z0-9_]{1,31})[\s]{1,31}'1'.*?$".Replace('\'', '"'),
                        RegexOptions.IgnoreCase);

                    Match match_weap = rgx.Match(s);
                    if (match_weap.Success)
                        RestrictedWeapons.Add(new Weapon(match_weap.Groups[1].Value));
                });
            }

            if (count > 0)
                WriteLog.Info(string.Format("dynamic_properties:: Done reading {0} settings from \"{1}\"", count, DSR));
        }

        /// <summary>
        /// Apply config values.
        /// </summary>
        public void CFG_Apply()
        {
            if (ConfigValues.ISNIPE_MODE)
            {
                WriteLog.Debug("Initializing iSnipe mode...");
                SNIPE_OnServerStart();
            }

            if (ConfigValues.Settings_enable_xlrstats)
            {
                WriteLog.Debug("Initializing XLRStats...");
                XLR_OnServerStart();
            }

            if (ConfigValues.Settings_enable_alive_counter)
                PlayerConnected += hud_alive_players;

            if (ConfigValues.Settings_enable_chat_alias)
            {
                WriteLog.Debug("Initializing Chat aliases...");
                InitChatAlias();
            }

            if (ConfigValues.ISNIPE_MODE && ConfigValues.ISNIPE_SETTINGS.ANTIKNIFE)
            {
                DisableKnife();
                WriteLog.Debug("Disable knife");
            }
            else
            {
                EnableKnife();
                WriteLog.Debug("Enable knife");
            }
            ConfigValues.Speed_maintenance_active = ConfigValues.Settings_movement_speed != 1;
            ConfigValues.Score_maintenance_active = ConfigValues.Settings_score_start > 0 || ConfigValues.Settings_score_limit > 0;
            GSCFunctions.SetDvarIfUninitialized("unlimited_ammo", "2");
            GSCFunctions.SetDvarIfUninitialized("unlimited_stock", "2");
            GSCFunctions.SetDvarIfUninitialized("unlimited_grenades", "2");
            UTILS_UnlimitedAmmo();
            UTILS_Maintain();

            Timed_messages_init();

            if (ConfigValues.Settings_antiweaponhack && CFG_DynPropRequirement("anti-weaponhack"))
                OnPlayerKilledEvent += WEAPONS_AntiWeaponHackKill;

            if (ConfigValues.Settings_didyouknow != "") {
                GSCFunctions.MakeDvarServerInfo("didyouknow", ConfigValues.Settings_didyouknow);
                GSCFunctions.MakeDvarServerInfo("motd", ConfigValues.Settings_didyouknow);
                GSCFunctions.MakeDvarServerInfo("g_motd", ConfigValues.Settings_didyouknow);
            }

            if (ConfigValues.Settings_achievements)
                ACHIEVEMENTS_Setup();

            CMD_JUMP(ConfigValues.Settings_jump_height);

            ME_ConfigValues_Apply();

            if (!string.IsNullOrWhiteSpace(ConfigValues.Settings_rewards))
                REWARDS_Setup();
            WriteLog.Debug("finished reward setup");

            //if (ConfigValues.Settings_movement_speed != 1)
                //UTILS_MarkForMaintenance(EntityExtensions.MaintainSpeed, 100);

            //if (ConfigValues.Settings_score_start > 0 ||  ConfigValues.Settings_score_limit > 0)
                //UTILS_MarkForMaintenance(EntityExtensions.MaintainScore, 100);
            //WriteLog.Debug("starting maintenance");
            //UTILS_StartMaintenance();
            //WriteLog.Debug("maintenance started");
            JW_Configure();
            if (ConfigValues.Settings_servertitle && CFG_DynPropRequirement("\"Server Title\""))
                if (ConfigValues.LockServer)
                    UTILS_ServerTitle("^1::LOCKED", "^1" + File.ReadAllText(ConfigValues.ConfigPath + @"Utils\internal\LOCKSERVER"));
                else
                    UTILS_ServerTitle_MapFormat();
        }

        /// <summary>
        /// Check if dynamic properties is enabled for some feature that needs dynamic properties.
        /// Write a message to the server console if it is not.
        /// </summary>
        /// <returns>true if dynamic properties are enabled.</returns>
        public static bool CFG_DynPropRequirement(string feature)
        {
            if (ConfigValues.Settings_dynamic_properties)
                return true;
            WriteLog.Info("You have to enable \"settings_dynamic_properties\" if you wish to use " + feature);
            return false;
        }

        /// <summary>
        /// Try to find a file in players2/ and admin/ (in that order).
        /// Outputs the found path.
        /// </summary>
        /// <returns>true if the file was found, false otherwise.</returns>
        public static bool CFG_FindServerFile(string name, out string path)
        {
            path = CFG_FindServerFile(name);
            return name != path;
        }

        /// <summary>
        /// Try to find a file in players2/ and admin/ (in that order).
        /// </summary>
        /// <returns>The found path. If no path was found, the input is returned.</returns>
        public static string CFG_FindServerFile(string name)
        {
            if (File.Exists("players2/" + name))
                return "players2/" + name;
            else if (File.Exists("admin/" + name))
                return "admin/" + name;
            else
                return name;
        }

        /// <summary>
        /// Get a float settings value from a settings key.
        /// If a float cannot be parsed from the Settings value, the value from DefaultSettings is used instead.
        /// </summary>
        /// <returns>The parsed float.</returns>
        public static float Sett_GetFloat(string key)
        {
            if (float.TryParse(Sett_GetString(key), out float res))
                return res;
            else
                return float.Parse(DefaultSettings.GetValue(key));
        }

        /// <summary>
        /// Get an int settings value from a settings key.
        /// If an int cannot be parsed from the Settings value, the value from DefaultSettings is used instead.
        /// </summary>
        /// <returns>The parsed int.</returns>
        public static int Sett_GetInt(string key)
        {
            if (int.TryParse(Sett_GetString(key), out int res))
                return res;
            else
                return int.Parse(DefaultSettings.GetValue(key));
        }

        /// <summary>
        /// Get a bool settings value from a settings key.
        /// If a bool cannot be parsed from the Settings value, the value from DefaultSettings is used instead.
        /// </summary>
        /// <returns>The parsed bool.</returns>
        public static bool Sett_GetBool(string key)
        {
            if (bool.TryParse(Sett_GetString(key), out bool res))
                return res;
            else
                return bool.Parse(DefaultSettings.GetValue(key));
        }

        /// <returns>
        /// A lang value from a lang key.
        /// If Lang does not have this key, the value from DefaultLang is returned instead.
        /// </returns>
        public static string Lang_GetString(string key) => GetString(key, Lang, DefaultLang);

        /// <returns>
        /// A settings value from a settings key.
        /// If Settings does not have this key, the value from DefaultSettings is returned instead.
        /// </returns>
        public static string Sett_GetString(string key) => GetString(key, Settings, DefaultSettings);

        /// <returns>
        /// A cmdlang value from a cmdlang key.
        /// If CmdLang does not have this key, the value from DefaultCmdLang is returned instead.
        /// </returns>
        public static string CmdLang_GetString(string key) => GetString(key, CmdLang, DefaultCmdLang);

        /// <returns>
        /// A value from a string dictionary, given a key. If the key doesn't exist in the first dictionary, use the second dictionary.
        /// </returns>
        private static string GetString(string key, Dictionary<string, string> dic, Dictionary<string, string> def)
        {
            if (dic.TryGetValue(key, out string value))
                return value;
            else
                return def.GetValue(key);
        }

        /// <summary>
        /// Write a dictionary to a file.
        /// </summary>
        public static void CFG_WriteDictionary(Dictionary<string, string> dict, string path)
        {
            List<string> lines = new List<string>();
            foreach (KeyValuePair<string, string> pair in dict)
                lines.Add(string.Join("=", pair.Key, pair.Value));
            File.WriteAllLines(path, lines.ToArray());
        }

        /// <summary>
        /// Read a dictionary from a file.
        /// </summary>
        public static void CFG_ReadDictionary(string path, ref Dictionary<string, string> dict)
        {
            foreach (string line in File.ReadAllLines(path))
            {
                int index = line.IndexOf('=');
                if (index == line.Length || index == 0)
                    continue;
                string key = line.Substring(0, index);
                string value = line.Substring(index + 1);
                dict[key] = value;
            }
        }
    }
}
