using System.Collections.Generic;
using System.IO;
using LastBullet.Combat;
using LastBullet.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 새로 가져온 캐릭터(JellyFishGirl, 에셋 Character Model)를 주인공 플레이어 모델로 쓴다 (2026-10-31 사용자 요청).
    //  1) 재질 8개를 URP Lit으로 바꾸고, 쓰는 그림(색·법선·가려짐·발광)만 이어 준다. 그림 크기는 1024로 줄여 가져온다.
    //  2) 동작 컨트롤러 Assets/Animations/PlayerCharacter.controller: 대기·걷기·달리기(Speed로 전환), 사격·피격(한 번 재생)
    //  3) 프리팹 Assets/Prefabs/PlayerCharacter.prefab: 원본에서 반사 프로브와 데모 스크립트(ChaScript)를 뺀 것
    //  4) 플레이어가 있는 모든 씬에서 Player의 캡슐(Body, Front, Gun)을 지우고 자식 Model로 캐릭터를 넣고 PlayerAnimator를 붙인다
    // 메뉴: LastBullet > Phase 28 주인공 캐릭터 적용
    public static class Phase28PlayerCharacterSetup
    {
        const string Root = "Assets/Models/Character/"; // 사용자가 가져온 에셋 폴더를 규칙 위치(Assets/Models)로 옮겼다
        const string ControllerPath = "Assets/Animations/PlayerCharacter.controller";
        const string PrefabPath = "Assets/Prefabs/PlayerCharacter.prefab";
        const float ModelScale = 1f;
        // 캐릭터는 총을 왼쪽 앞(몸 기준)에 들고 있어서, 총알이 나가는 자리(Muzzle)를 그 총 앞으로 옮긴다 (측정: 총 중심 약 (-0.29, 1.0, 0.5))
        static readonly Vector3 MuzzlePosition = new Vector3(-0.3f, 1.05f, 0.75f);

        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 28 주인공 캐릭터 적용")]
        static void Apply()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/JellyFishGirl.prefab") == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Assets/Models/Character(캐릭터 에셋)를 먼저 가져오세요.", "확인");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            ConvertMaterials();
            BuildController();
            BuildPrefab();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            foreach (var scenePath in Scenes)
                PutInScene(scenePath, prefab);
            AssetDatabase.SaveAssets();
        }

        // ───────────── 1. 재질, 그림 ─────────────

        static void ConvertMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var normalMaps = new HashSet<string>();
            var mats = new List<Material>();
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { Root + "Models/Materials" }))
                mats.Add(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid)));

            // 법선 그림은 먼저 "법선 맵"으로 가져오게 표시한다
            foreach (var m in mats)
            {
                if (m.shader == lit)
                    continue;
                var bump = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
                if (bump != null)
                    normalMaps.Add(AssetDatabase.GetAssetPath(bump));
            }
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "Textures" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;
                importer.maxTextureSize = 1024; // 4K 그림을 1024로 (화면의 작은 캐릭터라 충분하고 메모리를 아낀다)
                if (normalMaps.Contains(path))
                    importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }

            foreach (var m in mats)
            {
                if (m.shader == lit)
                    continue;
                var main = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                var color = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                var bump = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
                var occlusion = m.HasProperty("_OcclusionMap") ? m.GetTexture("_OcclusionMap") : null;
                var emission = m.HasProperty("_EmissionMap") ? m.GetTexture("_EmissionMap") : null;
                var emissionColor = m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor") : Color.black;
                float mode = m.HasProperty("_Mode") ? m.GetFloat("_Mode") : 0f;
                float cutoff = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.5f;
                if (mode >= 2f)
                    Debug.LogWarning($"[LastBullet] 캐릭터 재질 {m.name}: 반투명(Mode {mode})은 잘라내기로 바꿔요");

                m.shader = lit;
                m.SetTexture("_BaseMap", main);
                m.SetColor("_BaseColor", color);
                m.SetFloat("_Smoothness", 0.25f); // 로우폴리 느낌에 가까운 은은한 윤기
                m.SetFloat("_Metallic", 0f);
                if (bump != null)
                {
                    m.SetTexture("_BumpMap", bump);
                    m.EnableKeyword("_NORMALMAP");
                }
                if (occlusion != null)
                {
                    m.SetTexture("_OcclusionMap", occlusion);
                    m.EnableKeyword("_OCCLUSIONMAP");
                }
                if (emission != null && emissionColor.maxColorComponent > 0.01f)
                {
                    m.SetTexture("_EmissionMap", emission);
                    m.SetColor("_EmissionColor", emissionColor);
                    m.EnableKeyword("_EMISSION");
                    m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                }
                if (mode >= 1f)
                {
                    m.SetFloat("_AlphaClip", 1f);
                    m.SetFloat("_Cutoff", cutoff);
                    m.EnableKeyword("_ALPHATEST_ON");
                    m.renderQueue = 2450;
                }
                EditorUtility.SetDirty(m);
            }
            Debug.Log($"[LastBullet] 캐릭터 재질 {mats.Count}개를 URP Lit으로 바꿨어요.");
        }

        // ───────────── 2. 동작 컨트롤러 ─────────────

        static AnimationClip Clip(string fbxName)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath($"{Root}Animations/{fbxName}.fbx"))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }
            Debug.LogError($"[LastBullet] 동작을 못 찾음: {fbxName}");
            return null;
        }

        static void BuildController()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            var sm = controller.layers[0].stateMachine;

            var idle = sm.AddState("Idle");
            idle.motion = Clip("Mot_Wait01");
            var walk = sm.AddState("Walk");
            walk.motion = Clip("Mot_Walk01");
            var run = sm.AddState("Run");
            run.motion = Clip("Mot_Run01");
            var attack = sm.AddState("Shoot");
            attack.motion = Clip("Mot_Attack01");
            var damage = sm.AddState("Hit");
            damage.motion = Clip("Mot_Damage01");
            sm.defaultState = idle;

            // 걷기 5.05, 달리기 7.85 사이의 6.4를 경계로 한다
            Move(idle, walk, AnimatorConditionMode.Greater, 0.1f);
            Move(walk, idle, AnimatorConditionMode.Less, 0.1f);
            Move(walk, run, AnimatorConditionMode.Greater, 6.4f);
            Move(run, walk, AnimatorConditionMode.Less, 6.4f);

            // 사격·피격: 어느 동작에서든 끼어들어 한 번 재생하고 속도에 맞는 동작으로 돌아간다
            foreach (var (state, trigger) in new[] { (attack, "Shoot"), (damage, "Hit") })
            {
                var any = sm.AddAnyStateTransition(state);
                any.AddCondition(AnimatorConditionMode.If, 0f, trigger);
                any.hasExitTime = false;
                any.duration = 0.05f;
                any.canTransitionToSelf = false;
                Back(state, idle, walk, run);
            }
            EditorUtility.SetDirty(controller);
            Debug.Log("[LastBullet] 동작 컨트롤러 PlayerCharacter를 만들었어요.");
        }

        static void Move(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold)
        {
            var t = from.AddTransition(to);
            t.AddCondition(mode, threshold, "Speed");
            t.hasExitTime = false;
            t.duration = 0.12f;
        }

        // 끝나면 지금 속도에 맞는 동작으로 (걷기·달리기를 먼저 보고, 아니면 대기)
        static void Back(AnimatorState from, AnimatorState idle, AnimatorState walk, AnimatorState run)
        {
            var toRun = from.AddTransition(run);
            toRun.AddCondition(AnimatorConditionMode.Greater, 6.4f, "Speed");
            toRun.hasExitTime = true; toRun.exitTime = 0.9f; toRun.duration = 0.08f;
            var toWalk = from.AddTransition(walk);
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            toWalk.hasExitTime = true; toWalk.exitTime = 0.9f; toWalk.duration = 0.08f;
            var toIdle = from.AddTransition(idle);
            toIdle.hasExitTime = true; toIdle.exitTime = 0.9f; toIdle.duration = 0.08f;
        }

        // ───────────── 3. 프리팹 ─────────────

        static void BuildPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/JellyFishGirl.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstanceAndReturnNewOutermostRoots(instance, PrefabUnpackMode.Completely);
            instance.name = "PlayerCharacter";

            // 반사 프로브(데모용)와 데모 스크립트를 뺀다
            foreach (var probe in instance.GetComponentsInChildren<ReflectionProbe>(true))
                Object.DestroyImmediate(probe.gameObject);
            var demo = instance.GetComponent("ChaScript");
            if (demo != null)
                Object.DestroyImmediate(demo);

            var animator = instance.GetComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            animator.applyRootMotion = false; // 움직임은 PlayerMovement가 한다 (동작이 몸을 끌고 가지 않게)
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var r in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                r.updateWhenOffscreen = true; // 동작 중 몸이 화면 밖으로 잘리지 않게
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }

            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            Object.DestroyImmediate(instance);
            Debug.Log("[LastBullet] 프리팹 PlayerCharacter를 만들었어요.");
        }

        // ───────────── 4. 씬 ─────────────

        static void PutInScene(string scenePath, GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);
            var player = Object.FindFirstObjectByType<PlayerMovement>();
            if (player == null)
                return;
            if (player.transform.Find("Model") != null)
            {
                Debug.Log($"[LastBullet] {scene.name}: 이미 캐릭터가 있어요.");
                return;
            }

            // 캡슐 몸·앞 표시·총 모양은 지우고 캐릭터(자기 총을 들고 있다)로 바꾼다. 총구(Muzzle)와 카메라 대상은 그대로 둔다.
            foreach (var name in new[] { "Body", "Front", "Gun" })
            {
                var old = player.transform.Find(name);
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, player.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * ModelScale;

            var animator = player.gameObject.AddComponent<PlayerAnimator>();
            var so = new SerializedObject(animator);
            so.FindProperty("animator").objectReferenceValue = model.GetComponent<Animator>();
            so.FindProperty("movement").objectReferenceValue = player;
            so.FindProperty("controller").objectReferenceValue = player.GetComponent<CharacterController>();
            so.FindProperty("health").objectReferenceValue = player.GetComponent<PlayerHealth>();
            so.FindProperty("gun").objectReferenceValue = player.GetComponent<OneBulletGun>();
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LastBullet] {scene.name}: 주인공 캐릭터를 넣었어요.");
        }
    }
}
