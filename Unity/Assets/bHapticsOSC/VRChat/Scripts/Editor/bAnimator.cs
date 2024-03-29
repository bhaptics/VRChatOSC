﻿#if UNITY_EDITOR && VRC_SDK_VRCSDK3 && bHapticsOSC_HasAac
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

            foreach (KeyValuePair<bDeviceType, bDeviceTemplate> keyValuePair in bDevice.AllTemplates)
            {
                if (keyValuePair.Value.NodeCount <= 0)
                    continue;

                bUserSettings userSettings = null;
                if ((keyValuePair.Key == bDeviceType.VEST_FRONT) || (keyValuePair.Key == bDeviceType.VEST_BACK))
                    userSettings = editorComp.AllUserSettings[bDevice.AllTemplates[bDeviceType.VEST]];
                else
                    userSettings = editorComp.AllUserSettings[keyValuePair.Value];
                if (userSettings.CurrentPrefab == null)
                    continue;

                for (int node = 1; node < keyValuePair.Value.NodeCount + 1; node++)
                {
                    string nodeName = $"{bHapticsOSCIntegration.SystemName}_{keyValuePair.Value.Name.Replace(' ', '_')}_{node}";
                    CreateAnimatorLayerStates(node, nodeName, userSettings.TouchView_Default, userSettings.TouchView_Triggered, aac, editorComp, keyValuePair);
                }
            }
        }

        private static void CreateAnimatorLayerStates(int node, string nodeName, Color defaultCol, Color triggeredCol, AacFlBase aac, bHapticsOSCIntegration editorComp, KeyValuePair<bDeviceType, bDeviceTemplate> keyValuePair)
        {
            string layer_name = $"{keyValuePair.Value.Name.Replace(' ', '_')}_{node}";

            try { aac.RemoveAllSupportingLayers(layer_name); } catch { }
            AacFlLayer layer = aac.CreateSupportingFxLayer("ParameterCreation");
            AacFlBoolParameter boolParam = layer.BoolParameter(ConvertParameterAsBhaptics(nodeName));
            AacFlState exitState = layer.NewState("dummy");
            exitState.Exits();
            layer.EntryTransitionsTo(exitState);
            try { aac.RemoveAllSupportingLayers("ParameterCreation"); } catch { }

            float shaderDeviceIndex = bDevice.GetShaderIndex(keyValuePair.Key, node);
            Renderer[] renderers = bShader.FindRenderersFromIndex(shaderDeviceIndex, editorComp.avatar.gameObject);
            if (renderers.Length <= 0)
                return;

            layer = aac.CreateSupportingFxLayer(layer_name);

            AacFlState falseState = layer.NewState("False").WithWriteDefaultsSetTo(true);
            falseState.TransitionsFromEntry().When(boolParam.IsFalse());
            falseState.Exits().AfterAnimationFinishes();

            AacFlState trueState = layer.NewState("True", 1, 0).WithWriteDefaultsSetTo(true);
            trueState.TransitionsFromEntry().When(boolParam.IsTrue());
            trueState.Exits().AfterAnimationFinishes();

            foreach (Renderer renderer in renderers)
            {
                //bShader.SetTouchViewColors(renderer, defaultCol, triggeredCol);

                AacFlClip falseClip = aac.NewClip().Animating(clip => clip.Animates(renderer, $"material._Node{node}").WithOneFrame(0f));
                falseState = falseState.WithAnimation(falseClip);

                AacFlClip trueClip = aac.NewClip().Animating(clip => clip.Animates(renderer, $"material._Node{node}").WithOneFrame(1f));
                trueState = trueState.WithAnimation(trueClip);
            }
        }

        private static string ConvertParameterAsBhaptics(string parameter)
        {
            parameter = parameter.Replace(bHapticsOSCIntegration.SystemName, "bOSC_v1");
            if (parameter.Contains("Vest_Front"))
            {
                parameter = parameter.Replace("Vest_Front", "VestFront");
            }
            else if (parameter.Contains("Vest_Back"))
            {
                parameter = parameter.Replace("Vest_Back", "VestBack");
            }
            else if (parameter.Contains("Arm_Left"))
            {
                parameter = parameter.Replace("Arm_Left", "ForearmL");
            }
            else if (parameter.Contains("Arm_Right"))
            {
                parameter = parameter.Replace("Arm_Right", "ForearmR");
            }
            else if (parameter.Contains("Foot_Left"))
            {
                parameter = parameter.Replace("Foot_Left", "FootL");
            }
            else if (parameter.Contains("Foot_Right"))
            {
                parameter = parameter.Replace("Foot_Right", "FootR");
            }
            else if (parameter.Contains("Hand_Left"))
            {
                parameter = parameter.Replace("Hand_Left", "HandL");
            }
            else if (parameter.Contains("Hand_Right"))
            {
                parameter = parameter.Replace("Hand_Right", "HandR");
            }

            var tempCharArr = parameter.Split('_');
            if (int.TryParse(tempCharArr[tempCharArr.Length - 1], out int res))
            {
                parameter = parameter.Replace("_" + res, "_" + (res - 1));
            }

            return parameter;
        }
    }
}
#endif