using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRCSDK2;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Animations;
using System.IO;

// ver 1.1
// © 2019 gatosyocora

namespace VRCDeveloperTool
{

    public class NoBlinkSetter : EditorWindow
    {
        private VRC_AvatarDescriptor targetAvatar = null;
        private AnimatorOverrideController standingAnimController = null;
        private AnimatorOverrideController sittingAnimController = null;
        private SkinnedMeshRenderer faceRenderer = null;
        private string[] blendShapeNames = null;
        private List<int> blinkBlendShapeIndices = null;
        private string[] blinkBlendShapeNames = null;
        private Animator blinkAnimator = null;
        private AnimatorController blinkController = null;
        private AnimationClip blinkAnimClip = null;
        private bool hasVRCEyeTracking = false;

        private bool useAfkSystem = false;

        private bool isSettingNoBlink = false;

        private bool duplicateAvatarAnimatorController = true;

        private string noBlinkSetterFolderPath;
        private string saveFolderPath;

        private int afkMinute = 3;

        private const string NOBLINK_ANIMATOR_PATH = "/OriginFiles/blink reset.controller";
        private const string NOBLINK_ANIMATION_PATH = "/OriginFiles/blink reset.anim";
        private const string NOBLINK_PREFAB_FOR_EYETRACKING_PATH = "/OriginFiles/Body.prefab";
        private const string BLINK_CONTROLLER_PATH = "/OriginFiles/BlinkController.controller";
        private const string BLINK_ANIMATION_CLIP_PATH = "/OriginFiles/BlinkAnimation.anim";

        private const string SAVE_FOLDER_NAME = "NoBlink";

        private const string TARGET_STATE_NAME = "blink reset";
        private const string NO_BLINK_ANIMATOR_OBJ_NAME = "blink reset";

        private const string NOBLINK_ASSET_NAME = "_blink reset";

        private readonly string[] FACE_ANIM_NAMES = { "FIST", "FINGERPOINT", "HANDOPEN", "HANDGUN", "THUMBSUP", "VICTORY", "ROCKNROLL" };

        [MenuItem("VRCDeveloperTool/NoBlinkSetter")]
        private static void Create()
        {
            GetWindow<NoBlinkSetter>("NoBlinkSetter");
        }

        private void AfterGetWindow()
        {
            if (targetAvatar != null)
            {
                GetAvatarInfo(targetAvatar);
                saveFolderPath = GetSaveFolderPath(standingAnimController);
                isSettingNoBlink = CheckSettingNoBlink(targetAvatar.gameObject);
                duplicateAvatarAnimatorController = CheckNeedToDuplicateController(targetAvatar);
            }
        }

        private void OnEnable()
        {
            noBlinkSetterFolderPath = GetFolderPathFromName("NoBlinkSetter");
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            {
                targetAvatar = EditorGUILayout.ObjectField(
                    "Avatar",
                    targetAvatar,
                    typeof(VRC_AvatarDescriptor),
                    true
                ) as VRC_AvatarDescriptor;
            }
            if (EditorGUI.EndChangeCheck())
            {
                if (targetAvatar != null)
                {
                    GetAvatarInfo(targetAvatar);
                    saveFolderPath = GetSaveFolderPath(standingAnimController);
                    isSettingNoBlink = CheckSettingNoBlink(targetAvatar.gameObject);
                    duplicateAvatarAnimatorController = CheckNeedToDuplicateController(targetAvatar);
                }
            }

            if (targetAvatar != null)
            {
                // EyeTracking
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField("EyeTracking" + (hasVRCEyeTracking ? "対応アバター" : "非対応アバター"));
                    EditorGUILayout.LabelField("NoBlinkSetter" + (isSettingNoBlink ? "設定済みアバター" : "未設定アバター"));
                }

                if (targetAvatar != null && !isSettingNoBlink)
                    EditorGUILayout.HelpBox("Avatarを複製してNoBlinkを設定します", MessageType.Info);
                else if (targetAvatar != null && isSettingNoBlink)
                    EditorGUILayout.HelpBox("AnimationClipsを編集してNoBlinkに対応させます", MessageType.Warning);

                // VRC_AvatarDescripterに設定してあるAnimator
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Custom Standing Anims", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUI.BeginChangeCheck();
                    {
                        standingAnimController = EditorGUILayout.ObjectField(
                            "Standing Anims",
                            standingAnimController,
                            typeof(AnimatorOverrideController),
                            true
                        ) as AnimatorOverrideController;
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        targetAvatar.CustomStandingAnims = standingAnimController;
                    }

                    duplicateAvatarAnimatorController = EditorGUILayout.ToggleLeft("Standing Animsに設定されたAnimatorOverrideControllerを複製する", duplicateAvatarAnimatorController);
                }

                // CustomStandingAnims未設定時の警告表示
                if (targetAvatar != null && standingAnimController == null)
                {
                    EditorGUILayout.HelpBox("表情アニメーションをまばたき防止に対応させるためにはCustom Standing Animsの設定が必要です", MessageType.Warning);
                }

                EditorGUILayout.Space();

                // Standing AnimatorのEmoteに設定してあるAnimationファイル
                if (standingAnimController != null)
                {
                    EditorGUILayout.LabelField("AnimationClips(Standing Anims)", EditorStyles.boldLabel);
                    using (new EditorGUI.IndentLevelScope())
                    {
                        for (int i = 0; i < FACE_ANIM_NAMES.Length; i++)
                        {
                            if (standingAnimController == null || targetAvatar.CustomStandingAnims[FACE_ANIM_NAMES[i]].name == FACE_ANIM_NAMES[i])
                            {
                                targetAvatar.CustomStandingAnims[FACE_ANIM_NAMES[i]] = EditorGUILayout.ObjectField(
                                    FACE_ANIM_NAMES[i],
                                    null,
                                    typeof(AnimationClip),
                                    true
                                ) as AnimationClip;
                            }
                            else
                            {
                                targetAvatar.CustomStandingAnims[FACE_ANIM_NAMES[i]] = EditorGUILayout.ObjectField(
                                    FACE_ANIM_NAMES[i],
                                    targetAvatar.CustomStandingAnims[FACE_ANIM_NAMES[i]],
                                    typeof(AnimationClip),
                                    true
                                ) as AnimationClip;
                            }
                        }
                    }
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Blink", EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                {
                    // VRC_AvatarDescriptorに設定してあるFaceMesh
                    EditorGUI.BeginChangeCheck();
                    {
                        targetAvatar.VisemeSkinnedMesh = EditorGUILayout.ObjectField(
                            "Face Mesh",
                            targetAvatar.VisemeSkinnedMesh,
                            typeof(SkinnedMeshRenderer),
                            true
                        ) as SkinnedMeshRenderer;
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        faceRenderer = targetAvatar.VisemeSkinnedMesh;
                        blendShapeNames = GetBlendShapeNames(faceRenderer);
                    }


                    // FaceMesh未設定時の警告表示
                    if (faceRenderer == null)
                    {
                        var currentIndentLevel = EditorGUI.indentLevel;
                        EditorGUI.indentLevel = 0;
                        EditorGUILayout.HelpBox("アバターの表情のBlendShapeを持つSkinnedMeshRendererを設定してください", MessageType.Error);
                        EditorGUI.indentLevel = currentIndentLevel;
                    }

                    if (faceRenderer != null)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("BlendShape");

                            if (blinkBlendShapeNames == null)
                            {
                                GUILayout.FlexibleSpace();
                                if (GUILayout.Button("+"))
                                {
                                    blinkBlendShapeIndices.Add(-1);
                                }
                                if (GUILayout.Button("-"))
                                {
                                    if (blinkBlendShapeIndices.Count > 1)
                                    {
                                        blinkBlendShapeIndices.RemoveAt(blinkBlendShapeIndices.Count - 1);
                                    }
                                }
                            }
                        }
                        using (new EditorGUI.IndentLevelScope())
                        {
                            if (blinkBlendShapeNames != null)
                            {
                                foreach (var blinkBlendShapeName in blinkBlendShapeNames)
                                {
                                    EditorGUILayout.LabelField(blinkBlendShapeName);
                                }
                            }
                            else if (blinkBlendShapeIndices != null)
                            {
                                for (int i = 0; i < blinkBlendShapeIndices.Count; i++)
                                {
                                    blinkBlendShapeIndices[i] = EditorGUILayout.Popup(i + 1 + string.Empty, blinkBlendShapeIndices[i], blendShapeNames);
                                }
                                var currentIndentLevel = EditorGUI.indentLevel;
                                EditorGUI.indentLevel = 0;

                                if (blinkController == null || blinkAnimClip == null)
                                {
                                    EditorGUILayout.HelpBox("まばたきアニメーションを自動作成するためには\nまばたき用のBlendShapeを選択してください", MessageType.Error);
                                }
                                else
                                {
                                    EditorGUILayout.HelpBox("まばたき用BlendShapeが見つかりませんでした\nFaceMesh, BlinkController, BlinkAnimationが正しく設定されていることを確認してください", MessageType.Error);
                                }

                                EditorGUI.indentLevel = currentIndentLevel;
                            }
                        }
                    }

                    // まばたき用AnimatorController
                    EditorGUILayout.Space();

                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        blinkController = EditorGUILayout.ObjectField(
                            "Blink Controller",
                            blinkController,
                            typeof(AnimatorController),
                            true
                        ) as AnimatorController;

                        blinkAnimClip = EditorGUILayout.ObjectField(
                            "Blink Animation",
                            blinkAnimClip,
                            typeof(AnimationClip),
                            true
                        ) as AnimationClip;

                        if (check.changed)
                        {
                            if (blinkAnimClip != null)
                            {
                                blinkBlendShapeNames = GetBlinkBlendShapeNames(blinkAnimClip);
                            }
                        }
                    }
                }

                // まばたきアニメーション未設定時のエラー表示
                if (blinkController == null || blinkAnimClip == null)
                {
                    EditorGUILayout.HelpBox("まばたきアニメーションが設定されていません\n自動作成するにはFaceMeshとBlendShapeを設定してください", MessageType.Error);
                }

                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    saveFolderPath = EditorGUILayout.TextField("SaveFolder", saveFolderPath);

                    if (GUILayout.Button("Select", GUILayout.Width(100f)))
                    {
                        saveFolderPath = OpenFolderSelector("Select save folder", saveFolderPath);
                    }
                }

                EditorGUILayout.Space();

                useAfkSystem = EditorGUILayout.ToggleLeft("AFK機構を使う", useAfkSystem);

                EditorGUILayout.Space();

                if (GUILayout.Button("Create Afk System Animation"))
                {
                    var afkAnim = CreateAfkBlinkAnimation(blinkAnimClip, afkMinute * 60);

                    if (afkAnim != null)
                    {
                        AssetDatabase.CreateAsset(afkAnim, "Assets/"+afkAnim.name + ".anim");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }

            }

            if (targetAvatar != null && (blinkController == null || blinkAnimClip == null))
            {
                EditorGUI.BeginDisabledGroup(
                faceRenderer == null ||
                blinkBlendShapeIndices == null ||
                blinkBlendShapeIndices.Where(x => x == -1).Any());
                {
                    if (GUILayout.Button("まばたきアニメーションを自動作成する"))
                    {
                        SetBlinkAnimation(targetAvatar.name, faceRenderer, blinkBlendShapeIndices, blendShapeNames);
                    }
                }
                EditorGUI.EndDisabledGroup();
            }


            EditorGUI.BeginDisabledGroup(
                targetAvatar == null || 
                faceRenderer == null || 
                blinkController == null || 
                blinkAnimClip == null);
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Set NoBlink"))
                    {
                        var noBlinkAvatar = SetNoBlink(targetAvatar.gameObject, blinkAnimator);

                        if (noBlinkAvatar != null) {
                            targetAvatar = noBlinkAvatar;
                            GetAvatarInfo(targetAvatar);
                            saveFolderPath = GetSaveFolderPath(standingAnimController);
                            isSettingNoBlink = true;
                            duplicateAvatarAnimatorController = false;
                        }

                    }
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// まばたき防止の設定をする
        /// </summary>
        /// <param name="obj"></param>
        private VRC_AvatarDescriptor SetNoBlink(GameObject obj, Animator blinkAnimator)
        {
            GameObject objNoBlink;
            VRC_AvatarDescriptor noBlinkAvatar = null;
            SkinnedMeshRenderer faceMesh;
            // まばたき防止ギミックが設定されていなければ設定する
            GameObject noBlinkAnimatorObj = null;
            var noBlinkAnimatorObjTrans = (obj.transform).Find(NO_BLINK_ANIMATOR_OBJ_NAME);
            if (noBlinkAnimatorObjTrans == null)
            {
                objNoBlink = DuplicationAvatarGameObject(obj);
                objNoBlink.name = obj.name + NOBLINK_ASSET_NAME;
                obj.SetActive(false);

                noBlinkAvatar = objNoBlink.GetComponent<VRC_AvatarDescriptor>();
                faceMesh = noBlinkAvatar.VisemeSkinnedMesh;

                blinkAnimator = GetBlinkAnimator(faceMesh.gameObject);
                if (blinkAnimator == null) return null;

                // まばたき防止Animatorを設定する空オブジェクトを生成
                noBlinkAnimatorObj = new GameObject(NO_BLINK_ANIMATOR_OBJ_NAME);
                noBlinkAnimatorObj.transform.localPosition = Vector3.zero;
                noBlinkAnimatorObj.transform.localRotation = Quaternion.identity;
                noBlinkAnimatorObj.transform.localScale = Vector3.one;

                // まばたき防止Animatorを設定
                var noBlinkAnimatorController = CreateNoBlinkAnimatorController(objNoBlink.name, noBlinkSetterFolderPath);
                var noBlinkAnimator = noBlinkAnimatorObj.AddComponent<Animator>();
                noBlinkAnimator.runtimeAnimatorController = noBlinkAnimatorController;
                noBlinkAnimator.enabled = false;

                // blinkAnimatorから遡ってAvatarの子にまばたき防止Animator付きオブジェクトを設定
                Transform currentTrans = blinkAnimator.gameObject.transform;

                while (currentTrans.parent != null && currentTrans.parent != objNoBlink.transform)
                {
                    currentTrans = currentTrans.parent;
                }
                noBlinkAnimatorObj.transform.SetParent(objNoBlink.transform);
                currentTrans.SetParent(noBlinkAnimatorObj.transform);

                // まばたき防止Animationを設定
                var noBlinkAnim = CreateNoBlinkAnimationClip(objNoBlink.name, noBlinkSetterFolderPath);
                var path = GetHierarchyPathFromObj1ToObj2(noBlinkAnimatorObj, blinkAnimator.gameObject);
                ChangeAnimationKeysPath(ref noBlinkAnim, path);
                var states = noBlinkAnimatorController.layers[0].stateMachine.states.ToList();
                foreach (var s in states)
                {
                    if (s.state.name == TARGET_STATE_NAME)
                    {
                        s.state.motion = noBlinkAnim;
                        break;
                    }
                }
                
            }
            else
            {
                objNoBlink = obj;
                noBlinkAnimatorObj = noBlinkAnimatorObjTrans.gameObject;
                faceMesh = faceRenderer;
            }

            // まばたきアニメーションを最適化する
            var newBlinkAnimClip = CheckAndChangeBlinkAnimation(blinkAnimClip);

            if (newBlinkAnimClip != null)
            {
                blinkAnimClip = newBlinkAnimClip;
                blinkController.layers[0].stateMachine.states[0].state.motion = blinkAnimClip;
            }

            if (duplicateAvatarAnimatorController)
            {
                var fileName = standingAnimController.name + NOBLINK_ASSET_NAME;
                var animController = DuplicateAnimatorOverrideController(standingAnimController, fileName, saveFolderPath);
                noBlinkAvatar.CustomStandingAnims = animController;
                standingAnimController = animController;
            }

            // アニメーションオーバーライド用のAnimationClipを新しいパスに設定しなおす
            // まばたきシェイプキーを使用しているキーを削除する
            ChangeAndSetAnimationKeysPathForFaceAnimations(ref standingAnimController, faceMesh.gameObject, noBlinkAnimatorObj, blinkAnimator.gameObject);

            // アイトラするようにする
            if (hasVRCEyeTracking)
            {
                var originalbBodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(noBlinkSetterFolderPath + NOBLINK_PREFAB_FOR_EYETRACKING_PATH);
                var bodyPrefab = PrefabUtility.InstantiatePrefab(originalbBodyPrefab) as GameObject;
                bodyPrefab.transform.SetParent(objNoBlink.transform);
                bodyPrefab.transform.localPosition = Vector3.zero;
                bodyPrefab.transform.localRotation = Quaternion.identity;
            }

            return noBlinkAvatar;

        }

        private GameObject DuplicationAvatarGameObject(GameObject origObj)
        {
            var newObj = Object.Instantiate<GameObject>(origObj);
            newObj.transform.position = origObj.transform.position;
            newObj.transform.rotation = origObj.transform.rotation;

            return newObj;
        }

        /// <summary>
        /// まばたき用のAnimatorを取得する
        /// </summary>
        /// <param name="faceObj"></param>
        /// <returns></returns>
        private Animator GetBlinkAnimator(GameObject faceObj)
        {
            var currentTrans = faceObj.transform;
            Animator blinkAnimator = null;

            while (currentTrans.parent != null)
            {
                blinkAnimator = currentTrans.gameObject.GetComponent<Animator>();

                // 見つかったら返す (whileの条件によってルートオブジェクトは検索しない)
                if (blinkAnimator != null) break;

                // 一つ上の親へ行く
                currentTrans = currentTrans.parent;
            }

            return blinkAnimator;
        }


        /// <summary>
        /// アバターの情報を取得する
        /// </summary>
        private void GetAvatarInfo(VRC_AvatarDescriptor avatar)
        {
            if (avatar == null) return;

            faceRenderer = avatar.VisemeSkinnedMesh;

            if (faceRenderer != null)
            {
                // まばたきアニメーションを取得
                blinkAnimator = GetBlinkAnimator(faceRenderer.gameObject);
                if (blinkAnimator != null)
                {
                    blinkController = blinkAnimator.runtimeAnimatorController as AnimatorController;
                }
                else
                {
                    blinkController = null;
                    blinkAnimClip = null;
                }

                if (blinkController != null)
                {
                    blinkAnimClip = blinkController.layers[0].stateMachine.states[0].state.motion as AnimationClip;
                }

                // まばたきシェイプキーを取得
                blinkBlendShapeNames = GetBlinkBlendShapeNames(blinkAnimClip);

                // BlendShapeの一覧を取得
                blendShapeNames = GetBlendShapeNames(faceRenderer);
            }

            blinkBlendShapeIndices = new List<int>();
            blinkBlendShapeIndices.Add(-1);

            standingAnimController = avatar.CustomStandingAnims;
            sittingAnimController = avatar.CustomSittingAnims;

            hasVRCEyeTracking = IsVRCEyeTrackingAvatar(avatar);
        }

        /// <summary>
        /// まばたき防止用のアニメ―タ―ファイルを作成する
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private AnimatorController CreateNoBlinkAnimatorController(string fileName, string folderPath)
        {
            var new_assetsPath = AssetDatabase.GenerateUniqueAssetPath(saveFolderPath +"\\"+ fileName + ".controller");
            AssetDatabase.CopyAsset(folderPath + NOBLINK_ANIMATOR_PATH, new_assetsPath);
            var noBlinkAnimatorController_new = AssetDatabase.LoadAssetAtPath<AnimatorController>(new_assetsPath);

            return noBlinkAnimatorController_new;
        }

        /// <summary>
        /// まばたき防止用のAnimationClipを作成する
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private AnimationClip CreateNoBlinkAnimationClip(string fileName, string folderPath)
        {
            var new_assetsPath = AssetDatabase.GenerateUniqueAssetPath(saveFolderPath + "\\" + fileName + ".anim");
            AssetDatabase.CopyAsset(folderPath + NOBLINK_ANIMATION_PATH, new_assetsPath);

            var noBlinkAnim_new = AssetDatabase.LoadAssetAtPath<AnimationClip>(new_assetsPath);

            return noBlinkAnim_new;
        }

        /// <summary>
        /// AnimationKeyのパスを変更する
        /// </summary>
        /// <param name="animClip"></param>
        /// <param name="path"></param>
        private void ChangeAnimationKeysPath(ref AnimationClip animClip, string path)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(animClip).ToArray())
            {
                var m_binding = binding;
                m_binding.path = path;
                // AnimationClipよりAnimationCurveを取得
                var curve = AnimationUtility.GetEditorCurve(animClip, binding);
                AnimationUtility.SetEditorCurve(animClip, binding, null);
                // AnimationClipにキーリダクションを行ったAnimationCurveを設定
                AnimationUtility.SetEditorCurve(animClip, m_binding, curve);
            }
        }

        /// <summary>
        /// 特定のオブジェクトから特定のオブジェクトまでのパスを取得する
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private string GetHierarchyPathFromObj1ToObj2(GameObject obj1, GameObject obj2)
        {
            string path = obj2.name;
            var parent = obj2.transform.parent;
            while (parent != null)
            {
                if (parent.gameObject.name == obj1.name) return path;

                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        /// <summary>
        /// 設定されている表情用のAnimationClipのパスを変更し, まばたき防止用のパスを追加する。まばたきシェイプキーを削除する
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="targetObj"></param>
        /// <param name="noBlinkAnimatorObj"></param>
        private void ChangeAndSetAnimationKeysPathForFaceAnimations(ref AnimatorOverrideController controller, GameObject targetObj, GameObject noBlinkAnimatorObj, GameObject blinkAnimatorObj)
        {
            if (controller == null) return;

            var new_path = GetHierarchyPath(targetObj);

            AnimationClip animClip_origin = null, animClip = null;
            string fileName = "";
            bool containForBlendShape; // 表情変更用キーを含んでいるか

            // 表情用AnimatioClipすべてに対して
            for (int i = 0; i < FACE_ANIM_NAMES.Length; i++)
            {
                animClip_origin = controller[FACE_ANIM_NAMES[i]];

                // 何も設定されていなければ次のAnimationClipへ
                if (animClip_origin.name == FACE_ANIM_NAMES[i])
                {
                    controller[FACE_ANIM_NAMES[i]] = null;
                    continue;
                }

                // すでに設定済みのAnimationClipなら次へ
                var splitedFileName = (animClip_origin.name).Split('_');
                if (Regex.IsMatch(splitedFileName[splitedFileName.Length - 1], @"blink reset ?[0-9]*"))
                    continue;

                containForBlendShape = false;

                fileName = animClip_origin.name + NOBLINK_ASSET_NAME;
                animClip = Object.Instantiate(animClip_origin);

                // AnimationClipのBindingすべてに対して
                foreach (var binding in AnimationUtility.GetCurveBindings(animClip).ToArray())
                {
                    var m_binding = binding;

                    var path_blocks = (m_binding.path).Split('/');
                    var animTargetObjName = path_blocks[path_blocks.Length - 1];
                    // blinkAnimatorのEnableを操作するようなキーがあれば削除
                    if (animTargetObjName.Equals(blinkAnimatorObj.name)
                            && binding.type == typeof(Behaviour)
                            && binding.propertyName == "m_Enabled")
                    {
                        AnimationUtility.SetEditorCurve(animClip, binding, null);
                        containForBlendShape = true;
                        continue;
                    }
                    // まばたきシェイプキーを操作するものであれば削除
                    else if (binding.type == typeof(SkinnedMeshRenderer) &&
                            blinkBlendShapeNames.Contains(binding.propertyName.Replace("blendShape.", string.Empty)))
                    {
                        AnimationUtility.SetEditorCurve(animClip, binding, null);
                        continue;
                    }
                    // targetObjを操作するBindingであればパスを変更
                    else if (animTargetObjName.Equals(targetObj.name))
                    {
                        m_binding.path = new_path;
                        containForBlendShape = true;
                    }
                    // すでにまばたき防止Animator用のキーがあれば削除
                    else if (animTargetObjName.Equals(NO_BLINK_ANIMATOR_OBJ_NAME))
                    {
                        AnimationUtility.SetEditorCurve(animClip, binding, null);
                        continue;
                    }

                    // AnimationClipよりAnimationCurveを取得
                    var curve = AnimationUtility.GetEditorCurve(animClip, binding);
                    AnimationUtility.SetEditorCurve(animClip, binding, null);
                    // AnimationClipにキーリダクションを行ったAnimationCurveを設定
                    AnimationUtility.SetEditorCurve(animClip, m_binding, curve);
                }

                // まばたき防止Animator用のキーを追加
                int type = (FACE_ANIM_NAMES[i] != "FIST") ? NoBlinkKeyCopier.OTHER : NoBlinkKeyCopier.FIST;
                NoBlinkKeyCopier.AddNoBlinkKey(animClip, type);

                // 変更したBindingがあればファイルを複製してAnimatorControllerに設定する
                if (containForBlendShape)
                {
                    // ファイルを複製
                    AssetDatabase.CreateAsset(animClip, AssetDatabase.GenerateUniqueAssetPath(saveFolderPath + "/" +fileName + ".anim"));
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    controller[FACE_ANIM_NAMES[i]] = animClip;
                }
            }
        }

        /// <summary>
        /// 特定のオブジェクトまでのパスを取得する
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public string GetHierarchyPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                if (parent.parent == null) return path;

                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        /// <summary>
        /// NoBlinkが設定されているか調べる
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool CheckSettingNoBlink(GameObject obj)
        {
            return ((obj.transform).Find(NO_BLINK_ANIMATOR_OBJ_NAME) != null);
        }

        /// <summary>
        /// CustomStandingAnimsに設定されたAnimatorOverrideControllerの複製が必要か調べる
        /// </summary>
        /// <param name="avatar"></param>
        /// <returns></returns>
        private bool CheckNeedToDuplicateController(VRC_AvatarDescriptor avatar)
        {
            var controller = avatar.CustomStandingAnims;
            // 設定されていないので複製不可
            if (controller == null) return false;
            return !controller.name.EndsWith(NOBLINK_ASSET_NAME);
        }

        /// <summary>
        /// フォルダ名からフォルダパスを取得する
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetFolderPathFromName(string folderName)
        {
            var guid = AssetDatabase.FindAssets(folderName + " t:Folder").FirstOrDefault();
            return AssetDatabase.GUIDToAssetPath(guid);
        }

        /// <summary>
        /// フォルダを選択するダイアログを表示
        /// </summary>
        /// <param name="selectorText"></param>
        /// <param name="startFolder"></param>
        /// <returns></returns>
        private string OpenFolderSelector(string selectorText, string startFolder = null)
        {
            if (string.IsNullOrEmpty(startFolder))
            {
                startFolder = "Assets";
            }

            var selectedFolderPath = EditorUtility.OpenFolderPanel(selectorText, startFolder, string.Empty);
            selectedFolderPath = FileUtil.GetProjectRelativePath(selectedFolderPath);

            if (string.IsNullOrEmpty(selectedFolderPath))
            {
                selectedFolderPath = "Assets";
            }
            return selectedFolderPath;
        }

        private AnimationClip CreateAfkBlinkAnimation(AnimationClip defaultBlinkAnim, float afkTriggerTime)
        {
            if (defaultBlinkAnim == null) return null;

            var afkAnim = Instantiate(defaultBlinkAnim) as AnimationClip;

            var frameRate = afkAnim.frameRate;

            var bindings = AnimationUtility.GetCurveBindings(afkAnim);

            foreach (var binding in bindings)
            {
                // もし表情アニメーションのbindingじゃなかったら
                if (binding.type != typeof(SkinnedMeshRenderer)) continue;

                var curve = AnimationUtility.GetEditorCurve(afkAnim, binding);

                var timeOf1Set = curve.keys[curve.length - 1].time;
                var keyCountOf1Set = curve.length;

                int loopCount = 1;
                bool isFinished = false;
                while (loopCount < 10)
                {
                    for (int keyIndex = 0; keyIndex < keyCountOf1Set; keyIndex++)
                    {
                        var key = curve.keys[keyIndex];
                        var time = key.time + timeOf1Set * loopCount;

                        if (time >= afkTriggerTime)
                        {
                            isFinished = true;
                            break;
                        }

                        curve.AddKey(time, key.value);
                    }

                    if (isFinished) break;

                    loopCount++;
                }

                // 最後のフレームの1f後に目を閉じるキーを入れる
                var lastKeyTime = curve.keys[curve.length - 1].time;
                curve.AddKey(lastKeyTime + 1/frameRate, 100f);

                AnimationUtility.SetEditorCurve(afkAnim, binding, curve);
            }

            // LoopTimeをfalseにする
            var serializedObj = new SerializedObject(afkAnim);
            serializedObj.FindProperty("m_AnimationClipSetting.m_LoopTime").boolValue = false;
            serializedObj.ApplyModifiedProperties();

            return afkAnim;
        }

        /// <summary>
        /// VRChatのアイトラッキングに対応したアバターか判断する
        /// </summary>
        /// <param name="avatar"></param>
        /// <returns></returns>
        private bool IsVRCEyeTrackingAvatar(VRC_AvatarDescriptor avatar)
        {
            // アイトラッキングの条件
            // 参考:https://jellyfish-qrage.hatenablog.com/entry/2018/07/25/034610
            // - ボーンが「Armature/Hips/Spine/Chest/Neck/Head」という階層である
            // - Headボーンの下に「LeftEye, RightEye」というオブジェクト（ボーン）がある
            // - アバター直下にBodyというオブジェクトがある
            // * Bodyの上から4つのシェイプキーがまばたきに使われる

            var avatarAnimator = avatar.gameObject.GetComponent<Animator>();
            if (avatarAnimator == null) return false;
            var headTrans = avatarAnimator.GetBoneTransform(HumanBodyBones.Head);
            if (headTrans == null) return false;

            var headPath = GetHierarchyPath(headTrans.gameObject);
            if (!headPath.EndsWith("Armature/Hips/Spine/Chest/Neck/Head")) return false;

            if (headTrans.Find("LeftEye") == null || headTrans.Find("RightEye") == null) return false;

            var avatarTrans = avatar.gameObject.transform;
            if (avatarTrans.Find("Body") == null) return false;

            return true;
        }

        /// <summary>
        /// まばたき用のシェイプキー名を取得する
        /// </summary>
        /// <param name="blinkClip"></param>
        /// <returns></returns>
        private string[] GetBlinkBlendShapeNames(AnimationClip blinkAnimClip)
        {
            if (blinkAnimClip == null) return null;

            var bindings = AnimationUtility.GetCurveBindings(blinkAnimClip);

            var blinkBlendShapeNames = bindings
                                        .Where(x => x.type == typeof(SkinnedMeshRenderer))
                                        .Select(x => x.propertyName.Replace("blendShape.", string.Empty))
                                        .ToArray();

            return blinkBlendShapeNames;
        }

        /// <summary>
        /// AnimatorOverrideControllerを複製する
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        private AnimatorOverrideController DuplicateAnimatorOverrideController(AnimatorOverrideController controller, string fileName, string saveFolderPath)
        {
            var originalPath = AssetDatabase.GetAssetPath(controller);
            var ext = Path.GetExtension(originalPath);
            var newPath = AssetDatabase.GenerateUniqueAssetPath(saveFolderPath + "\\" + fileName + ext);
            AssetDatabase.CopyAsset(originalPath, newPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var newController = AssetDatabase.LoadAssetAtPath(newPath, typeof(AnimatorOverrideController)) as AnimatorOverrideController;

            return newController;
        }

        /// <summary>
        /// AnimationClipを複製する
        /// </summary>
        /// <param name="animClip"></param>
        /// <returns></returns>
        private AnimationClip DuplicateAnimationClip(AnimationClip animClip, string fileName, string saveFolderPath)
        {
            var originPath = AssetDatabase.GetAssetPath(animClip);
            var ext = Path.GetExtension(originPath);
            var newPath = AssetDatabase.GenerateUniqueAssetPath(saveFolderPath + "\\" + fileName + ext);
            AssetDatabase.CopyAsset(originPath, newPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var newAnimClip = AssetDatabase.LoadAssetAtPath(newPath, typeof(AnimationClip)) as AnimationClip;
            return newAnimClip;
        }

        /// <summary>
        /// まばたきアニメーションが最適か調べて必要であれば設定する
        /// </summary>
        private AnimationClip CheckAndChangeBlinkAnimation(AnimationClip blinkAnimClip)
        {
            var blinkBindings = AnimationUtility.GetCurveBindings(blinkAnimClip)
                                    .Where(x => x.type == typeof(SkinnedMeshRenderer));

            // 3秒以内にまばたき用のアニメーションキーがあるか調べる
            bool needShiftAnimationKeys = false;
            float shiftStartTime = 0f;
            foreach (var binding in blinkBindings)
            {
                var curve = AnimationUtility.GetEditorCurve(blinkAnimClip, binding);
                var keys = curve.keys;

                for (int i = 0; i < keys.Length; i++)
                {
                    if (keys[i].time < 3f && keys[i].time != 0f)
                    {
                        needShiftAnimationKeys = true;
                        shiftStartTime = keys[i].time;
                        break;
                    }

                    if (keys[i].time >= 3f) break;
                }

                if (needShiftAnimationKeys) break;
            }

            if (needShiftAnimationKeys)
            {
                var fileName = blinkAnimClip.name + NOBLINK_ASSET_NAME;
                var newBlinkAnimClip = DuplicateAnimationClip(blinkAnimClip, fileName, saveFolderPath);

                blinkBindings = AnimationUtility.GetCurveBindings(newBlinkAnimClip)
                                    .Where(x => x.type == typeof(SkinnedMeshRenderer));

                foreach (var binding in blinkBindings)
                {
                    var curve = AnimationUtility.GetEditorCurve(newBlinkAnimClip, binding);
                    var newKeys = curve.keys;

                    for (int i = 0; i < newKeys.Length; i++)
                    {
                        // 他のBlendShapeを弄るキーも同じようにずらすためにずらすキーの開始反映にindexではなくtimeを使う
                        if (newKeys[i].time < shiftStartTime) continue;

                        newKeys[i].time += (3f - shiftStartTime);
                    }
                    curve.keys = newKeys;

                    AnimationUtility.SetEditorCurve(newBlinkAnimClip, binding, curve);
                }

                return newBlinkAnimClip;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// まばたきアニメーションを作成して、設定する
        /// </summary>
        /// <param name="faceRenderer"></param>
        /// <param name="blinkBlendShapeIndexList"></param>
        /// <param name="blendShapeNames"></param>
        private void SetBlinkAnimation(string avatarName, SkinnedMeshRenderer faceRenderer, List<int> blinkBlendShapeIndexList, string[] blendShapeNames)
        {
            blinkBlendShapeNames = new string[blinkBlendShapeIndexList.Count()];

            if (!string.IsNullOrEmpty(avatarName))
            {
                avatarName = "_" + avatarName;
            }

            for (int i = 0; i < blinkBlendShapeNames.Length; i++)
            {
                blinkBlendShapeNames[i] = blendShapeNames[blinkBlendShapeIndexList[i]];
            }

            if (blinkAnimator == null)
            {
                blinkAnimator = faceRenderer.gameObject.AddComponent<Animator>();
            }

            if (blinkController == null)
            {
                var originBlinkControllerPath = noBlinkSetterFolderPath + BLINK_CONTROLLER_PATH;
                var newBlinkControllerPath = AssetDatabase.GenerateUniqueAssetPath(saveFolderPath + "\\" + "BlinkController" + avatarName + ".controller");
                AssetDatabase.CopyAsset(originBlinkControllerPath, newBlinkControllerPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                blinkController = AssetDatabase.LoadAssetAtPath(newBlinkControllerPath, typeof(AnimatorController)) as AnimatorController;
                blinkAnimator.runtimeAnimatorController = blinkController as RuntimeAnimatorController;
            }

            if (blinkAnimClip == null)
            {
                var originBlinkAnimClipPath = noBlinkSetterFolderPath + BLINK_ANIMATION_CLIP_PATH;
                var newBlinkAnimClipPath = AssetDatabase.GenerateUniqueAssetPath(saveFolderPath + "\\" + "BlinkAnimation" + avatarName + ".anim");
                AssetDatabase.CopyAsset(originBlinkAnimClipPath, newBlinkAnimClipPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                blinkAnimClip = AssetDatabase.LoadAssetAtPath(newBlinkAnimClipPath, typeof(AnimationClip)) as AnimationClip;
                blinkController.layers[0].stateMachine.states[0].state.motion = blinkAnimClip;

                var originalBlinkBinding = AnimationUtility.GetCurveBindings(blinkAnimClip).First();
                var blinkCurve = AnimationUtility.GetEditorCurve(blinkAnimClip, originalBlinkBinding);

                AnimationUtility.SetEditorCurve(blinkAnimClip, originalBlinkBinding, null);

                foreach (var blinkBlendShapeName in blinkBlendShapeNames)
                {
                    var blinkBinding = new EditorCurveBinding();
                    blinkBinding.propertyName = "blendShape." + blinkBlendShapeName;
                    blinkBinding.path = string.Empty;
                    blinkBinding.type = typeof(SkinnedMeshRenderer);
                    AnimationUtility.SetEditorCurve(blinkAnimClip, blinkBinding, blinkCurve);
                }
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// BlendShapeの一覧を取得する
        /// </summary>
        /// <param name="renderer"></param>
        /// <returns></returns>
        private string[] GetBlendShapeNames(SkinnedMeshRenderer renderer)
        {
            var faceMesh = renderer.sharedMesh;
            var blendShapeNameList = new List<string>();
            for (int blendShapeIndex = 0; blendShapeIndex < faceMesh.blendShapeCount; blendShapeIndex++)
            {
                var blendShapeName = faceMesh.GetBlendShapeName(blendShapeIndex);
                blendShapeNameList.Add(blendShapeName);
            }
            return blendShapeNameList.ToArray();
        }

        /// <summary>
        /// 保存先フォルダパスを取得する
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        private string GetSaveFolderPath(AnimatorOverrideController controller)
        {
            var saveFolderPath = string.Empty;
            if (standingAnimController != null)
            {
                var customStandingAnimsPath = AssetDatabase.GetAssetPath(controller);
                var controllerFolderPath = Path.GetDirectoryName(customStandingAnimsPath);

                if (controllerFolderPath.EndsWith(SAVE_FOLDER_NAME))
                {
                    saveFolderPath = controllerFolderPath;
                }
                else
                {
                    saveFolderPath = controllerFolderPath + "\\" + SAVE_FOLDER_NAME;
                    if (!Directory.Exists(saveFolderPath))
                    {
                        AssetDatabase.CreateFolder(controllerFolderPath, SAVE_FOLDER_NAME);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }
            }

            if (string.IsNullOrEmpty(saveFolderPath))
            {
                var avatarName = targetAvatar.name.Replace(NOBLINK_ASSET_NAME, string.Empty);
                var animationFolderPath = "Assets/NoBlinkAnimations";
                if (!Directory.Exists(animationFolderPath))
                {
                    AssetDatabase.CreateFolder("Assets", "NoBlinkAnimations");
                }
                var avatarFolderPath = animationFolderPath + "/" + avatarName;
                if (!Directory.Exists(avatarFolderPath))
                {
                    AssetDatabase.CreateFolder(animationFolderPath, avatarName);
                }
                saveFolderPath = avatarFolderPath;
            }

            return saveFolderPath;
        }
    }

}
