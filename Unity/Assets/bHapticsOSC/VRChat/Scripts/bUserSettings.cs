#if UNITY_EDITOR && VRC_SDK_VRCSDK3 && bHapticsOSC_HasAac
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace bHapticsOSC.VRChat
{
    [System.Serializable]
    public class bUserSettings : ScriptableObject
    {
        [SerializeField] public HumanBodyBones Bone;
        [SerializeField] public GameObject CurrentPrefab;
        [SerializeField] public List<string> CustomContactTags = new List<string>();

        [SerializeField] public Color TouchView_Default = new Color(0, 0, 0, 0);
        private Color touchView_Default = new Color(0, 0, 0, 0);
        [SerializeField] public Color TouchView_Triggered = new Color(0, 1, 1, 0.5f);
        private Color touchView_Triggered = new Color(0, 1, 1, 0.5f);

        [SerializeField] private bool _showMesh = true;
        [SerializeField] private bool _isMobile = false;
        public System.Action<bUserSettings> OnShowMeshChange;

        public bool ShowMesh
        {
            get => _showMesh;
            set
            {
                if (_showMesh == value)
                    return;
                _showMesh = value;
                OnShowMeshChange?.Invoke(this);
            }
        }

        // Read-only: the platform is set in ResetTo and in FindExistingPrefab, both of which swap
        // the prefab themselves.
        public bool IsMobile => _isMobile;

        public void FindExistingPrefab(bDeviceTemplate device)
        {
            if (CurrentPrefab != null)
                return;
            foreach (GameObject obj in (GameObject[])FindObjectsOfType(typeof(GameObject)))
            {
                if (!PrefabUtility.IsPartOfAnyPrefab(obj))
                    continue;

                Object objPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(obj);
                if (objPrefab == null)
                    continue;

                // All four variants, not just the PC pair. The null check above matters: not every
                // device ships every variant, and an empty slot matches anything with no source.
                bool objIsMesh = (objPrefab == device.PrefabMesh) || (objPrefab == device.PrefabMeshMobile);
                bool objIsPlain = (objPrefab == device.Prefab) || (objPrefab == device.PrefabMobile);
                if (!objIsMesh && !objIsPlain)
                    continue;

                _showMesh = objIsMesh;
                _isMobile = (objPrefab == device.PrefabMobile) || (objPrefab == device.PrefabMeshMobile);
                //if (_showMesh)
                //    bShader.GetTouchViewColors(device.ShaderIndex, obj, ref TouchView_Default, ref TouchView_Triggered);

                CurrentPrefab = obj;
                CustomContactTags.Clear();
                bContacts.ScanForExistingTags(this);

                break;
            }
        }

        public void SwapPrefabs(Animator animator, GameObject newPrefab, bool resetTransform = false)
        {
            if (CurrentPrefab != null)
                Undo.RecordObject(CurrentPrefab, $"[{bHapticsOSCIntegration.SystemName}] Swapped Prefabs");

            Transform parent = animator.GetBoneTransform(Bone);
            GameObject spawnedPrefab = (GameObject)PrefabUtility.InstantiatePrefab(newPrefab);

            Undo.RegisterCreatedObjectUndo(spawnedPrefab, $"[{bHapticsOSCIntegration.SystemName}] Swapped Prefabs");
            Undo.SetTransformParent(spawnedPrefab.transform, parent, $"[{bHapticsOSCIntegration.SystemName}] Swapped Prefabs");

            GameObject baseObj = newPrefab;
            if (!resetTransform && (CurrentPrefab != null))
                baseObj = CurrentPrefab;

            spawnedPrefab.transform.localPosition = baseObj.transform.localPosition;
            spawnedPrefab.transform.localEulerAngles = baseObj.transform.localEulerAngles;

            // Blender-exported rigs often carry a bone scale of 100, which would blow a vest up to
            // 86m across. Divide it back out - but only for values read off the prefab, since
            // anything copied from the live instance has already been through it.
            spawnedPrefab.transform.localScale = (baseObj == newPrefab)
                ? DivideByBoneScale(baseObj.transform.localScale, parent)
                : baseObj.transform.localScale;

            string[] currentTags = CustomContactTags.ToArray();

            Color currentTouchViewDefault = TouchView_Default;
            Color currentTouchViewTriggered = TouchView_Triggered;

            if (CurrentPrefab != null)
                Undo.DestroyObjectImmediate(CurrentPrefab);

            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());

            CustomContactTags.Clear();
            CustomContactTags.AddRange(currentTags);

            TouchView_Default = currentTouchViewDefault;
            TouchView_Triggered = currentTouchViewTriggered;

            CurrentPrefab = spawnedPrefab;
        }

        private static Vector3 DivideByBoneScale(Vector3 scale, Transform bone)
        {
            if (bone == null)
                return scale;

            Vector3 boneScale = bone.lossyScale;
            return new Vector3(
                Mathf.Approximately(boneScale.x, 0f) ? scale.x : scale.x / boneScale.x,
                Mathf.Approximately(boneScale.y, 0f) ? scale.y : scale.y / boneScale.y,
                Mathf.Approximately(boneScale.z, 0f) ? scale.z : scale.z / boneScale.z);
        }

        public void SelectCurrentPrefab()
            => Selection.activeGameObject = CurrentPrefab;

        public void DestroyCurrentPrefab()
        {
            if (CurrentPrefab == null)
                return;
            Undo.DestroyObjectImmediate(CurrentPrefab);
            CurrentPrefab = null;
            CustomContactTags.Clear();
            TouchView_Default = touchView_Default;
            TouchView_Triggered = touchView_Triggered;
        }

        // Setting ShowMesh is what spawns the device, and the spawn reads both flags - so settle
        // them first, or it gets built on the old values and immediately rebuilt.
        public void ResetTo(bool isMobile, bool showMesh)
        {
            _isMobile = isMobile;

            DestroyCurrentPrefab();
            _showMesh = !showMesh;
            ShowMesh = showMesh;
            CustomContactTags.Clear();
            TouchView_Default = touchView_Default;
            TouchView_Triggered = touchView_Triggered;
        }

        // The RESET button. Rebuilds the device in place, keeping the platform and variant it is on.
        public void Reset()
            => ResetTo(_isMobile, _showMesh);
    }
}
#endif