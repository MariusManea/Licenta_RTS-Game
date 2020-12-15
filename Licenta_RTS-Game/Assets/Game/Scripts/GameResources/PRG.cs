using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTS
{
    public static class PRG
    {
        private static uint seed;

        public static void SetSeed(uint _seed)
        {
            seed = _seed;
        }

        public static uint GetNextRandom()
        {
            seed = (seed * seed * 829 + seed * 4801 + 11251) % int.MaxValue;
            return seed;
        }

    }
}
