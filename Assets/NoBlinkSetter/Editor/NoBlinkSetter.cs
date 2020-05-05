using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using VRCSDK2;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor.Animations;
using System.IO;
using UnityEngine.Animations;
using Gatosyocora;

// ver 1.1
// © 2019 gatosyocora

namespace VRCDeveloperTool
{

    public class NoBlinkSetter : EditorWindow
    {
        private VRC_AvatarDescriptor targetAvatar = null;
        private AnimatorOverrideController standingAnimController = null;
        private SkinnedMeshRenderer faceRenderer = null;
        private string[] blendShapeNames = null;
        private List<int> blinkBlendShapeIndices = null;
        private Animator blinkAnimator = null;
        private AnimatorController blinkController = null;
        private AnimationClip blinkAnimClip = null;
        private bool hasVRCEyeTracking = false;

        private bool useAfkSystem = false;

        private bool isSettingNoBlink = false;

        private bool duplicateAvatarAnimatorController = true;

        private string noBlinkSetterFolderPath;
        private string saveFolderPath;

        private float afkMinute = 3f;
        private Transform afkConstraintTarget;
        private bool isSettingAfkSystem = false;

        public enum AFK_EFFECT_TYPE {ZZZ, BUBBLE, CUSTOM};
        private AFK_EFFECT_TYPE afkEffectType = AFK_EFFECT_TYPE.ZZZ;
        private GameObject afkEffect = null;

        private const string NOBLINK_ANIMATOR_PATH = "/OriginFiles/blink reset.controller";
        private const string NOBLINK_ANIMATION_PATH = "/OriginFiles/blink reset.anim";
        private const string NOBLINK_PREFAB_FOR_EYETRACKING_PATH = "/OriginFiles/Body.prefab";
        private const string BLINK_CONTROLLER_PATH = "/OriginFiles/BlinkController.controller";
        private const string BLINK_ANIMATION_CLIP_PATH = "/OriginFiles/BlinkAnimation.anim";
        private const string AFK_EFFECT_ZZZ_PATH = "/AFK System/Prefab/Object2.prefab";
        private const string AFK_EFFECT_BUBBLE_PATH = "/AFK System/Prefab/Object1.prefab";

        private const string SAVE_FOLDER_NAME = "NoBlink";

        private const string TARGET_STATE_NAME = "blink reset";
        private const string NO_BLINK_ANIMATOR_OBJ_NAME = "blink reset";

        private const string NOBLINK_ASSET_NAME = "_blink reset";

        private const string AFK_ASSET_NAME = "_afk";

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
                isSettingAfkSystem = CheckSettingAfkSystem(targetAvatar.gameObject, blinkController);
                duplicateAvatarAnimatorController = CheckNeedToDuplicateController(targetAvatar);
            }
        }

        private void OnEnable()
        {
            noBlinkSetterFolderPath = GatoEditorUtility.GetFolderPathFromName("NoBlinkSetter");
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
                    afkEffectType = AFK_EFFECT_TYPE.ZZZ;
                    GetAvatarInfo(targetAvatar);
                    saveFolderPath = GetSaveFolderPath(standingAnimController);
                    isSettingNoBlink = CheckSettingNoBlink(targetAvatar.gameObject);
                    isSettingAfkSystem = CheckSettingAfkSystem(targetAvatar.gameObject, blinkController);
                    duplicateAvatarAnimatorController = CheckNeedToDuplicateController(targetAvatar);
                }
            }

            if (targetAvatar != null)
            {
                // Avatar Status
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField("アイトラッキング" + (hasVRCEyeTracking ? "対応アバター" : "非対応アバター"));
                    EditorGUILayout.LabelField("まばたき防止" + (isSettingNoBlink ? "設定済みアバター" : "未設定アバター"));
                    EditorGUILayout.LabelField("AFK機構" + (isSettingAfkSystem ? "設定済みアバター" : "未設定アバター"));
                }

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

                        if (faceRenderer != null)
                        {
                            blendShapeNames = GetBlendShapeNames(faceRenderer);
                            blinkAnimator = GetBlinkAnimator(faceRenderer.gameObject);
                        }
                        if (blinkAnimator != null)
                        {
                            blinkController = blinkAnimator.runtimeAnimatorController as AnimatorController;
                        }
                        if (blinkController != null)
                        {
                            blinkAnimClip = GetBlinkAnimationFromBlinkController(blinkController);
                        }
                        if (blinkAnimClip != null && faceRenderer != null)
                        {
                            blinkBlendShapeIndices = GetBlinkBlendShapeIndices(blinkAnimClip, faceRenderer);
                        }
                    }


                    // FaceMesh未設定時の警告表示
                    if (faceRenderer == null)
                    {
                        GatoEditorUtility.NonIndentHelpBox("アバターの表情のBlendShapeを持つSkinnedMeshRendererを設定してください", MessageType.Error);
                    }

                    if (faceRenderer != null)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.LabelField("BlendShape");

                            GUILayout.FlexibleSpace();
                            if (GUILayout.Button("+"))
                            {
                                blinkBlendShapeIndices.Add(-1);
                            }
                            if (GUILayout.Button("-"))
                            {
                                blinkBlendShapeIndices.RemoveAt(blinkBlendShapeIndices.Count-1);
                                if (blinkBlendShapeIndices.Count <= 0)
                                {
                                    blinkBlendShapeIndices.Add(-1);
                                }
                            }
                        }
                        using (new EditorGUI.IndentLevelScope())
                        {
                            if (blinkBlendShapeIndices != null)
                            {
                                for (int i = 0; i < blinkBlendShapeIndices.Count; i++)
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        blinkBlendShapeIndices[i] = EditorGUILayout.Popup(i + 1 + string.Empty, blinkBlendShapeIndices[i], blendShapeNames);

                                        if (GUILayout.Button("x", GUILayout.Width(30f)))
                                        {
                                            blinkBlendShapeIndices.RemoveAt(i);
                                            if (blinkBlendShapeIndices.Count <= 0)
                                            {
                                                blinkBlendShapeIndices.Add(-1);
                                            }
                                        }
                                    }
                                }

                                if (blinkBlendShapeIndices.Count() >= 2)
                                {
                                    GatoEditorUtility.NonIndentHelpBox(
                                        "まばたき用BlendShape以外が表示されている場合はxを押して削除してください", 
                                        MessageType.Warning);
                                }

                                if (blinkAnimClip != null && blinkBlendShapeIndices.All(x => x == -1))
                                {
                                    GatoEditorUtility.NonIndentHelpBox(
                                        "まばたき用BlendShapeが見つかりませんでした\nFaceMeshとBlinkAnimationを設定して自動取得してください", 
                                        MessageType.Error);

                                    using (new EditorGUI.DisabledGroupScope(
                                        faceRenderer == null ||
                                        blinkAnimClip == null
                                    ))
                                    {
                                        GatoEditorUtility.NonIndentButton(
                                            "BlinkAnimationからBlinkBlendShapeを自動取得",
                                            () => {
                                                blinkBlendShapeIndices = GetBlinkBlendShapeIndices(blinkAnimClip, faceRenderer);
                                            });
                                    }
                                }
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
                                blinkBlendShapeIndices = GetBlinkBlendShapeIndices(blinkAnimClip, faceRenderer);
                                if (blinkController != null)
                                {
                                    SetBlinkAnimationClipToBlinkController(blinkAnimClip, blinkController);
                                }
                            }
                        }
                    }
                }

                // まばたきアニメーション未設定時のエラー表示
                if (blinkController == null || blinkAnimClip == null)
                {
                    EditorGUILayout.HelpBox("まばたきアニメーションが設定されていません\n自動作成するにはBlinkのFaceMeshとBlendShapeを設定してください", MessageType.Error);

                    EditorGUI.BeginDisabledGroup(
                        faceRenderer == null ||
                        blinkBlendShapeIndices == null ||
                        blinkBlendShapeIndices.Where(x => x == -1).Any());
                    {
                        if (GUILayout.Button("まばたきアニメーションを自動作成する"))
                        {
                            SetBlinkAnimation(targetAvatar.name, faceRenderer, blinkBlendShapeIndices);
                        }
                    }
                    EditorGUI.EndDisabledGroup();
                }

                EditorGUILayout.Space();

                useAfkSystem = EditorGUILayout.ToggleLeft("AFK System", useAfkSystem, EditorStyles.boldLabel);

                if (useAfkSystem)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            afkMinute = EditorGUILayout.FloatField("AFKになるまでの時間(分)", afkMinute);

                            if (GUILayout.Button("3分"))
                            {
                                afkMinute = 3f;
                            }
                            if (GUILayout.Button("5分"))
                            {
                                afkMinute = 5f;
                            }
                        }

                        EditorGUILayout.Space();

                        afkEffectType = (AFK_EFFECT_TYPE)EditorGUILayout.EnumPopup("AFK中のエフェクト", afkEffectType);

                        if (afkEffectType == AFK_EFFECT_TYPE.CUSTOM)
                        {
                            using (var check = new EditorGUI.ChangeCheckScope())
                            {
                                afkEffect = EditorGUILayout.ObjectField(
                                                    "AFK中に表示するObject",
                                                    afkEffect,
                                                    typeof(GameObject),
                                                    true) as GameObject;

                                if (check.changed && afkEffect != null)
                                {
                                    afkConstraintTarget = afkEffect.transform.parent;
                                }
                            }
                        }

                        afkConstraintTarget = EditorGUILayout.ObjectField(
                                                "AFKエフェクトの接続先",
                                                afkConstraintTarget,
                                                typeof(Transform),
                                                true) as Transform;

                        if (afkEffectType == AFK_EFFECT_TYPE.CUSTOM)
                        {
                            GatoEditorUtility.NonIndentHelpBox(
                                "AFK用Object設定時にその親オブジェクトが接続先として設定されます", 
                                MessageType.Warning);
                        }
                    }
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
            }

            EditorGUILayout.Space();

            if (targetAvatar != null)
            {
                if (!isSettingNoBlink && useAfkSystem)
                    EditorGUILayout.HelpBox("Avatarを複製してまばたき防止機構とAFK機構を設定します", MessageType.Info);
                else if (!isSettingNoBlink)
                    EditorGUILayout.HelpBox("Avatarを複製してまばたき防止機構を設定します", MessageType.Info);
                else if (isSettingNoBlink && useAfkSystem && !isSettingAfkSystem)
                    EditorGUILayout.HelpBox("Avatarを複製してAFK機構を設定します", MessageType.Info);
                else if (useAfkSystem && isSettingAfkSystem)
                    EditorGUILayout.HelpBox("AFK機構は再設定できません。AFK機構を設定する前のアバターを選択してください", MessageType.Error);
                else
                    EditorGUILayout.HelpBox("AnimationClipsを編集してNoBlinkに対応させます", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(
                targetAvatar == null || 
                faceRenderer == null || 
                blinkController == null || 
                blinkAnimClip == null ||
                blinkBlendShapeIndices == null ||
                blinkBlendShapeIndices.All(x => x == -1) ||
                (useAfkSystem && isSettingAfkSystem));
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
                            isSettingNoBlink = CheckSettingNoBlink(targetAvatar.gameObject);
                            isSettingAfkSystem = CheckSettingAfkSystem(targetAvatar.gameObject, blinkController);
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
            if (!isSettingNoBlink)
            {
                objNoBlink = DuplicationAvatarGameObject(obj);
                objNoBlink.name = GatoEditorUtility.AddKeywordToEnd(obj.name, NOBLINK_ASSET_NAME);
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
                var noBlinkAnimatorController = CreateNoBlinkAnimatorController("AnimationStopController_"+obj.name, noBlinkSetterFolderPath);
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
                var noBlinkAnim = CreateNoBlinkAnimationClip("AnimationStopAnimation_" +obj.name, noBlinkSetterFolderPath);
                var path = GatoEditorUtility.GetHierarchyPathFromObj1ToObj2(noBlinkAnimatorObj, blinkAnimator.gameObject);
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
            else if (!isSettingAfkSystem && useAfkSystem)
            {
                objNoBlink = DuplicationAvatarGameObject(obj);
                objNoBlink.name = GatoEditorUtility.AddKeywordToEnd(obj.name, AFK_ASSET_NAME);
                obj.SetActive(false);

                noBlinkAvatar = objNoBlink.GetComponent<VRC_AvatarDescriptor>();
                faceMesh = noBlinkAvatar.VisemeSkinnedMesh;

                blinkAnimator = GetBlinkAnimator(faceMesh.gameObject);
                if (blinkAnimator == null) return null;

                var noBlinkAnimatorObjTrans = objNoBlink.transform.Find(NO_BLINK_ANIMATOR_OBJ_NAME);
                noBlinkAnimatorObj = noBlinkAnimatorObjTrans.gameObject;
            }
            else
            {
                objNoBlink = obj;
                noBlinkAvatar = objNoBlink.GetComponent<VRC_AvatarDescriptor>();
                var noBlinkAnimatorObjTrans = objNoBlink.transform.Find(NO_BLINK_ANIMATOR_OBJ_NAME);
                noBlinkAnimatorObj = noBlinkAnimatorObjTrans.gameObject;
                faceMesh = faceRenderer;
            }

            var blinkBlendShapeNames = BlendShapeIndicesToName(blinkBlendShapeIndices, faceMesh);

            // まばたきアニメーションを最適化する
            AnimationClip newBlinkAnimClip;
            var createdNewBlinkAnimation = CheckAndChangeBlinkAnimation(blinkAnimClip, blinkBlendShapeNames, out newBlinkAnimClip);

            if (createdNewBlinkAnimation)
            {
                blinkAnimClip = newBlinkAnimClip;
                SetBlinkAnimationClipToBlinkController(blinkAnimClip, blinkController);
            }

            // AFK Systemを設定する
            if (!isSettingAfkSystem && useAfkSystem)
            {
                var constraintObj = new GameObject("Constraint");
                var constraintTrans = constraintObj.transform;
                constraintTrans.SetParent(faceMesh.transform);
                constraintTrans.localPosition = Vector3.zero;
                constraintTrans.localRotation = Quaternion.identity;

                if (afkConstraintTarget != null)
                {
                    afkConstraintTarget = GatoEditorUtility.GetCorrespondTransformBetweenDuplicatedObjects(obj, objNoBlink, afkConstraintTarget);
                    constraintTrans.position = afkConstraintTarget.position;
                    constraintTrans.rotation = afkConstraintTarget.rotation;

                    var constraint = constraintObj.AddComponent<ParentConstraint>();
                    var source = new ConstraintSource();
                    source.sourceTransform = afkConstraintTarget;
                    source.weight = 1f;
                    constraint.AddSource(source);
                    constraint.constraintActive = true;
                }

                if (afkEffectType == AFK_EFFECT_TYPE.ZZZ)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath(noBlinkSetterFolderPath + AFK_EFFECT_ZZZ_PATH, typeof(GameObject)) as GameObject;
                    afkEffect = Instantiate(prefab) as GameObject;
                    afkEffect.name = "afk_zzz";
                }
                else if (afkEffectType == AFK_EFFECT_TYPE.BUBBLE)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath(noBlinkSetterFolderPath + AFK_EFFECT_BUBBLE_PATH, typeof(GameObject)) as GameObject;
                    afkEffect = Instantiate(prefab) as GameObject;
                    afkEffect.name = "afk_bubble";
                }
                else
                {
                    var duplicatedAfkEffect = GatoEditorUtility.GetCorrespondTransformBetweenDuplicatedObjects(obj, objNoBlink, afkEffect.transform);
                    afkEffect = duplicatedAfkEffect.gameObject;
                    afkEffect.name = "afk_" + afkEffect.name;
                }

                var afkEffectTrans = afkEffect.transform;
                afkEffectTrans.SetParent(constraintTrans);

                if (afkEffectType != AFK_EFFECT_TYPE.CUSTOM)
                {
                    afkEffectTrans.localPosition = new Vector3(0, 0.1f, 0.1f);
                    afkEffectTrans.localRotation = Quaternion.identity;
                }

                // afk用のまばたきアニメーションを作成
                var afkBlinkAnimClip = CreateAfkBlinkAnimation(blinkAnimClip, afkMinute * 60, blinkAnimator, afkEffect, blinkBlendShapeNames);

                if (afkBlinkAnimClip != null)
                {
                    var fileName = GatoEditorUtility.AddKeywordToEnd(blinkController.name, AFK_ASSET_NAME) + ".controller";
                    blinkController = GatoEditorUtility.DuplicateAsset<AnimatorController>(blinkController, saveFolderPath +"\\"+fileName);
                    blinkAnimator.runtimeAnimatorController = blinkController;
                    blinkAnimClip = afkBlinkAnimClip;
                    SetBlinkAnimationClipToBlinkController(blinkAnimClip, blinkController);
                }

                foreach (int blinkBlendShapeIndex in blinkBlendShapeIndices)
                {
                    faceMesh.SetBlendShapeWeight(blinkBlendShapeIndex, 100f);
                }

                blinkAnimator.enabled = false;

                objNoBlink.name = GatoEditorUtility.AddKeywordToEnd(objNoBlink.name, AFK_ASSET_NAME);
            }

            if (duplicateAvatarAnimatorController)
            {
                var fileName = GatoEditorUtility.AddKeywordToEnd(standingAnimController.name, NOBLINK_ASSET_NAME)+".overrideController";
                var animController = GatoEditorUtility.DuplicateAsset<AnimatorOverrideController>(standingAnimController, saveFolderPath + "\\" +fileName);

                if (animController != null)
                {
                    noBlinkAvatar.CustomStandingAnims = animController;
                    standingAnimController = animController;
                }
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

            blinkBlendShapeIndices = new List<int>
            {
                -1
            };

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
                    blinkAnimClip = GetBlinkAnimationFromBlinkController(blinkController);
                }

                // まばたきシェイプキーを取得
                blinkBlendShapeIndices = GetBlinkBlendShapeIndices(blinkAnimClip, faceRenderer);

                // BlendShapeの一覧を取得
                blendShapeNames = GetBlendShapeNames(faceRenderer);

                if (blinkAnimClip != null)
                {
                    blinkBlendShapeIndices = GetBlinkBlendShapeIndices(blinkAnimClip, faceRenderer);
                }
            }

            standingAnimController = avatar.CustomStandingAnims;

            hasVRCEyeTracking = IsVRCEyeTrackingAvatar(avatar);

            if (afkEffectType != AFK_EFFECT_TYPE.CUSTOM || afkConstraintTarget == null)
            {
                var avatarAnimator = avatar.gameObject.GetComponent<Animator>();
                afkConstraintTarget = avatarAnimator.GetBoneTransform(HumanBodyBones.Head);
            }
        }

        /// <summary>
        /// まばたき防止用のアニメ―タ―ファイルを作成する
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private AnimatorController CreateNoBlinkAnimatorController(string fileName, string folderPath)
        {
            GatoEditorUtility.CreateNoExistFolders(saveFolderPath);
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
            GatoEditorUtility.CreateNoExistFolders(saveFolderPath);
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
        /// 設定されている表情用のAnimationClipのパスを変更し, まばたき防止用のパスを追加する。まばたきシェイプキーを削除する
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="targetObj"></param>
        /// <param name="noBlinkAnimatorObj"></param>
        private void ChangeAndSetAnimationKeysPathForFaceAnimations(ref AnimatorOverrideController controller, GameObject targetObj, GameObject noBlinkAnimatorObj, GameObject blinkAnimatorObj)
        {
            if (controller == null) return;

            var new_path = GatoEditorUtility.GetHierarchyPath(targetObj);

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

                fileName = GatoEditorUtility.AddKeywordToEnd(animClip_origin.name, NOBLINK_ASSET_NAME) ;
                animClip = Object.Instantiate(animClip_origin);

                var blinkBlendShapeNames = BlendShapeIndicesToName(blinkBlendShapeIndices, faceRenderer);

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
                    GatoEditorUtility.CreateNoExistFolders(saveFolderPath);
                    AssetDatabase.CreateAsset(animClip, AssetDatabase.GenerateUniqueAssetPath(saveFolderPath + "/" +fileName + ".anim"));
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    controller[FACE_ANIM_NAMES[i]] = animClip;
                }
            }
        }

        /// <summary>
        /// NoBlinkが設定されているか調べる
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool CheckSettingNoBlink(GameObject obj)
        {
            if (obj == null) return false;

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

        /// <summary>
        /// AFK機構用のまばたきアニメーションを作成する
        /// </summary>
        /// <param name="defaultBlinkAnim"></param>
        /// <param name="afkTriggerTime"></param>
        /// <param name="blinkAnimator"></param>
        /// <param name="effectObj"></param>
        /// <returns></returns>
        private AnimationClip CreateAfkBlinkAnimation(AnimationClip defaultBlinkAnim, float afkTriggerTime, Animator blinkAnimator, GameObject effectObj, List<string> blinkBlendShapeNames)
        {
            if (defaultBlinkAnim == null) return null;

            AnimationClip afkAnim;
            string fileName = GatoEditorUtility.AddKeywordToEnd(defaultBlinkAnim.name, AFK_ASSET_NAME) + ".anim";
            afkAnim = defaultBlinkAnim;
            afkAnim.name += AFK_ASSET_NAME;
            var defaultPath = AssetDatabase.GetAssetPath(defaultBlinkAnim);
            // RenameAssetの際に存在するファイルだとRenameできないのでUniqueな名前を取得する
            var newAnimPath = AssetDatabase.GenerateUniqueAssetPath(Path.GetDirectoryName(defaultPath)+ "\\" + fileName);
            fileName = Path.GetFileNameWithoutExtension(newAnimPath);
            AssetDatabase.RenameAsset(defaultPath, fileName);
            afkAnim = AssetDatabase.LoadAssetAtPath<AnimationClip>(newAnimPath) as AnimationClip;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var bindings = AnimationUtility.GetCurveBindings(afkAnim);

            var timeOf1Set = defaultBlinkAnim.length;

            foreach (var binding in bindings)
            {
                // 表情アニメーションのbindingだけ処理する
                if (binding.type != typeof(SkinnedMeshRenderer)) continue;

                var blendShapeName = binding.propertyName.Replace("blendShape.", string.Empty);

                var curve = AnimationUtility.GetEditorCurve(afkAnim, binding);

                var keyCountOf1Set = curve.length;

                int loopCount = 1;
                bool isFinished = false;
                float time = 0f;
                while (time < 3600f)
                {
                    for (int keyIndex = 0; keyIndex < keyCountOf1Set; keyIndex++)
                    {
                        var key = curve.keys[keyIndex];
                        time = key.time + timeOf1Set * loopCount;

                        if (time >= afkTriggerTime)
                        {
                            isFinished = true;
                            break;
                        }

                        var newKey = new Keyframe
                        {
                            time = time,
                            value = key.value,
                            weightedMode = WeightedMode.Both
                        };

                        curve.AddKey(newKey);
                    }

                    if (isFinished) break;
                    loopCount++;
                }

                // まばたき用のシェイプキーであれば最後に目を閉じる
                if (blinkBlendShapeNames.Contains(blendShapeName))
                {
                    // AFKに移行する時間の1秒前に目をあけるキーを入れる
                    // 1秒前から徐々に目を閉じていくアニメーションになる
                    // 1秒以内に変化させるキーがあればこれは追加しない
                    if (curve.keys.Last().time < afkTriggerTime - 1)
                    {
                        var afkBeforeKey = new Keyframe
                        {
                            time = afkTriggerTime - 1f,
                            value = 0f,
                            weightedMode = WeightedMode.Both
                        };
                        curve.AddKey(afkBeforeKey);
                    }

                    // AFKに移行する時間に目を閉じるキーを入れる
                    var afkKey = new Keyframe
                    {
                        time = afkTriggerTime,
                        value = 100f,
                        weightedMode = WeightedMode.Both
                    };
                    curve.AddKey(afkKey);
                }

                AnimationUtility.SetEditorCurve(afkAnim, binding, curve);
            }

            // AFKEffectのキーを追加する
            // 特定のGameObjectを0フレーム目に非アクティブ, 最後のフレームでアクティブ
            var path = GatoEditorUtility.GetHierarchyPathFromObj1ToObj2(blinkAnimator.gameObject, effectObj);
            var effectBinding = new EditorCurveBinding
            {
                type = typeof(GameObject),
                path = path,
                propertyName = "m_IsActive"
            };
            var effectCurve = new AnimationCurve(
                                    new Keyframe(0f, 0f, 0f, 1f),
                                    new Keyframe(afkTriggerTime, 1f, float.PositiveInfinity, float.PositiveInfinity)
                              );
            AnimationUtility.SetEditorCurve(afkAnim, effectBinding, effectCurve);

            // LoopTimeをfalseにする
            var serialied = new SerializedObject(afkAnim);
            var property = serialied.FindProperty("m_AnimationClipSettings.m_LoopTime");
            property.boolValue = false;
            serialied.ApplyModifiedProperties();
            serialied.Dispose();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

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

            var headPath = GatoEditorUtility.GetHierarchyPath(headTrans.gameObject);
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
        private List<string> GetBlinkBlendShapeNames(AnimationClip blinkAnimClip)
        {
            if (blinkAnimClip == null) return null;

            var bindings = AnimationUtility.GetCurveBindings(blinkAnimClip);

            var blinkBlendShapeNames = bindings
                                        .Where(x => x.type == typeof(SkinnedMeshRenderer))
                                        .Select(x => x.propertyName.Replace("blendShape.", string.Empty))
                                        .ToList();

            return blinkBlendShapeNames;
        }

        /// <summary>
        /// まばたきアニメーションが最適か調べて必要であれば設定する
        /// </summary>
        private bool CheckAndChangeBlinkAnimation(AnimationClip blinkAnimClip, List<string> blinkBlendShapeNames, out AnimationClip newBlinkAnimClip)
        {
            var blinkBindings = AnimationUtility.GetCurveBindings(blinkAnimClip)
                                    .Where(x => x.type == typeof(SkinnedMeshRenderer));

            // 3秒以内にまばたき用のアニメーションキーがあるか調べる
            // timeが0ではなく、valueが0より大きい最初のキーが3秒ずらす最初のキーとなる
            bool needShiftAnimationKeys = false;
            float shiftStartTime = 0f;
            foreach (var binding in blinkBindings)
            {
                var blendShapeName = binding.propertyName.Replace("blendShape.", string.Empty);

                if (!blinkBlendShapeNames.Contains(blendShapeName)) continue;

                var curve = AnimationUtility.GetEditorCurve(blinkAnimClip, binding);
                var keys = curve.keys;

                for (int i = 0; i < keys.Length; i++)
                {
                    if (keys[i].time < 3f && keys[i].time != 0f && keys[i].value > 0f)
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
                var fileName = GatoEditorUtility.AddKeywordToEnd(blinkAnimClip.name, NOBLINK_ASSET_NAME) + ".anim";
                newBlinkAnimClip = GatoEditorUtility.DuplicateAsset<AnimationClip>(blinkAnimClip, saveFolderPath+"\\"+fileName);

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

                return true;
            }
            else
            {
                newBlinkAnimClip = null;
                return false;
            }
        }

        /// <summary>
        /// まばたきアニメーションを作成して、設定する
        /// </summary>
        /// <param name="faceRenderer"></param>
        /// <param name="blinkBlendShapeIndexList"></param>
        /// <param name="blendShapeNames"></param>
        private void SetBlinkAnimation(string avatarName, SkinnedMeshRenderer faceRenderer, List<int> blinkBlendShapeIndexList)
        {
            var blinkBlendShapeNames = BlendShapeIndicesToName(blinkBlendShapeIndexList, faceRenderer);

            if (!string.IsNullOrEmpty(avatarName))
            {
                avatarName = "_" + avatarName;
            }

            if (blinkAnimator == null)
            {
                blinkAnimator = faceRenderer.gameObject.AddComponent<Animator>();
            }

            if (blinkController == null)
            {
                var originBlinkControllerPath = noBlinkSetterFolderPath + BLINK_CONTROLLER_PATH;
                var newBlinkControllerPath = saveFolderPath + "\\" + "BlinkController" + avatarName + ".controller";
                blinkController = GatoEditorUtility.DuplicateAsset<AnimatorController>(originBlinkControllerPath, newBlinkControllerPath);
                blinkAnimator.runtimeAnimatorController = blinkController as RuntimeAnimatorController;
            }

            if (blinkAnimClip == null)
            {
                var originBlinkAnimClipPath = noBlinkSetterFolderPath + BLINK_ANIMATION_CLIP_PATH;
                var newBlinkAnimClipPath = saveFolderPath + "\\" + "BlinkAnimation" + avatarName + ".anim";
                blinkAnimClip = GatoEditorUtility.DuplicateAsset<AnimationClip>(originBlinkAnimClipPath, newBlinkAnimClipPath);
                SetBlinkAnimationClipToBlinkController(blinkAnimClip, blinkController);

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
            if (controller != null)
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
                }
            }

            if (string.IsNullOrEmpty(saveFolderPath))
            {
                var avatarName = targetAvatar.name.Replace(NOBLINK_ASSET_NAME, string.Empty);
                saveFolderPath = "Assets\\NoBlinkAnimations\\" + avatarName;
            }

            return saveFolderPath;
        }

        /// <summary>
        /// BlendShapeの名前をIndexに変換する
        /// </summary>
        /// <param name="blendShapeNames"></param>
        /// <param name="renderer"></param>
        /// <returns></returns>
        private int[] BlendShapeNameToIndex(List<string> blendShapeNames, SkinnedMeshRenderer renderer)
        {
            var blendShapeIndexList = new List<int>();

            var mesh = renderer.sharedMesh;
            var blendShapeCount = mesh.blendShapeCount;
            for (int i = 0; i < blendShapeCount; i++)
            {
                var blendShapeName = mesh.GetBlendShapeName(i);
                if (blendShapeNames.Contains(blendShapeName))
                {
                    blendShapeIndexList.Add(i);

                    if (blendShapeIndexList.Count() >= blendShapeNames.Count()) break;
                }
            }

            return blendShapeIndexList.ToArray();
        }

        /// <summary>
        /// BlendShapeのIndexを名前に変換する
        /// </summary>
        /// <param name="blendShapeIndices"></param>
        /// <param name="renderer"></param>
        /// <returns></returns>
        private List<string> BlendShapeIndicesToName(List<int> blendShapeIndices, SkinnedMeshRenderer renderer)
        {
            var blendShapeNameList = new List<string>();

            var mesh = renderer.sharedMesh;

            foreach(var blendShapeIndex in blendShapeIndices)
            {
                blendShapeNameList.Add(mesh.GetBlendShapeName(blendShapeIndex));
            }

            return blendShapeNameList;
        }

        /// <summary>
        /// アニメーションファイルからBlendShapeのIndexを取得する
        /// </summary>
        /// <param name="blinkAnimClip"></param>
        /// <param name="faceRenderer"></param>
        /// <returns></returns>
        private List<int> GetBlinkBlendShapeIndices(AnimationClip blinkAnimClip, SkinnedMeshRenderer faceRenderer)
        {
            if (blinkAnimClip == null || faceRenderer == null) return new List<int> { -1 };

            var blinkBlendShapeNames = GetBlinkBlendShapeNames(blinkAnimClip);
            var blinkBlendShapeIndices = BlendShapeNameToIndex(blinkBlendShapeNames, faceRenderer).ToList();
            return blinkBlendShapeIndices;
        }

        /// <summary>
        /// AFK機構が設定済みか調べる
        /// </summary>
        /// <param name="rootObject"></param>
        /// <param name="blinkController"></param>
        /// <returns></returns>
        private bool CheckSettingAfkSystem(GameObject rootObject, AnimatorController blinkController)
        {
            var afkAssetPattern = ".*" + AFK_ASSET_NAME + ".*";

            if (rootObject == null) return false;

            if (Regex.IsMatch(rootObject.name, afkAssetPattern))
                return true;

            if (blinkController == null) return false;

            if (Regex.IsMatch(blinkController.name, afkAssetPattern))
                return true;

            return false;
        }

        private bool SetBlinkAnimationClipToBlinkController(AnimationClip animClip, AnimatorController controller)
        {
            if (animClip == null || controller == null) return false;

            controller.layers[0].stateMachine.states[0].state.motion = animClip;
            return true;
        }

        private AnimationClip GetBlinkAnimationFromBlinkController(AnimatorController controller)
        {
            if (controller == null) return null;
            return controller.layers[0].stateMachine.states[0].state.motion as AnimationClip;
        }
    }
}
