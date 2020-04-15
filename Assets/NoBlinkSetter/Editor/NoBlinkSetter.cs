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
        private VRC_AvatarDescriptor m_avatar = null;
        private AnimatorOverrideController standingAnimController = null;
        private AnimatorOverrideController sittingAnimController = null;
        private SkinnedMeshRenderer m_face = null;
        private bool hasEyeTracking = false;

        private bool isSettingNoBlink = false;

        private string noBlinkSetterFolderPath;
        private string saveFolderPath;

        private const string NOBLINK_ANIMATOR_PATH = "/OriginFiles/blink reset.controller";
        private const string NOBLINK_ANIMATION_PATH = "/OriginFiles/blink reset.anim";
        private const string NOBLINK_PREFAB_FOR_EYETRACKING_PATH = "/OriginFiles/Body.prefab";

        private const string TARGET_STATE_NAME = "blink reset";
        private const string NO_BLINK_ANIMATOR_OBJ_NAME = "blink reset";

        private readonly string[] FACE_ANIM_NAMES = { "FIST", "FINGERPOINT", "HANDOPEN", "HANDGUN", "THUMBSUP", "VICTORY", "ROCKNROLL" };

        [MenuItem("VRCDeveloperTool/NoBlinkSetter")]
        private static void Create()
        {
            GetWindow<NoBlinkSetter>("NoBlinkSetter");
        }

        private void AfterGetWindow()
        {
            if (m_avatar != null)
                GetAvatarInfo(m_avatar);
        }

        private void OnEnable()
        {
            noBlinkSetterFolderPath = GetFolderPathFromName("NoBlinkSetter");
        }

        private void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            {
                m_avatar = EditorGUILayout.ObjectField(
                    "Avatar",
                    m_avatar,
                    typeof(VRC_AvatarDescriptor),
                    true
                ) as VRC_AvatarDescriptor;
            }
            if (EditorGUI.EndChangeCheck())
            {
                if (m_avatar != null)
                {
                    GetAvatarInfo(m_avatar);
                    isSettingNoBlink = CheckSettingNoBlink(m_avatar.gameObject);
                }
            }

            if (m_avatar != null)
            {
                // VRC_AvatarDescripterに設定してあるAnimator
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Custom Standing Anims", EditorStyles.boldLabel);
                {
                    EditorGUI.indentLevel++;
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
                        m_avatar.CustomStandingAnims = standingAnimController;
                    }
                    EditorGUI.indentLevel--;
                }
                
                if (m_avatar != null && !isSettingNoBlink)
                    EditorGUILayout.HelpBox("Avatarを複製して設定します", MessageType.Info);
                else if (m_avatar != null && isSettingNoBlink)
                    EditorGUILayout.HelpBox("AnimationClipsをNoBlinkに対応させます。", MessageType.Warning);

                // CustomStandingAnimsが設定されていない時の例外処理
                if (m_avatar != null && standingAnimController == null)
                {
                    EditorGUILayout.HelpBox("Set Custom Standing Anims in VRC_AvatarDescripter", MessageType.Warning);
                }


                // VRC_AvatarDescriptorに設定してあるFaceMesh
                EditorGUILayout.Space();
                EditorGUI.BeginChangeCheck();
                {
                    m_avatar.VisemeSkinnedMesh = EditorGUILayout.ObjectField(
                        "Face Mesh",
                        m_avatar.VisemeSkinnedMesh,
                        typeof(SkinnedMeshRenderer),
                        true
                    ) as SkinnedMeshRenderer;
                }
                if (EditorGUI.EndChangeCheck())
                {
                    m_face = m_avatar.VisemeSkinnedMesh;
                }

                // CustomStandingAnimsが設定されていない時の例外処理
                if (m_avatar != null && m_face == null)
                {
                    EditorGUILayout.HelpBox("Set Face Mesh in VRC_AvatarDescripter", MessageType.Warning);
                }

                // Standing AnimatorのEmoteに設定してあるAnimationファイル
                if (standingAnimController != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("AnimationClips(Standing Anims)", EditorStyles.boldLabel);
                    {

                        EditorGUI.indentLevel++;
                        for (int i = 0; i < FACE_ANIM_NAMES.Length; i++)
                        {
                            if (standingAnimController == null || m_avatar.CustomStandingAnims[FACE_ANIM_NAMES[i]].name == FACE_ANIM_NAMES[i])
                            {
                                m_avatar.CustomStandingAnims[FACE_ANIM_NAMES[i]] = EditorGUILayout.ObjectField(
                                    FACE_ANIM_NAMES[i],
                                    null,
                                    typeof(AnimationClip),
                                    true
                                ) as AnimationClip;
                            }
                            else
                            {
                                m_avatar.CustomStandingAnims[FACE_ANIM_NAMES[i]] = EditorGUILayout.ObjectField(
                                    FACE_ANIM_NAMES[i],
                                    m_avatar.CustomStandingAnims[FACE_ANIM_NAMES[i]],
                                    typeof(AnimationClip),
                                    true
                                ) as AnimationClip;
                            }
                        }
                        EditorGUI.indentLevel--;
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

                EditorGUILayout.Space();

                hasEyeTracking = EditorGUILayout.ToggleLeft("EyeTracking対応アバター", hasEyeTracking);

                EditorGUILayout.Space();

            }


            EditorGUI.BeginDisabledGroup(m_avatar == null || m_face == null);
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Set NoBlink"))
                    {
                        SetNoBlink(m_avatar.gameObject);
                    }
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        /// <summary>
        /// まばたき防止の設定をする
        /// </summary>
        /// <param name="obj"></param>
        private void SetNoBlink(GameObject obj)
        {
            GameObject objNoBlink;
            SkinnedMeshRenderer faceMesh;
            Animator blinkAnimator;
            // まばたき防止ギミックが設定されていなければ設定する
            GameObject noBlinkAnimatorObj = null;
            var noBlinkAnimatorObjTrans = (obj.transform).Find(NO_BLINK_ANIMATOR_OBJ_NAME);
            if (noBlinkAnimatorObjTrans == null)
            {
                objNoBlink = DuplicationAvatarGameObject(obj);
                objNoBlink.name = obj.name + "_blink reset";
                obj.SetActive(false);

                faceMesh = objNoBlink.GetComponent<VRC_AvatarDescriptor>().VisemeSkinnedMesh;

                blinkAnimator = GetBlinkAnimator(faceMesh.gameObject);

                if (blinkAnimator == null) return;

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
                while (currentTrans.parent != objNoBlink.transform)
                    currentTrans = currentTrans.parent;
                currentTrans.parent = noBlinkAnimatorObj.transform;
                noBlinkAnimatorObj.transform.parent = objNoBlink.transform;

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
                faceMesh = m_face;
                blinkAnimator = GetBlinkAnimator(faceMesh.gameObject);
            }

            // アニメーションオーバーライド用のAnimationClipを新しいパスに設定しなおす
            ChangeAndSetAnimationKeysPathForFaceAnimations(ref standingAnimController, faceMesh.gameObject, noBlinkAnimatorObj, blinkAnimator.gameObject);

            // アイトラするようにする
            if (hasEyeTracking)
            {
                var originalbBodyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(noBlinkSetterFolderPath + NOBLINK_PREFAB_FOR_EYETRACKING_PATH);
                var bodyPrefab = PrefabUtility.InstantiatePrefab(originalbBodyPrefab) as GameObject;
                bodyPrefab.transform.SetParent(objNoBlink.transform);
                bodyPrefab.transform.localPosition = Vector3.zero;
                bodyPrefab.transform.localRotation = Quaternion.identity;
            }

        }

        private GameObject DuplicationAvatarGameObject(GameObject origObj)
        {
            var newObj = Object.Instantiate<GameObject>(origObj);
            newObj.transform.position = newObj.transform.position;
            newObj.transform.rotation = newObj.transform.rotation;

            return newObj;
        }

        /// <summary>
        /// まばたき用のAnimatorを取得する
        /// </summary>
        /// <param name="faceObj"></param>
        /// <returns></returns>
        private Animator GetBlinkAnimator(GameObject faceObj)
        {
            var currentObj = faceObj;
            Animator blinkAnimator = null;

            while (currentObj.transform.parent != null)
            {
                blinkAnimator = currentObj.GetComponent<Animator>();

                // 見つかったら返す (whileの条件によってルートオブジェクトは検索しない)
                if (blinkAnimator != null)
                    return blinkAnimator;

                // 一つ上の親へ行く
                currentObj = currentObj.transform.parent.gameObject;
            }

            return null;
        }


        /// <summary>
        /// アバターの情報を取得する
        /// </summary>
        private void GetAvatarInfo(VRC_AvatarDescriptor avatar)
        {
            if (avatar == null) return;

            m_face = avatar.VisemeSkinnedMesh;

            standingAnimController = avatar.CustomStandingAnims;
            sittingAnimController = avatar.CustomSittingAnims;

            saveFolderPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(avatar.CustomStandingAnims));
            Debug.Log(saveFolderPath);

            if (string.IsNullOrEmpty(saveFolderPath))
            {
                string animationFolderPath = noBlinkSetterFolderPath + "/Animations";
                if (!Directory.Exists(animationFolderPath))
                {
                    AssetDatabase.CreateFolder(noBlinkSetterFolderPath, "Animations");
                }
                saveFolderPath = animationFolderPath;
            }
        }

        /// <summary>
        /// まばたき防止用のアニメ―タ―ファイルを作成する
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private AnimatorController CreateNoBlinkAnimatorController(string fileName, string folderPath)
        {
            var new_assetsPath = AssetDatabase.GenerateUniqueAssetPath(saveFolderPath +"/"+ fileName + ".controller");
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
            var new_assetsPath = AssetDatabase.GenerateUniqueAssetPath(saveFolderPath + "/" + fileName + ".anim");
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
        /// 設定されている表情用のAnimationClipのパスを変更し, まばたき防止用のパスを追加する
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="targetObj"></param>
        /// <param name="noBlinkAnimatorObj"></param>
        private void ChangeAndSetAnimationKeysPathForFaceAnimations(ref AnimatorOverrideController controller, GameObject targetObj, GameObject noBlinkAnimatorObj, GameObject blinkAnimatorObj)
        {
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

                fileName = animClip_origin.name + "_blink reset";
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
                    AssetDatabase.CreateAsset(animClip, AssetDatabase.GenerateUniqueAssetPath(saveFolderPath + fileName + ".anim"));
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
    }

}
