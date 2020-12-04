using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTS
{
    public enum CursorState { Select, Move, Attack, PanLeft, PanRight, PanUp, PanDown, Harvest, RallyPoint }
    public enum ResourceType { Money, Power, Ore, Unknown }

    public enum GameSize { Small = 300, Medium = 500, Big = 800, Huge = 1000}
}
