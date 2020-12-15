using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RTS
{
    public enum CursorState { Select, Move, Attack, PanLeft, PanRight, PanUp, PanDown, Harvest, RallyPoint }
    public enum ResourceType { Money, Power, Ore, Unknown }

    public enum GameSize { Small = 400, Medium = 600, Big = 800, Huge = 1000}
}
