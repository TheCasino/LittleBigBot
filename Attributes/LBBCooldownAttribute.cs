using LittleBigBot.Entities;
using Qmmands;

namespace LittleBigBot.Attributes
{
    public class LBBCooldownAttribute: CooldownAttribute
    {
        public LBBCooldownAttribute(int amount, double per, CooldownMeasure cooldownMeasure, CooldownType bucketType) : base(amount, per, cooldownMeasure, bucketType)
        {
        }
    }
}