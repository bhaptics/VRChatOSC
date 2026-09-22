#if UNITY_EDITOR && VRC_SDK_VRCSDK3 && bHapticsOSC_HasAac
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace bHapticsOSC.VRChat
{
    public static class bDevice
    {
        public static Dictionary<bDeviceType, bDeviceTemplate> AllTemplates;

        static bDevice()
        {
            AllTemplates = new Dictionary<bDeviceType, bDeviceTemplate>();

            AllTemplates[bDeviceType.HEAD] = new bDeviceTemplate { Name = "Head", HasBone = true, Bone = HumanBodyBones.Head, NodeCount = 6 };

            AllTemplates[bDeviceType.VEST] = new bDeviceTemplate { Name = "Vest", HasBone = true, Bone = HumanBodyBones.Chest };
            AllTemplates[bDeviceType.VEST_FRONT] = new bDeviceTemplate { Name = "Vest Front", NodeCount = 20, ShaderIndex = 1.1f };
            AllTemplates[bDeviceType.VEST_BACK] = new bDeviceTemplate { Name = "Vest Back", NodeCount = 20, ShaderIndex = 1.2f };

            AllTemplates[bDeviceType.ARM_LEFT] = new bDeviceTemplate { Name = "Arm Left", HasBone = true, Bone = HumanBodyBones.LeftLowerArm, NodeCount = 6, ShaderIndex = 2f };
            AllTemplates[bDeviceType.ARM_RIGHT] = new bDeviceTemplate { Name = "Arm Right", HasBone = true, Bone = HumanBodyBones.RightLowerArm, NodeCount = 6, ShaderIndex = 3f };

            AllTemplates[bDeviceType.HAND_LEFT] = new bDeviceTemplate { Name = "Hand Left", HasBone = true, Bone = HumanBodyBones.LeftHand, NodeCount = 3, ShaderIndex = 4f };
            AllTemplates[bDeviceType.HAND_RIGHT] = new bDeviceTemplate { Name = "Hand Right", HasBone = true, Bone = HumanBodyBones.RightHand, NodeCount = 3, ShaderIndex = 5f };

            AllTemplates[bDeviceType.FOOT_LEFT] = new bDeviceTemplate { Name = "Foot Left", HasBone = true, Bone = HumanBodyBones.LeftFoot, NodeCount = 3, ShaderIndex = 8f };
            AllTemplates[bDeviceType.FOOT_RIGHT] = new bDeviceTemplate { Name = "Foot Right", HasBone = true, Bone = HumanBodyBones.RightFoot, NodeCount = 3, ShaderIndex = 9f };

            // DefaultShowMesh off until the glove gets a body: its mesh variant is eight indicator
            // spheres with nothing to sit on. Put it back to true once there is a model.
            AllTemplates[bDeviceType.GLOVE_LEFT] = new bDeviceTemplate { Name = "Glove Left", HasBone = true, Bone = HumanBodyBones.LeftHand, NodeCount = 8, ShaderIndex = 10f, DefaultShowMesh = false };
            AllTemplates[bDeviceType.GLOVE_RIGHT] = new bDeviceTemplate { Name = "Glove Right", HasBone = true, Bone = HumanBodyBones.RightHand, NodeCount = 8, ShaderIndex = 11f, DefaultShowMesh = false };

            foreach (bDeviceTemplate settings in AllTemplates.Values)
            {
                string nameWithoutSpaces = settings.Name.Replace(" ", "");

                string withoutMeshStr = $"Assets/bHapticsOSC/VRChat/Prefabs/Without Mesh/{nameWithoutSpaces}.prefab";
                if (File.Exists(withoutMeshStr))
                {
                    settings.Prefab = (GameObject)EditorGUIUtility.Load(withoutMeshStr);
                }

                string withMeshStr = $"Assets/bHapticsOSC/VRChat/Prefabs/With Mesh/{nameWithoutSpaces}.prefab";
                if (File.Exists(withMeshStr))
                {
                    settings.PrefabMesh = (GameObject)EditorGUIUtility.Load(withMeshStr);
                }

                string withoutMeshMobileStr = $"Assets/bHapticsOSC/VRChat/Prefabs/Mobile Without Mesh/{nameWithoutSpaces}.prefab";
                if (File.Exists(withoutMeshMobileStr))
                {
                    settings.PrefabMobile = (GameObject)EditorGUIUtility.Load(withoutMeshMobileStr);
                }

                string withMeshMobileStr = $"Assets/bHapticsOSC/VRChat/Prefabs/Mobile With Mesh/{nameWithoutSpaces}.prefab";
                if (File.Exists(withMeshMobileStr))
                {
                    settings.PrefabMeshMobile = (GameObject)EditorGUIUtility.Load(withMeshMobileStr);
                }
            }
        }

        public static float GetShaderIndex(this bDeviceType type, int node = 1)
        {
            float index = AllTemplates[type].ShaderIndex;

            switch (type)
            {
                case bDeviceType.HAND_RIGHT:
                    return index + ((node + 1) * 0.1f);
                case bDeviceType.HAND_LEFT:
                    return index + ((node + 1) * 0.1f);
                case bDeviceType.GLOVE_LEFT:
                    return index + ((node + 1) * 0.1f);
                case bDeviceType.GLOVE_RIGHT:
                    return index + ((node + 1) * 0.1f);
                default:
                    return index;
            }
        }
    }
}
#endif