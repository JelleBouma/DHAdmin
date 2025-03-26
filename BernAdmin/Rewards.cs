using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using InfinityScript;

namespace LambAdmin
{
    public partial class DHAdmin
    {
        static event Action<Entity, int> OnScoreRewardEvent = (player, reward) => { };
        static event Action<Entity> OnTimerExpireEvent = player => { };
        static event Action<Entity> OnTimerStartEvent = player => { };
        static Entity TopScorePlayer = null;

        /// <summary>
        /// A reward for completing a mission. Note that a "reward" is not necessarily a buff but can be negative as well.
        /// </summary>
        class Reward
        {
            /// <summary>
            /// Possible types: speed, score, weapon, perks, fx, chat, health, lovecraftian, radar, explode, timer, hud and achievement progress where the type is the name of the achievement.
            /// </summary>
            public string RewardType;
            public string RewardAmount;

            /// <summary>
            /// Create a reward object from a type and amount.
            /// The amount can include simple maths (+-) and keywords "self" and "other" for calculations based upon values relating to the mission.
            /// </summary>
            public Reward(string rewardType, string rewardAmount)
            {
                RewardType = rewardType.Trim();
                if (RewardType == "speed")
                    ConfigValues.Speed_maintenance_active = true;
                if (rewardType == "lovecraftian")
                    ConfigValues.Lovecraftian_active = true;
                if (rewardType == "timer")
                    ConfigValues.Timers_active = true;
                ParseRewardAmount(rewardAmount);
            }

            /// <summary>
            /// Parse the reward amount input into a string which is better suited for processing.
            /// </summary>
            private void ParseRewardAmount(string amount)
            {
                if (RewardType == "score" || RewardType == "speed")
                {
                    RewardAmount = amount.RemoveWhitespace().Replace("+-", "-").Replace("-", "+-");
                    if (RewardAmount.StartsWith("+"))
                        RewardAmount = RewardAmount.Substring(1);
                }
                else
                    RewardAmount = amount.Trim();
            }

            /// <summary>
            /// Issue the reward to a receiver.
            /// There is an optional other entity parameter for reward amount calculation.
            /// </summary>
            public void IssueTo(Entity receiver, Entity other)
            {
                if (RewardAmount == "reset")
                    Reset(receiver);
                else
                    switch (RewardType)
                    {
                        case "health":
                            receiver.Health = Math.Min(receiver.Health + int.Parse(RewardAmount), receiver.MaxHealth);
                            break;
                        case "lovecraftian":
                            receiver.SetField("horror", int.Parse(RewardAmount));
                            break;
                        case "speed": // movement speed
                            receiver.AddSpeed(CalculateReward(receiver.GetSpeed(), other != null && other.IsPlayer ? other.GetSpeed() : 0));
                            break;
                        case "score":
                            int score = (int)CalculateReward(receiver.GetScore(), other != null && other.IsPlayer ? other.GetScore() : 0);
                            receiver.AddScore(score);
                            OnScoreRewardEvent(receiver, score);
                            break;
                        case "weapon":
                            string weapon = RewardAmount.Trim();
                            string remember = receiver.GetCurrentPrimaryWeapon();
                            if (remember != "none") {
                                receiver.SetField("remembered_weapon", remember);
                                receiver.TakeWeapon(remember);
                            }
                            weapon = weapon == "other" ? other.GetField<string>("currentweapon") : weapon;
                            WriteLog.Debug("other check end");
                            int indexChange = weapon.EndsWith("next") ? 1 : weapon.EndsWith("previous") ? -1 : 0;
                            if (indexChange != 0)
                            {
                                int currentIndex = receiver.GetField<int>("weapon_index") + indexChange;
                                int currentList = int.Parse("0" + new string(weapon.Where(char.IsDigit).ToArray()));
                                if (weapon.StartsWith("c"))
                                    currentIndex %= WeaponRewardLists[currentList].Count;
                                if (currentIndex >= 0 && currentIndex < WeaponRewardLists[currentList].Count)
                                {
                                    receiver.SetField("weapon_index", currentIndex);
                                    weapon = WeaponRewardLists[currentList][currentIndex].FullName;
                                    HUD_UpdateTopLeftInformation(receiver);
                                }
                                else
                                    return;
                            }
                            receiver.GivePersistentWeapon(weapon);
                            break;
                        case "perks":
                            foreach (string perk in RewardAmount.Split(','))
                                receiver.SetPerk(perk, true, true);
                            break;
                        case "fx":
                            if (!receiver.HasField(RewardAmount))
                            {
                                receiver.SetField(RewardAmount, true);
                                receiver.StartPlayingFX(RewardAmount);
                            }
                            break;
                        case "chat":
                            WriteChatToAll(RewardAmount.Format(new Dictionary<string, string>() {
                                { "<self>", receiver.Name },
                                { "<other>", other == null ? "" : other.Name }
                            }));
                            break;
                        case "radar":
                            receiver.HasRadar = true;
                            break;
                        case "timer":
                            if (!receiver.HasField("ticks_left"))
                            {
                                receiver.SetField("ticks_left", int.Parse(RewardAmount));
                                OnTimerStartEvent(receiver);
                            }
                            receiver.SetField("ticks_left", int.Parse(RewardAmount));
                            HUD_SetRewardTimer(receiver);
                            break;
                        case "hud":
                            HUD_SetRewardMessage(receiver, RewardAmount);
                            break;
                        case "explode":
                            ME_SpawnExplosion(receiver, receiver, 300, int.Parse(RewardAmount), 0);
                            break;
                        case "attach":
                            string[] parts = RewardAmount.Split('|', ',');
                            string tag = parts[0];
                            string model = parts[1];
                            Vector3 offset = new Vector3(int.Parse(parts[2]), int.Parse(parts[3]), int.Parse(parts[4]));
                            Vector3 angles = new Vector3(int.Parse(parts[5]), int.Parse(parts[6]), int.Parse(parts[7]));
                            Entity toAttach = ME_Spawn(model, receiver.Origin, Vector3.Zero);
                            toAttach.SetContents(0);
                            toAttach.LinkTo(receiver, tag, offset, angles);
                            receiver.ShowPart(tag);
                            break;
                        default: // achievement progress, RewardType is the achievement name
                            string[] objectiveAndProgress = RewardAmount.Split(',');
                            string progress = objectiveAndProgress[objectiveAndProgress.Length - 1];
                            Action<Entity, string, int> progressAction;
                            if (progress == "-")
                                progressAction = ACHIEVEMENTS_DisableProgress;
                            else if (progress == "0")
                                progressAction = ACHIEVEMENTS_ResetProgress;
                            else
                                progressAction = (e, s, i) => ACHIEVEMENTS_Progress(e, s, i, int.Parse(progress));
                            if (objectiveAndProgress.Length == 2)
                                progressAction(receiver, RewardType, int.Parse(objectiveAndProgress[0]));
                            else
                                ACHIEVEMENTS_ForAllObjectives(receiver, RewardType, progressAction);
                            break;
                    }
            }

            /// <summary>
            /// Reset this reward type for the player.
            /// </summary>
            public void Reset(Entity player)
            {
                switch (RewardType)
                {
                    case "speed":
                        if (player.HasField("speed"))
                            player.SetSpeed(ConfigValues.Settings_movement_speed);
                        break;
                    case "weapon":
                        if (player.HasField("weapon"))
                            player.ClearField("weapon");
                        if (RewardAmount != "reset" && player.IsAlive && player.HasWeapon(RewardAmount) && player.HasField("remembered_weapon"))
                        {
                            player.TakeWeapon(RewardAmount);
                            WriteLog.Debug("took weapon for " + player.Name);
                            player.GiveAndSwitchTo(player.GetField<string>("remembered_weapon"));
                        }
                        break;
                    case "perks":
                        foreach (string perk in RewardAmount.Split(','))
                            player.UnSetPerk(perk);
                        break;
                    case "fx":
                        player.ClearField(RewardAmount);
                        break;
                    case "lovecraftian":
                        player.ClearField("horror");
                        break;
                    case "radar":
                        player.HasRadar = false;
                        break;
                    case "hud":
                        HUD_ResetRewardMessage(player);
                        break;
                    case "timer":
                        HUD_ResetRewardTimer(player);
                        if (player.HasField("ticks_left"))
                        {
                            player.ClearField("ticks_left");
                            OnTimerExpireEvent(player);
                        }
                        break;
                }
            }

            /// <summary>
            /// Calculate reward amount using simple maths (+-) and the values "self" and "other".
            /// </summary>
            /// <returns>Calculated reward amount.</returns>
            public float CalculateReward(float self, float other)
            {
                float res = 0;
                string[] sum = RewardAmount.Split('+');
                foreach (string atom in sum)
                    if (float.TryParse(atom, out float a))
                        res += a;
                    else if (atom.StartsWith("-"))
                        res -= atom.Contains("self") ? self : other;
                    else
                        res += atom.Contains("self") ? self : other;
                return res;
            }
        }

        /// <summary>
        /// A mission that awards one or more rewards on completion.
        /// </summary>
        class Mission
        {
            private static readonly string[] MissionTypeArr = { "changeclass", "shoot", "kill", "die", "win", "pickup", "objective_destroy", "topscore", "spawn", "during_timer", "end_timer" };
            public static List<string> MissionTypes = new List<string>(MissionTypeArr);
            public string Type = null;
            public List<string> Prefix = new List<string>();
            private List<int> prefixClasses = new List<int>();
            private Weapons prefixWeapons = new Weapons();
            private List<string> prefixMods = new List<string>();
            public List<string> Suffix = new List<string>();
            private List<int> suffixClasses = new List<int>();
            public List<Reward> Rewards = new List<Reward>();
            private bool ShouldBeAlive { get; set; } = false;

            /// <summary>
            /// Create a Mission object from a mission string (including rewards).
            /// </summary>
            public Mission(string description)
            {
                string[] parts = description.Split(':', 2);
                ParseMission(parts[0]);
                string[] rewardParts = parts[1].Split(':');
                for (int ii = 0; ii < rewardParts.Length; ii += 2)
                    Rewards.Add(new Reward(rewardParts[ii], rewardParts[ii + 1]));
            }

            /// <summary>
            /// Parse the mission.
            /// </summary>
            private void ParseMission(string mission)
            {
                string[] parts = mission.Split(',');
                bool prefix = true;
                foreach (string part in parts)
                    if (MissionTypes.Contains(part))
                    {
                        Type = part;
                        prefix = false;
                    }
                    else if (prefix)
                        Prefix.Add(part);
                    else
                        Suffix.Add(part);
                prefixClasses = Prefix.ParseInts();
                suffixClasses = Suffix.ParseInts();
                foreach (string prefixPart in Prefix.FilterInts())
                    if (prefixPart.StartsWith("MOD"))
                        prefixMods.Add(prefixPart);
                    else if (prefixPart == "alive")
                        ShouldBeAlive = true;
                    else
                        prefixWeapons.AddRange(new Weapons(prefixPart));
            }

            /// <summary>
            /// Issue the rewards to the player when he shoots a weapon for mission type "shoot".
            /// </summary>
            public void IssueOnShoot(Entity shooter, Parameter weapon)
            {
                if (prefixWeapons.EmptyOrContainsName((string)weapon))
                    IssueRewards(shooter, null);
            }

            /// <summary>
            /// Issue the rewards to the killed player for mission type "die" and to the killer for mission type "kill".
            /// </summary>
            public void IssueOnKill(Entity victim, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
            {
                if (attacker.IsPlayer)
                {
                    if (ShouldBeAlive && !attacker.IsAlive)
                        return;
                    if (prefixClasses.EmptyOrContains(attacker.GetClassNumber()) && prefixWeapons.EmptyOrContainsName(weapon) && prefixMods.EmptyOrContains(mod) && suffixClasses.EmptyOrContains(victim.GetClassNumber()))
                    {
                        if (Type == "kill" && victim != attacker)
                            IssueRewards(attacker, victim);
                        if (Type == "die")
                            IssueRewards(victim, attacker);
                    }
                }
                else if (Type == "die")
                    IssueRewards(victim, attacker);
            }

            public void IssueOnSpawn(Entity receiver)
            {
                string receiverClass = receiver.HasField("currentClass") ? receiver.GetField<string>("currentClass") : null;
                int classNumber = receiverClass == null ? 0 : int.Parse(receiverClass.Last() + "");
                if (prefixClasses.EmptyOrContains(classNumber))
                    IssueRewards(receiver, null);
            }

            /// <summary>
            /// Issue the rewards to the player who just got the top score.
            /// Reset the rewards for the player who just lost the top score.
            /// </summary>
            public void IssueOnTopScore(Entity receiver, int scoreChange)
            {
                if (scoreChange > 0)
                    if (TopScorePlayer == null)
                    {
                        IssueRewards(receiver, TopScorePlayer);
                        TopScorePlayer = receiver;
                    }
                    else if (receiver != TopScorePlayer && receiver.GetScore() > TopScorePlayer.GetScore())
                    {
                        IssueRewards(receiver, TopScorePlayer);
                        ResetRewards(TopScorePlayer);
                        TopScorePlayer = receiver;
                    }
            }

            /// <summary>
            /// Issue the rewards to the player for winning.
            /// </summary>
            public void IssueOnWin()
            {
                WriteLog.Debug("ISSUEING FOR WIN");
                Entity winner = null;
                if (Players.Any())
                {
                    foreach (Entity player in Players)
                        if (winner == null || player.Score > winner.Score)
                            winner = player;
                    WriteLog.Debug("ISSUEING FOR WIN TO " + winner.Name);
                    IssueRewards(winner, null);
                }
            }

            /// <summary>
            /// Issue the rewards to the player for changing class.
            /// </summary>
            public void IssueOnClassChange(Entity changer, string oldClass, string newClass)
            {
                WriteLog.Debug("issueing rewards for " + changer.Name + "who changed from " + oldClass + " to " + newClass);
                int oldNumber = oldClass == null ? 0 : int.Parse(oldClass.Last() + "");
                int newNumber = int.Parse(newClass.Last() + "");
                if (prefixClasses.EmptyOrContains(oldNumber) && suffixClasses.EmptyOrContains(newNumber))
                    IssueRewards(changer, null);
            }

            /// <summary>
            /// Issue the rewards for this mission.
            /// </summary>
            public void IssueRewards(Entity receiver, Entity other)
            {
                WriteLog.Debug($"issue rewards start for {receiver.Name}");
                foreach (Reward reward in Rewards)
                    reward.IssueTo(receiver, other);
            }

            /// <summary>
            /// Reset the rewards for this mission.
            /// </summary>
            public void ResetRewards(Entity player)
            {
                foreach (Reward reward in Rewards)
                    reward.Reset(player);
            }
        }

        List<Mission> Missions = new List<Mission>();

        /// <summary>
        /// Set up the reward system in accordance with "settings_rewards".
        /// </summary>
        public void REWARDS_Setup()
        {
            string[] rewards = File.ReadAllLines(ConfigValues.ConfigPath + @"Rewards\" + ConfigValues.Settings_rewards + ".txt");
            foreach (string reward in rewards)
                if (!string.IsNullOrWhiteSpace(reward))
                {
                    if (reward.Contains(":"))
                    {
                        Mission mission = new Mission(reward);
                        Missions.Add(mission);
                        REWARDS_StartTracking(mission);
                    } else
                    {
                        WeaponRewardLists.Add(new Weapons(reward));
                    }
                }
        }

        /// <summary>
        /// Start tracking a mission.
        /// </summary>
        private void REWARDS_StartTracking(Mission mission)
        {
            switch (mission.Type)
            {
                case "changeclass":
                    OnClassChangeEvent += mission.IssueOnClassChange;
                    break;
                case "spawn":
                    PlayerActuallySpawned += mission.IssueOnSpawn;
                    break;
                case "kill":
                case "die":
                    OnPlayerKilledEvent += mission.IssueOnKill;
                    break;
                case "shoot":
                    PlayerConnected += p => p.OnNotify("weapon_fired", mission.IssueOnShoot);
                    break;
                case "pickup":
                    OnWeaponPickup += mission.IssueRewards;
                    break;
                case "objective_destroy":
                    OnObjectiveDestroy += mission.IssueRewards;
                    break;
                case "topscore":
                    OnScoreRewardEvent += mission.IssueOnTopScore;
                    break;
                case "win":
                    OnGameEnded += mission.IssueOnWin;
                    break;
                case "during_timer":
                    OnTimerStartEvent += p => mission.IssueRewards(p, null);
                    OnTimerExpireEvent += mission.ResetRewards;
                    break;
                case "end_timer":
                    OnTimerExpireEvent += p => mission.IssueRewards(p, null);
                    break;
                default:
                    WriteLog.Error($"Cannot track unknown mission type: {mission.Type}");
                    break;
            }
        }

    }
}
