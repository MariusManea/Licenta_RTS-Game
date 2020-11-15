using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace RTS
{
    public static class ResourceManager
    {
        public static float NormalScrollSpeed { get { return 0.5f; } }
        public static float FastScrollSpeed { get { return 1.5f; } }
        public static float CameraMovementTime { get { return 15; } }
        public static float RotateSpeed { get { return 3; } }

        private static Vector3 zoomAmount = new Vector3(0, -5, 5);
        public static Vector3 ZoomAmount { get { return zoomAmount; } }
        public static int ScrollWidth { get { return 30; } }
        public static float MinCameraHeight { get { return 10; } }
        public static float MaxCameraHeight { get { return 80; } }

        private static Vector3 invalidPosition = new Vector3(-99999, -99999, -99999);
        public static Vector3 InvalidPosition { get { return invalidPosition; } }

        private static Bounds invalidBounds = new Bounds(new Vector3(-99999, -99999, -99999), new Vector3(0, 0, 0));
        public static Bounds InvalidBounds { get { return invalidBounds; } }

        private static GUISkin selectBoxSkin;
        public static GUISkin SelectBoxSkin { get { return selectBoxSkin; } }

        public static void StoreSelectBoxItems(GUISkin skin)
        {
            selectBoxSkin = skin;
        }
    }
}