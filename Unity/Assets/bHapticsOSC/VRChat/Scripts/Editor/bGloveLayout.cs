#if UNITY_EDITOR && VRC_SDK_VRCSDK3 && bHapticsOSC_HasAac
using UnityEditor;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Constraint.Components;

namespace bHapticsOSC.VRChat
{
	/// <summary>
	/// Lays a glove onto an avatar. Glove prefabs carry no coordinates - every node comes from the rig.
	/// </summary>
	public static class bGloveLayout
	{
		// Must match the object names in all four Glove prefabs.
		private const string NodeNamePrefix = "Node ";

		// One Ctrl+Z takes the whole pass back. No group opened here on purpose - a prefab swap calls
		// this right after replacing the instance, and its group has to cover both.
		private static readonly string UndoName = $"[{bHapticsOSCIntegration.SystemName}] Glove Layout";

		// Indexed alongside the finger chains below; the two must stay the same length.
		private static readonly string[] FingerNames =
			{ "Thumb", "Index finger", "Middle finger", "Ring finger", "Little finger" };

		// Every finger bone is optional in a Humanoid rig, so each chain lists the distal bone first
		// and falls back up the finger.
		private static readonly HumanBodyBones[][] FingerChainsLeft =
		{
			new[] { HumanBodyBones.LeftThumbDistal, HumanBodyBones.LeftThumbIntermediate, HumanBodyBones.LeftThumbProximal },
			new[] { HumanBodyBones.LeftIndexDistal, HumanBodyBones.LeftIndexIntermediate, HumanBodyBones.LeftIndexProximal },
			new[] { HumanBodyBones.LeftMiddleDistal, HumanBodyBones.LeftMiddleIntermediate, HumanBodyBones.LeftMiddleProximal },
			new[] { HumanBodyBones.LeftRingDistal, HumanBodyBones.LeftRingIntermediate, HumanBodyBones.LeftRingProximal },
			new[] { HumanBodyBones.LeftLittleDistal, HumanBodyBones.LeftLittleIntermediate, HumanBodyBones.LeftLittleProximal },
		};

		private static readonly HumanBodyBones[][] FingerChainsRight =
		{
			new[] { HumanBodyBones.RightThumbDistal, HumanBodyBones.RightThumbIntermediate, HumanBodyBones.RightThumbProximal },
			new[] { HumanBodyBones.RightIndexDistal, HumanBodyBones.RightIndexIntermediate, HumanBodyBones.RightIndexProximal },
			new[] { HumanBodyBones.RightMiddleDistal, HumanBodyBones.RightMiddleIntermediate, HumanBodyBones.RightMiddleProximal },
			new[] { HumanBodyBones.RightRingDistal, HumanBodyBones.RightRingIntermediate, HumanBodyBones.RightRingProximal },
			new[] { HumanBodyBones.RightLittleDistal, HumanBodyBones.RightLittleIntermediate, HumanBodyBones.RightLittleProximal },
		};

		// Fixed to the hand, offset from the wrist in hand lengths along the basis below.
		private static readonly (int Index, string Role, Vector3 Offset)[] StaticNodes =
		{
			(5, "Wrist",             new Vector3(0.0f, 0.0f,  0.0f)),
			(6, "Palm (thumb side)", new Vector3(0.5f, 0.1f,  0.2f)),
			(7, "Palm (outer side)", new Vector3(0.5f, 0.1f, -0.2f)),
		};

		// Null for anything that is not a glove, so callers can hand over any device type.
		private static HumanBodyBones[][] FingerChainsFor(bDeviceType type) => type switch
		{
			bDeviceType.GLOVE_LEFT => FingerChainsLeft,
			bDeviceType.GLOVE_RIGHT => FingerChainsRight,
			_ => null,
		};

		public static bool IsGlove(bDeviceType type) => FingerChainsFor(type) != null;

		/// <summary>
		/// Lays out one glove from scratch; a no-op for any other device. Overwrites every node, so it
		/// runs only on a fresh prefab or from REALIGN TO HAND.
		/// </summary>
		public static void Apply(bHapticsOSCIntegration editorComp, bDeviceType type)
		{
			HumanBodyBones[][] chains = FingerChainsFor(type);
			if (chains == null)
			{
				return;
			}

			if (!bConstraints.TryGetDeviceSettings(editorComp, type, out bUserSettings settings))
			{
				return;
			}

			bDeviceTemplate template = bDevice.AllTemplates[type];
			Animator animator = editorComp.avatarAnimator;

			Transform hand = animator.GetBoneTransform(template.Bone);
			if (!hand)
			{
				bGloveReport.CannotLayOut(template.Name, template.Bone, settings.CurrentPrefab);
				return;
			}

			Transform root = settings.CurrentPrefab.transform;
			bGloveReport report = new();

			// Names the group without opening one, so Edit > Undo reads as the layout.
			Undo.SetCurrentGroupName(UndoName);

			// Driven by the finger list, not the hierarchy, so a renamed or unwired node still gets
			// reported instead of being skipped silently.
			for (int index = 0; index < chains.Length; index++)
				PlaceFinger(animator, root, index, chains[index], report);

			PlaceStaticNodes(animator, root, hand, chains, type, report);
			report.Emit(template.Name, hand.name, settings.CurrentPrefab);
		}

		// Placed even without a constraint - the motor still belongs on the fingertip, it just will
		// not follow the finger curling.
		private static void PlaceFinger(Animator animator, Transform root, int index, HumanBodyBones[] chain, bGloveReport report)
		{
			string role = FingerNames[index];
			string name = NodeNamePrefix + index;

			Transform node = root.Find(name);
			if (!node)
			{
				report.NodeMissing(role, name);
				return;
			}

			VRCParentConstraint constraint = node.GetComponent<VRCParentConstraint>();
			Transform bone = ResolveFingerBone(animator, chain, out int step);
			SetNodeEnabled(node, bone);

			if (!bone)
			{
				if (constraint)
					UnwireConstraint(constraint);

				report.BoneMissing(role, node.name);
				return;
			}

			MoveNodeToFingertip(node, bone, step);

			if (!constraint)
			{
				report.ConstraintMissing(role, node.name);
				return;
			}

			WireConstraint(constraint, bone);

			if (step > 0)
				report.FellBackToBone(role, node.name, chain[0], bone.name);
			else
				report.Following(role, node.name, bone.name);
		}

		private static void PlaceStaticNodes(Animator animator, Transform root, Transform hand,
			HumanBodyBones[][] chains, bDeviceType type, bGloveReport report)
		{
			// Not an early return: the wrist node needs no palm direction, so a rig too sparse to give
			// one still gets that motor placed.
			bool hasBasis = TryBuildHandBasis(animator, hand, chains, type == bDeviceType.GLOVE_LEFT, report,
				out Vector3 finger, out Vector3 palmar, out Vector3 across, out float handLength);

			if (!hasBasis)
				report.PalmDirectionUnknown();

			foreach ((int index, string role, Vector3 offset) in StaticNodes)
			{
				string name = NodeNamePrefix + index;

				Transform node = root.Find(name);
				if (!node)
				{
					report.NodeMissing(role, name);
					continue;
				}

				if (!hasBasis && (offset != Vector3.zero))
				{
					report.NeedsPalmDirection(role, node.name);
					continue;
				}

				Undo.RecordObject(node, UndoName);
				node.SetPositionAndRotation(
					hand.position
						+ (finger * (offset.x * handLength))
						+ (palmar * (offset.y * handLength))
						+ (across * (offset.z * handLength)),
					hand.rotation);
				PrefabUtility.RecordPrefabInstancePropertyModifications(node);

				SetMotorOffset(node, Vector3.zero);
				report.PlacedOnHand(role, node.name, hand.name);
			}
		}

		private static Transform ResolveFingerBone(Animator animator, HumanBodyBones[] chain, out int step)
		{
			for (step = 0; step < chain.Length; step++)
			{
				Transform bone = animator.GetBoneTransform(chain[step]);
				if (bone)
				{
					return bone;
				}
			}

			return null;
		}

		// Decided fresh each pass, so mapping the finger and pressing REALIGN brings the node back.
		private static void SetNodeEnabled(Transform node, bool enabled)
		{
			if (node.gameObject.activeSelf == enabled)
			{
				return;
			}

			Undo.RecordObject(node.gameObject, UndoName);
			node.gameObject.SetActive(enabled);
			PrefabUtility.RecordPrefabInstancePropertyModifications(node.gameObject);
		}

		// The component stays for the next pass to rewire. Its source must go: active and source-less
		// it pins the node at the hand origin.
		private static void UnwireConstraint(VRCParentConstraint constraint)
		{
			Undo.RecordObject(constraint, UndoName);
			constraint.Sources.Clear();
			constraint.IsActive = false;
			PrefabUtility.RecordPrefabInstancePropertyModifications(constraint);
		}

		// Locked, or the editor rebakes ParentPositionOffset to hold the node's current pose.
		private static void WireConstraint(VRCParentConstraint constraint, Transform bone)
		{
			Undo.RecordObject(constraint, UndoName);

			VRCConstraintSource source = VRCConstraintSource.CreateDefault();
			source.SourceTransform = bone;
			source.Weight = 1f;

			constraint.Sources.Clear();
			constraint.Sources.Add(source);
			constraint.IsActive = true;
			constraint.Locked = true;

			// Unrecorded, a prefab instance discards this on the next domain reload.
			PrefabUtility.RecordPrefabInstancePropertyModifications(constraint);
		}

		// Constraints only solve at runtime, so nothing else places the node in the editor. The tip
		// reach is measured through the node, so it survives any bone scale.
		private static void MoveNodeToFingertip(Transform node, Transform bone, int fallbackSteps)
		{
			Undo.RecordObject(node, UndoName);
			node.SetPositionAndRotation(bone.position, bone.rotation);
			PrefabUtility.RecordPrefabInstancePropertyModifications(node);

			SetMotorOffset(node, node.InverseTransformPoint(FingertipPosition(bone, fallbackSteps)));
		}

		// Fingertip reach on a finger, zero on a node already standing where the motor belongs.
		// Derived either way, so REALIGN resets it - unlike scale and rotation, which stay the user's.
		private static void SetMotorOffset(Transform node, Vector3 localPosition)
		{
			foreach (Transform child in node)
			{
				Undo.RecordObject(child, UndoName);
				child.localPosition = localPosition;
				PrefabUtility.RecordPrefabInstancePropertyModifications(child);
			}
		}

		// The wider the knuckle pair the better conditioned the across axis, so walk out from the thumb
		// side. Thumb excluded: its proximal sits below the knuckle line and tilts the axis out of the palm.
		private static bool TryGetKnuckleSpan(Animator animator, HumanBodyBones[][] chains,
			out Transform thumbSide, out Transform outerSide, out bool full)
		{
			thumbSide = outerSide = null;
			int mapped = 0;

			for (int i = 1; i < chains.Length; i++)
			{
				HumanBodyBones[] chain = chains[i];
				Transform knuckle = animator.GetBoneTransform(chain[chain.Length - 1]);
				if (!knuckle)
					continue;

				mapped++;
				if (thumbSide)
					outerSide = knuckle;
				else
					thumbSide = knuckle;
			}

			full = (mapped == chains.Length - 1);
			return outerSide;
		}

		// Verified against a T-pose, where palmar should come out pointing at world down.
		private static bool TryBuildHandBasis(Animator animator, Transform hand, HumanBodyBones[][] chains,
			bool isLeft, bGloveReport report,
			out Vector3 finger, out Vector3 palmar, out Vector3 across, out float handLength)
		{
			finger = palmar = across = Vector3.zero;
			handLength = 0f;

			if (!TryGetKnuckleSpan(animator, chains, out Transform thumbSide, out Transform outerSide, out bool full))
			{
				return false;
			}

			// The middle knuckle sits straight ahead of the wrist and sets the scale. Any other mapped
			// knuckle gives a slightly rotated axis, still better than leaving the motors on the wrist.
			Transform middle = animator.GetBoneTransform(isLeft ? HumanBodyBones.LeftMiddleProximal : HumanBodyBones.RightMiddleProximal);
			Transform ahead = middle ? middle : thumbSide;

			handLength = Vector3.Distance(hand.position, ahead.position);
			if (handLength <= 0f)
			{
				return false;
			}

			finger = (ahead.position - hand.position).normalized;
			across = (thumbSide.position - outerSide.position).normalized;

			// Parallel axes mean the knuckles line up with the wrist: no palm plane to find, and
			// normalising the zero cross product would silently flatten every offset.
			Vector3 normal = Vector3.Cross(finger, across);
			if (normal.sqrMagnitude < 0.0001f)
			{
				return false;
			}

			palmar = normal.normalized * (isLeft ? -1f : 1f);

			if (!full)
				report.PalmDirectionNarrow(thumbSide.name, outerSide.name);

			return true;
		}

		// A distal missing from the mapping is usually still in the hierarchy, one child level down per
		// fallback step. Never a constraint source - tip markers are outside the Humanoid contract.
		private static Vector3 FingertipPosition(Transform bone, int fallbackSteps)
		{
			// Judged against the finger's own length, so this holds at any avatar scale.
			float segment = bone.parent ? Vector3.Distance(bone.parent.position, bone.position) : 0f;

			Transform tip = bone;
			for (int level = 0; (level <= fallbackSteps) && (tip.childCount > 0); level++)
				tip = tip.GetChild(0);

			if ((tip != bone) && (segment > 0f))
			{
				// The first child may be a nail mesh or an accessory rather than the tip.
				float reach = Vector3.Distance(bone.position, tip.position);
				if ((reach > segment * 0.1f) && (reach < segment * (fallbackSteps + 2f)))
				{
					return tip.position;
				}
			}

			if (segment > 0f)
			{
				return bone.position + ((bone.position - bone.parent.position).normalized * segment * 0.8f);
			}

			return bone.position;
		}

	}
}
#endif
