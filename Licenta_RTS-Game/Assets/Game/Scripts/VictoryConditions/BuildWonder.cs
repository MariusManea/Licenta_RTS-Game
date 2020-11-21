using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildWonder : VictoryCondition
{

    public override string GetDescription()
    {
        return "Building Wonder";
    }

    public override bool PlayerMeetsConditions(Player player)
    {
        Wonder wonder = player.GetComponentInChildren<Wonder>();
        return player && !player.IsDead() && wonder && !wonder.UnderConstruction();
    }
}
