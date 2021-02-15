using System;
using System.Collections.Generic;
using System.IO;
using InfinityScript;

namespace LambAdmin
{
    public partial class DHAdmin
    {
        static event Action<Entity, int> OnScoreRewardEvent = (player, reward) => { };
        static Entity TopScorePlayer = null;

        class Reward
        {
            public string RewardType;
            public string RewardAmount;

            public Reward(string rewardType, string rewardAmount)
            {
                RewardType = rewardType.Trim();
                if (RewardType == "speed")
                    UTILS_Maintain(EntityExtensions.MaintainSpeed, 100);
                ParseRewardAmount(rewardAmount);
            }

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

            public void IssueTo(Entity receiver, Entity other)
            {
                switch (RewardType)
                {
                    case "speed":
                        receiver.AddSpeed(CalculateReward(receiver.GetSpeed(), other != null && other.IsPlayer ? other.GetSpeed() : 0));
                        break;
                    case "score":
                        int score = (int)CalculateReward(receiver.GetScore(), other != null && other.IsPlayer ? other.GetScore() : 0);
                        receiver.AddScore(score);
                        OnScoreRewardEvent(receiver, score);
                        break;
                    case "weapon":
                        receiver.SetField("remembered_weapon", receiver.GetCurrentPrimaryWeapon());
                        receiver.TakeAllWeapons();
                        receiver.GivePersistentWeapon(RewardAmount);
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

            public void Reset(Entity player)
            {
                switch (RewardType)
                {
                    case "speed":
                        if (player.HasField("speed"))
                            player.SetSpeed(ConfigValues.Settings_movement_speed);
                        break;
                    case "weapon":
                        player.ClearField("weapon");
                        if (player.HasWeapon(RewardAmount))
                        {
                            player.TakeWeapon(RewardAmount);
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
                }
            }

            public float CalculateReward(float self, float other)
            {
                WriteLog.Debug("CalculateReward " + self + " " + other);
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

        class Mission
        {
            private static readonly string[] MissionTypeArr = { "shoot", "kill", "die", "win", "pickup", "objective_destroy", "topscore" };
            public static List<string> MissionTypes = new List<string>(MissionTypeArr);
            public string Type;
            public List<string> Prefix = new List<string>();
            private List<int> prefixClasses = new List<int>();
            private Weapons prefixWeapons = new Weapons();
            private List<string> prefixMods = new List<string>();
            public List<string> Suffix = new List<string>();
            private List<int> suffixClasses = new List<int>();
            public List<Reward> Rewards = new List<Reward>();

            public Mission(string description)
            {
                string[] parts = description.Split(':', 2);
                ParseMission(parts[0]);
                string[] rewardParts = parts[1].Split(':');
                for (int ii = 0; ii < rewardParts.Length; ii += 2)
                    Rewards.Add(new Reward(rewardParts[ii], rewardParts[ii + 1]));
            }

            private void ParseMission(string mission)
            {
                string[] parts = mission.Split(',');
                bool prefix = true;
                foreach (string part in parts)
                    if (MissionTypes.Contains(part))
                    {
                        Type = part;
                        WriteLog.Debug("Type " + Type);
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
                    else
                        prefixWeapons.AddRange(new Weapons(prefixPart));
            }

            public void IssueOnShoot(Entity shooter, Parameter weapon)
            {
                if (prefixWeapons.EmptyOrContainsName((string)weapon))
                    IssueRewards(shooter, null);
            }

            public void IssueOnKill(Entity victim, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
            {
                if (attacker.IsPlayer)
                {
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

            public void IssueOnWin()
            {
                WriteLog.Debug("ISSUEING FOR WIN");
                Entity winner = null;
                foreach (Entity player in Players)
                    if (winner == null || player.Score > winner.Score)
                        winner = player;
                WriteLog.Debug("ISSUEING FOR WIN TO " + winner.Name);
                IssueRewards(winner, null);
            }

            public void IssueRewards(Entity receiver, Entity other)
            {
                foreach (Reward reward in Rewards)
                    reward.IssueTo(receiver, other);
            }

            public void ResetRewards(Entity player)
            {
                foreach (Reward reward in Rewards)
                    reward.Reset(player);
            }
        }

        List<Mission> Missions = new List<Mission>();

        public void REWARDS_Setup()
        {
            if (!string.IsNullOrWhiteSpace(ConfigValues.Settings_rewards_weapon_list))
                WeaponRewardList = new Weapons(ConfigValues.Settings_rewards_weapon_list);
            string[] rewards;
            if (ConfigValues.Settings_rewards.Contains("|"))
                rewards = ConfigValues.Settings_rewards.Split('|');
            else
                rewards = File.ReadAllLines(ConfigValues.ConfigPath + @"Rewards\" + ConfigValues.Settings_rewards + ".txt");
            foreach (string reward in rewards)
                if (!string.IsNullOrWhiteSpace(reward))
                {
                    Mission mission = new Mission(reward);
                    Missions.Add(mission);
                    REWARDS_StartTracking(mission);
                }
        }

        private void REWARDS_StartTracking(Mission mission)
        {
            switch (mission.Type)
            {
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
            }
        }

    }
}
