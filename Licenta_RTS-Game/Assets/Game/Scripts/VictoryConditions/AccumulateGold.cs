using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class AccumulateGold : VictoryCondition
{

    public int amount = 25000;

    private ResourceType type = ResourceType.Gold;

    public override string GetDescription()
    {
        return "Accumulating Gold";
    }

    public override bool PlayerMeetsConditions(Player player)
    {
        return player && !player.IsDead() && player.GetResourceAmount(type) >= amount;
    }
}
