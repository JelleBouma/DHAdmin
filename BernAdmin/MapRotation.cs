using System.Collections.Generic;
using System.IO;
using System;

namespace LambAdmin
{
    public partial class DGAdmin
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
                TotalWeight += weight;
            }

            public string GetRandomMap()
            {
                return maps[Random.Next(maps.Length)];
            }
        }

        public void MR_Setup()
        {
            MR_ReadCurrentLine();
            MR_ReadDSPL();
            OnGameEnded += MR_OnGameEnded;
        }

        public void MR_OnGameEnded()
        {
            AfterDelay(11100, () =>
            {
                MR_Rotate();
            });
        }

        public void MR_ReadCurrentLine()
        {
            ConfigValues.sv_current_dsr = new DSPLLine(new StreamReader("players2\\RG.dspl").ReadLine()).mode + ".dsr";
        }

        public void MR_ReadDSPL()
        {
            foreach (string line in File.ReadAllLines("players2\\" + ConfigValues.settings_dspl + ".dspl"))
            {
                string[] parts = line.Split(',');
                if (ConfigValues.settings_dsr_repeat || parts[parts.Length - 2] != ConfigValues.sv_current_dsr.Split('.')[0])
                    DSPL.Add(new DSPLLine(parts));
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
            WriteLog.Error("There is a problem with the weights in the dspl " + ConfigValues.settings_dspl);
            return null;
        }

        public void MR_Rotate()
        {
            WriteLog.Info("Using DHAdmin map rotation.");
            DSPLLine line = MR_GetWeightedRandomLine();
            CMD_mode(line.mode, line.GetRandomMap());
        }

    }
}
