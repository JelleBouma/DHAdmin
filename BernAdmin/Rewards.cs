﻿using System.Collections.Generic;
using InfinityScript;

namespace LambAdmin
{
    public partial class DHAdmin
    {
        class Reward
        {
            private static string[] MissionTypeArr = { "kill", "pickup", "objective_destroy" };
            public static List<string> MissionTypes = new List<string>(MissionTypeArr);

            public string Mission;
            public List<string> MissionPrefix = new List<string>();
            public List<string> MissionSuffix = new List<string>();
            public string RewardType;
            public string RewardAmount;

            public Reward(string reward)
            {
                string[] parts = reward.Split(':');
                ParseMission(parts[0]);
                RewardType = parts[1];
                RewardAmount = parts[2];
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
                        Mission = part;
                        prefix = false;
                        continue;
                    }
                    if (prefix)
                        MissionPrefix.Add(part);
                    else
                        MissionSuffix.Add(part);
                }
            }

            private void StartTracking()
            {
                switch (Mission)
                {
                    case "kill":
                        OnPlayerKilledEvent += IssueOnKill;
                        break;
                    case "pickup":
                        OnWeaponPickup += IssueOnUse;
                        break;
                    case "objective_destroy":
                        OnObjectiveDestroy += IssueOnUse;
                        break;
                }
            }

            public void IssueOnKill(Entity victim, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
            {
                List<int> prefixClasses = MissionPrefix.ParseInts();
                List<int> suffixClasses = MissionSuffix.ParseInts();
                List<string> prefixWeapons = MissionPrefix.FilterInts();
                if (prefixClasses.EmptyOrContains(attacker.GetClassNumber()) && prefixWeapons.EmptyOrContains(weapon) && suffixClasses.EmptyOrContains(attacker.GetClassNumber()))
                    IssueTo(attacker);
            }

            public void IssueOnUse(Entity receiver, Entity usable)
            {
                IssueTo(receiver);
            }

            public void IssueTo(Entity receiver)
            {
                switch (RewardType)
                {
                    case "speed":
                        receiver.SetSpeed(receiver.GetSpeed() + float.Parse(RewardAmount));
                        break;
                    case "score":
                        receiver.AddScore(int.Parse(RewardAmount));
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
                }
            }
        }

        List<Reward> Rewards = new List<Reward>();

        public void REWARDS_Setup()
        {
            string[] rewards = ConfigValues.Settings_rewards.Split('|');
            foreach (string reward in rewards)
                Rewards.Add(new Reward(reward));
            OnPlayerKilledEvent += REWARDS_OnKill;
        }


        public void REWARDS_OnKill(Entity victim, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            foreach (Reward reward in Rewards)
                reward.Reset(victim);
        }

    }
}
