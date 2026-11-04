using LastBullet.Combat;
using LastBullet.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 총 모양을 AK47로 바꾸고 재장전 모션을 넣는다 (2026-11-04 사용자 요청, 에셋 Free Pack - Gun / PolyOne Studio).
    //   모델: Assets/PolyOne/Free Gun/Prefabs/SM_Ak47.prefab (몸통 AK47_Body, 탄창 Ak47_Magazine, 총알 AK47_Bullet). 총신이 -X쪽, 위가 +Y다.
    //   3인칭: R.Hand 아래 ThirdPersonGun(ThirdPersonGunPose)에 AK47을 넣는다. 캐릭터의 원래 총(modelGun)은 다시 끈다. 총은 가슴 앞에 두 손으로 든다(양팔 IK).
    //          AK47을 Y로 90도 돌려서 총구가 앞(+Z)을 보게 하고, 손잡이(모델 안 (0.157, -0.04, 0))를 주먹 가운데에 쥐게 한다.
    //   1인칭: Main Camera/FirstPersonGun/Model/Arm 아래 Ak47Holder에 같은 방식으로 넣는다 (손 뼈 기준 좌표). 옛 Arm/Gun(원래 총 메시)은 지운다.
    //   재장전 모션: AK47마다 MagazineReloadMotion — 탄창이 아래로 빠졌다가 새 탄창이 올라와 끼워진다. 3인칭 총은 총구가 위로 들렸다 내려오고(ThirdPersonGunPose),
    //          1인칭 총은 아래로 내려갔다 올라온다(FirstPersonGun).
    // 먼저 Phase 51(1인칭 손)·59(연사 총)가 있어야 한다. 다시 실행하면 새로 만든다. 메뉴: LastBullet > Phase 61 AK47로 바꾸기
    public static class Phase61Ak47Setup
    {
        const string PrefabPath = "Assets/PolyOne/Free Gun/Prefabs/SM_Ak47.prefab";

        // 손 뼈 기준 주먹 가운데, 총의 방향 (총신 = 손의 앞 +Z, 총의 위 = 손의 -X)
        static readonly Vector3 FistInHand = new Vector3(-0.02f, -0.055f, 0.10f);
        static readonly Quaternion HandRotation = Quaternion.LookRotation(Vector3.forward, Vector3.left);

        // 총 모델(+Z 총구, +Y 위로 돌린 뒤) 안에서 손으로 쥘 점: AK47 안 (0.157, -0.04, 0) → Y 90도 회전 → (0, -0.04, -0.157)
        static readonly Vector3 GripInModel = new Vector3(0f, -0.04f, -0.157f);

        const float ThirdScale = 1.6f; // 3인칭 총 크기 (캐릭터가 머리 큰 만화 체형이라 실제(0.78m)보다 크게. 2026-11-04 사용자: 총이 너무 작다)
        const float Scale = 1.3f;      // 1인칭 총·손 기준 크기

        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 61 AK47로 바꾸기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[LastBullet] {PrefabPath}를 찾지 못했어요 (Free Pack - Gun 에셋을 먼저 가져오세요)");
                return;
            }

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = GameObject.FindWithTag("Player");
                var camera = Camera.main;
                var autoGun = player != null ? player.GetComponent<AutoGun>() : null;
                Transform hand = null;
                SkinnedMeshRenderer modelGun = null;
                if (player != null)
                {
                    foreach (var t in player.GetComponentsInChildren<Transform>(true))
                        if (t.name == "R.Hand")
                            hand = t;
                    foreach (var r in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        if (r.name == "modelGun")
                            modelGun = r;
                }
                var model = camera != null ? camera.transform.Find("FirstPersonGun/Model") : null;
                var arm = model != null ? model.Find("Arm") : null;
                if (hand == null || autoGun == null || arm == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: 손 뼈·AutoGun·1인칭 팔 중 없는 것이 있어 건너뜀 (먼저 Phase 51·59)");
                    continue;
                }

                // ── 3인칭 ──
                if (modelGun != null)
                    modelGun.enabled = false;
                Remove(hand, "ThirdPersonGun");
                Remove(hand, "ThirdPersonPistol");
                float parentScale = hand.lossyScale.x > 0f ? hand.lossyScale.x : 1f;
                var third = new GameObject("ThirdPersonGun").transform;
                third.SetParent(hand, false);
                third.localScale = Vector3.one * (ThirdScale / parentScale);
                var pose = third.gameObject.AddComponent<ThirdPersonGunPose>();
                var poseSo = new SerializedObject(pose);
                poseSo.FindProperty("body").objectReferenceValue = player.transform;
                poseSo.FindProperty("chest").objectReferenceValue = Find(player, "Chest");
                poseSo.FindProperty("rightUpper").objectReferenceValue = Find(player, "R.Arm");
                poseSo.FindProperty("rightMid").objectReferenceValue = Find(player, "R.ForeArm");
                poseSo.FindProperty("rightHand").objectReferenceValue = hand;
                poseSo.FindProperty("leftUpper").objectReferenceValue = Find(player, "L.Arm");
                poseSo.FindProperty("leftMid").objectReferenceValue = Find(player, "L.ForeArm");
                poseSo.FindProperty("leftHand").objectReferenceValue = Find(player, "L.Hand");
                poseSo.FindProperty("gripInGun").vector3Value = GripInModel;
                poseSo.FindProperty("gun").objectReferenceValue = autoGun;
                poseSo.ApplyModifiedPropertiesWithoutUndo();
                var thirdAk = PlaceAk(prefab, third, autoGun, ShadowCastingMode.On);

                // ── 1인칭 ──
                Remove(arm, "Gun");
                Remove(arm, "Ak47Holder");
                var holder = new GameObject("Ak47Holder").transform;
                holder.SetParent(arm, false);
                holder.localRotation = HandRotation;
                holder.localScale = Vector3.one * Scale;
                holder.localPosition = FistInHand - HandRotation * (GripInModel * Scale);
                PlaceAk(prefab, holder, autoGun, ShadowCastingMode.Off);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 총을 AK47로 바꿨어요 (3인칭 {thirdAk.name}, 1인칭 Ak47Holder).");
            }
            AssetDatabase.SaveAssets();
        }

        // 총 모델을 만들고 (총구가 +Z로 가게 Y 90도 돌림), 재장전 모션을 붙인다
        static GameObject PlaceAk(GameObject prefab, Transform parent, AutoGun gun, ShadowCastingMode shadows)
        {
            var ak = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            ak.name = "Ak47";
            ak.transform.localPosition = Vector3.zero;
            ak.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            ak.transform.localScale = Vector3.one;

            Transform magazine = null;
            foreach (var t in ak.GetComponentsInChildren<Transform>(true))
                if (t.name == "Ak47_Magazine")
                    magazine = t;
            // 탄창이 빠지는 방향 = 총 모델의 아래(-Y). 탄창 부품 부모 기준이라 그대로 down이다.
            var motion = ak.AddComponent<MagazineReloadMotion>();
            var so = new SerializedObject(motion);
            so.FindProperty("gun").objectReferenceValue = gun;
            so.FindProperty("magazine").objectReferenceValue = magazine;
            so.FindProperty("slideDirection").vector3Value = Vector3.down;
            so.FindProperty("travel").floatValue = 0.22f;
            so.ApplyModifiedPropertiesWithoutUndo();

            var material = GetMaterial();
            foreach (var r in ak.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = material;
                r.shadowCastingMode = shadows;
                r.receiveShadows = shadows == ShadowCastingMode.On;
            }
            return ak;
        }

        // 총 전용 머티리얼. FBX 안의 재질(Props_Gun_M)은 그림이 없어 흰색으로 나오므로, 에셋 팩의 그림이 든 재질(Props_FreeGun_M)을 복사해 쓴다.
        // 이미 있으면 사용자가 고친 것이므로 그대로 둔다.
        static Material GetMaterial()
        {
            const string path = "Assets/Materials/Ak47.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            AssetDatabase.CopyAsset("Assets/PolyOne/Free Gun/Materials/Props_FreeGun_M.mat", path);
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        static Transform Find(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name)
                    return t;
            return null;
        }

        static void Remove(Transform parent, string name)
        {
            var old = parent.Find(name);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
        }
    }
}
