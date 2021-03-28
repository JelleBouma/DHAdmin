using System;
using InfinityScript;

namespace LambAdmin
{
    public partial class DHAdmin
    {

        public void SNIPE_OnServerStart()
        {
            WriteLog.Info("Initializing isnipe settings...");
            PlayerActuallySpawned += SNIPE_OnPlayerSpawn;
            OnPlayerDamageEvent += SNIPE_PeriodicChecks;
            PlayerConnected += SNIPE_OnPlayerConnect;

            if (ConfigValues.ISNIPE_SETTINGS.ANTIKNIFE)
            {
                DisableKnife();
                WriteLog.Info("Knife auto-disabled.");
            }

            AfterDelay(5000, () =>
            {
                if (GSCFunctions.GetDvar("g_gametype") == "infect")
                    EnableKnife();
            });

            WriteLog.Info("Done initializing isnipe settings.");
        }

        public void SNIPE_OnPlayerSpawn(Entity player)
        {
            CMD_HideBombIcon(player);
            CMD_GiveMaxAmmo(player);

            if (ConfigValues.ISNIPE_SETTINGS.ANTIPLANT)
                OnInterval(1000, () =>
                {
                    if (!player.IsAlive)
                        return false;
                    if (player.CurrentWeapon.Equals("briefcase_bomb_mp"))
                    {
                        player.TakeWeapon("briefcase_bomb_mp");
                        player.IPrintLnBold(Lang_GetString("Message_PlantingNotAllowed"));
                        return true;
                    }
                    return true;
                });

            if (ConfigValues.ISNIPE_SETTINGS.ANTIHARDSCOPE)
            {
                player.SetField("adscycles", 0);
                player.SetField("letmehardscope", 0);
                OnInterval(50, () =>
                {
                    if (!player.IsAlive)
                        return false;
                    if (player.GetField<int>("letmehardscope") == 1)
                        return true;
                    if (GSCFunctions.GetDvar("g_gametype") == "infect" && player.GetTeam() != "allies")
                        return true;
                    float ads = player.PlayerAds();
                    int adscycles = player.GetField<int>("adscycles");
                    if (ads == 1f)
                        adscycles++;
                    else
                        adscycles = 0;

                    if (adscycles > 5)
                    {
                        player.AllowAds(false);
                        player.IPrintLnBold(Lang_GetString("Message_HardscopingNotAllowed"));
                    }

                    if (player.AdsButtonPressed() && ads == 0)
                        player.AllowAds(true);

                    player.SetField("adscycles", adscycles);
                    return true;
                });
            }
        }

        public void SNIPE_PeriodicChecks(Entity player, Entity inflictor, Entity attacker, int damage, int dFlags, string mod, string weapon, Vector3 point, Vector3 dir, string hitLoc)
        {
            if (ConfigValues.ISNIPE_SETTINGS.ANTIFALLDAMAGE && mod == "MOD_FALLING")
            {
                player.Health += damage;
                return;
            }
            if (!attacker.IsPlayer)
                return;
            if (attacker.HasField("CMD_FLY") && (attacker.IsSpectating() || !attacker.IsAlive))
                player.Health += damage;
            if (weapon == "iw5_usp45_mp_tactical" && GSCFunctions.GetDvar("g_gametype") == "infect" && attacker.GetTeam() != "allies")
                return;
            if (ConfigValues.ISNIPE_SETTINGS.ANTICRTK && weapon == "throwingknife_mp" && attacker.Origin.DistanceTo2D(player.Origin) < 200f)
            {
                player.Health += damage;
                attacker.IPrintLnBold(Lang_GetString("Message_CRTK_NotAllowed"));
            }
            if (ConfigValues.ISNIPE_SETTINGS.ANTIBOLTCANCEL && UTILS_GetFieldSafe<int>(attacker, "weapon_fired_boltcancel") == 1)
                player.Health += damage;
        }

        public void SNIPE_OnPlayerConnect(Entity player)
        {
            if (ConfigValues.ISNIPE_SETTINGS.ANTIBOLTCANCEL)
                EventDispatcher_AntiBoltCancel(player);
            player.OnNotify("giveLoadout", (ent) =>
            {
                CMD_GiveMaxAmmo(ent);
            });
            if (GSCFunctions.GetDvar("g_gametype") == "infect")
                player.OnNotify("giveLoadout", (ent) =>
                {
                    ent.TakeAllWeapons();
                    switch (ent.GetTeam())
                    {
                        case "allies":
                            ent.GiveWeapon("iw5_l96a1_mp_l96a1scope_xmags");
                            ent.SwitchToWeaponImmediate("iw5_l96a1_mp_l96a1scope_xmags");
                            break;
                        default:
                            ent.GiveWeapon("iw5_usp45_mp_tactical");
                            ent.ClearPerks();
                            AfterDelay(100, () => ent.SwitchToWeaponImmediate("iw5_usp45_mp_tactical"));
                            ent.SetWeaponAmmoClip("iw5_usp45_mp_tactical", 0);
                            ent.SetWeaponAmmoStock("iw5_usp45_mp_tactical", 0);
                            ent.SetPerk("specialty_tacticalinsertion", true, true);
                            break;
                    }
                });
        }

        #region CMDS

        public void CMD_GiveMaxAmmo(Entity player)
        {
            player.GiveMaxAmmo(player.CurrentWeapon);
        }

        public void CMD_HideBombIcon(Entity player)
        {
            player.SetClientDvar("waypointIconHeight", "1");
            player.SetClientDvar("waypointIconWidth", "1");
        }

        public void CMD_knife(bool state)
        {
            if (state)
                EnableKnife();
            else
                DisableKnife();
        }

        #endregion

        private void EventDispatcher_AntiBoltCancel(Entity player)
        {
            //Asynchronous hell

            player.SetField("fired", 0);
            player.SetField("weapon_fired_boltcancel", 0);

            player.NotifyOnPlayerCommand("weapon_reloading", "+reload");
            player.OnNotify("weapon_fired", (p, _) =>
            {
                p.SetField("fired", 1);
                AfterDelay(300, () => p.SetField("fired", 0));
            });
            player.OnNotify("weapon_reloading", (p) =>
            {
                if (UTILS_GetFieldSafe<int>(p, "fired") == 1)
                    if (UTILS_GetFieldSafe<int>(p, "weapon_fired_boltcancel") == 0)
                    {
                        p.SetField("weapon_fired_boltcancel", 1);
                        AfterDelay(2000, () => p.SetField("weapon_fired_boltcancel", 0));
                    }
                    else
                    {
                        p.IPrintLnBold(Lang_GetString("Message_BoltCancel_NotAllowed"));
                        p.AllowAds(false);
                        p.StunPlayer(1);
                        AfterDelay(300, () =>
                        {
                            p.AllowAds(true);
                            p.SetField("fired", 0);
                            p.SetField("weapon_fired_boltcancel", 0);
                            p.StunPlayer(0);
                        });
                    }
            });
        }
    }
}
