using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTS;

public class OreDeposit : Resource
{
    private int numBlocks;

    protected override void Start()
    {
        base.Start();
        numBlocks = GetComponentsInChildren<Ore>().Length;
        resourceType = ResourceType.Ore;
    }

    protected override void Update()
    {
        base.Update();
        if (amountLeft < 1)
        {
            Destroy(this.gameObject);
            return;
        }
        float percentLeft = (float)amountLeft / (float)capacity;
        if (percentLeft < 0) percentLeft = 0;
        int numBlocksToShow = (int)Mathf.Ceil(percentLeft * numBlocks);
        Ore[] blocks = GetComponentsInChildren<Ore>();
        if (numBlocksToShow >= 0 && numBlocksToShow < blocks.Length)
        {
            Ore[] sortedBlocks = new Ore[blocks.Length];
            //sort the list from highest to lowest
            foreach (Ore ore in blocks)
            {
                sortedBlocks[blocks.Length - int.Parse(ore.name)] = ore;
            }
            for (int i = numBlocksToShow; i < sortedBlocks.Length; i++)
            {
                sortedBlocks[i].GetComponent<Renderer>().enabled = false;
            }
            CalculateBounds();
        }
    }
}
