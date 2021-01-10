using System;
using System.Collections.Generic;
using InfinityScript;

namespace LambAdmin
{
    public partial class DHAdmin
    {

        private static readonly string[] MissionTypeArr = { "kill", "die", "pickup", "objective_destroy", "topscore" };
        static event Action<Entity, int> OnScoreRewardEvent = (player, reward) => { };
        static Entity TopScorePlayer = null;

        class Reward
        {
            public string RewardType;
            public string RewardAmount;

            public Reward(string rewardType, string rewardAmount)
            {
                RewardType = rewardType.Trim();
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
            public static List<string> MissionTypes = new List<string>(MissionTypeArr);
            public string MissionType;
            public List<string> MissionPrefix = new List<string>();
            private List<int> prefixClasses = new List<int>();
            private List<string> prefixWeapons = new List<string>();
            private List<string> prefixMods = new List<string>();
            public List<string> MissionSuffix = new List<string>();
            private List<int> suffixClasses = new List<int>();
            public List<Reward> Rewards = new List<Reward>();

            public Mission(string description)
            {
                string[] parts = description.Split(':', 2);
                ParseMission(parts[0]);
                string[] rewardParts = parts[1].Split(':');
                for (int ii = 0; ii < rewardParts.Length; ii += 2)
                    Rewards.Add(new Reward(rewardParts[ii], rewardParts[ii + 1]));
                StartTracking();
            }

            private void ParseMission(string mission)
            {
                string[] parts = mission.Split(',');
                bool prefix = true;
                foreach (string part in parts)
                {
                    if (MissionTypes.Contains(part))
                    {
                        MissionType = part;
                        prefix = false;
                        continue;
                    }
                    if (prefix)
                        MissionPrefix.Add(part);
                    else
                        MissionSuffix.Add(part);
                }
                List<int> prefixClasses = MissionPrefix.ParseInts();
                List<int> suffixClasses = MissionSuffix.ParseInts();
                foreach (string prefixPart in MissionPrefix.FilterInts())
                    if (prefixPart.StartsWith("MOD"))
                        prefixMods.Add(prefixPart);
                    else
                        prefixWeapons.Add(prefixPart);
            }

            private void StartTracking()
            {
                switch (MissionType)
                {
                    case "kill":
                    case "die":
                        OnPlayerKilledEvent += IssueOnKill;
                        break;
                    case "pickup":
                        OnWeaponPickup += IssueRewards;
                        break;
                    case "objective_destroy":
                        OnObjectiveDestroy += IssueRewards;
                        break;
                    case "topscore":
                        OnScoreRewardEvent += IssueOnTopScore;
                        break;
                }
            }

            public void IssueOnKill(Entity victim, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
            {
                WriteLog.Debug("kill or die");
                if (prefixClasses.EmptyOrContains(attacker.GetClassNumber()) && prefixWeapons.EmptyOrContains(weapon) && prefixMods.EmptyOrContains(mod) && suffixClasses.EmptyOrContains(victim.GetClassNumber()))
                {
                    if (MissionType == "kill" && victim != attacker)
                        IssueRewards(attacker, victim);
                    if (MissionType == "die")
                        IssueRewards(victim, attacker);
                }
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
            if (ConfigValues.Settings_rewards.Contains("next") || ConfigValues.Settings_rewards.Contains("previous"))
                WeaponRewardList = new Weapons(ConfigValues.Settings_rewards_weapon_list);
            string[] rewards = ConfigValues.Settings_rewards.Split('|');
            foreach (string reward in rewards)
                Missions.Add(new Mission(reward));
        }

    }
}
