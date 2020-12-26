using System.Collections.Generic;
using System.IO;
using System;

namespace LambAdmin
{
    public partial class DHAdmin
    {

        public static string[] AllMapList = {"mp_alpha", "mp_bootleg", "mp_bravo", "mp_carbon", "mp_dome"
        , "mp_exchange", "mp_hardhat", "mp_interchange", "mp_lambeth", "mp_mogadishu", "mp_paris", "mp_plaza2",
        "mp_radar", "mp_seatown", "mp_underground", "mp_village", "mp_italy", "mp_park", "mp_morningwood", "mp_overwatch", "mp_aground_ss",
        "mp_courtyard_ss", "mp_cement", "mp_hillside_ss", "mp_meteora", "mp_qadeem", "mp_restrepo_ss", "mp_terminal_cls", "mp_crosswalk_ss",
        "mp_six_ss", "mp_burn_ss", "mp_shipbreaker", "mp_roughneck", "mp_nola", "mp_moab"};
        public static List<DSPLLine> DSPL = new List<DSPLLine>();
        public static int TotalWeight;
        public static Random Random = new Random();

        public class DSPLLine
        {
            public string[] maps;
            public string mode;
            public int weight;

            public DSPLLine(string line) : this(line.Split(',')) { }

            public DSPLLine(string[] parts)
            {
                if (parts[0].Trim() == "*")
                {
                    maps = AllMapList;
                }
                else
                {
                    maps = new string[parts.Length - 2];
                    for (int pp = 0; pp < parts.Length - 2; pp++)
                    {
                        maps[pp] = parts[pp].Trim();
                    }
                }
                mode = parts[parts.Length - 2].Trim();
                weight = int.Parse(parts[parts.Length - 1].Trim());
            }

            public string GetRandomMap()
            {
                return maps[Random.Next(maps.Length)];
            }
        }

        public void MR_Setup()
        {
            WriteLog.Debug("MR_setup");
            MR_ReadCurrentLine();
            MR_ReadDSPL();
            OnGameEnded += MR_PrepareRotation;
        }

        public void MR_ReadCurrentLine()
        {
            ConfigValues.sv_current_dsr = new DSPLLine(new StreamReader("players2\\RG.dspl").ReadLine()).mode + ".dsr";
        }

        public void MR_ReadDSPL()
        {
            foreach (string line in File.ReadAllLines("players2\\" + ConfigValues.Settings_dspl + ".dspl"))
            {
                string[] parts = line.Split(',');
                if (ConfigValues.Settings_dsr_repeat || parts[parts.Length - 2] != ConfigValues.sv_current_dsr.Split('.')[0])
                {
                    DSPLLine dsplLine = new DSPLLine(parts);
                    DSPL.Add(dsplLine);
                    TotalWeight += dsplLine.weight;
                }
            }
        }

        public DSPLLine MR_GetWeightedRandomLine()
        {
            int weightedRandom = Random.Next(TotalWeight);
            foreach (DSPLLine line in DSPL)
            {
                if (weightedRandom < line.weight)
                {
                    return line;
                }
                else
                {
                    weightedRandom -= line.weight;
                }
            }
            WriteLog.Error("There is a problem with the weights in the dspl " + ConfigValues.Settings_dspl);
            return null;
        }

        public void MR_PrepareRotation()
        {
            WriteLog.Info("Preparing DHAdmin map rotation.");
            DSPLLine line = MR_GetWeightedRandomLine();
            using (StreamWriter DSPLStream = new StreamWriter("players2\\RG.dspl"))
            {
                DSPLStream.WriteLine(line.GetRandomMap() + "," + line.mode + ",1000");
            }
        }

    }
}
