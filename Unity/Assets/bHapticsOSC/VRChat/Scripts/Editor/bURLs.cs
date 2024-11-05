#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace bHapticsOSC.VRChat
{
    [InitializeOnLoad]
    internal class bURLs : Editor
    {
        [MenuItem("bHapticsOSC/Avatar How-to Guide")]
        private static void OpenGuide()
            => Application.OpenURL("https://bhaptics.notion.site/How-to-Upload-an-Avatar-with-bHaptics-Devices-PC-c0479c68b8984b9d9048423b8c44f503");

        [MenuItem("bHapticsOSC/GitHub Repository")]
        private static void OpenRepository()
            => Application.OpenURL("https://github.com/bhaptics/VRChatOSC");

        [MenuItem("bHapticsOSC/bHaptics Homepage")]
        private static void OpenbHapticsHomepage()
            => Application.OpenURL("https://www.bhaptics.com/");
    }
}
#endif