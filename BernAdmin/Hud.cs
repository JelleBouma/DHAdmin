using System.Collections.Generic;
using System.IO;
using InfinityScript;

namespace LambAdmin
{
    public partial class DHAdmin
    {

        void HUD_PrecacheShaders()
        {
            string file = ConfigValues.ConfigPath + @"Hud\precacheshaders.txt";
            if (File.Exists(file))
            {
                foreach (string line in File.ReadAllLines(file))
                {
                    GSCFunctions.PreCacheShader(line);
                }
            }
        }

    }
}
