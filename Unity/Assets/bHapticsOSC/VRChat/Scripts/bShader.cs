#if UNITY_EDITOR && VRC_SDK_VRCSDK3 && bHapticsOSC_HasAac
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace bHapticsOSC.VRChat
{
    public static class bShader
    {
		public static Renderer[] FindRenderersFromIndex(float index, GameObject obj)
		{
			Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
			// Empty rather than null: callers index straight into the result, and the Without Mesh
			// variants legitimately have no renderers.
			if ((renderers == null) || (renderers.Length <= 0))
				return System.Array.Empty<Renderer>();

			List<Renderer> output = new List<Renderer>();
			foreach (Renderer renderer in renderers)
			{
				if (renderer == null)
					continue;

				Material[] materials = renderer.sharedMaterials;
				if ((materials == null) || (materials.Length <= 0))
					continue;

				foreach (Material material in materials)
				{
					if ((material == null) || !material.HasProperty("_Device"))
						continue;
					// Approximate, not exact: the index is float arithmetic (4f + node * 0.1f)
					// compared against a value out of a material, and those need not match bit for bit.
					if (Mathf.Approximately(material.GetFloat("_Device"), index))
					{
						output.Add(renderer);
						break;
					}
				}
			}

			return output.ToArray();
		}

		public static void GetTouchViewColors(float shaderIndex, GameObject obj, ref Color defaultCol, ref Color triggeredCol)
        {
			Renderer renderer = FindRenderersFromIndex(shaderIndex, obj).FirstOrDefault();

			if (renderer == null)
				return;

			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
			renderer.GetPropertyBlock(materialPropertyBlock);

			defaultCol = materialPropertyBlock.GetColor("_DefaultColor");
			triggeredCol = materialPropertyBlock.GetColor("_TouchColor");
		}

		public static void SetTouchViewColors(Renderer renderer, Color defaultCol, Color triggeredCol)
		{
			if (renderer == null)
				return;

			MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();

			materialPropertyBlock.SetColor("_DefaultColor", defaultCol);
			materialPropertyBlock.SetColor("_TouchColor", triggeredCol);

			renderer.SetPropertyBlock(materialPropertyBlock);
		}
	}
}
#endif