using System.Reflection;
using UnityEditor;

namespace RTSLockstep.Utility
{
    public static class DevelopmentHelpers
    {
        public static void ClearConsole()
        {
#if UNITY_EDITOR
            var assembly = Assembly.GetAssembly(typeof(SceneView));
            var type = assembly.GetType("UnityEditor.LogEntries");
            var method = type.GetMethod("Clear");
            method.Invoke(new object(), null);
#endif
        }
    }
}