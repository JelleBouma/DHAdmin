using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InfinityScript;
using System.IO;

namespace LambAdmin
{
    public partial class DHAdmin
    {
        private int ProcessID = System.Diagnostics.Process.GetCurrentProcess().Id;

        private int DefaultKnifeAddress;
        private unsafe int* KnifeRange;
        private unsafe int* ZeroAddress;

        public unsafe void SetupKnife()
        {

            try
            {
                byte?[] search1 = new byte?[23]
                {
                  new byte?( 139),
                  new byte?(),
                  new byte?(),
                  new byte?(),
                  new byte?( 131),
                  new byte?(),
                  new byte?( 4),
                  new byte?(),
                  new byte?( 131),
                  new byte?(),
                  new byte?( 12),
                  new byte?( 217),
                  new byte?(),
                  new byte?(),
                  new byte?(),
                  new byte?( 139),
                  new byte?(),
                  new byte?( 217),
                  new byte?(),
                  new byte?(),
                  new byte?(),
                  new byte?( 217),
                  new byte?( 5)
                };
                KnifeRange = (int*)(FindMem(search1, 1, 4194304, 5242880) + search1.Length);
                if ((int)KnifeRange == search1.Length)
                {
                    byte?[] search2 = new byte?[25]
                    {
                        new byte?( 139),
                        new byte?(),
                        new byte?(),
                        new byte?(),
                        new byte?( 131),
                        new byte?(),
                        new byte?( 24),
                        new byte?(),
                        new byte?( 131),
                        new byte?(),
                        new byte?( 12),
                        new byte?( 217),
                        new byte?(),
                        new byte?(),
                        new byte?(),
                        new byte?( 141),
                        new byte?(),
                        new byte?(),
                        new byte?(),
                        new byte?( 217),
                        new byte?(),
                        new byte?(),
                        new byte?(),
                        new byte?( 217),
                        new byte?( 5)
                    };
                    KnifeRange = (int*)(FindMem(search2, 1, 4194304, 5242880) + search2.Length);
                    if ((int)KnifeRange == search2.Length)
                        KnifeRange = null;
                }
                DefaultKnifeAddress = *KnifeRange;
                byte?[] search3 = new byte?[24]
                {
                  new byte?( 217),
                  new byte?( 92),
                  new byte?(),
                  new byte?(),
                  new byte?( 216),
                  new byte?(),
                  new byte?(),
                  new byte?( 216),
                  new byte?(),
                  new byte?(),
                  new byte?( 217),
                  new byte?( 92),
                  new byte?(),
                  new byte?(),
                  new byte?( 131),
                  new byte?(),
                  new byte?( 1),
                  new byte?( 15),
                  new byte?( 134),
                  new byte?(),
                  new byte?( 0),
                  new byte?( 0),
                  new byte?( 0),
                  new byte?( 217)
                };
                ZeroAddress = (int*)(FindMem(search3, 1, 4194304, 5242880) + search3.Length + 2);

                if (!((int)KnifeRange != 0 && DefaultKnifeAddress != 0 && (int)ZeroAddress != 0))
                    WriteLog.Error("Error finding address: NoKnife Plugin will not work");
            }
            catch (Exception ex)
            {
                WriteLog.Error("Error in NoKnife Plugin. Plugin will not work.");
                WriteLog.Error(ex.ToString());
            }




            //looks bad, but actually the else will always be fired "first"

            if (DefaultKnifeAddress == (int)ZeroAddress)
            {
                if (!File.Exists(ConfigValues.ConfigPath + @"Knife\addr_" + ProcessID))
                {
                    //    print("now it will be feked");
                    WriteLog.Error("Error: NoKnife will not work.");
                    return;
                }

                // print("restoring proper knife addr");

                DefaultKnifeAddress = int.Parse(File.ReadAllText(ConfigValues.ConfigPath + @"Knife\addr_" + ProcessID));

                //  print("done");

            }
            else
            {
                File.WriteAllText(ConfigValues.ConfigPath + @"Knife\addr_" + ProcessID, DefaultKnifeAddress.ToString());     //save for when it's feked
                                                                                                                             //  print("knife def addr saved");
            }
        }

        public unsafe void DisableKnife()
        {
            *KnifeRange = (int)ZeroAddress;
        }

        public unsafe void EnableKnife()
        {
            *KnifeRange = DefaultKnifeAddress;
        }

        private unsafe int FindMem(byte?[] search, int num = 1, int start = 16777216, int end = 63963136)
        {
            try
            {
                int num1 = 0;
                for (int index1 = start; index1 < end; ++index1)
                {
                    byte* numPtr = (byte*)index1;
                    bool flag = false;
                    for (int index2 = 0; index2 < search.Length; ++index2)
                    {
                        if (search[index2].HasValue)
                        {
                            int num2 = *numPtr;
                            byte? nullable = search[index2];
                            if ((num2 != nullable.GetValueOrDefault() ? 1 : (!nullable.HasValue ? 1 : 0)) != 0)
                                break;
                        }
                        if (index2 == search.Length - 1)
                        {
                            if (num == 1)
                            {
                                flag = true;
                            }
                            else
                            {
                                ++num1;
                                if (num1 == num)
                                    flag = true;
                            }
                        }
                        else
                            ++numPtr;
                    }
                    if (flag)
                        return index1;
                }
            }
            catch (Exception ex)
            {
                WriteLog.Error("Error in DHAdmin::FindMem:" + ex.Message);
            }
            return 0;
        }
    }
}
