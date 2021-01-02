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

        public class DSPLLine
        {
            public List<string> maps = new List<string>();
            public string mode;
            public int weight;

            public DSPLLine(string line) : this(line.Split(',')) { }

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

        public void MR_ReadCurrentLine()
        {
            using (StreamReader DSPLStream = new StreamReader(DH))
                ConfigValues.Current_DSR = new DSPLLine(DSPLStream.ReadLine()).mode + ".dsr";
        }

        public void MR_ReadDSPL()
        {
            if (CFG_FindServerFile(ConfigValues.Settings_dspl + ".dspl", out string dsplFile))
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
            else
                WriteLog.Error("DSPL file does not exist: " + dsplFile + ", please set \"settings_dspl\" in settings.txt to the dspl file you want to use.");
        }

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

        public void MR_PrepareRotation()
        {
            WriteLog.Info("Preparing DHAdmin map rotation.");
            DSPLLine line = MR_GetWeightedRandomLine();
            using (StreamWriter DSPLStream = new StreamWriter(DH))
                DSPLStream.Write(line.maps.GetRandom() + "," + line.mode + ",1000");
        }

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
