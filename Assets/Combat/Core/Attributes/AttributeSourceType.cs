using UnityEngine;

namespace TTH.Combat.Attributes
{
    public enum AttributeSourceType
    {
        Progression,   // level, potion → bị cap
        Bonus          // item, buff → không cap
    }
}
