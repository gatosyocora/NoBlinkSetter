using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

// ver 1.0
// © 2019-2-26 gatosyocora

namespace VRCDeveloperTool
{
    public static class NoBlinkKeyCopier
    {

        public static int OTHER = 0;
        public static int FIST = 1;

        private const string NO_BLINK_KEY_FILE_PATH = "Assets/NoBlinkSetter/OriginFiles/NoBlinkKey.anim";
        private const string NO_BLINK_KEY_FIST_FILE_PATH = "Assets/NoBlinkSetter/OriginFiles/NoBlinkKey_FIST.anim";

        /// <summary>
        /// animClipにまばたき防止用Animatorを操作するキーを追加する
        /// </summary>
        /// <param name="animClip"></param>
        /// <param name="animType"></param>
        public static void AddNoBlinkKey(AnimationClip animClip, int animType)
        {
            string path = (animType == OTHER) ? NO_BLINK_KEY_FILE_PATH : NO_BLINK_KEY_FIST_FILE_PATH;
            var noBlinkKeyAnimClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            CopyAnimationKeys(noBlinkKeyAnimClip, animClip);
        }

        // originClipに設定されたAnimationKeyをすべてtargetclipにコピーする
        private static void CopyAnimationKeys(AnimationClip originClip, AnimationClip targetClip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(originClip).ToArray())
            {
                // AnimationClipよりAnimationCurveを取得
                AnimationCurve curve = AnimationUtility.GetEditorCurve(originClip, binding);
                // AnimationClipにキーリダクションを行ったAnimationCurveを設定
                AnimationUtility.SetEditorCurve(targetClip, binding, curve);
            }
        }
    }
}

