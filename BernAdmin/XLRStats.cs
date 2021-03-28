using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfinityScript;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace LambAdmin
{
    public partial class DHAdmin
    {
        public class XLR_database
        {
            public struct XLREntry
            {
                [XmlAttribute]
                public long kills;
                [XmlAttribute]
                public long deaths;
                [XmlAttribute]
                public long headshots;
                [XmlAttribute]
                public long tk_kills;
                [XmlAttribute]
                public long shots_total;
                [XmlAttribute]
                public float score;
            }
            [Flags] public enum XLRUpdateFlags
            {
                kill = 1,
                death = 2,
                headshot = 4,
                tk_kill = 8,
                weapon_fired = 16
            }

            public static string FilePath = @"Utils\internal\XLRStats.xml";
            // <GUID, XLREntry>
            public volatile SerializableDictionary<long, XLREntry> xlr_players = new SerializableDictionary<long, XLREntry>();
            
            public void Init()
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(SerializableDictionary<long, XLREntry>));
                using (FileStream fs = new FileStream(ConfigValues.ConfigPath + FilePath, FileMode.Open))
                {
                    xlr_players = (SerializableDictionary<long, XLREntry>)xmlSerializer.Deserialize(fs);
                }

            }

            public void Update(long GUID, XLRUpdateFlags flags)
            {
                if (!xlr_players.ContainsKey(GUID))
                    return;
                XLREntry entry = xlr_players[GUID];
                if (flags.HasFlag(XLRUpdateFlags.kill))
                    entry.kills += 1;
                if (flags.HasFlag(XLRUpdateFlags.death))
                    entry.deaths += 1;
                if (flags.HasFlag(XLRUpdateFlags.headshot))
                    entry.headshots += 1;
                if (flags.HasFlag(XLRUpdateFlags.tk_kill))
                    entry.tk_kills += 1;
                if (flags.HasFlag(XLRUpdateFlags.weapon_fired))
                    entry.shots_total += 1;

                entry.score = Math_score(entry);

                xlr_players[GUID] = entry;
            }

            public void Save()
            {
                try
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(SerializableDictionary<long, XLREntry>));
                    FileStream fs = new FileStream(ConfigValues.ConfigPath + FilePath, FileMode.Create);
                    xmlSerializer.Serialize(fs, xlr_players);
                }
                catch(Exception ex)
                {
                    MainLog.WriteError(ex.Message);
                    MainLog.WriteError(ex.StackTrace);
                    WriteLog.Error(ex.Message);
                    WriteLog.Error(ex.StackTrace);
                }
            }

            public bool CMD_Register(long GUID)
            {
                if (xlr_players.ContainsKey(GUID))
                    return false;
                xlr_players.Add(GUID, new XLREntry { deaths = 0, headshots = 0, shots_total = 0, tk_kills = 0, kills = 0, score = 0 });
                return true;
            }

            public List<KeyValuePair<long, XLREntry>> CMD_XLRTOP(int amount)
            {
                amount = Math.Min(amount, xlr_players.Count);
                List<KeyValuePair<long, XLREntry>> top = xlr_players.ToList();
                if (xlr_players.Count == 0)
                    return top;
                top.Sort((pair1, pair2) => pair1.Value.score.CompareTo(pair2.Value.score));
                return top.Take(amount).ToList();
            }

            public float Math_kd(XLREntry entry)
            {
                return entry.kills / (float)((entry.deaths == 0) ? 1 : entry.deaths);
            }
            public float Math_precision(XLREntry entry)
            {
                return (entry.shots_total == 0) ?
                    0 :
                    (entry.kills - entry.tk_kills) / (float)entry.shots_total;
            }
            public float Math_score(XLREntry entry)
            {
                return Math_kd(entry) * Math_precision(entry) * 100;
            }
        }

        public volatile XLR_database xlr_database;

        public void XLR_OnServerStart()
        {
            xlr_database = new XLR_database();
            xlr_database.Init();
            PlayerConnected += XLR_OnPlayerConnected;
            OnPlayerKilledEvent += XLR_OnPlayerKilled;
            WriteLog.Info("Done initializing XLRstats.");
        }

        public void XLR_OnPlayerKilled(Entity player, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            try
            {
                xlr_database.Update(player.GUID, XLR_database.XLRUpdateFlags.death);

                if (!attacker.IsPlayer || (attacker == player))
                    return;

                XLR_database.XLRUpdateFlags flags = new XLR_database.XLRUpdateFlags();
                flags = flags | XLR_database.XLRUpdateFlags.kill;
                if (mod == "MOD_HEAD_SHOT")
                    flags = flags | XLR_database.XLRUpdateFlags.headshot;
                if (weapon == "throwingknife_mp")
                    flags = flags | XLR_database.XLRUpdateFlags.tk_kill;

                xlr_database.Update(attacker.GUID, flags);
            }
            catch (Exception e)
            {
                WriteLog.Error("Error at XLR::OnPlayerKilled");
                WriteLog.Error(e.Message);
            }
        }

        public void XLR_OnPlayerConnected(Entity player)
        {
            player.OnNotify("weapon_fired", new Action<Entity, Parameter>((_player, args) =>
            {
                xlr_database.Update(player.GUID, XLR_database.XLRUpdateFlags.weapon_fired);
            }));
        }
    }
}
