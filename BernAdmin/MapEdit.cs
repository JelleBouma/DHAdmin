using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InfinityScript;
namespace LambAdmin
{
    public partial class DHAdmin
    {
        List<Entity> ServerStart = new List<Entity>();
        static List<Entity> MapObjectives = new List<Entity>();
        List<Entity> WeaponPickups = new List<Entity>();
        static event Action<Entity, Entity> OnWeaponPickup = (player, pickup) => { };
        static event Action<Entity, Entity> OnObjectiveDestroy = (destroyer, objective) => { };
        List<Entity> lastSpawned = new List<Entity>();
        List<string> lastSpawnedParts = new List<string>();
        private static Entity _airdropCollision = GetCrateCollision();
        private static int Fx_explode;
        private static int Fx_smoke;
        private static int Fx_fire;
        private static int Fx_redcircle = GSCFunctions.LoadFX("misc/ui_flagbase_red");
        private static int Fx_goldcircle = GSCFunctions.LoadFX("misc/ui_flagbase_gold");

        static string[] _collisionDefault = { "collision", "0,0,0", "0,0,0", "true" };
        static string[] _weaponDefault = { "weapon", "0,0,0", "0,0,0", "*-stinger_mp", "death", "true", "yaw", "3" };
        static string[] _weaponCircleDefault = { "weaponcircle", "0,0,0", "0,0,0", "0,0,50", "0,0,0", "*-stinger_mp", "death", "true", "yaw", "3" };
        static string[] _objectiveDefault = { "objective", "0,0,0", "0,0,0", "0,0,0", "0,0,0", "Objective", "true", "objective", "60", "4" };
        static string[] _skullmundDefault = { "skullmund", "0,0,0", "40", "10" };
        static string[] _default = { "", "0,0,0", "0,0,0" };

        public static Dictionary<string, List<string>> MapEditDefaults = new Dictionary<string, List<string>>()
        {
            { "collision", _collisionDefault.ToList() },
            { "weapon", _weaponDefault.ToList() },
            { "weaponcircle", _weaponCircleDefault.ToList() },
            { "skullmund", _skullmundDefault.ToList() },
            { "objective", _objectiveDefault.ToList()},
            { "", _default.ToList() }
        };

        /// <summary>
        /// Load the OnServerStart mapedit file.
        /// </summary>
        public void ME_OnServerStart()
        {
            ServerStart = ME_Load("OnServerStart");
        }

        /// <summary>
        /// Apply the mapedit config values: settings_map_edit
        /// </summary>
        public void ME_ConfigValues_Apply()
        {
            if (!ConfigValues.Settings_map_edit.Contains("OnServerStart"))
                ServerStart.Delete();
            ME_Load(ConfigValues.Settings_map_edit.Replace("OnServerStart", ""));
            explosive_barrel_melee_damage();
            if (WeaponPickups.Count > 0)
            {
                PlayerConnected += player => ME_TrackUsables(player, WeaponPickups, null, ME_PickupWeapon);
                OnPlayerKilledEvent += ME_OnKill;
                PlayerDisconnected += ME_OnDisconnect;
            }
            if (MapObjectives.Count > 0)
            {
                PlayerConnected += player => ME_TrackUsables(player, MapObjectives, ME_CanPlant, ME_TryToUseBomb);
                Fx_explode = GSCFunctions.LoadFX("explosions/tanker_explosion");
                Fx_smoke = GSCFunctions.LoadFX("smoke/car_damage_blacksmoke");
                Fx_fire = GSCFunctions.LoadFX("smoke/car_damage_blacksmoke_fire");
                ME_TickBombs();
                HUD_InitTopLeftTimers();
                PlayerConnected += HUD_UpdateTopLeftInformation;
            }
        }

        /// <summary>
        /// When a player gets killed, release the picked up weapons the killed player is wielding.
        /// </summary>
        public void ME_OnKill(Entity deadguy, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc) => ME_ReleaseWeapons(deadguy);

        /// <summary>
        /// When a player disconnects, release the picked up weapons the disconnected player was wielding.
        /// </summary>
        public void ME_OnDisconnect(Entity disconnector) => ME_ReleaseWeapons(disconnector);

        /// <summary>
        /// Load a mapedit file and execute it.
        /// </summary>
        /// <returns>All spawned entities</returns>
        public List<Entity> ME_Load(string filenames)
        {
            List<Entity> spawned = new List<Entity>();
            foreach (string filename in filenames.Split(','))
                if (!string.IsNullOrWhiteSpace(filename))
                {
                    WriteLog.Debug("loading map edit file: " + filename);
                    bool forThisMap = false;
                    foreach (string line in File.ReadAllLines(ConfigValues.ConfigPath + "MapEdit/" + filename + ".txt"))
                        if (!line.Contains("|"))
                        {
                            forThisMap = line == ConfigValues.Mapname;
                            WriteLog.Debug("for map: " + ConfigValues.Mapname + " " + forThisMap);
                        }
                        else if (forThisMap)
                            spawned.AddRange(ME_Spawn(line));
                }
            return spawned;
        }

        /// <summary>
        /// Spawn entities according to a mapedit line.
        /// </summary>
        /// <returns>All entities spawned from this line</returns>
        public List<Entity> ME_Spawn(string line)
        {
            return ME_Spawn(line.Split('|').ToList());
        }

        /// <summary>
        /// Spawn entities according to a mapedit line.
        /// </summary>
        /// <returns>All entities spawned from this line</returns>
        public List<Entity> ME_Spawn(List<string> parts)
        {
            List<Entity> res = new List<Entity>();
            if (MapEditDefaults.TryGetValue(parts[0], out List<string> defaultParts))
                parts.FillWith(defaultParts);
            else
                parts.FillWith(MapEditDefaults[""]);
            parts[0] = parts[0] == "+" ? MapEditDefaults.GetValue("+")[0] : parts[0];
            MapEditDefaults["+"] = parts;
            string name = parts[0];
            Vector3 origin = parts[1].ToVector3();
            switch (name)
            {
                case "explodable":
                    res.AddRange(ME_SpawnExplodableBarrel(origin, parts[2].ToVector3()));
                    break;
                case "vehicle_jeep_destructible":
                case "vehicle_hummer_destructible":
                case "vehicle_pickup_destructible_mp":
                    res.Add(ME_Spawn(name, origin, parts[2].ToVector3(), "destructible_vehicle"));
                    break;
                case "collision":
                    res.Add(SpawnCrate(origin, parts[2].ToVector3(), bool.Parse(parts[3])));
                    break;
                case "weapon":
                    res.Add(ME_SpawnWeapon(origin, parts[2].ToVector3(), parts[3], parts[4], bool.Parse(parts[5]), parts[6], int.Parse(parts[7])));
                    break;
                case "weaponcircle":
                    res = ME_SpawnWeaponCircle(origin, parts[2].ToVector3(), parts[3].ToVector3(), parts[4].ToVector3(), parts[5], parts[6], bool.Parse(parts[7]), parts[8], int.Parse(parts[9]));
                    break;
                case "objective":
                    res = ME_SpawnObjective(origin, parts[2].ToVector3(), parts[3].ToVector3(), parts[4].ToVector3(), parts[5], bool.Parse(parts[6]), parts[7], int.Parse(parts[8]), int.Parse(parts[9]));
                    break;
                case "skullmund":
                    res = ME_SpawnSkullmund(origin, int.Parse(parts[2]), int.Parse(parts[3]));
                    break;
                default:
                    if (name.StartsWith("fx:"))
                        res.Add(ME_SpawnFX(name.Substring(3), origin, parts[2].ToVector3()));
                    else
                        res.Add(ME_Spawn(name, origin, parts[2].ToVector3()));
                    break;
            }
            return res;
        }

        /// <summary>
        /// Spawn a model at a defined origin and with a defined rotation.
        /// </summary>
        /// <returns>The spawned model.</returns>
        public Entity ME_Spawn(string model, Vector3 origin, Vector3 angles)
        {
            Entity ent = GSCFunctions.Spawn("script_model", origin);
            ent.SetModel(model);
            ent.Angles = angles;
            return ent;
        }

        /// <summary>
        /// Spawn a model at a defined origin, with a defined rotation and with a defined target name.
        /// A target name is needed for vehicles and barrels spawned at game start to be explodable.
        /// </summary>
        /// <returns>The spawned model.</returns>
        public Entity ME_Spawn(string model, Vector3 origin, Vector3 angles, string targetName)
        {
            Entity ent = GSCFunctions.Spawn("script_model", origin);
            ent.SetModel(model);
            ent.Angles = angles;
            ent.TargetName = targetName;
            return ent;
        }

        /// <summary>
        /// Spawn a collidable, explodable, red barrel.
        /// </summary>
        /// <returns>The spawned barrel entity and the collision entity.</returns>
        List<Entity> ME_SpawnExplodableBarrel(Vector3 origin, Vector3 angles)
        {
            List<Entity> barrelAndCollision = new List<Entity>();
            Entity barrel = ME_Spawn("com_barrel_benzin", origin, angles, "explodable_barrel");
            barrelAndCollision.Add(barrel);
            Entity crate = SpawnCrate(new Vector3(barrel.Origin.X, barrel.Origin.Y, barrel.Origin.Z + 30), new Vector3(90, 0, 0), false);
            barrelAndCollision.Add(crate);
            barrel.SetField("collision", crate);
            barrel.OnNotify("exploding", b => b.GetField<Entity>("collision").Delete());
            return barrelAndCollision;
        }

        /// <summary>
        /// Spawn an fx at a defined origin and with a defined rotation.
        /// </summary>
        /// <returns>The spawned FX.</returns>
        public Entity ME_SpawnFX(string fxName, Vector3 origin, Vector3 angles)
        {
            return ME_SpawnFX(GSCFunctions.LoadFX(fxName), origin, angles);
        }

        /// <summary>
        /// Spawn an fx at a defined origin and with a defined rotation.
        /// </summary>
        /// <returns>The spawned FX.</returns>
        public Entity ME_SpawnFX(int fxID, Vector3 origin, Vector3 angles)
        {
            Vector3 upangles = GSCFunctions.VectorToAngles(angles + new Vector3(0, 0, 1000));
            Vector3 forward = GSCFunctions.AnglesToForward(upangles);
            Vector3 right = GSCFunctions.AnglesToRight(upangles);
            Entity effect = GSCFunctions.SpawnFX(fxID, origin, forward, right);
            GSCFunctions.TriggerFX(effect);
            return effect;
        }

        /// <summary>
        /// Spawn a collidable care package, can be used to handle collision for other mapedit entities.
        /// </summary>
        /// <returns>The spawned collision entity.</returns>
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

        /// <summary>
        /// Spawn a weapon that can be picked up and an fx circle that is yellow when the weapon can be picked up, red when it cannot.
        /// </summary>
        /// <returns>The spawned weapon and fx.</returns>
        public List<Entity> ME_SpawnWeaponCircle(Vector3 circleOrigin, Vector3 circleAngles, Vector3 weaponOffset, Vector3 weaponAngles, string weapons, string respawn, bool eatWeapons, string rotation, int rotationSeconds)
        {
            List<Entity> weaponCircle = new List<Entity>();
            Entity circleEnt = ME_SpawnFX(Fx_goldcircle, circleOrigin, circleAngles);
            weaponCircle.Add(circleEnt);
            Entity weaponEnt = ME_SpawnWeapon(circleOrigin + weaponOffset, weaponAngles, weapons, respawn, eatWeapons, rotation, rotationSeconds);
            weaponEnt.SetField("circle", circleEnt);
            weaponCircle.Add(weaponEnt);
            return weaponCircle;
        }

        /// <summary>
        /// Spawn a weapon that can be picked up by pressing the use key (default "f") within proximity.
        /// </summary>
        /// <returns>The spawned weapon and fx.</returns>
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
        /// <summary>
        /// Spawn a search and destroy style objective, bombs can be planted and defused by anyone in free-for-all.
        /// </summary>
        /// <returns>The spawned objective entity, bomb entity (initially invisible) and possibly collision entities.</returns>
        public List<Entity> ME_SpawnObjective(Vector3 origin, Vector3 angles, Vector3 bombOrigin, Vector3 bombAngles, string name, bool visible, string icon, int timer, int plantTime)
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
            MapObjectives.Add(objective);
            return list;
        }

        /// <summary>
        /// Spawn a collidable skullmund with a defined origin, bottom radius and amount of skulls on the lowest ring.
        /// </summary>
        /// <returns>All skull and collision entities.</returns>
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

        /// <summary>
        /// Check if a player is allowed to plant or defuse a bomb on this objective.
        /// </summary>
        bool ME_CanPlant(Entity player, Entity objective)
        {
            return !objective.HasField("bomb") || objective.GetField<Entity>("bomb") != player;
        }

        /// <summary>
        /// Start planting or defusing a bomb on a search and destroy style objective.
        /// </summary>
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
                    HUD_UpdateTopLeftInformation();
                    HUD_UpdateTimer(objective);
                }
                user.TakeWeapon("briefcase_bomb_mp");
                user.SwitchToWeapon(switchback);
            });
        }

        /// <summary>
        /// Tick the timer of all bombs planted on search and destroy style objectives.
        /// </summary>
        public void ME_TickBombs()
        {
            OnInterval(1000, () =>
            {
                foreach (Entity objective in MapObjectives)
                    if (objective.HasField("bomb"))
                    {
                        int ticks_left = objective.GetField<int>("ticks_left") - 1;
                        if (ticks_left == 0)
                            ME_Explode(objective);
                        objective.SetField("ticks_left", ticks_left);
                    }
                    else
                        objective.SetField("ticks_left", objective.GetField<int>("timer"));
                return true;
            });
        }

        /// <summary>
        /// Explode a search and destroy style objective.
        /// </summary>
        public void ME_Explode(Entity objective)
        {
            objective.SetField("usable", false);
            Entity destroyer = objective.GetField<Entity>("bomb");
            objective.SetField("destroyer", destroyer);
            GSCFunctions.PlayFX(Fx_explode, objective.Origin);
            objective.PlaySound("cobra_helicopter_crash");
            objective.GetField<Entity>("suitcase").Hide();
            objective.Hide();
            GSCFunctions.Objective_Delete(objective.GetField<int>("objectiveID"));
            objective.RadiusDamage(objective.Origin, 200, 200, 40, destroyer, "MOD_EXPLOSIVE", "com_bomb_objective");
            if (objective.HasField("exploder"))
                objective.GetField<Entity>("exploder").Show();
            objective.PlayLoopSound("fire_vehicle_med");
            OnInterval(400, () =>
            {
                GSCFunctions.PlayFX(Fx_fire, objective.Origin);
                GSCFunctions.PlayFX(Fx_smoke, objective.Origin);
                return true;
            });
            ME_SpawnFX(Fx_smoke, objective.Origin, new Vector3(0, 0, 0));
            OnObjectiveDestroy(destroyer, objective);
        }

        /// <summary>
        /// Track a list of usable mapedit entities for a player, given the check if the entity is usable and the action to be taken on use.
        /// </summary>
        void ME_TrackUsables(Entity player, List<Entity> usables, Func<Entity, Entity, bool> usabilityCheck, Action<Entity, Entity> use)
        {
            ME_TrackUsableMessage(player, usables, usabilityCheck);
            ME_TrackUse(player, usables, usabilityCheck, use);
        }

        /// <summary>
        /// Track when a message must be shown to the player that they can use a usable mapedit entity.
        /// </summary>
        void ME_TrackUsableMessage(Entity player, List<Entity> usables, Func<Entity, Entity, bool> usabilityCheck)
        {
            OnInterval(200, () =>
            {
                foreach (Entity usable in usables)
                    if (ME_IsUsableFor(usable, player, usabilityCheck))
                    {
                        player.DisableWeaponPickup();
                        HUD_ShowMessage(player, usable.GetField<string>("message"));
                        return true;
                    }
                HUD_HideMessage(player);
                if (ConfigValues.Settings_dropped_weapon_pickup)
                    player.EnableWeaponPickup();
                return usables.Count != 0;
            });
        }

        /// <summary>
        /// Track when the player tries to use a usable mapedit entity.
        /// </summary>
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

        /// <summary>
        /// Check if a usable mapedit entity is usable for the player.
        /// </summary>
        bool ME_IsUsableFor(Entity usable, Entity player, Func<Entity, Entity, bool> check)
        {
            return usable.GetField<bool>("usable") && player.IsAlive && player.Origin.DistanceTo(usable.Origin) <= 100 && (check == null || check(player, usable));
        }

        /// <summary>
        /// Pick up a weapon from the weapon pickup mapedit entity and give it to the player.
        /// </summary>
        public void ME_PickupWeapon(Entity player, Entity pickup)
        {
            if (pickup.HasField("eat_weapons"))
            {
                string offhand = player.GetCurrentOffhand();
                player.TakeAllWeapons();
                player.GiveWeapon(offhand);
            }
            string weaponName = pickup.GetField<string>("weapon_name");
            player.GiveAndSwitchTo(weaponName);
            if (pickup.GetField<string>("respawn") == "death")
                ME_TakeWeapon(player, pickup);
            OnWeaponPickup(player, pickup);
        }

        /// <summary>
        /// Remove the weapon from the weapon pickup mapedit entity rendering it unusable until the weapon returns.
        /// Assign the weapon to the player.
        /// </summary>
        void ME_TakeWeapon(Entity player, Entity weaponSource)
        {
            ME_TakeWeapon(weaponSource);
            int ii = 0;
            while (player.HasField("pickup" + ii))
                ii++;
            player.SetField("pickup" + ii, weaponSource);
        }

        /// <summary>
        /// Remove the weapon from the weapon pickup mapedit entity rendering it unusable until the weapon returns.
        /// </summary>
        void ME_TakeWeapon(Entity weaponSource)
        {
            weaponSource.SetField("usable", false);
            weaponSource.Hide();
            if (weaponSource.HasField("circle"))
                ME_ToggleCircle(weaponSource, false);
        }

        /// <summary>
        /// Release all weapons this player has taken from weapon pickup mapedit entities back to the entities.
        /// </summary>
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

        /// <summary>
        /// Make the weapon pickup mapedit entity usable again.
        /// </summary>
        void ME_ReleaseWeapon(Entity weaponSource)
        {
            weaponSource.SetField("usable", true);
            weaponSource.Show();
            if (weaponSource.HasField("circle"))
                ME_ToggleCircle(weaponSource, true);
        }

        /// <summary>
        /// Change the fx circle of a weapon pickup mapedit entity to gold or red.
        /// </summary>
        void ME_ToggleCircle(Entity weaponSource, bool gold)
        {
            int circle_fx = gold ? Fx_goldcircle : Fx_redcircle;
            Entity oldCircle = weaponSource.GetField<Entity>("circle");
            weaponSource.SetField("circle", ME_SpawnFX(circle_fx, oldCircle.Origin, oldCircle.Angles));
            oldCircle.Delete();
        }

        /// <summary>
        /// Get a care package target entity to get collision from.
        /// </summary>
        public static Entity GetCrateCollision()
        {
            Entity cp = GSCFunctions.GetEnt("care_package", "targetname");
            return GSCFunctions.GetEnt(cp.Target, "targetname");
        }

        /// <summary>
        /// Set barrel_damage_think damage notify for all red barrels.
        /// </summary>
        public void explosive_barrel_melee_damage()
        {
            foreach (Entity ent in GetEntities())
                if (ent.TargetName == "explodable_barrel")
                    ent.OnNotify("damage", barrel_damage_think);
        }

        /// <summary>
        /// When a red barrel gets damaged by melee or impact, set it on fire.
        /// </summary>
        public void barrel_damage_think(Entity barrel, Parameter amount, Parameter attacker, Parameter direction_vec, Parameter P, Parameter type, Parameter modelName, Parameter partName, Parameter tagName, Parameter iDFlags, Parameter weapon)
        {
            if (((string)type == "MOD_MELEE" || (string)type == "MOD_IMPACT") && barrel.TargetName == "explodable_barrel")
                AfterDelay(100, () =>
                    barrel.Notify("damage", 40, attacker, direction_vec, P, "MOD_PISTOL_BULLET", modelName, partName, tagName, iDFlags, weapon));
        }

    }
}