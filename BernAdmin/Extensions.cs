using System;
using System.Collections.Generic;
using System.Linq;
using InfinityScript;

namespace LambAdmin
{ 
    public static partial class Extensions
    {

        public static string[] AllPerks =
        {
            "specialty_longersprint",
            "specialty_fastreload",
            "specialty_scavenger",
            "specialty_blindeye",
            //"specialty_paint",
            "specialty_coldblooded",
            "specialty_quickdraw",
            //"_specialty_blastshield",
            "specialty_detectexplosive",
            //"specialty_autospot",
            "specialty_bulletaccuracy",
            "specialty_quieter",
            "specialty_stalker"
        };

        public static float GetSpeed(this Entity player)
        {
            if (player.HasField("speed"))
                return player.GetField<float>("speed");
            else
                return 1;
        }

        public static void SetSpeed(this Entity player, float speed)
        {
            player.SetField("speed", speed);
            player.MaintainSpeed();
        }

        public static void AddSpeed(this Entity player, float speed)
        {
            player.SetSpeed(player.GetSpeed() + speed);
        }

        public static void MaintainSpeed(this Entity player)
        {
            if (player.HasField("speed"))
                player.SetMoveSpeedScale(player.GetField<float>("speed"));
        }

        public static float GetScore(this Entity player)
        {
            return player.HasField("score") ? player.GetField<int>("score") : player.Score;
        }

        public static void AddScore(this Entity player, int score)
        {
            score += player.HasField("score") ? player.GetField<int>("score") : 0;
            player.SetField("score", score);
            player.Score = score;
        }

        public static void MaintainScore(this Entity player)
        {
            if (player.HasField("score"))
                player.Score = player.GetField<int>("score");
            if (DHAdmin.ConfigValues.Settings_score_limit > 0 && !DHAdmin.GameEnded && player.Score >= DHAdmin.ConfigValues.Settings_score_limit)
                DHAdmin.CMD_end();
        }

        public static int GetClassNumber(this Entity player)
        {
            if (player.HasField("currentClass"))
                return int.Parse(player.GetField<string>("currentClass").Last() + "");
            else
                return 0;
        }

        public static bool IsClass(this Entity player, int classNumber)
        {
            return player.HasField("currentClass") && player.GetClassNumber() == classNumber;
        }

        public static bool EmptyOrContains<T>(this List<T> list, T t)
        {
            return list.Count == 0 || list.Contains(t);
        }

        public static void RemoveIntersection<T>(this List<T> l, List<T> r)
        {
            foreach (T t in r)
                l.Remove(t);
        }

        public static List<T> Plus<T>(this List<T> l1, List<T> l2)
        {
            List<T> res = new List<T>();
            res.AddRange(l1);
            res.AddRange(l2);
            return res;
        }

        public static T GetRandom<T>(this List<T> l)
        {
            return l[DHAdmin.Random.Next(l.Count)];
        }

        public static List<int> ParseInts(this List<string> list) {
            return list.FindAll(e => int.TryParse(e, out _)).ConvertAll(i => int.Parse(i));
        }

        public static List<string> FilterInts(this List<string> list)
        {
            return list.FindAll(e => !int.TryParse(e, out _));
        }

        public static void FillWith<T>(this List<T> l, List<T> d)
        {
            l.AddRange(d.Skip(l.Count));
        }

        public static void FillWith(this List<string> l, List<string> d)
        {
            for (int ii = 0; ii < l.Count && ii < d.Count; ii++)
                if (l[ii] == "")
                    l[ii] = d[ii];
            l.AddRange(d.Skip(l.Count));
        }

        public static void BecomeKillionaire(this Entity player)
        {
            DHAdmin.WriteLog.Debug(player.Name + " becoming killionaire");
            player.SetField("killionaire", true);
            DHAdmin.WriteLog.Debug(player.Name + " taking weapons");
            player.TakeAllWeapons();
            DHAdmin.WriteLog.Debug(player.Name + " giving golden gun");
            player.GiveWeapon("iw5_ak47_mp_camo11");
            DHAdmin.WriteLog.Debug(player.Name + " setting perks");
            player.ClearPerks();
            foreach (string perk in AllPerks) {
                DHAdmin.WriteLog.Debug(player.Name + " giving perk " + perk);
                player.SetPerk(perk, true, true);
            }
            player.DisableWeaponPickup();
            int addr = GSCFunctions.LoadFX("props/cash_player_drop");
            BaseScript.OnInterval(200, () => {
                GSCFunctions.PlayFX(addr, player.GetEye());
                return (bool)player.GetField("killionaire");
            });
            DHAdmin.WriteLog.Debug(player.Name + " became killionaire");
            player.SwitchToWeaponImmediate("iw5_ak47_mp_camo11");
            BaseScript.AfterDelay(1000, () => {
                if (player.CurrentWeapon == "none")
                    player.SwitchToWeaponImmediate("iw5_ak47_mp_camo11");
            });
        }

        public static bool HasAmmoFor(this Entity player, string weapon)
        {
            int ammo = player.GetWeaponAmmoClip(weapon, "left") + player.GetWeaponAmmoClip(weapon, "right") + player.GetWeaponAmmoStock(weapon);
            return ammo > 0;
        }

        public static string RemoveColors(this string message)
        {
            foreach (string color in DHAdmin.Data.Colors.Keys)
                message = message.Replace(color, "");
            return message;
        }

        public static string[] Split(this string value, string separator)
        {
            return value.Split(new [] { separator }, StringSplitOptions.None);
        }

        public static void LogTo(this string message, params DHAdmin.SLOG[] logs)
        {
            foreach (DHAdmin.SLOG log in logs)
                log.WriteInfo(message);
        }

        public static string GetTeam(this Entity player)
        {
            return player.SessionTeam;
        }

        public static bool IsSpectating(this Entity player)
        {
            return player.GetTeam() == "spectator";
        }

        public static void Add<TKey, TValue>(this Dictionary<TKey, TValue> me, Dictionary<TKey, TValue> add)
        {
            foreach (var item in add)
                me[item.Key] = item.Value;
        }

        public static Dictionary<TKey, TValue> Plus<TKey, TValue>(this Dictionary<TKey, TValue> me, Dictionary<TKey, TValue> add)
        {
            return new Dictionary<TKey, TValue>()
            {
                me,
                add
            };
        }

        public static List<TValue> GetValues<TKey, TValue>(this Dictionary<TKey, TValue> me)
        {
            return me.Values.ToList();
        }

        public static string Format(this string str, Dictionary<string, string> format)
        {
            foreach (KeyValuePair<string, string> pair in format)
                str = str.Replace(pair.Key, pair.Value);
            return str;
        }

        public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
        {
            if (dict.TryGetValue(key, out TValue value))
                return value;
            throw new ArgumentOutOfRangeException();
        }

        public static string[] Condense(this string[] arr, int condenselevel = 40, string separator = ", ")
        {
            if (arr.Length < 1)
                return arr;
            List<string> lines = new List<string>();
            int index = 0;
            string line = arr[index++];
            while (index < arr.Length)
            {
                if ((line + separator + arr[index]).RemoveColors().Length > condenselevel)
                {
                    lines.Add(line);
                    line = arr[index];
                    index++;
                    continue;
                }
                line += separator + arr[index];
                index++;
            }
            lines.Add(line);
            return lines.ToArray();
        }

        public static DHAdmin.HWID GetHWID(this Entity player)
        {
            return new DHAdmin.HWID(player);
        }

        public static string GetHWIDRaw(this Entity player)
        {
            int address = DHAdmin.Data.HWIDDataSize * player.GetEntityNumber() + DHAdmin.Data.HWIDOffset;
            string formattedhwid = "";
            unsafe
            {
                for (int i = 0; i < 12; i++)
                {
                    formattedhwid += (*(byte*)(address + i)).ToString("x2");
                }
            }
            return formattedhwid;
        }

        public static bool IsHex(this char ch)
        {
            return DHAdmin.Data.HexChars.Contains(ch);
        }

        public static DHAdmin.XNADDR GetXNADDR(this Entity player)
        {
            return new DHAdmin.XNADDR(player);
        }
    }
}
