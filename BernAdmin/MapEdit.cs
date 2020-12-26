﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InfinityScript;
namespace LambAdmin
{
    public partial class DHAdmin
    {
        List<Entity> extraExplodables = new List<Entity>();
        List<Entity> Objectives = new List<Entity>();
        List<Entity> WeaponPickups = new List<Entity>();
        static event Action<Entity, Entity> OnWeaponPickup = (t1, t2) => { };
        static event Action<Entity, Entity> OnObjectiveDestroy = (destroyer, objective) => { };
        private static Entity _airdropCollision = getCrateCollision();
        private static int fx_explode;
        private static int fx_smoke;
        private static int fx_fire;
        private static int redcircle_fx = GSCFunctions.LoadFX("misc/ui_flagbase_red");
        private static int goldcircle_fx = GSCFunctions.LoadFX("misc/ui_flagbase_gold");

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
                extraExplodables.AddRange(SpawnHummer(new Vector3(-627, 103, -415)));
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
        }

        public void ME_ConfigValues_Apply()
        {
            if (ConfigValues.Settings_map_edit != "")
                ME_Load();
            //if (ConfigValues.settings_snd)
            //{
                //fx_explode = GSCFunctions.LoadFX("explosions/tanker_explosion");
                //fx_smoke = GSCFunctions.LoadFX("smoke/car_damage_blacksmoke");
                //fx_fire = GSCFunctions.LoadFX("smoke/car_damage_blacksmoke_fire");
                //SpawnObjectives();
            //}
            if (!ConfigValues.Settings_extra_explodables)
                deleteExtraExplodables();
            explosive_barrel_melee_damage();
            if (WeaponPickups.Count > 0)
            {
                PlayerConnected += ME_OnConnect;
                void ME_OnConnect(Entity player) => ME_TrackUsables(player, WeaponPickups, null, ME_PickupWeapon);
                OnPlayerKilledEvent += ME_OnKill;
                PlayerDisconnected += ME_OnDisconnect;
            }
            if (Objectives.Count > 0)
            {
                PlayerConnected += ME_OnConnect;
                void ME_OnConnect(Entity player) => ME_TrackUsables(player, Objectives, ME_CanPlant, ME_TryToUseBomb);
                fx_explode = GSCFunctions.LoadFX("explosions/tanker_explosion");
                fx_smoke = GSCFunctions.LoadFX("smoke/car_damage_blacksmoke");
                fx_fire = GSCFunctions.LoadFX("smoke/car_damage_blacksmoke_fire");
                ME_TickBombs();
            }
        }

        public void deleteExtraExplodables()
        {
            foreach (Entity ent in extraExplodables)
                ent.Delete();
        }

        public void ME_OnKill(Entity deadguy, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc) => ME_ReleaseWeapons(deadguy);

        public void ME_OnDisconnect(Entity disconnector) => ME_ReleaseWeapons(disconnector);

        public void ME_Load()
        {
            bool forThisMap = false;
            foreach (string line in File.ReadAllLines(ConfigValues.ConfigPath + "MapEdit/" + ConfigValues.Settings_map_edit + ".txt"))
            {
                if (!line.Contains("|"))
                    forThisMap = line == ConfigValues.mapname;
                else if (forThisMap)
                    ME_Spawn(line);
            }
        }

        string[] previousParts;
        public List<Entity> ME_Spawn(string line)
        {
            List<Entity> res = new List<Entity>();
            string[] parts = line.Split('|');
            Vector3 origin = parts[1].ToVector3();
            Vector3 angles;
            if (parts.Length > 2)
                angles = parts[2].ToVector3();
            else
                angles = new Vector3(0, 0, 0);
            if (parts[0] == "+")
                parts = previousParts;
            else
                previousParts = parts;
            string model = parts[0];
            switch (model)
            {
                case "collision":
                    res.Add(SpawnCrate(origin, angles, bool.Parse(parts[3])));
                    break;
                case "weapon":
                    res.Add(ME_SpawnWeapon(origin, angles, parts[3], parts[4], bool.Parse(parts[5]), parts[6], int.Parse(parts[7])));
                    break;
                case "weaponcircle":
                    res = ME_SpawnWeaponCircle(origin, angles, parts[3].ToVector3(), parts[4].ToVector3(), parts[5], parts[6], bool.Parse(parts[7]), parts[8], int.Parse(parts[9]));
                    break;
                case "objective":
                    res = ME_SpawnObjective(origin, angles, parts[3].ToVector3(), parts[4].ToVector3(), int.Parse(parts[5]), int.Parse(parts[6]), bool.Parse(parts[7]), parts[8], parts[9]);
                    break;
                case "skullmund":
                    res = ME_SpawnSkullmund(origin, int.Parse(parts[2]), int.Parse(parts[3]));
                    break;
                default:
                    if (model.StartsWith("fx:"))
                        res.Add(ME_SpawnFX(model.Substring(3), origin, angles));
                    else
                        res.Add(ME_Spawn(model, origin, angles));
                    break;
            }
            return res;
        }

        public Entity ME_Spawn(string model, Vector3 origin, Vector3 angles)
        {
            Entity ent = GSCFunctions.Spawn("script_model", origin);
            ent.SetModel(model);
            ent.Angles = angles;
            return ent;
        }

        public Entity ME_SpawnFX(string fxName, Vector3 origin, Vector3 angles)
        {
            return ME_SpawnFX(GSCFunctions.LoadFX(fxName), origin, angles);
        }

        public Entity ME_SpawnFX(int fxID, Vector3 origin, Vector3 angles)
        {
            Vector3 upangles = GSCFunctions.VectorToAngles(angles + new Vector3(0, 0, 1000));
            Vector3 forward = GSCFunctions.AnglesToForward(upangles);
            Vector3 right = GSCFunctions.AnglesToRight(upangles);
            Entity effect = GSCFunctions.SpawnFX(fxID, origin, forward, right);
            GSCFunctions.TriggerFX(effect);
            return effect;
        }

        public List<Entity> ME_SpawnWeaponCircle(Vector3 circleOrigin, Vector3 circleAngles, Vector3 weaponOffset, Vector3 weaponAnglesOffset, string weapons, string respawn, bool eatWeapons, string rotation, int rotationSeconds)
        {
            List<Entity> weaponCircle = new List<Entity>();
            Entity circleEnt = ME_SpawnFX(goldcircle_fx, circleOrigin, circleAngles);
            weaponCircle.Add(circleEnt);
            Entity weaponEnt = ME_SpawnWeapon(circleOrigin + weaponOffset, circleAngles + weaponAnglesOffset, weapons, respawn, eatWeapons, rotation, rotationSeconds);
            weaponEnt.SetField("circle", circleEnt);
            weaponCircle.Add(weaponEnt);
            return weaponCircle;
        }

        public Entity ME_SpawnWeapon(Vector3 origin, Vector3 angles, string weapons, string respawn, bool eatWeapons, string rotation, int rotationSeconds)
        {
            Weapon randomWeapon = new Weapons(weapons).GetRandom();
            Entity ent = ME_Spawn(randomWeapon.Model, origin, angles);
            randomWeapon.Allow();
            ent.SetField("weapon_name", randomWeapon.Name);
            ent.SetField("respawn", respawn);
            if (eatWeapons)
                ent.SetField("eat_weapons", true);
            ent.SetField("usable", true);
            ent.SetField("message", "Press ^3[{+activate}] ^7to get weapon");
            if (rotation != "" && rotationSeconds != 0)
                ent.FullRotationEach(rotation, rotationSeconds);
            WeaponPickups.Add(ent);
            return ent;
        }

        int objectiveID = 31;
        public List<Entity> ME_SpawnObjective(Vector3 origin, Vector3 angles, Vector3 bombOrigin, Vector3 bombAngles, int plantTime, int timer, bool visible, string icon, string name)
        {
            List<Entity> list = new List<Entity>();
            Entity objective = GSCFunctions.Spawn("script_model", origin);
            objective.Angles = angles;
            list.Add(objective);
            Entity bomb = ME_Spawn("prop_suitcase_bomb", bombOrigin, bombAngles);
            bomb.Hide();
            list.Add(bomb);
            objective.SetField("suitcase", bomb);
            objective.SetField("plant_time", plantTime);
            objective.SetField("timer", timer);
            objective.SetField("ticks_left", timer);
            objective.SetField("objectiveID", objectiveID);
            objective.SetField("name", name);
            objective.SetField("usable", true);
            objective.SetField("message", "Press ^3[{+activate}] ^7to plant bomb");
            GSCFunctions.Objective_Add(objectiveID--, "active", origin, icon);
            if (visible)
            {
                objective.SetModel("com_bomb_objective");
                Entity exploder = ME_Spawn("com_bomb_objective_d", new Vector3(origin.X, origin.Y - 2, origin.Z + 2), new Vector3(angles.X, angles.Y - 180, angles.Z));
                list.Add(exploder);
                exploder.Hide();
                objective.SetField("exploder", exploder);
                list.Add(SpawnCrate(new Vector3(origin.X - 14, origin.Y + 6, origin.Z + 30), new Vector3(0, -90, 0), false));
                list.Add(SpawnCrate(new Vector3(origin.X - 14, origin.Y - 3, origin.Z + 30), new Vector3(0, -90, 0), false));
                list.Add(SpawnCrate(new Vector3(origin.X + 6, origin.Y + 6, origin.Z + 30), new Vector3(0, -90, 0), false));
                list.Add(SpawnCrate(new Vector3(origin.X + 6, origin.Y - 3, origin.Z + 30), new Vector3(0, -90, 0), false));
            }
            else
                objective.Hide();
            Objectives.Add(objective);
            return list;
        }

        public List<Entity> ME_SpawnSkullmund(Vector3 origin, int radius, int points)
        {
            List<Entity> mund = new List<Entity>();
            int stepIn = 10;
            int stepUp = 9;
            int crateStepUp = 3;
            int counter = 0;
            while (radius > stepIn)
            {
                for (int ii = 0; ii < points; ii++)
                {
                    double slice = 2 * Math.PI / points;
                    double angle = slice * ii;
                    int newX = (int)(origin.X + radius * Math.Cos(angle));
                    int newY = (int)(origin.Y + radius * Math.Sin(angle));
                    mund.Add(ME_Spawn("africa_skulls_pile_large", new Vector3(newX, newY, origin.Z), new Vector3((float)(Random.NextDouble() * 5f), (float)(Random.NextDouble() * 360f), (float)(Random.NextDouble() * 5f))));
                    if (counter % crateStepUp == 0)
                        mund.Add(SpawnCrate(new Vector3(newX, newY, origin.Z), new Vector3(40f, ii * 1f / (points * 1f) * 360f, 0), false));
                }
                radius -= stepIn;
                origin.Z += stepUp;
                points = (int)(radius * 2 * Math.PI * 0.035f + 1);
                counter++;
            }
            mund.Add(SpawnCrate(origin + new Vector3(-6, 0, -20), new Vector3(90, 0, 0), false));
            mund.Add(SpawnCrate(origin + new Vector3(6, 0, -20), new Vector3(90, 0, 0), false));
            mund.Add(SpawnCrate(origin + new Vector3(0, -6, -20), new Vector3(90, 0, 0), false));
            mund.Add(SpawnCrate(origin + new Vector3(0, 6, -20), new Vector3(90, 0, 0), false));
            return mund;
        }

        bool ME_CanPlant(Entity player, Entity objective)
        {
            return !objective.HasField("bomb") || objective.GetField<Entity>("bomb") != player;
        }

        void ME_TryToUseBomb(Entity user, Entity objective)
        {
            WriteLog.Debug("ME_TryToUseBomb");
            string switchback = user.CurrentWeapon;
            user.GiveWeapon("briefcase_bomb_mp");
            user.SwitchToWeapon("briefcase_bomb_mp");
            AfterDelay(objective.GetField<int>("plant_time") * 1000, () =>
            {
                if (user.CurrentWeapon == "briefcase_bomb_mp")
                {
                    if (objective.HasField("bomb"))
                    {
                        objective.ClearField("bomb");
                        objective.GetField<Entity>("suitcase").Hide();
                        objective.SetField("message", "Press ^3[{+activate}] ^7to plant bomb");
                    }
                    else
                    {
                        objective.SetField("bomb", user);
                        objective.GetField<Entity>("suitcase").Show();
                        objective.SetField("message", "Press ^3[{+activate}] ^7to defuse bomb");
                    }
                    HUD_UpdateObjectives();
                }
                user.TakeWeapon("briefcase_bomb_mp");
                user.SwitchToWeapon(switchback);
            });
        }

        public void ME_TickBombs()
        {
            OnInterval(1000, () =>
            {
                foreach (Entity objective in Objectives)
                    if (objective.HasField("bomb"))
                    {
                        int ticks_left = objective.GetField<int>("ticks_left") - 1;
                        if (ticks_left == 0)
                            ME_Explode(objective);
                        objective.SetField("ticks_left", ticks_left);
                    }
                    else
                        objective.SetField("ticks_left", objective.GetField<int>("timer"));
                HUD_UpdateObjectives();
                return Objectives.Count != 0;
            });
        }

        public void ME_Explode(Entity objective)
        {
            objective.SetField("usable", false);
            Entity destroyer = objective.GetField<Entity>("bomb");
            objective.SetField("destroyer", destroyer);
            GSCFunctions.PlayFX(fx_explode, objective.Origin);
            objective.PlaySound("cobra_helicopter_crash");
            objective.GetField<Entity>("suitcase").Hide();
            objective.Hide();
            GSCFunctions.Objective_Delete(objective.GetField<int>("objectiveID"));
            objective.RadiusDamage(objective.Origin, 200, 200, 40, destroyer, "MOD_EXPLOSIVE", "com_bomb_objective");
            if (objective.HasField("exploder"))
                objective.GetField<Entity>("exploder").Show();
            objective.PlayLoopSound("fire_vehicle_med");
            ME_SpawnFX(fx_fire, objective.Origin, new Vector3(0, 0, 0));
            ME_SpawnFX(fx_smoke, objective.Origin, new Vector3(0, 0, 0));
            OnObjectiveDestroy(destroyer, objective);
        }

        List<Entity> spawnBarrels(Vector3 origin, Vector3 end, float amount, bool explosive, float var, bool endOnLast) // to be deprecated
        {
            List<Entity> list = new List<Entity>();
            for (int ii = 0; ii < amount; ii++)
            {
                float progress = endOnLast && amount > 1 ? ii / (amount - 1) : ii / amount;
                Entity barrel = GSCFunctions.Spawn("script_model", new Vector3(origin.X + (end.X - origin.X) * progress, origin.Y + (end.Y - origin.Y) * progress, origin.Z + (end.Z - origin.Z) * progress));
                barrel.Angles = new Vector3((float)(Random.NextDouble() * var), (float)(Random.NextDouble() * 360f), (float)(Random.NextDouble() * var));
                if (explosive)
                {
                    barrel.SetModel("com_barrel_benzin");
                    barrel.TargetName = "explodable_barrel";
                    Entity crate = SpawnCrate(new Vector3(barrel.Origin.X, barrel.Origin.Y, barrel.Origin.Z + 30), new Vector3(90, 0, 0), false);
                    list.Add(crate);
                    barrel.SetField("collision", crate);
                    barrel.OnNotify("exploding", barrel_explosion_think);
                }
                else
                {
                    barrel.SetModel("com_barrel_biohazard_rust");
                }
                list.Add(barrel);
                DebugEnt(barrel);
            }
            return list;
        }

        private void DebugEnt(Entity ent)
        {
            WriteLog.Debug(ent.Model + "|" + ent.Origin + "|" + ent.Angles);
        }

        void barrel_explosion_think(Entity barrel)
        {
            barrel.GetField<Entity>("collision").Delete();
        }

        List<Entity> spawnJeep(Vector3 origin) // to be deprecated
        {
            List<Entity> list = new List<Entity>();
            Entity jeep = GSCFunctions.Spawn("script_model", origin);
            jeep.Angles = new Vector3(0, 99, 5);
            jeep.SetModel("vehicle_jeep_destructible");
            jeep.TargetName = "destructible_vehicle";
            list.Add(jeep);
            list.AddRange(SpawnCollision(new Vector3(origin.X + 50, origin.Y - 100, origin.Z + 30), new Vector3(origin.X + 30, origin.Y + 60, origin.Z + 30), new Vector3(0, 40, 0), 5, false));
            return list;
        }

        List<Entity> SpawnHummer(Vector3 origin) // to be deprecated
        {
            List<Entity> list = new List<Entity>();
            Entity hummer = GSCFunctions.Spawn("script_model", origin);
            hummer.Angles = new Vector3(1, -40, 0);
            hummer.SetModel("vehicle_hummer_destructible");
            hummer.TargetName = "destructible_vehicle";
            list.Add(hummer);
            list.AddRange(SpawnCollision(new Vector3(origin.X - 65, origin.Y + 65, origin.Z + 30), new Vector3(origin.X + 96, origin.Y - 58, origin.Z + 30), new Vector3(0, 50, 0), 5, false));
            list.AddRange(SpawnCollision(new Vector3(origin.X - 75, origin.Y + 45, origin.Z + 30), new Vector3(origin.X + 86, origin.Y - 78, origin.Z + 30), new Vector3(0, 50, 0), 5, false));
            return list;
        }

        List<Entity> spawnTruck(Vector3 origin, Vector3 angles) // to be deprecated
        {
            List<Entity> list = new List<Entity>();
            Entity truck = GSCFunctions.Spawn("script_model", origin);
            truck.Angles = angles;
            truck.SetModel("vehicle_pickup_destructible_mp");
            truck.TargetName = "destructible_vehicle";
            list.Add(truck);
            list.AddRange(SpawnCollision(new Vector3(origin.X - 65, origin.Y + 50, origin.Z + 30), new Vector3(origin.X + 118, origin.Y - 58, origin.Z + 30), new Vector3(0, 57, 0), 5, false));
            list.AddRange(SpawnCollision(new Vector3(origin.X - 75, origin.Y + 30, origin.Z + 30), new Vector3(origin.X + 108, origin.Y - 78, origin.Z + 30), new Vector3(0, 57, 0), 5, false));
            list.AddRange(SpawnCollision(new Vector3(origin.X - 7, origin.Y + 5, origin.Z + 60), new Vector3(origin.X - 7, origin.Y + 5, origin.Z + 50), new Vector3(0, 57, 0), 1, false));
            return list;
        }

        List<Entity> SpawnCollision(Vector3 origin, Vector3 end, Vector3 angle, float amount, bool visible) // to be deprecated
        {
            List<Entity> list = new List<Entity>();
            for (int ii = 0; ii < amount; ii++)
            {
                float progress = ii / amount;
                list.Add(SpawnCrate(new Vector3(origin.X + (end.X - origin.X) * progress, origin.Y + (end.Y - origin.Y) * progress, origin.Z + (end.Z - origin.Z) * progress), angle, visible));
                WriteLog.Debug("collision" + "|" + new Vector3(origin.X + (end.X - origin.X) * progress, origin.Y + (end.Y - origin.Y) * progress, origin.Z + (end.Z - origin.Z) * progress) + "|" + angle);
            }
            return list;
        }

        void TrackObjectivesForPlayer(Entity player) // to be deprecated
        {
            player.NotifyOnPlayerCommand("use_button_pressed", "+activate");
            player.OnNotify("use_button_pressed", tryToUseBomb);
            void tryToUseBomb(Entity user)
            {
                foreach (Entity objective in Objectives)
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
                                if (objective.HasField("bomb"))
                                {
                                    objective.ClearField("bomb");
                                    objective.GetField<Entity>("suitcase").Hide();
                                    
                                }
                                else
                                {
                                    objective.SetField("bomb", user);
                                    objective.GetField<Entity>("suitcase").Show();
                                    objective.SetField("message", "Press ^3[{+activate}] ^7to defuse bomb");
                                }
                            user.TakeWeapon("briefcase_bomb_mp");
                            user.SwitchToWeapon(switchback);
                        });
                    }
                }
            }
        }

        bool HandleObjectivesMessage(Entity player) // to be deprecated
        {
            HudElem message = getUsablesMessage(player);
            foreach (Entity objective in Objectives)
            {
                if (!objective.GetField<bool>("destroyed") && player.Origin.DistanceTo(objective.Origin) <= 100)
                {
                    player.DisableWeaponPickup();
                    if (objective.HasField("bomb") && objective.GetField<Entity>("bomb").Name != player.Name)
                    {
                        HandleMessage(player, objective, "Press ^3[{+activate}] ^7to defuse bomb");
                        return true;
                    }
                    else if(!objective.HasField("bomb"))
                    {
                        HandleMessage(player, objective, "Press ^3[{+activate}] ^7to plant bomb");
                        return true;
                    }
                }
            }
            DontDisplayMessage(player, message);
            player.EnableWeaponPickup();
            return true;
        }

        void ME_TrackUsables(Entity player, List<Entity> usables, Func<Entity, Entity, bool> usabilityCheck, Action<Entity, Entity> use)
        {
            ME_TrackUsableMessage(player, usables, usabilityCheck);
            ME_TrackUse(player, usables, usabilityCheck, use);
        }

        void ME_TrackUsableMessage(Entity player, List<Entity> usables, Func<Entity, Entity, bool> usabilityCheck)
        {
            OnInterval(200, () =>
            {
                foreach (Entity usable in usables)
                    if (ME_IsUsableFor(usable, player, usabilityCheck))
                    {
                        HUD_ShowMessage(player, usable.GetField<string>("message"));
                        return true;
                    }
                HUD_HideMessage(player);
                return usables.Count != 0;
            });
        }

        void ME_TrackUse(Entity player, List<Entity> usables, Func<Entity, Entity, bool> usabilityCheck, Action<Entity, Entity> action)
        {
            player.NotifyOnPlayerCommand("use_button_pressed", "+activate");
            player.OnNotify("use_button_pressed", tryToUse);
            void tryToUse(Entity user)
            {
                foreach (Entity usable in usables)
                    if (ME_IsUsableFor(usable, player, usabilityCheck))
                    {
                        WriteLog.Debug("action time");
                        action(user, usable);
                    }
            }
        }

        bool ME_IsUsableFor(Entity usable, Entity player, Func<Entity, Entity, bool> check)
        {
            return usable.GetField<bool>("usable") && player.Origin.DistanceTo(usable.Origin) <= 100 && (check == null || check(player, usable));
        }

        void ME_TrackWeaponPickupsFor(Entity player) // to be deprecated
        {
            player.NotifyOnPlayerCommand("use_button_pressed", "+activate");
            player.OnNotify("use_button_pressed", tryToGetWeapon);
            void tryToGetWeapon(Entity receiver)
            {
                foreach (Entity pickup in WeaponPickups)
                    if (pickup.GetField<bool>("usable") && receiver.Origin.DistanceTo(pickup.Origin) <= 100)
                        ME_PickupWeapon(receiver, pickup);
            }
        }

        public void ME_PickupWeapon(Entity player, Entity pickup)
        {
            if (pickup.HasField("eat_weapons"))
                player.TakeAllWeapons();
            string gunName = pickup.GetField<string>("weapon_name");
            player.GiveWeapon(gunName);
            player.SetWeaponAmmoStock(gunName, 99);
            player.SetWeaponAmmoClip(gunName, 99);
            if (pickup.GetField<string>("respawn") == "death")
                ME_TakeWeapon(player, pickup);
            AfterDelay(50, () =>
            {
                player.SwitchToWeaponImmediate(gunName);
                OnWeaponPickup(player, pickup);
            });
        }

        bool HandleWeaponPickupsMessage(Entity player) // to be deprecated
        {
            HudElem message = getUsablesMessage(player);
            foreach (Entity pickup in WeaponPickups)
                if (pickup.GetField<bool>("usable") && player.Origin.DistanceTo(pickup.Origin) <= 100)
                {
                    player.DisableWeaponPickup();
                    HandleMessage(player, pickup, "Press ^3[{+activate}] ^7to get weapon");
                    return true;
                }
            DontDisplayMessage(player, message);
            if (ConfigValues.Settings_dropped_weapon_pickup)
                player.EnableWeaponPickup();
            return true;
        }

        void ME_TakeWeapon(Entity player, Entity weaponSource)
        {
            ME_TakeWeapon(weaponSource);
            int ii = 0;
            while (player.HasField("pickup" + ii))
                ii++;
            player.SetField("pickup" + ii, weaponSource);
        }

        void ME_TakeWeapon(Entity weaponSource)
        {
            weaponSource.SetField("usable", false);
            weaponSource.Hide();
            if (weaponSource.HasField("circle"))
                ME_ToggleCircle(weaponSource, false);

        }

        void ME_ReleaseWeapons(Entity player)
        {
            int ii = 0;
            while (player.HasField("pickup" + ii))
            {
                ME_ReleaseWeapon(player.GetField<Entity>("pickup" + ii));
                player.ClearField("pickup" + ii);
                ii++;
            }
        }

        void ME_ReleaseWeapon(Entity weaponSource)
        {
            weaponSource.SetField("usable", true);
            weaponSource.Show();
            if (weaponSource.HasField("circle"))
                ME_ToggleCircle(weaponSource, true);
        }

        void ME_ToggleCircle(Entity weaponSource, bool gold)
        {
            int circle_fx = gold ? goldcircle_fx : redcircle_fx;
            Entity oldCircle = weaponSource.GetField<Entity>("circle");
            weaponSource.SetField("circle", ME_SpawnFX(circle_fx, oldCircle.Origin, oldCircle.Angles));
            oldCircle.Delete();
        }

        bool HandleMessage(Entity player, Entity ent, string text) // to be deprecated
        {
            HudElem message = getUsablesMessage(player);
            
            if (ent.GetField<bool>("usable") && player.Origin.DistanceTo(ent.Origin) <= 100)
                DisplayMessage(player, message, text);
            else
                DontDisplayMessage(player, message);
            return true;
        }

        HudElem getUsablesMessage(Entity player) // to be deprecated
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

        void DisplayMessage(Entity player, HudElem message, string text) // to be deprecated
        {
            message.Alpha = .85f;
            message.SetText(text);
        }

        void DontDisplayMessage(Entity player, HudElem message) // to be deprecated
        {
            message.Alpha = 0;
            message.SetText("");
        }

        public static Entity getCrateCollision()
        {
            Entity cp = GSCFunctions.GetEnt("care_package", "targetname");
            return GSCFunctions.GetEnt(cp.Target, "targetname");
        }

        public static Entity SpawnCrate(Vector3 origin, Vector3 angles, bool visible)
        {
            Entity ent = GSCFunctions.Spawn("script_model", origin);
            if (visible)
                ent.SetModel("com_plasticcase_friendly");
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
                    ent.OnNotify("damage", barrel_damage_think);
            }
        }

        public void barrel_damage_think(Entity barrel, Parameter amount, Parameter attacker, Parameter direction_vec, Parameter P, Parameter type, Parameter modelName, Parameter partName, Parameter tagName, Parameter iDFlags, Parameter weapon)
        {
            if (((string)type == "MOD_MELEE" || (string)type == "MOD_IMPACT") && barrel.TargetName == "explodable_barrel")
                AfterDelay(100, () =>
                    barrel.Notify("damage", 40, attacker, direction_vec, P, "MOD_PISTOL_BULLET", modelName, partName, tagName, iDFlags, weapon));
        }

    }

    public static partial class Extensions
    {
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

        public static void Translate(this List<Entity> entities, Vector3 translation)
        {
            foreach (Entity entity in entities)
                entity.Origin += translation;
        }

        public static void Rotate(this List<Entity> entities, Vector3 rotation)
        {
            foreach (Entity entity in entities)
                entity.Angles += rotation;
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
    }
}