using System.Collections.Generic;
using InfinityScript;

namespace LambAdmin
{
    public partial class DHAdmin
    {

        class Reward
        {
            public string Mission;
            public string RewardType;
            public string RewardAmount;

            public Reward(string reward)
            {
                string[] parts = reward.Split(':');
                Mission = parts[0];
                RewardType = parts[1];
                RewardAmount = parts[2];
                StartTracking();
            }

            private void StartTracking()
            {
                switch (Mission)
                {
                    case "pickup":
                        OnWeaponPickup += IssueOnPickup;
                        break;
                }
            }

            public void IssueOnPickup(Entity receiver, Entity pickup)
            {
                AfterDelay(500, () =>
                {
                    IssueTo(receiver);
                });
            }

            public void IssueTo(Entity receiver)
            {
                switch (RewardType)
                {
                    case "speed":
                        receiver.SetSpeed(receiver.GetSpeed() + float.Parse(RewardAmount));
                        break;
                }
            }

            public void Reset(Entity player)
            {
                switch (RewardType)
                {
                    case "speed":
                        player.SetSpeed(ConfigValues.settings_movement_speed);
                        break;
                }
            }
        }

        List<Reward> Rewards = new List<Reward>();

        public void REWARDS_Setup()
        {
            string[] rewards = ConfigValues.settings_reward.Split('|');
            foreach (string reward in rewards)
            {
                Rewards.Add(new Reward(reward));
            }
            OnPlayerKilledEvent += REWARDS_OnKill;
        }


        public void REWARDS_OnKill(Entity victim, Entity inflictor, Entity attacker, int damage, string mod, string weapon, Vector3 dir, string hitLoc)
        {
            foreach (Reward reward in Rewards)
                reward.Reset(victim);
        }

    }
}
