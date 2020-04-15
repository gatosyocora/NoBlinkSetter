using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

// ver 1.0
// © 2019-2-26 gatosyocora

namespace VRCDeveloperTool
{
    public class NoBlinkKeyAdder : Editor
    {

        private const string NO_BLINK_ANIMATOR_OBJ_NAME = "blink reset";

        // None
        [MenuItem("CONTEXT/Motion/Add NoBlink Key", priority = 30)]
        private static void AddNoBlinkKeyForFaceAnimations(MenuCommand command)
        {
            AddNoBlinkKey(command, NoBlinkKeyCopier.OTHER);
        }

        [MenuItem("CONTEXT/Motion/Add NoBlink Key for FIST", priority = 31)]
        private static void AddNoBlinkKeyForFISTFaceAnimations(MenuCommand command)
        {
            AddNoBlinkKey(command, NoBlinkKeyCopier.FIST);
        }

        [MenuItem("CONTEXT/Motion/Clear NoBlink Key", priority = 32)]
        private static void ClearNoBlinkKeyForFaceAnimations(MenuCommand command)
        {
            ClearNoBlinkKey(command.context as AnimationClip);
        }

        /// <summary>
        /// まばたき防止Animatorを操作するキーを追加する
        /// </summary>
        /// <param name="command"></param>
        /// <param name="value"></param>
        private static void AddNoBlinkKey(MenuCommand command, int type)
        {
            AnimationClip animClip = command.context as AnimationClip;

            // すでにまばたき防止Animator用のキーがあれば削除
            ClearNoBlinkKey(animClip);

            // まばたき防止Animator用のキーを追加
            NoBlinkKeyCopier.AddNoBlinkKey(animClip, type);
        }

        /// <summary>
        /// まばたき防止Animator用のキーがあれば削除
        /// </summary>
        /// <param name="command"></param>
        private static void ClearNoBlinkKey(AnimationClip animClip)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(animClip).ToArray())
            {
                var path_blocks = (binding.path).Split('/');
                if (path_blocks[path_blocks.Length - 1] == NO_BLINK_ANIMATOR_OBJ_NAME)
                {
                    AnimationUtility.SetEditorCurve(animClip, binding, null);
                    continue;
                }
            }
        }
    }


}
