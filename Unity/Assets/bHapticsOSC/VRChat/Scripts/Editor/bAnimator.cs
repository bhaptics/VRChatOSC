#if UNITY_EDITOR && VRC_SDK_VRCSDK3 && bHapticsOSC_HasAac
using AnimatorAsCode.V0;
using System.Collections.Generic;
using UnityEngine;

namespace bHapticsOSC.VRChat
{
    public static class bAnimator
    {
        public static void CreateAllNodes(bHapticsOSCIntegration editorComp)
        {
            AacFlBase aac = editorComp.CreateAnimatorAsCode();

            foreach (KeyValuePair<bDeviceType, bDeviceTemplate> pair in bDevice.AllTemplates)
            {
                if (pair.Value.NodeCount <= 0)
                    continue;

                bUserSettings userSettings = null;
                if (pair.Key is bDeviceType.VEST_FRONT or bDeviceType.VEST_BACK)
                {
                    userSettings = editorComp.AllUserSettings[bDevice.AllTemplates[bDeviceType.VEST]];
                }
                else
                {
                    userSettings = editorComp.AllUserSettings[pair.Value];
                }
                
                if (userSettings.CurrentPrefab == null)
                {
                    continue;
                }

                for (int node = 0; node < pair.Value.NodeCount; node++)
                {
                    string nodeName = $"{bHapticsOSCIntegration.SystemName}/{pair.Value.Name.Replace(" ", "")}/{node}";
                    CreateAnimatorLayerStates(node, nodeName, userSettings.TouchView_Default, userSettings.TouchView_Triggered, aac, editorComp, pair);
                }
            }
        }

        private static void CreateAnimatorLayerStates(int node, string nodeName, Color defaultCol, Color triggeredCol, AacFlBase aac, bHapticsOSCIntegration editorComp, KeyValuePair<bDeviceType, bDeviceTemplate> keyValuePair)
        {
            string layerName = $"{keyValuePair.Value.Name.Replace(" ", "/")}/{node}";

            try
            {
                aac.RemoveAllSupportingLayers(layerName);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
            
            AacFlLayer layer = aac.CreateSupportingFxLayer("ParameterCreation");
            
            string parameter = ConvertParameterAsBhaptics(nodeName);
            AacFlBoolParameterGroup boolParams = layer.BoolParameters($"{parameter}/self", $"{parameter}/others");

            AacFlState exitState = layer.NewState("dummy");
            exitState.Exits();
            layer.EntryTransitionsTo(exitState);
            
            try
            {
                aac.RemoveAllSupportingLayers("ParameterCreation");
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }

            float shaderDeviceIndex = bDevice.GetShaderIndex(keyValuePair.Key, node);
            Renderer[] renderers = bShader.FindRenderersFromIndex(shaderDeviceIndex, editorComp.avatar.gameObject);

            if (renderers.Length <= 0)
            {
                return;
            }

            layer = aac.CreateSupportingFxLayer(layerName);

            AacFlState falseState = layer.NewState("False").WithWriteDefaultsSetTo(true);
            falseState.TransitionsFromEntry().When(boolParams.AreFalse());
            falseState.Exits().AfterAnimationFinishes();

            AacFlState trueState = layer.NewState("True", 1, 0).WithWriteDefaultsSetTo(true);
            trueState.TransitionsFromEntry().When(boolParams.IsAnyTrue());
            trueState.Exits().AfterAnimationFinishes();

            int shaderNode = keyValuePair.Key == bDeviceType.VEST_BACK ? node / 4 * 8 - node + 4 : node + 1;

            foreach (Renderer renderer in renderers)
            {
                //bShader.SetTouchViewColors(renderer, defaultCol, triggeredCol);

                AacFlClip falseClip = aac.NewClip().Animating(clip => clip.Animates(renderer, $"material._Node{shaderNode}").WithOneFrame(0f));
                falseState = falseState.WithAnimation(falseClip);

                AacFlClip trueClip = aac.NewClip().Animating(clip => clip.Animates(renderer, $"material._Node{shaderNode}").WithOneFrame(1f));
                trueState = trueState.WithAnimation(trueClip);
            }
        }

        private static string ConvertParameterAsBhaptics(string parameter)
        {
            parameter = parameter.Replace(bHapticsOSCIntegration.SystemName, "bOSC/v2");
            if (parameter.Contains("ArmLeft"))
            {
                parameter = parameter.Replace("ArmLeft", "ForearmL");
            }
            else if (parameter.Contains("ArmRight"))
            {
                parameter = parameter.Replace("ArmRight", "ForearmR");
            }
            else if (parameter.Contains("FootLeft"))
            {
                parameter = parameter.Replace("FootLeft", "FootL");
            }
            else if (parameter.Contains("FootRight"))
            {
                parameter = parameter.Replace("FootRight", "FootR");
            }
            else if (parameter.Contains("HandLeft"))
            {
                parameter = parameter.Replace("HandLeft", "HandL");
            }
            else if (parameter.Contains("HandRight"))
            {
                parameter = parameter.Replace("HandRight", "HandR");
            }

            return parameter;
        }
    }
}
#endif