using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 3인칭에서 캐릭터가 들고 있는 총을 pistol1 모델로 바꾼다 (2026-10-31 사용자 요청: 1인칭 총과 같은 총으로).
    //   캐릭터의 원래 총(modelGun, 캐릭터 모델에 붙은 메시)은 끄기만 한다 (지우지 않는다).
    //   pistol1을 오른손 뼈(R.Hand) 아래 ThirdPersonPistol로 붙인다. 손의 앞(+Z)이 총구 방향이다.
    //   머티리얼은 1인칭 총과 같은 FirstPersonGun_Pistol.mat을 같이 쓴다 (Phase 49가 먼저 있어야 한다).
    // CameraZoom은 1인칭에서 Player 아래의 모든 모습을 숨기므로 이 총도 1인칭에서는 자동으로 숨는다 (그림자만 남음).
    // ThirdPersonGunPose가 동작이 끝난 뒤 총구를 캐릭터 앞으로 돌린다 (주먹 가운데에 쥔 채로 손목만 돌리는 효과).
    // 크기·위치는 아래 상수에서 맞춘다. 다시 실행하면 지우고 새로 붙인다.
    // 메뉴: LastBullet > Phase 50 3인칭 총을 pistol1로 바꾸기
    public static class Phase50ThirdPersonPistolSetup
    {
        const string ModelPath = "Assets/Low Poly Guns/Models/Guns/pistol1/pistol1.fbx";
        const string MaterialPath = "Assets/Materials/FirstPersonGun_Pistol.mat";

        // pistol1 모델은 길이 1.4m라 줄여서 쓴다. 손바닥 위치(모델 안의 손잡이 점)를 손 뼈 원점에 맞춘다.
        const float Scale = 0.5f;
        static readonly Vector3 GripInMesh = new Vector3(0f, -0.08f, -0.48f); // 모델 안에서 손으로 쥘 점 (손잡이 가운데, 모델 분석으로 찾은 값)
        static readonly Vector3 HandOffset = new Vector3(-0.02f, -0.055f, 0.10f); // 손 뼈 기준으로 그 점이 놓일 자리 = 주먹 가운데 (손가락이 -Y쪽으로 말려 있다)
        // 손잡이가 주먹을 뚫고 지나는 방향 = 손의 X축. 총의 위쪽 = 손의 -X(엄지 쪽), 총구 = 손의 앞(+Z)
        static readonly Quaternion HandRotation = Quaternion.LookRotation(Vector3.forward, Vector3.left);

        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 50 3인칭 총을 pistol1로 바꾸기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (prefab == null || material == null)
            {
                Debug.LogError("[LastBullet] pistol1 모델이나 FirstPersonGun_Pistol.mat이 없어요 (먼저 Phase 49)");
                return;
            }

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = GameObject.FindWithTag("Player");
                Transform hand = null;
                SkinnedMeshRenderer oldGun = null;
                if (player != null)
                {
                    foreach (var t in player.GetComponentsInChildren<Transform>(true))
                        if (t.name == "R.Hand")
                            hand = t;
                    foreach (var r in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        if (r.name == "modelGun")
                            oldGun = r;
                }
                if (hand == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: 오른손 뼈(R.Hand)를 찾지 못해 건너뜀");
                    continue;
                }

                var old = hand.Find("ThirdPersonPistol");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
                if (oldGun != null)
                    oldGun.enabled = false;

                var pistol = (GameObject)PrefabUtility.InstantiatePrefab(prefab, hand);
                pistol.name = "ThirdPersonPistol";
                var rotation = HandRotation;
                pistol.transform.localRotation = rotation;
                pistol.transform.localScale = Vector3.one * Scale;
                // 손 뼈의 크기가 1이 아닐 수 있어 월드 크기 기준으로 맞춘다
                float parentScale = hand.lossyScale.x > 0f ? hand.lossyScale.x : 1f;
                pistol.transform.localScale = Vector3.one * (Scale / parentScale);
                pistol.transform.localPosition = (HandOffset - rotation * GripInMesh * Scale) / parentScale;

                var pose = pistol.AddComponent<LastBullet.Player.ThirdPersonGunPose>();
                var poseSo = new SerializedObject(pose);
                poseSo.FindProperty("hand").objectReferenceValue = hand;
                poseSo.FindProperty("body").objectReferenceValue = player.transform;
                poseSo.FindProperty("fistInHand").vector3Value = HandOffset;
                poseSo.FindProperty("gripInMesh").vector3Value = GripInMesh;
                poseSo.ApplyModifiedPropertiesWithoutUndo();

                foreach (var renderer in pistol.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 3인칭 총을 pistol1로 바꿨어요 (원래 총 modelGun은 꺼 둠).");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
