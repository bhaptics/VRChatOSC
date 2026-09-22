#if UNITY_EDITOR && VRC_SDK_VRCSDK3 && bHapticsOSC_HasAac
using System.Collections.Generic;
using UnityEngine;

namespace bHapticsOSC.VRChat
{
	/// <summary>
	/// Everything bGloveLayout can say about a pass, and the single console message it becomes.
	/// Keeping the sentences here leaves the layout code reading as mechanics.
	/// </summary>
	public class bGloveReport
	{
		// The console list shows the first two lines of a message. Line one carries the verdict and
		// line two has to say there is more, or a blank second line reads as the whole report.
		private const string Hint = "Click this message for the per-motor detail, and to find the glove in the Hierarchy.";

		private const string Ok = "[-] ";
		private const string Issue = "[!] ";

		private const string Footer = "Move any motor or change its constraint by hand if you like; APPLY INTEGRATION will not touch them. Press REALIGN TO HAND to derive everything from the rig again.";

		private readonly List<string> lines = new List<string>();
		private int issues;

		public void Following(string role, string node, string bone)
			=> Pass($"{role}: {node} follows {bone}.");

		public void PlacedOnHand(string role, string node, string hand)
			=> Pass($"{role}: {node} placed on {hand}.");

		public void FellBackToBone(string role, string node, HumanBodyBones wanted, string bone)
			=> Fail($"{role}: {wanted} is not mapped, so {node} follows {bone} instead. The motor still sits on the fingertip, but it will lag when the finger curls.");

		public void ConstraintMissing(string role, string node)
			=> Fail($"{role}: {node} was placed, but it has no VRC Parent Constraint and will not follow the finger. That is fine if you removed it on purpose.");

		public void BoneMissing(string role, string node)
			=> Fail($"{role}: no bone for it is mapped in the Humanoid rig, so {node} was disabled. Map the finger and press REALIGN TO HAND, or place the motor by hand.");

		public void NodeMissing(string role, string node)
			=> Fail($"{role}: \"{node}\" is missing from the glove. Was it renamed or deleted? Remove and re-add the device to restore it.");

		public void PalmDirectionUnknown()
			=> Fail("Palm direction: could not be worked out from this rig - either fewer than two finger knuckles are mapped, or the ones that are line up with the wrist.");

		public void NeedsPalmDirection(string role, string node)
			=> Fail($"{role}: {node} is offset from the wrist, so it needs the palm direction and was left where it is. Place it by hand.");

		public void PalmDirectionNarrow(string thumbSide, string outerSide)
			=> Fail($"Wrist and palm: not every knuckle is mapped, so the palm direction spans only {thumbSide} to {outerSide}. The motors were placed, but a narrow span tilts the axis - check them by hand.");

		// The one failure that ends the pass before there is anything to collect.
		public static void CannotLayOut(string deviceName, HumanBodyBones hand, GameObject glove)
			=> Debug.LogWarning($"[{bHapticsOSCIntegration.SystemName}] {deviceName} could not be laid out: {hand} is not mapped in the avatar's Humanoid rig.", glove);

		// Passing the glove as the context object is what makes clicking the entry select it.
		public void Emit(string deviceName, string hand, GameObject glove)
		{
			string headline = (issues == 0) ? $"all {lines.Count} motors placed."
				: (issues == 1) ? "1 motor needs attention."
				: $"{issues} motors need attention.";

			string message = $"[{bHapticsOSCIntegration.SystemName}] {deviceName} laid out on {hand} - {headline}\n"
				+ $"{Hint}\n\n"
				+ string.Join("\n", lines)
				+ $"\n\n{Footer}";

			if (issues > 0)
				Debug.LogWarning(message, glove);
			else
				Debug.Log(message, glove);
		}

		private void Pass(string line) => lines.Add(Ok + line);

		private void Fail(string line)
		{
			lines.Add(Issue + line);
			issues++;
		}
	}
}
#endif
