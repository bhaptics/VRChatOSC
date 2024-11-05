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
            => Application.OpenURL("https://bhaptics.notion.site/How-to-play-VRChat-with-bHaptics-1226d5724b8b80229ab9e0001ab70b61");

        [MenuItem("bHapticsOSC/GitHub Repository")]
        private static void OpenRepository()
            => Application.OpenURL("https://github.com/bhaptics/VRChatOSC");

        [MenuItem("bHapticsOSC/bHaptics Homepage")]
        private static void OpenbHapticsHomepage()
            => Application.OpenURL("https://www.bhaptics.com/");
    }
}
#endif