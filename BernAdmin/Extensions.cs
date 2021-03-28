using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InfinityScript;

namespace LambAdmin
{
    public static class EntityExtensions
    {

        public static bool IsConnecting(this Entity player)
        {
            return player.HasField("isConnecting") && player.GetField<int>("isConnecting") == 1;
        }

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

        /// <summary>
        /// Gets the script score if set, otherwise the game score.
        /// </summary>
        public static int GetScore(this Entity player)
        {
            return player.HasField("score") ? player.GetField<int>("score") : player.Score;
        }

        public static void AddScore(this Entity player, int score)
        {
            score += player.HasField("score") ? player.GetField<int>("score") : 0;
            player.SetField("score", score);
            player.Score = score;
            DHAdmin.HUD_UpdateTopInformation(player);
        }

        public static void MaintainScore(this Entity player)
        {
            if (player.HasField("score"))
                player.Score = player.GetField<int>("score");
            if (DHAdmin.ConfigValues.Settings_score_limit > 0 && !DHAdmin.GameEnded && player.Score >= DHAdmin.ConfigValues.Settings_score_limit)
                DHAdmin.CMDS_EndRound();
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

        private static bool PersistentWeaponsInitialised = false;
        public static void GivePersistentWeapon(this Entity player, string weapon)
        {
            weapon = weapon.Trim();
            int indexChange = weapon == "next" ? 1 : weapon == "previous" ? -1 : 0;
            if (indexChange != 0)
            {
                int currentIndex = player.GetField<int>("weapon_index") + indexChange;
                if (currentIndex >= 0 && currentIndex < DHAdmin.WeaponRewardList.Count)
                {
                    player.SetField("weapon_index", currentIndex);
                    weapon = DHAdmin.WeaponRewardList[currentIndex].FullName;
                    DHAdmin.HUD_UpdateTopLeftInformation(player);
                }
                else
                    return;
            }
            player.GiveAndSwitchTo(weapon);
            player.SetField("weapon", weapon);
            if (!PersistentWeaponsInitialised)
            {
                DHAdmin.PlayerActuallySpawned += MaintainWeapon;
                PersistentWeaponsInitialised = true;
            }
        }

        public static void MaintainWeapon(this Entity player)
        {
            DHAdmin.WriteLog.Debug("maintaining weapon for " + player.Name);
            if (player.HasField("weapon"))
            {
                player.TakeAllWeapons();
                player.GiveAndSwitchTo(player.GetField<string>("weapon"));
            }
        }

        public static void GiveAndSwitchTo(this Entity player, string weapon)
        {
            player.GiveWeapon(weapon);
            player.SetWeaponAmmoStock(weapon, 99);
            player.SetWeaponAmmoClip(weapon, 99);
            BaseScript.AfterDelay(50, () => player.SwitchToWeaponImmediate(weapon));
            BaseScript.AfterDelay(2000, () => { if (player.CurrentWeapon == "none") player.SwitchToWeaponImmediate(weapon); }); // check that the first switch succeeded (sometimes it doesnt, propably because of lag)
        }

        public static void StartPlayingFX(this Entity player, string fx)
        {
            int addr = GSCFunctions.LoadFX(fx);
            BaseScript.OnInterval(200, () => {
                GSCFunctions.PlayFX(addr, player.GetEye());
                return player.HasField(fx);
            });
        }

        public static void FullRotationEach(this Entity ent, string rotationType, int seconds)
        {
            BaseScript.OnInterval(seconds * 1000, () =>
            {
                switch (rotationType)
                {
                    case "pitch":
                        ent.RotatePitch(360, seconds);
                        return true;
                    case "roll":
                        ent.RotateRoll(360, seconds);
                        return true;
                    case "yaw":
                        ent.RotateYaw(360, seconds);
                        return true;
                }
                return false;
            });
        }

        public static string GetTeam(this Entity player)
        {
            return player.SessionTeam;
        }

        public static bool IsSpectating(this Entity player)
        {
            return player.GetTeam() == "spectator";
        }

        public static bool HasAmmoFor(this Entity player, string weapon)
        {
            int ammo = player.GetWeaponAmmoClip(weapon, "left") + player.GetWeaponAmmoClip(weapon, "right") + player.GetWeaponAmmoStock(weapon);
            return ammo > 0;
        }

        public static DHAdmin.PlayerInfo GetInfo(this Entity player)
        {
            return new DHAdmin.PlayerInfo(player);
        }

        public static DHAdmin.GroupsDatabase.Group GetGroup(this Entity entity, DHAdmin.GroupsDatabase database)
        {
            KeyValuePair<DHAdmin.PlayerInfo, string>? playerFromGroups = database.FindEntryFromPlayersAND(entity.GetInfo());
            if (playerFromGroups == null)
                return database.GetGroup("default");
            DHAdmin.GroupsDatabase.Group grp = database.GetGroup(playerFromGroups.Value.Value);
            if (grp != null)
                return grp;
            else
            {
                DHAdmin.WriteLog.Error("# Player " + entity.Name + ": GUID=" + entity.GUID + ", HWID = " + entity.HWID + ", IP:" + entity.IP.ToString());
                DHAdmin.WriteLog.Error("# Is in nonexistent group: " + playerFromGroups);
                return database.GetGroup("default");
            }
        }

        public static bool IsLogged(this Entity entity)
        {
            return File.ReadAllLines(DHAdmin.ConfigValues.ConfigPath + @"Groups\internal\loggedinplayers.txt").ToList().Contains(entity.GetInfo().GetIdentifiers());
        }

        public static void SetLogged(this Entity entity, bool state)
        {
            List<string> loggedinfile = File.ReadAllLines(DHAdmin.ConfigValues.ConfigPath + @"Groups\internal\loggedinplayers.txt").ToList();
            string identifiers = entity.GetInfo().GetIdentifiers();
            bool isalreadylogged = loggedinfile.Contains(identifiers);

            if (isalreadylogged && !state)
            {
                loggedinfile.Remove(identifiers);
                File.WriteAllLines(DHAdmin.ConfigValues.ConfigPath + @"Groups\internal\loggedinplayers.txt", loggedinfile.ToArray());
            }
            else if (!isalreadylogged && state)
            {
                loggedinfile.Add(identifiers);
                File.WriteAllLines(DHAdmin.ConfigValues.ConfigPath + @"Groups\internal\loggedinplayers.txt", loggedinfile.ToArray());
            }
        }

        public static bool IsImmune(this Entity entity, DHAdmin.GroupsDatabase database)
        {
            return database.FindMatchingPlayerFromImmunes(entity.GetInfo()) != null;
        }

        public static void SetImmune(this Entity entity, bool state, DHAdmin.GroupsDatabase database)
        {
            DHAdmin.PlayerInfo playerFromImmunes = database.FindMatchingPlayerFromImmunes(entity.GetInfo());
            if (playerFromImmunes == null && state)
                database.ImmunePlayers.Add(entity.GetInfo());
            if (playerFromImmunes != null && !state)
                database.ImmunePlayers.Remove(playerFromImmunes);
        }

        public static bool HasPermission(this Entity player, string permission_string, DHAdmin.GroupsDatabase database)
        {
            return database.GetEntityPermission(player, permission_string);
        }

        public static bool SetGroup(this Entity player, string groupname, DHAdmin.GroupsDatabase database)
        {
            groupname = groupname.ToLowerInvariant();
            player.SetLogged(false);
            if (database.GetGroup(groupname) == null)
                return false;
            var matchedplayerinfo = database.FindEntryFromPlayersAND(player.GetInfo());
            if (matchedplayerinfo != null)
            {
                if (groupname == "default")
                {
                    database.Players.Remove(matchedplayerinfo.Value.Key);
                }
                else
                    database.Players[matchedplayerinfo.Value.Key] = groupname;
            }
            else if (groupname != "default")
                database.Players[player.GetInfo()] = groupname;
            return true;
        }

        //CHANGE
        public static bool FixPlayerIdentifiers(this Entity player, DHAdmin.GroupsDatabase database)
        {
            player.SetLogged(false);
            var matchedplayerinfo = database.FindEntryFromPlayersOR(player.GetInfo());
            if (matchedplayerinfo != null)
            {
                database.Players.Remove(matchedplayerinfo.Value.Key);
                database.Players[DHAdmin.PlayerInfo.CommonIdentifiers(player.GetInfo(), matchedplayerinfo.Value.Key)] = matchedplayerinfo.Value.Value;
                return true;
            }
            return false;
        }

        public static string GetFormattedName(this Entity player, DHAdmin.GroupsDatabase database)
        {
            DHAdmin.GroupsDatabase.Group grp = player.GetGroup(database);
            var alias = "";
            if (DHAdmin.ChatAlias.Keys.Contains(player.GUID))
                alias = DHAdmin.ChatAlias[player.GUID];
            if (!string.IsNullOrWhiteSpace(grp.short_name))
                return DHAdmin.Lang_GetString("FormattedNameRank").Format(new Dictionary<string, string>()
                {
                    { "<shortrank>", grp.short_name },
                    { "<rankname>",  grp.group_name},
                    { "<name>", (alias != "")?alias : player.Name },
                });
            return DHAdmin.Lang_GetString("FormattedNameRankless").Format(new Dictionary<string, string>()
                {
                    { "<name>", (alias != "")?alias : player.Name },
                });
        }

        public static bool IsSpying(this Entity player)
        {
            return File.ReadAllLines(DHAdmin.ConfigValues.ConfigPath + @"Commands\internal\spyingplayers.txt").ToList().Contains(player.GetInfo().GetIdentifiers());
        }

        public static void SetSpying(this Entity player, bool state)
        {
            List<string> spyingfile = File.ReadAllLines(DHAdmin.ConfigValues.ConfigPath + @"Commands\internal\spyingplayers.txt").ToList();
            string identifiers = player.GetInfo().GetIdentifiers();
            bool isalreadyspying = spyingfile.Contains(identifiers);

            if (isalreadyspying && !state)
            {
                spyingfile.Remove(identifiers);
                File.WriteAllLines(DHAdmin.ConfigValues.ConfigPath + @"Commands\internal\spyingplayers.txt", spyingfile.ToArray());
            }
            else if (!isalreadyspying && state)
            {
                spyingfile.Add(identifiers);
                File.WriteAllLines(DHAdmin.ConfigValues.ConfigPath + @"Commands\internal\spyingplayers.txt", spyingfile.ToArray());
            }
        }

        public static bool IsMuted(this Entity player)
        {
            return File.ReadAllLines(DHAdmin.ConfigValues.ConfigPath + @"Commands\internal\mutedplayers.txt").ToList().Contains(player.GetInfo().GetIdentifiers());
        }

        public static void SetMuted(this Entity player, bool state)
        {
            List<string> mutedfile = File.ReadAllLines(DHAdmin.ConfigValues.ConfigPath + @"Commands\internal\mutedplayers.txt").ToList();
            string identifiers = player.GetInfo().GetIdentifiers();
            bool isalreadymuted = mutedfile.Contains(identifiers);
            if (isalreadymuted && !state)
            {
                mutedfile.Remove(identifiers);
                File.WriteAllLines(DHAdmin.ConfigValues.ConfigPath + @"Commands\internal\mutedplayers.txt", mutedfile.ToArray());
                return;
            }
            if (!isalreadymuted && state)
            {
                mutedfile.Add(identifiers);
                File.WriteAllLines(DHAdmin.ConfigValues.ConfigPath + @"Commands\internal\mutedplayers.txt", mutedfile.ToArray());
            }
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

        public static DHAdmin.XNADDR GetXNADDR(this Entity player)
        {
            return new DHAdmin.XNADDR(player);
        }
    }

    public static class StringExtensions
    {
        public static string RemoveColors(this string message)
        {
            foreach (string color in DHAdmin.Data.Colors.Keys)
                message = message.Replace(color, "");
            return message;
        }

        public static string RemoveWhitespace(this string str)
        {
            return string.Join("", str.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        }

        public static string[] Split(this string str, string separator)
        {
            return str.Split(new[] { separator }, StringSplitOptions.None);
        }

        public static string[] Split(this string str, char separator, int limit)
        {
            return str.Split(new[] { separator }, limit);
        }

        public static string Format(this string str, Dictionary<string, string> format)
        {
            foreach (KeyValuePair<string, string> pair in format)
                str = str.Replace(pair.Key, pair.Value);
            return str;
        }

        public static Vector3 ToVector3(this string coordinates)
        {
            coordinates.ToVector3(out Vector3 res);
            return res;
        }

        public static bool ToVector3(this string coordinates, out Vector3 vector3)
        {
            string filtered = new string(coordinates.Where(c => char.IsDigit(c) || c == '-' || c == '.' || c == ',').ToArray());
            string[] xyz = filtered.Split(',');
            if (xyz.Length == 3)
            {
                vector3 = new Vector3(float.Parse(xyz[0]), float.Parse(xyz[1]), float.Parse(xyz[2]));
                return true;
            }
            else
            {
                vector3 = new Vector3(0, 0, 0);
                return false;
            }
        }

        public static void LogTo(this string message, params DHAdmin.SLOG[] logs)
        {
            foreach (DHAdmin.SLOG log in logs)
                log.WriteInfo(message);
        }
    }

    public static class ListExtensions
    {
        public static bool EmptyOrContains<T>(this List<T> list, T t)
        {
            return list.Count == 0 || list.Contains(t);
        }

        public static void RemoveIntersection<T>(this List<T> l, List<T> r)
        {
            foreach (T t in r)
                l.Remove(t);
        }

        public static T GetRandom<T>(this List<T> l)
        {
            return l[DHAdmin.Random.Next(l.Count)];
        }

        public static List<int> ParseInts(this List<string> list)
        {
            return list.FindAll(e => int.TryParse(e, out _)).ConvertAll(i => int.Parse(i));
        }

        public static List<string> FilterInts(this List<string> list)
        {
            return list.FindAll(e => !int.TryParse(e, out _));
        }

        public static void FillWith(this List<string> l, List<string> d)
        {
            for (int ii = 0; ii < l.Count && ii < d.Count; ii++)
                if (l[ii] == "")
                    l[ii] = d[ii];
            l.AddRange(d.Skip(l.Count));
        }
    }

    public static class Extensions
    {

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
                }
                else
                    line += separator + arr[index];
                index++;
            }
            lines.Add(line);
            return lines.ToArray();
        }

        public static bool IsHex(this char ch)
        {
            return DHAdmin.Data.HexChars.Contains(ch);
        }

        public static List<T> Clone<T>(this List<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
        }

        public static void Delete(this List<Entity> entities)
        {
            foreach (Entity entity in entities)
                entity.Delete();
        }
    }
}
