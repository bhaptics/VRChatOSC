#if UNITY_EDITOR && VRC_SDK_VRCSDK3 && bHapticsOSC_HasAac
using UnityEngine;

namespace bHapticsOSC.VRChat
{
    public class bDeviceTemplate
    {
        public string Name;
        public float ShaderIndex;
        public int NodeCount;
        public HumanBodyBones Bone;
        public bool HasBone;

        // Which variant a freshly added device starts on. Only a default, and only read on add - a
        // device already in the scene has its variant taken from the prefab it instances.
        public bool DefaultShowMesh = true;

        public GameObject Prefab;
        public GameObject PrefabMesh;
        public GameObject PrefabMobile;
        public GameObject PrefabMeshMobile;
    }
}
#endif