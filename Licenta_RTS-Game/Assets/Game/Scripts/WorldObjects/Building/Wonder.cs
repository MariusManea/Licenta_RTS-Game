using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wonder : Building
{
    public override bool Sellable()
    {
        return false;
    }
    protected override bool ShouldMakeDecision()
    {
        return false;
    }
}
