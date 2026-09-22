#if UNITY_EDITOR && VRC_SDK_VRCSDK3 && bHapticsOSC_HasAac
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;

namespace bHapticsOSC.VRChat
{
    public static class bConstraints
    {
		private static readonly HumanBodyBones[] HandBonesLeft = new HumanBodyBones[]
		{
			HumanBodyBones.LeftHand,
			HumanBodyBones.LeftIndexProximal,
			HumanBodyBones.LeftIndexIntermediate,
		};

		private static readonly HumanBodyBones[] HandBonesRight = new HumanBodyBones[]
		{
			HumanBodyBones.RightHand,
			HumanBodyBones.RightIndexProximal,
			HumanBodyBones.RightIndexIntermediate,
		};

		// Only rewires constraints that are still there, so deleted ones stay deleted across an APPLY.
		public static void Apply(bHapticsOSCIntegration editorComp)
		{
			if (TryGetDeviceSettings(editorComp, bDeviceType.HAND_LEFT, out bUserSettings leftHandSettings))
				ApplyContraintSources(editorComp.avatarAnimator, leftHandSettings.CurrentPrefab, HandBonesLeft);

			if (TryGetDeviceSettings(editorComp, bDeviceType.HAND_RIGHT, out bUserSettings rightHandSettings))
				ApplyContraintSources(editorComp.avatarAnimator, rightHandSettings.CurrentPrefab, HandBonesRight);
		}

		// Shared with bGloveLayout, which lays gloves out on its own.
		public static bool TryGetDeviceSettings(bHapticsOSCIntegration editorComp, bDeviceType deviceType, out bUserSettings deviceSettings)
        {
			deviceSettings = editorComp.AllUserSettings[bDevice.AllTemplates[deviceType]];
			return (deviceSettings.CurrentPrefab != null);
		}

		// Pairs each constraint with its counterpart in the prefab asset, not by list position, so
		// adding or deleting one on the instance does not shift the others onto the wrong bone.
		private static void ApplyContraintSources(this Animator animator, GameObject prefabInstance, HumanBodyBones[] bones)
		{
			GameObject asset = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefabInstance);
			if (!asset)
			{
				Debug.LogWarning($"[{bHapticsOSCIntegration.SystemName}] {prefabInstance.name} is no longer a prefab instance, so there is nothing to match its ParentConstraints against and none were wired. Remove and re-add the device.");
				return;
			}

			ParentConstraint[] template = asset.GetComponentsInChildren<ParentConstraint>(true);
			foreach (ParentConstraint parentConstraint in prefabInstance.GetComponentsInChildren<ParentConstraint>(true))
			{
				int jointIndex = System.Array.IndexOf(template, PrefabUtility.GetCorrespondingObjectFromOriginalSource(parentConstraint));
				if ((jointIndex < 0) || (jointIndex >= bones.Length))
					continue;

				var constraintSource = new ConstraintSource();
				constraintSource.weight = 1f;
				constraintSource.sourceTransform = animator.GetBoneTransform(bones[jointIndex]);
				if (constraintSource.sourceTransform)
				{
					if (parentConstraint.sourceCount <= 0)
						parentConstraint.AddSource(constraintSource);
					else
						parentConstraint.SetSource(0, constraintSource);

					// Unrecorded, this is lost on the next domain reload. No Undo.RecordObject: APPLY
					// destroys its own component, so a record here would only half-undo it.
					PrefabUtility.RecordPrefabInstancePropertyModifications(parentConstraint);
				}
			}
		}
	}
}
#endif
