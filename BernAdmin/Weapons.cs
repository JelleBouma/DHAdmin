﻿using InfinityScript;
using System;
using System.Collections.Generic;

namespace LambAdmin
{
    public partial class DHAdmin
    {
        private static Weapon[] _AR =
        {
            new Weapon("iw5_m4_mp", "weapon_m4_iw5"),
            new Weapon("iw5_m16_mp", "weapon_m16_iw5"),
            new Weapon("iw5_scar_mp", "weapon_scar_iw5"),
            new Weapon("iw5_cm901_mp", "weapon_cm901"),
            new Weapon("iw5_type95_mp", "weapon_type95_iw5"),
            new Weapon("iw5_g36c_mp", "weapon_g36_iw5"),
            new Weapon("iw5_acr_mp", "weapon_remington_acr_iw5"),
            new Weapon("iw5_mk14_mp", "weapon_m14_iw5"),
            new Weapon("iw5_ak47_mp", "weapon_ak47_iw5"),
            new Weapon("iw5_fad_mp", "weapon_fad_iw5")
        };
        private static Weapon[] _SMG =
        {
            new Weapon("iw5_mp5_mp", "weapon_mp5_iw5"),
            new Weapon("iw5_ump45_mp", "weapon_ump45_iw5"),
            new Weapon("iw5_p90_mp", "weapon_p90_iw5"),
            new Weapon("iw5_pp90m1_mp", "weapon_pp90m1_iw5"),
            new Weapon("iw5_m9_mp", "weapon_uzi_m9_iw5"),
            new Weapon("iw5_mp7_mp", "weapon_mp7_iw5")
        };
        private static Weapon[] _LMG =
        {
            new Weapon("iw5_sa80_mp", "weapon_sa80_iw5"),
            new Weapon("iw5_mg36_mp", "weapon_mg36"),
            new Weapon("iw5_pecheneg_mp", "weapon_pecheneg_iw5"),
            new Weapon("iw5_mk46_mp", "weapon_mk46_iw5"),
            new Weapon("iw5_m60_mp", "weapon_m60_iw5")
        };
        private static Weapon[] _SR =
        {
            new Weapon("iw5_barrett_mp_barrettscope", "weapon_m82_scope_iw5"),
            new Weapon("iw5_l96a1_mp_l96a1scope", "weapon_l96a1_scope_iw5"),
            new Weapon("iw5_dragunov_mp_dragunovscope", "weapon_dragunov_scope_iw5"),
            new Weapon("iw5_as50_mp_as50scope", "weapon_as50_scope_iw5"),
            new Weapon("iw5_rsass_mp_rsassscope", "weapon_rsass_scope_iw5"),
            new Weapon("iw5_msr_mp_msrscope", "weapon_remington_msr_scope_iw5")
        };
        private static Weapon[] _SG =
        {
            new Weapon("iw5_usas12_mp", "weapon_usas12_iw5"),
            new Weapon("iw5_ksg_mp", "weapon_ksg_iw5"),
            new Weapon("iw5_spas12_mp", "weapon_spas12_iw5"),
            new Weapon("iw5_aa12_mp", "weapon_aa12_iw5"),
            new Weapon("iw5_striker_mp", "weapon_striker_iw5"),
            new Weapon("iw5_1887_mp", "weapon_model1887")
        };
        private static Weapon[] _RS =
        {
            new Weapon("riotshield_mp", "weapon_riot_shield_mp")
        };
        private static Weapon[] _MP =
        {
            new Weapon("iw5_fmg9_mp", "weapon_fmg_iw5"),
            new Weapon("iw5_mp9_mp", "weapon_mp9_iw5"),
            new Weapon("iw5_skorpion_mp", "weapon_skorpion_iw5"),
            new Weapon("iw5_g18_mp", "weapon_g18_iw5")
        };
        private static Weapon[] _HG =
        {
            new Weapon("iw5_usp45_mp", "weapon_usp45_iw5"),
            new Weapon("iw5_p99_mp", "weapon_walther_p99_iw5"),
            new Weapon("iw5_mp412_mp", "weapon_mp412"),
            new Weapon("iw5_44magnum_mp", "weapon_44_magnum_iw5"),
            new Weapon("iw5_fnfiveseven_mp", "weapon_fn_fiveseven_iw5"),
            new Weapon("iw5_deserteagle_mp", "weapon_desert_eagle_iw5")
        };
        private static Weapon[] _L =
        {
            new Weapon("iw5_smaw_mp", "weapon_smaw"),
            new Weapon("javelin_mp", "weapon_javelin"),
            new Weapon("stinger_mp", "Stinger"),
            new Weapon("xm25_mp", "weapon_xm25"),
            new Weapon("m320_mp", "weapon_m320_gl"),
            new Weapon("rpg_mp", "weapon_rpg7")
        };

        private static Weapon[] _ENV =
        {
            new Weapon("destructible_car", "destructible_car")
        };
        private static Weapon[] _ETC =
        {
            new Weapon("briefcase_bomb_mp", "prop_suitcase_bomb"),
            new Weapon("none", "none")
        };

        static Weapons AR = new Weapons(_AR);
        static Weapons SMG = new Weapons(_SMG);
        static Weapons LMG = new Weapons(_LMG);
        static Weapons SR = new Weapons(_SR);
        static Weapons SG = new Weapons(_SG);
        static Weapons RS = new Weapons(_RS);
        static Weapons MP = new Weapons(_MP);
        static Weapons HG = new Weapons(_HG);
        static Weapons L = new Weapons(_L);
        static Weapons ENV = new Weapons(_ENV);
        static Weapons ETC = new Weapons(_ETC);

        public static Dictionary<string, Weapons> WeaponDictionary = new Dictionary<string, Weapons>()
        {
            { "AR", AR },
            { "SMG", SMG },
            { "LMG", LMG },
            { "SR", SR },
            { "SG", SG },
            { "RS", RS },
            { "MP", MP },
            { "HG", HG },
            { "L", L },
            { "*", AR + SMG + LMG + SR + SG + RS + MP + HG + L },
            { "ENV", ENV },
            { "ETC", ETC },
            { "*+", AR + SMG + LMG + SR + SG + RS + MP + HG + L + ENV + ETC }
        };

        public static Weapons RestrictedWeapons = new Weapons();

        public class Weapon : IEquatable<Weapon>
        {
            public string Name;
            public string Attachments;
            public string Model;
            public string FullName;

            public Weapon(string name)
            {
                Weapon baseWeapon = WeaponDictionary.GetValue("*+").Find(w => name.StartsWith(w.Name) || w.Name.Contains(name));
                if (name.Length > baseWeapon.Name.Length)
                {
                    FullName = name;
                    Attachments = FullName.Substring(baseWeapon.Name.Length);
                }
                else
                {
                    FullName = baseWeapon.Name;
                    Attachments = "";
                }
                Name = baseWeapon.Name;
                Model = baseWeapon.Model;
            }

            public Weapon(string name, string model)
            {
                Name = name;
                Attachments = "";
                Model = model;
                FullName = name;
            }

            public bool IsAllowed()
            {
                return !RestrictedWeapons.ContainsName(Name);
            }

            public void Allow()
            {
                RestrictedWeapons.RemoveAll(w => w.Name == Name);
            }

            public void Restrict()
            {
                RestrictedWeapons.Add(this);
            }

            override public string ToString()
            {
                return Name;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as Weapon);
            }

            public bool Equals(Weapon other)
            {
                return other != null && FullName == other.FullName;
            }

            public override int GetHashCode()
            {
                return 733961487 + EqualityComparer<string>.Default.GetHashCode(FullName);
            }
        }

        public class Weapons : List<Weapon>
        {
            public Weapons() { }
            public Weapons(Weapon[] weapons) : base(weapons) { }
            public Weapons(string weapons)
            {
                string[] addAndFilter = weapons.Split('-');
                string[] adds = addAndFilter[0].Split(',');
                foreach (string add in adds)
                    if (WeaponDictionary.ContainsKey(add))
                        AddRange(WeaponDictionary.GetValue(add));
                    else
                        Add(new Weapon(add));
                if (addAndFilter.Length == 2)
                {
                    string[] filters = { "riotshield_mp", "javelin_mp", "stinger_mp" };
                    if (addAndFilter[1] != "")
                        filters = addAndFilter[1].Split(',');
                    foreach (string filter in filters)
                        if (WeaponDictionary.ContainsKey(filter))
                            this.RemoveIntersection(WeaponDictionary.GetValue(filter));
                        else
                            Remove(new Weapon(filter));
                }
            }

            public bool ContainsName(string name)
            {
                foreach (Weapon weapon in this)
                    if (weapon.Name == name)
                        return true;
                return false;
            }

            public static Weapons operator +(Weapons w1, Weapons w2)
            {
                Weapons res = new Weapons();
                res.AddRange(w1);
                res.AddRange(w2);
                return res;
            }

            public override string ToString()
            {
                return string.Join(", ", this);
            }
        }

        public void WEAPONS_AntiWeaponHackKill(Entity victim, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            WriteLog.Debug(victim.Name + " killed by inflictor " + inflictor.Name + " and attacker " + attacker.Name);
            WriteLog.Debug("weapon used: " + weapon);
            if (weapon != null && !new Weapon(weapon).IsAllowed())
                try
                {
                    WriteLog.Info("----STARTREPORT----");
                    WriteLog.Info("Bad weapon detected: " + weapon + " at player " + attacker.Name);
                    HaxLog.WriteInfo("----STARTREPORT----");
                    HaxLog.WriteInfo("BAD WEAPON: " + weapon);
                    HaxLog.WriteInfo("Player Info:");
                    HaxLog.WriteInfo(attacker.Name);
                    HaxLog.WriteInfo(attacker.GUID.ToString());
                    HaxLog.WriteInfo(attacker.IP.ToString());
                    HaxLog.WriteInfo(attacker.GetEntityNumber().ToString());
                }
                finally
                {
                    WriteLog.Info("----ENDREPORT----");
                    HaxLog.WriteInfo("----ENDREPORT----");
                    victim.Health += damage;
                    CMDS_Rek(attacker);
                    WriteChatToAll(Command.GetString("rek", "message").Format(new Dictionary<string, string>()
                    {
                        {"<target>", attacker.Name},
                        {"<targetf>", attacker.GetFormattedName(database)},
                        {"<issuer>", ConfigValues.ChatPrefix},
                        {"<issuerf>", ConfigValues.ChatPrefix},
                    }));
                }
        }

    }
}
