using System;
using System.Collections.Generic;
using InfinityScript;
namespace LambAdmin
{
    public partial class DHAdmin
    {

        public static Entity _airdropCollision = getCrateCollision(false);

        public static string[] gunModels = { "weapon_ak47_iw5", "weapon_scar_iw5", "weapon_mp5_iw5", "weapon_p90_iw5",  "weapon_m60_iw5", "weapon_as50_iw5",
            "weapon_remington_msr_iw5",  "weapon_aa12_iw5", "weapon_model1887", "weapon_smaw",
            "weapon_xm25", "weapon_m320_gl", "weapon_m4_iw5", "weapon_m16_iw5", "weapon_cm901", "weapon_type95_iw5", "weapon_remington_acr_iw5", "weapon_m14_iw5", "weapon_g36_iw5", "weapon_fad_iw5", "weapon_ump45_iw5", "weapon_pp90m1_iw5", "weapon_uzi_m9_iw5", "weapon_mp7_iw5",
            "weapon_dragunov_iw5", "weapon_m82_iw5", "weapon_l96a1_iw5", "weapon_rsass_iw5", "weapon_sa80_iw5", "weapon_mg36", "weapon_pecheneg_iw5", "weapon_mk46_iw5", "weapon_usas12_iw5", "weapon_ksg_iw5", "weapon_spas12_iw5", "weapon_striker_iw5", "weapon_rpg7"
        };
        public static string[] gunNames = { "iw5_ak47_mp", "iw5_scar_mp", "iw5_mp5_mp", "iw5_p90_mp", "iw5_m60_mp", "iw5_as50_mp_as50scope",
            "iw5_msr_mp_msrscope", "iw5_aa12_mp", "iw5_1887_mp", "iw5_smaw_mp",
            "xm25_mp", "m320_mp", "iw5_m4_mp", "iw5_m16_mp", "iw5_cm901_mp", "iw5_type95_mp", "iw5_acr_mp", "iw5_mk14_mp", "iw5_g36c_mp", "iw5_fad_mp", "iw5_ump45_mp", "iw5_pp90m1_mp", "iw5_m9_mp", "iw5_mp7_mp",
            "iw5_dragunov_mp_dragunovscope", "iw5_barrett_mp_barrettscope", "iw5_l96a1_mp_l96a1scope", "iw5_rsass_mp_rsassscope", "iw5_sa80_mp", "iw5_mg36_mp", "iw5_pecheneg_mp", "iw5_mk46_mp", "iw5_usas12_mp", "iw5_ksg_mp", "iw5_spas12_mp", "iw5_striker_mp", "rpg_mp"
        };

        Entity mund;

        List<Entity> extraExplodables = new List<Entity>();
        List<Entity> objectives = new List<Entity>();
        public int fx_explode;
        public int fx_smoke;
        public int fx_fire;

        public void ME_OnServerStart()
        {
            if (GSCFunctions.GetDvar("mapname") == "mp_hardhat")
            {
                extraExplodables.AddRange(spawnBarrels(new Vector3(488, -200, 288), new Vector3(493, -65, 288), 4, true, 4f, false));
                extraExplodables.AddRange(spawnBarrels(new Vector3(-164, -240, 288), new Vector3(-168, -107, 288), 4, true, 4f, false));
                extraExplodables.AddRange(spawnBarrels(new Vector3(1629, 151, 185), new Vector3(1629, 151, 185), 1, true, 4f, false));
                extraExplodables.AddRange(spawnBarrels(new Vector3(1380, 698, 184), new Vector3(1380, 698, 184), 1, true, 4f, false));
                extraExplodables.AddRange(spawnJeep(new Vector3(790, -280, 282)));
            }
            if (GSCFunctions.GetDvar("mapname") == "mp_dome")
            {
                extraExplodables.AddRange(spawnBarrels(new Vector3(346, 1063, -314), new Vector3(363, 1240, -308), 4, true, 4f, false));
                extraExplodables.AddRange(spawnBarrels(new Vector3(-198, 803, -323), new Vector3(-198, 803, -323), 1, true, 4f, false));
                extraExplodables.AddRange(spawnBarrels(new Vector3(958, 2470, -254), new Vector3(958, 2470, -254), 1, true, 4f, false));
                extraExplodables.AddRange(spawnHummer(new Vector3(-627, 103, -415)));
            }
            if (GSCFunctions.GetDvar("mapname") == "mp_carbon")
            {
                extraExplodables.AddRange(spawnBarrels(new Vector3(-3625, -2983, 3618), new Vector3(-3460, -2983, 3618), 4, true, 2f, false));
                extraExplodables.AddRange(spawnBarrels(new Vector3(-3620, -3020, 3618), new Vector3(-3490, -3020, 3618), 4, true, 2f, false));
                extraExplodables.AddRange(spawnBarrels(new Vector3(-1889, -3755, 3787), new Vector3(-1910, -3830, 3787), 2, true, 3f, false));
                extraExplodables.AddRange(spawnBarrels(new Vector3(-843, -3922, 3922), new Vector3(-843, -3922, 3922), 1, true, 2f, false));
                extraExplodables.AddRange(spawnBarrels(new Vector3(-1198, -3303, 3922), new Vector3(-1198, -3303, 3922), 1, true, 2f, false));
                extraExplodables.AddRange(spawnTruck(new Vector3(-1190, -4200, 3776), new Vector3(3, 150, -2)));
                extraExplodables.AddRange(spawnTruck(new Vector3(-450, -3680, 3898), new Vector3(-1, 150, 1)));
            }
            if (GSCFunctions.GetDvar("mapname") == "mp_roughneck")
            {
                extraExplodables.AddRange(spawnBarrels(new Vector3(-1101, 242, -8), new Vector3(-1101, 242, -8), 1, true, 1f, true));
                extraExplodables.AddRange(spawnBarrels(new Vector3(1536, -261, -180), new Vector3(1536, -261, -180), 1, true, 1f, true));
                extraExplodables.AddRange(spawnBarrels(new Vector3(-1563, -1030, -172), new Vector3(-1563, -1030, -172), 1, true, 1f, true));
                extraExplodables.AddRange(spawnBarrels(new Vector3(-2330, 2152, 514), new Vector3(-2257, 2227, 513), 2, true, 1f, true)); // highpoint
                extraExplodables.AddRange(spawnBarrels(new Vector3(1592, 422, -172), new Vector3(1570, 445, -172), 2, true, 1f, true)); // flammable double
                extraExplodables.AddRange(spawnBarrels(new Vector3(1443, 668, -172), new Vector3(1443, 668, -172), 1, true, 1f, true)); // flammable single
            }
            if (GSCFunctions.GetDvar("mapname") == "mp_hillside_ss")
            {
                GSCFunctions.PreCacheShader("iw5_cardicon_capsule");
                GSCFunctions.PreCacheShader("cardicon_treasurechest");
                GSCFunctions.PreCacheShader("iw5_cardicon_frank");
                GSCFunctions.PreCacheShader("iw5_cardicon_elite_17");
            }
        }

        public void ME_ConfigValues_Apply()
        {
            if (ConfigValues.settings_skullmund)
                spawnMund();
            if (ConfigValues.settings_snd)
            {
                fx_explode = GSCFunctions.LoadFX("explosions/tanker_explosion");
                fx_smoke = GSCFunctions.LoadFX("smoke/car_damage_blacksmoke");
                fx_fire = GSCFunctions.LoadFX("smoke/car_damage_blacksmoke_fire");
                spawnObjectives();
            }
            if (!ConfigValues.settings_extra_explodables)
                deleteExtraExplodables();
            explosive_barrel_melee_damage();
        }

        public void deleteExtraExplodables()
        {
            foreach (Entity ent in extraExplodables)
            {
                ent.Delete();
            }
        }

        public void spawnObjectives()
        {
            objectives.Add(SpawnObjective(new Vector3(882, -666, 2180), new Vector3(0, 0, 0), new Vector3(898, -668, 2188), new Vector3(0, -35, 0), false, "iw5_cardicon_capsule", "Cocaine Garage"));
            objectives.Add(SpawnObjective(new Vector3(959, 101, 2208), new Vector3(0, 0, 0), new Vector3(954, 99, 2212), new Vector3(0, 220, 0), false, "cardicon_treasurechest", "Treasure Vault"));
            objectives.Add(SpawnObjective(new Vector3(1213, -536, 2316), new Vector3(0, 0, 0), new Vector3(1231, -505, 2316), new Vector3(0, 0, 0), false, "iw5_cardicon_frank", "Frankenstein Book Collection"));
            objectives.Add(SpawnObjective(new Vector3(825, -840, 2316), new Vector3(0, 90, 0), new Vector3(838, -782, 2316), new Vector3(0, 180, 0), true, "iw5_cardicon_elite_17", "Warhead Pallet"));
            foreach (Entity objective in objectives)
            {
                OnInterval(500, () =>
                {
                    TickBomb(objective);
                    return !objective.GetField<bool>("destroyed");
                });
            }
        }

        public void spawnFrankenstein(Vector3 origin)
        {
            Entity frankenstein = GSCFunctions.Spawn("script_model", origin);
            frankenstein.SetModel("accessories_book03");
        }

        int objectiveID = 31;
        public Entity SpawnObjective(Vector3 origin, Vector3 angles, Vector3 bomb_origin, Vector3 bomb_angles, bool visible, string icon, string name)
        {
            Entity objective = GSCFunctions.Spawn("script_model", origin);
            objective.Angles = angles;
            Entity bomb = GSCFunctions.Spawn("script_model", bomb_origin);
            bomb.SetModel("prop_suitcase_bomb");
            bomb.Angles = bomb_angles;
            bomb.Hide();
            objective.SetField("suitcase", bomb);
            objective.SetField("destroyed", false);
            objective.SetField("ticks", 0);
            objective.SetField("objectiveID", objectiveID);
            objective.SetField("name", name);
            GSCFunctions.Objective_Add(objectiveID, "active", origin, icon);
            objectiveID--;
            if (visible)
            {
                objective.SetModel("com_bomb_objective");
                Entity exploder = GSCFunctions.Spawn("script_model", new Vector3(origin.X, origin.Y - 2, origin.Z + 2));
                exploder.SetModel("com_bomb_objective_d");
                exploder.Angles = new Vector3(angles.X, angles.Y - 180, angles.Z);
                exploder.Hide();
                objective.SetField("exploder", exploder);
                spawnCrate(new Vector3(origin.X - 14, origin.Y + 6, origin.Z + 30), new Vector3(0, 0, 0), false);
                spawnCrate(new Vector3(origin.X - 14, origin.Y - 3, origin.Z + 30), new Vector3(0, 0, 0), false);
                spawnCrate(new Vector3(origin.X + 6, origin.Y + 6, origin.Z + 30), new Vector3(0, 0, 0), false);
                spawnCrate(new Vector3(origin.X + 6, origin.Y - 3, origin.Z + 30), new Vector3(0, 0, 0), false);
            }
            else
            {
                objective.Hide();
            }
            return objective;
        }

        public void TickBomb(Entity objective)
        {
            if(objective.HasField("bomb"))
            {
                int ticks = objective.GetField<int>("ticks") + 1;
                if (ticks == 80)
                    Explode(objective, IsLast(objective));
                objective.SetField("ticks", ticks);
            }
            else
            {
                objective.SetField("ticks", 0);
            }
        }

        public bool IsLast(Entity last)
        {
            foreach (Entity objective in objectives) {
                if (objective != last && !objective.GetField<bool>("destroyed"))
                    return false;
            }
            return true;
        }

        public void Explode(Entity objective, bool isLast)
        {
            Entity destroyer = objective.GetField<Entity>("bomb");
            int score = destroyer.GetField<int>("score");
            if (isLast)
                destroyer.SetField("score", score + 3000000);
            else
                destroyer.SetField("score", score + 1000000);
            objective.SetField("destroyed", true);
            GSCFunctions.PlayFX(fx_explode, objective.Origin);
            objective.PlaySound("cobra_helicopter_crash");
            objective.GetField<Entity>("suitcase").Hide();
            objective.Hide();
            GSCFunctions.Objective_Delete(objective.GetField<int>("objectiveID"));
            objective.RadiusDamage(objective.Origin, 200, 200, 40, destroyer, "MOD_EXPLOSIVE", "com_bomb_objective");
            if (objective.HasField("exploder"))
            {
                objective.GetField<Entity>("exploder").Show();
            }
            objective.PlayLoopSound("fire_vehicle_med");
            OnInterval(400, () =>
            {
                GSCFunctions.PlayFX(fx_fire, objective.Origin);
                GSCFunctions.PlayFX(fx_smoke, objective.Origin);
                return true;
            });
            if (isLast)
            {
                AfterDelay(500, () =>
                {
                    CMD_end();
                });
            }
        }

        public void spawnMund()
        {
            Random random = new Random();
            int gun = (int)Math.Floor(random.NextDouble() * gunNames.Length);
            Vector3 origin;
            int radius = 153;
            int points = 35;
            int stepIn = 10;
            int stepUp = 9;
            int crateStepUp = 3;
            bool crateVisible = false;
            origin = new Vector3(1543f, 307f, 228f);
            int counter = 0;
            while (radius > stepIn)
            {
                for (int ii = 0; ii < points; ii++)
                {
                    double slice = 2 * Math.PI / points;
                    double angle = slice * ii;
                    int newX = (int)(origin.X + radius * Math.Cos(angle));
                    int newY = (int)(origin.Y + radius * Math.Sin(angle));
                    Entity skulls = GSCFunctions.Spawn("script_model", new Vector3(newX, newY, origin.Z));
                    skulls.SetModel("africa_skulls_pile_large");
                    skulls.Angles = new Vector3((float)(random.NextDouble() * 5f), (float)(random.NextDouble() * 360f), (float)(random.NextDouble() * 5f));
                    if (counter % crateStepUp == 0)
                    {
                        spawnCrate(new Vector3(newX, newY, origin.Z), new Vector3(40f, ii * 1f / (points * 1f) * 360f, 0), crateVisible);
                    }
                }
                radius -= stepIn;
                origin.Z += stepUp;
                points = (int)(radius * 2 * Math.PI * 0.035f + 1);
                counter++;
            }
            origin.Z += 70;
            Entity gunEnt = GSCFunctions.Spawn("script_model", origin);
            gunEnt.SetModel(gunModels[gun]);
            gunEnt.SetField("gun_name", gunNames[gun]);
            gunEnt.Angles = new Vector3(-90, 0, 0);
            OnInterval(3000, () =>
            {
                gunEnt.RotateRoll(360, 3); return true;
            });
            origin.Z -= 90;
            origin.X -= 6;
            spawnCrate(origin, new Vector3(90, 0, 0), crateVisible);
            origin.X += 12;
            spawnCrate(origin, new Vector3(90, 0, 0), crateVisible);
            origin.X -= 6;
            origin.Y -= 6;
            spawnCrate(origin, new Vector3(90, 0, 0), crateVisible);
            origin.Y += 12;
            spawnCrate(origin, new Vector3(90, 0, 0), crateVisible);
            spawnBarrels(new Vector3(1500, 1370, 238), new Vector3(1310, 1305, 239), 6, false, 4f, false);
            spawnCollision(new Vector3(1500, 1370, 275), new Vector3(1310, 1305, 275), new Vector3(0, 20, 0), 4, false);
            spawnJunk(new Vector3(535, 360, 277), new Vector3(500, 475, 263), new Vector3(400, 540, 263), new Vector3(340, 580, 250));
            spawnCollision(new Vector3(666, 360, 300), new Vector3(500, 520, 300), new Vector3(0, -45, 0), 4, false);
            spawnCollision(new Vector3(666, 360, 360), new Vector3(500, 520, 360), new Vector3(0, -45, 0), 4, false);
            spawnCollision(new Vector3(490, 530, 300), new Vector3(255, 639, 300), new Vector3(0, -30, 0), 5, false);
            spawnCollision(new Vector3(490, 530, 360), new Vector3(255, 639, 360), new Vector3(0, -30, 0), 5, false);
            mund = gunEnt;
        }

        List<Entity> spawnBarrels(Vector3 origin, Vector3 end, float amount, bool explosive, float var, bool endOnLast)
        {
            List<Entity> list = new List<Entity>();
            Random random = new Random();
            for (int ii = 0; ii < amount; ii++)
            {
                float progress = endOnLast && amount > 1 ? ii / (amount - 1) : ii / amount;
                Entity barrel = GSCFunctions.Spawn("script_model", new Vector3(origin.X + (end.X - origin.X) * progress, origin.Y + (end.Y - origin.Y) * progress, origin.Z + (end.Z - origin.Z) * progress));
                barrel.Angles = new Vector3((float)(random.NextDouble() * var), (float)(random.NextDouble() * 360f), (float)(random.NextDouble() * var));
                if (explosive)
                {
                    barrel.SetModel("com_barrel_benzin");
                    barrel.TargetName = "explodable_barrel";
                    Entity crate = spawnCrate(new Vector3(barrel.Origin.X, barrel.Origin.Y, barrel.Origin.Z + 30), new Vector3(90, 0, 0), false);
                    list.Add(crate);
                    barrel.SetField("collision", crate);
                    barrel.OnNotify("exploding", barrel_explosion_think);
                }
                else
                {
                    barrel.SetModel("com_barrel_biohazard_rust");
                }
                list.Add(barrel);
            }
            return list;
        }

        void barrel_explosion_think(Entity barrel)
        {
            barrel.GetField<Entity>("collision").Delete();
        }

        List<Entity> spawnJeep(Vector3 origin)
        {
            List<Entity> list = new List<Entity>();
            Entity jeep = GSCFunctions.Spawn("script_model", origin);
            jeep.Angles = new Vector3(0, 99, 5);
            jeep.SetModel("vehicle_jeep_destructible");
            jeep.TargetName = "destructible_vehicle";
            list.Add(jeep);
            list.AddRange(spawnCollision(new Vector3(origin.X + 50, origin.Y - 100, origin.Z + 30), new Vector3(origin.X + 30, origin.Y + 60, origin.Z + 30), new Vector3(0, 40, 0), 5, false));
            return list;
        }

        List<Entity> spawnHummer(Vector3 origin)
        {
            List<Entity> list = new List<Entity>();
            Entity hummer = GSCFunctions.Spawn("script_model", origin);
            hummer.Angles = new Vector3(1, -40, 0);
            hummer.SetModel("vehicle_hummer_destructible");
            hummer.TargetName = "destructible_vehicle";
            list.Add(hummer);
            list.AddRange(spawnCollision(new Vector3(origin.X - 65, origin.Y + 65, origin.Z + 30), new Vector3(origin.X + 96, origin.Y - 58, origin.Z + 30), new Vector3(0, 50, 0), 5, false));
            list.AddRange(spawnCollision(new Vector3(origin.X - 75, origin.Y + 45, origin.Z + 30), new Vector3(origin.X + 86, origin.Y - 78, origin.Z + 30), new Vector3(0, 50, 0), 5, false));
            return list;
        }

        List<Entity> spawnTruck(Vector3 origin, Vector3 angles)
        {
            List<Entity> list = new List<Entity>();
            Entity truck = GSCFunctions.Spawn("script_model", origin);
            truck.Angles = angles;
            truck.SetModel("vehicle_pickup_destructible_mp");
            truck.TargetName = "destructible_vehicle";
            list.Add(truck);
            list.AddRange(spawnCollision(new Vector3(origin.X - 65, origin.Y + 50, origin.Z + 30), new Vector3(origin.X + 118, origin.Y - 58, origin.Z + 30), new Vector3(0, 57, 0), 5, false));
            list.AddRange(spawnCollision(new Vector3(origin.X - 75, origin.Y + 30, origin.Z + 30), new Vector3(origin.X + 108, origin.Y - 78, origin.Z + 30), new Vector3(0, 57, 0), 5, false));
            list.AddRange(spawnCollision(new Vector3(origin.X - 7, origin.Y + 5, origin.Z + 60), new Vector3(origin.X - 7, origin.Y + 5, origin.Z + 50), new Vector3(0, 57, 0), 1, false));
            return list;
        }

        void spawnJunk(Vector3 junk1, Vector3 junk2, Vector3 junk3, Vector3 junk4)
        {
            Entity junk1Ent = GSCFunctions.Spawn("script_model", junk1);
            junk1Ent.SetModel("afr_junk_scrap_pile_01");
            Entity junk2Ent = GSCFunctions.Spawn("script_model", junk2);
            junk2Ent.SetModel("junk_scrap_pile_03");
            Entity junk3Ent = GSCFunctions.Spawn("script_model", junk3);
            junk3Ent.SetModel("junk_scrap_pile_03");
            Entity junk4Ent = GSCFunctions.Spawn("script_model", junk4);
            junk4Ent.SetModel("junk_scrap_pile_03");
        }

        List<Entity> spawnCollision(Vector3 origin, Vector3 end, Vector3 angle, float amount, bool visible)
        {
            List<Entity> list = new List<Entity>();
            for (int ii = 0; ii < amount; ii++)
            {
                float progress = ii / amount;
                list.Add(spawnCrate(new Vector3(origin.X + (end.X - origin.X) * progress, origin.Y + (end.Y - origin.Y) * progress, origin.Z + (end.Z - origin.Z) * progress), angle, visible));
            }
            return list;
        }

        void trackObjectivesForPlayer(Entity player)
        {
            player.NotifyOnPlayerCommand("use_button_pressed", "+activate");
            player.OnNotify("use_button_pressed", tryToUseBomb);
            OnInterval(250, () => handleObjectivesMessage(player));
            void tryToUseBomb(Entity user)
            {
                foreach (Entity objective in objectives)
                {
                    if (!objective.GetField<bool>("destroyed") && (!objective.HasField("bomb") || objective.GetField<Entity>("bomb") != user) && user.Origin.DistanceTo(objective.Origin) <= 100)
                    {
                        string switchback = user.CurrentWeapon;
                        user.GiveWeapon("briefcase_bomb_mp");
                        user.SwitchToWeapon("briefcase_bomb_mp");
                        AfterDelay(4000, () =>
                        {
                            WriteLog.Debug(user.CurrentWeapon);
                            if (user.CurrentWeapon == "briefcase_bomb_mp")
                            {
                                if (objective.HasField("bomb"))
                                {
                                    objective.ClearField("bomb");
                                    objective.GetField<Entity>("suitcase").Hide();
                                    WriteChatToAll("^3" + objective.GetField<string>("name") + ": ^4bomb defused");
                                }
                                else
                                {
                                    objective.SetField("bomb", user);
                                    objective.GetField<Entity>("suitcase").Show();
                                    WriteChatToAll("^3" + objective.GetField<string>("name") + ": ^1bomb planted");
                                }
                            }
                            user.TakeWeapon("briefcase_bomb_mp");
                            user.SwitchToWeapon(switchback);
                        });
                    }
                }
            }
        }

        bool handleObjectivesMessage(Entity player)
        {
            HudElem message = getUsablesMessage(player);
            foreach (Entity objective in objectives)
            {
                if (!objective.GetField<bool>("destroyed") && player.Origin.DistanceTo(objective.Origin) <= 100)
                {
                    player.DisableWeaponPickup();
                    if (objective.HasField("bomb") && objective.GetField<Entity>("bomb").Name != player.Name)
                    {
                        handleMessage(player, objective, "Press ^3[{+activate}] ^7to defuse bomb");
                        return true;
                    }
                    else if(!objective.HasField("bomb"))
                    {
                        handleMessage(player, objective, "Press ^3[{+activate}] ^7to plant bomb");
                        return true;
                    }
                }
            }
            dontDisplayMessage(player, message);
            player.EnableWeaponPickup();
            return true;
        }

        void trackGunForPlayer(Entity player, Entity gun)
        {
            player.NotifyOnPlayerCommand("use_button_pressed", "+activate");
            player.OnNotify("use_button_pressed", tryToGetGun);
            OnInterval(250, () => handleMessage(player, gun, "Press ^3[{+activate}] ^7to get weapon"));
            void tryToGetGun(Entity receiver)
            {
                if (receiver.Origin.DistanceTo(gun.Origin) <= 100)
                {
                    string gunName = gun.GetField<string>("gun_name");
                    receiver.GiveWeapon(gunName);
                    receiver.SetWeaponAmmoStock(gunName, 99);
                    receiver.SetWeaponAmmoClip(gunName, 99);
                    AfterDelay(50, () =>
                    {
                        receiver.SwitchToWeaponImmediate(gunName);
                    });
                }
            }
        }

        bool handleMessage(Entity player, Entity ent, string text)
        {
            HudElem message = getUsablesMessage(player);
            if (player.Origin.DistanceTo(ent.Origin) <= 100)
            {
                displayMessage(player, message, text);
            }
            else
            {
                dontDisplayMessage(player, message);
            }
            return true;
        }

        HudElem getUsablesMessage(Entity player)
        {
            if (!player.HasField("hud_message"))
            {
                HudElem msg = HudElem.CreateFontString(player, HudElem.Fonts.Default, 1.6f);
                msg.SetPoint("CENTER", "CENTER", 0, 110);
                msg.HideWhenInMenu = true;
                msg.HideWhenDead = true;
                msg.Alpha = 0;
                msg.Archived = true;
                msg.Sort = 20;
                player.SetField("hud_message", msg);
            }
            return player.GetField<HudElem>("hud_message");
        }

        void displayMessage(Entity player, HudElem message, string text)
        {
            message.Alpha = .85f;
            message.SetText(text);
        }

        void dontDisplayMessage(Entity player, HudElem message)
        {
            message.Alpha = 0;
            message.SetText("");
        }

        public static Entity getCrateCollision(bool altCrate)
        {
            Entity cp;
            cp = GSCFunctions.GetEnt("airdrop_crate", "targetname");
            if (cp != null && altCrate) return GSCFunctions.GetEnt(cp.Target, "targetname");
            else
            {
                cp = GSCFunctions.GetEnt("care_package", "targetname");
                return GSCFunctions.GetEnt(cp.Target, "targetname");
            }
        }

        public static Entity spawnCrate(Vector3 origin, Vector3 angles, bool visible)
        {
            Entity ent = GSCFunctions.Spawn("script_model", origin);
            if (visible) ent.SetModel("com_plasticcase_friendly");
            ent.Angles = angles;
            ent.CloneBrushModelToScriptModel(_airdropCollision);
            ent.SetContents(1);
            return ent;
        }

        public void explosive_barrel_melee_damage()
        {
            for (int ii = 0; ii < 2048; ii++)
            {
                Entity ent = Entity.GetEntity(ii);
                if (ent.TargetName == "explodable_barrel")
                {
                    ent.OnNotify("damage", barrel_damage_think);
                }
            }
        }

        public void barrel_damage_think(Entity barrel, Parameter amount, Parameter attacker, Parameter direction_vec, Parameter P, Parameter type, Parameter modelName, Parameter partName, Parameter tagName, Parameter iDFlags, Parameter weapon)
        {
            if ((string)type != "MOD_MELEE" && (string)type != "MOD_IMPACT") return;
            if (barrel.TargetName != "explodable_barrel") return;
            AfterDelay(100, () =>
                barrel.Notify("damage", 40, attacker, direction_vec, P, "MOD_PISTOL_BULLET", modelName, partName, tagName, iDFlags, weapon));
        }

    }
}