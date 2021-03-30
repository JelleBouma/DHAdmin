using System.Collections.Generic;
using System.IO;
using System;

namespace LambAdmin
{
    public partial class DHAdmin
    {
        public static List<DSPLLine> DSPL = new List<DSPLLine>();
        public static int TotalWeight;
        public static Random Random = new Random();
        public static string DH;

        /// <summary>
        /// A DSPL line.
        /// The DSPL is the file which contains the possible map and game-mode (DSR) combinations to switch to and their weights (higher weight = more likely to be picked).
        /// Each line contains one or more maps, one game-mode (DSR) and the weight for this line to be picked.
        /// </summary>
        public class DSPLLine
        {
            public List<string> maps = new List<string>();
            public string mode;
            public int weight;

            /// <summary>
            /// Parse a DSPLLine from a string.
            /// </summary>
            public DSPLLine(string line) : this(line.Split(',')) { }

            /// <summary>
            /// Parse a DSPLLine from the string parts. Each map is a part, the mode is a part and the weight is a part.
            /// </summary>
            public DSPLLine(string[] parts)
            {
                if (parts[0].Trim() == "*")
                    maps = ConfigValues.AvailableMaps.GetValues();
                else
                    for (int pp = 0; pp < parts.Length - 2; pp++)
                        maps.Add(parts[pp].Trim());
                mode = parts[parts.Length - 2].Trim();
                weight = int.Parse(parts[parts.Length - 1].Trim());
            }
        }

        /// <summary>
        /// Set up the map rotation.
        /// </summary>
        public void MR_Setup()
        {
            WriteLog.Debug("MR_setup");
            DH = CFG_FindServerFile("DH.dspl");
            if (ConfigValues.Settings_enable_dlcmaps)
                ConfigValues.AvailableMaps = Data.AllMapNames;
            MR_ReadCurrentLine();
            MR_ReadDSPL();
            OnGameEnded += MR_PrepareRotation;
        }

        /// <summary>
        /// Read the current game mode from "DH.dspl".
        /// </summary>
        public void MR_ReadCurrentLine()
        {
            using (StreamReader DSPLStream = new StreamReader(DH))
                ConfigValues.Current_DSR = new DSPLLine(DSPLStream.ReadLine()).mode + ".dsr";
        }

        /// <summary>
        /// Read the DSPL in accordance with "settings_dspl" and "settings_dsr_repeat".
        /// </summary>
        public void MR_ReadDSPL()
        {
            if (CFG_FindServerFile(ConfigValues.Settings_dspl + ".dspl", out string dsplFile))
            {
                foreach (string line in File.ReadAllLines(dsplFile))
                    if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("//"))
                    {
                        string[] parts = line.Split("//")[0].Split(',');
                        if (ConfigValues.Settings_dsr_repeat || parts[parts.Length - 2] != ConfigValues.Current_DSR.Split('.')[0])
                        {
                            DSPLLine dsplLine = new DSPLLine(parts);
                            DSPL.Add(dsplLine);
                            TotalWeight += dsplLine.weight;
                        }
                    }
            }
            else
                WriteLog.Error("DSPL file does not exist: " + dsplFile + ", please set \"settings_dspl\" in settings.txt to the dspl file you want to use.");
        }

        /// <summary>
        /// Pick a DSPLLine randomly in accordance with their weights (higher weight = higher chance to be picked).
        /// </summary>
        /// <returns>The randomly picked DSPLLine</returns>
        public DSPLLine MR_GetWeightedRandomLine()
        {
            int weightedRandom = Random.Next(TotalWeight);
            foreach (DSPLLine line in DSPL)
                if (weightedRandom < line.weight)
                    return line;
                else
                    weightedRandom -= line.weight;
            WriteLog.Error("There is a problem with the weights in the dspl " + ConfigValues.Settings_dspl);
            return null;
        }

        /// <summary>
        /// Pick a (weighted) random DSPLLine and write it to DH.dspl for TeknoMW3 to read on map rotation.
        /// </summary>
        public void MR_PrepareRotation()
        {
            WriteLog.Info("Preparing DHAdmin map rotation.");
            DSPLLine line = MR_GetWeightedRandomLine();
            using (StreamWriter DSPLStream = new StreamWriter(DH))
                DSPLStream.Write(line.maps.GetRandom() + "," + line.mode + ",1000");
        }

        /// <summary>
        /// Immediately switch mode (and optionally map).
        /// </summary>
        /// <param name="dsrname">Name of the DSR (gamemode) without extension ".dsr"</param>
        /// <param name="map">Code of the map, for example "mp_village".</param>
        public void MR_SwitchModeImmediately(string dsrname, string map = "")
        {
            if (string.IsNullOrWhiteSpace(map))
                map = ConfigValues.Mapname;
            map = map.Replace("default:", "");
            using (StreamWriter DSPLStream = new StreamWriter(DH))
                DSPLStream.Write(map + "," + dsrname + ",1000");
            OnExitLevel();
            ExecuteCommand("map_rotate");
        }

    }
}
