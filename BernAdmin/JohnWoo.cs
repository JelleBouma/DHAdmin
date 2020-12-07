using System;
using System.Linq;
using InfinityScript;
namespace LambAdmin
{
    public partial class DGAdmin
    {

        public void JW_Configure()
        {
            if (ConfigValues.johnwoo_improved_reload)
                PlayerConnected += JW_ImprovedReload;
            if (ConfigValues.johnwoo_pistol_throw)
            {
                PlayerConnected += JW_PistolThrow_TrackThrow;
                OnPlayerKilledEvent += JW_PistolThrow_OnDeath;
                JW_PistolThrow_OnInterval();
            }
            if (ConfigValues.johnwoo_momentum)
                OnPlayerDamageEvent += JW_Momentum;
        }

        public void JW_ImprovedReload(Entity player)
        {
            player.OnNotify("reload", reload_think);

            void reload_think(Entity reloader)
            {
                string weapon = player.CurrentWeapon;
                if (weapon.Contains("_akimbo") && player.GetWeaponAmmoStock(weapon) == 0)
                {
                    int clip = player.GetWeaponAmmoClip(weapon, "left") + player.GetWeaponAmmoClip(weapon, "right");
                    player.SetWeaponAmmoClip(weapon, (int)Math.Floor(clip / 2.0d), "left");
                    player.SetWeaponAmmoClip(weapon, (int)Math.Ceiling(clip / 2.0d), "right");
                }
            }
        }

        public void JW_PistolThrow_OnInterval()
        {
            OnInterval(50, () =>
            {
                foreach (Entity player in from player in Players where player.IsAlive select player)
                    weapon_interval_think(player, player.CurrentWeapon);
                return true;
            });

            void weapon_interval_think(Entity player, string weapon)
            {
                if ((player.HasField("gunThrowMode") || player.HasField("grenadeBackup")) && (player.HasAmmoFor(weapon) || !(weapon.Contains("_akimbo") || Data.Pistols.ContainsKey(weapon))))
                {
                    player.ClearField("gunThrowMode");
                    string currentOffhand = player.GetCurrentOffhand();
                    player.TakeWeapon(currentOffhand);
                    if (player.HasField("grenadeBackup"))
                    {
                        if (!player.HasField("grenadeBackupDelay") || player.GetField<int>("grenadeBackupDelay") == 0)
                        {
                            string backupClass = player.GetField<string>("grenadeBackupClass");
                            player.SetOffhandPrimaryClass(backupClass);
                            string backup = player.GetField<string>("grenadeBackup");
                            player.GiveWeapon(backup);
                            int backupAmmo = player.GetField<int>("grenadeBackupAmmo");
                            player.SetWeaponAmmoClip(backup, backupAmmo);
                            WriteLog.Debug("got backupClass = " + backupClass);
                            WriteLog.Debug("got backup = " + backup);
                            WriteLog.Debug("got backupAmmo = " + backupAmmo);
                            player.ClearField("grenadeBackup");
                            player.ClearField("grenadeBackupClass");
                            player.ClearField("grenadeBackupAmmo");
                            player.ClearField("grenadeBackupDelay");
                        }
                        else
                        {
                            player.SetField("grenadeBackupDelay", player.GetField<int>("grenadeBackupDelay") - 1);
                        }
                    }
                }
                if (!player.HasField("gunThrowMode") && !player.HasAmmoFor(weapon) && (weapon.Contains("_akimbo") || Data.Pistols.ContainsKey(weapon)))
                {
                    if (!player.HasField("grenadeBackup"))
                    {
                        string backup = player.GetCurrentOffhand();
                        string backupClass = player.GetOffhandPrimaryClass();
                        int backupAmmo = player.GetWeaponAmmoClip(backup);
                        player.TakeWeapon(backup);
                        player.SetOffhandPrimaryClass("throwingknife");
                        WriteLog.Debug("set backupClass = " + backupClass);
                        WriteLog.Debug("set backup = " + backup);
                        WriteLog.Debug("set backupAmmo = " + backupAmmo);
                        player.SetField("grenadeBackup", backup);
                        player.SetField("grenadeBackupClass", backupClass);
                        player.SetField("grenadeBackupAmmo", backupAmmo);
                    }
                    player.GiveWeapon("throwingknife_mp");
                    player.SetField("gunThrowMode", true);
                }
            }
        }

        public void JW_PistolThrow_OnDeath(Entity player, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            player.ClearField("gunThrowMode");
            player.ClearField("grenadeBackup");
            player.ClearField("grenadeBackupClass");
            player.ClearField("grenadeBackupAmmo");
            player.ClearField("grenadeBackupDelay");
        }

        public void JW_PistolThrow_TrackThrow(Entity player)
        {
            player.OnNotify("grenade_fire", grenade_fire_think);

            void grenade_fire_think(Entity self, Parameter grenade, Parameter grenadeName)
            {
                if (player.HasField("gunThrowMode"))
                {
                    player.SetField("grenadeBackupDelay", 8);
                    string currentWeapon = player.CurrentWeapon;
                    player.TakeWeapon(currentWeapon);
                    bool akimbo = currentWeapon.Contains("_akimbo");
                    if (akimbo)
                    {
                        currentWeapon = currentWeapon.Replace("_akimbo", "");
                        player.GiveWeapon(currentWeapon);
                        player.SwitchToWeaponImmediate(currentWeapon);
                        player.SetWeaponAmmoClip(currentWeapon, 0);
                        player.SetWeaponAmmoStock(currentWeapon, 0);
                    }
                    Entity grenadeEnt = grenade.As<Entity>();
                    string weaponModel = Data.Pistols[currentWeapon];
                    Entity gunEnt = GSCFunctions.Spawn("script_model", new Vector3(grenadeEnt.Origin.X + 1, grenadeEnt.Origin.Y - 4, grenadeEnt.Origin.Z + 7));
                    gunEnt.SetModel(weaponModel);
                    gunEnt.Angles = grenadeEnt.Angles;
                    gunEnt.EnableLinkTo();
                    grenadeEnt.EnableLinkTo();
                    gunEnt.NotSolid();
                    gunEnt.LinkTo(grenadeEnt);
                    grenadeEnt.Hide();
                    AfterDelay(10000, () =>
                    {
                        gunEnt.Delete();
                        grenadeEnt.Delete();
                    });
                    player.ClearField("gunThrowMode");
                }
            }
        }


        public void JW_Momentum(Entity victim, Entity inflictor, Entity attacker, int damage, int dFlags, string mod, string weapon, Vector3 point, Vector3 dir, string hitLoc)
        {
            if (attacker != null && attacker.IsPlayer && !attacker.IsOnGround())
            {
                WriteLog.Debug("JOHN WOO!! " + weapon);
                int extradmg;
                if (weapon == "iw5_g18_mp_akimbo")
                    extradmg = 2;
                else
                    extradmg = 5;
                int newHealth = victim.Health - extradmg;
                if (newHealth < 0)
                {
                    victim.Suicide();
                }
                else
                {
                    victim.Health -= extradmg;
                }
            }
            else
            {
                if (mod == "MOD_FALLING")
                {
                    WriteLog.Debug("reducing falling damage");
                    victim.Health += damage / 2;
                }
            }
        }
    }
}