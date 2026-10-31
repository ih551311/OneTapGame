using LastBullet.Combat;
using LastBullet.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 1인칭 총을 만든다 (2026-10-31 사용자 요청). 미술관·숲·성 마을·사막의 Main Camera 아래에 FirstPersonGun을 놓는다.
    //   FirstPersonGun            기능 (FirstPersonGun.cs: 나타남, 반동, 흔들림, 조준)
    //     Model                   겉모습. 임시 신호총 도형 4개 (여기를 3ds Max 모델로 바꿔 끼운다)
    //       Body, Barrel, MuzzleRing, Grip
    // 머티리얼은 부품 종류마다 전용: FirstPersonGun_Body.mat, _Barrel.mat, _Muzzle.mat, _Grip.mat (Assets/Materials)
    // 다시 실행하면 지우고 새로 만든다. 메뉴: LastBullet > Phase 48 1인칭 총 만들기
    public static class Phase48FirstPersonGunSetup
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 48 1인칭 총 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var body = ArtSlots.GetOrCreateMaterial("FirstPersonGun_Body", new Color32(0x7A, 0x80, 0x8C, 0xFF));
            var barrel = ArtSlots.GetOrCreateMaterial("FirstPersonGun_Barrel", new Color32(0x9A, 0xA0, 0xAC, 0xFF));
            var muzzle = ArtSlots.GetOrCreateMaterial("FirstPersonGun_Muzzle", new Color32(0xE8, 0x6A, 0x1E, 0xFF));
            var grip = ArtSlots.GetOrCreateMaterial("FirstPersonGun_Grip", new Color32(0x9A, 0x64, 0x3C, 0xFF));

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = GameObject.FindWithTag("Player");
                var camera = Camera.main;
                if (player == null || camera == null || player.GetComponent<OneBulletGun>() == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: Player·카메라·총이 없어 건너뜀");
                    continue;
                }

                var old = camera.transform.Find("FirstPersonGun");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                var root = new GameObject("FirstPersonGun");
                root.transform.SetParent(camera.transform, false);
                var model = new GameObject("Model").transform;
                model.SetParent(root.transform, false);
                model.localScale = Vector3.one * 0.55f; // 화면에서 너무 크지 않게 (모양 바꿀 때 이 크기를 맞춘다)

                // 앞(+Z)을 보는 임시 신호총. 단위 m, 모델 가운데가 손잡이 위쪽.
                Part("Body", PrimitiveType.Cube, model, new Vector3(0f, 0f, 0.02f), Vector3.zero, new Vector3(0.07f, 0.09f, 0.30f), body);
                Part("Barrel", PrimitiveType.Cylinder, model, new Vector3(0f, 0.012f, 0.24f), new Vector3(90f, 0f, 0f), new Vector3(0.05f, 0.1f, 0.05f), barrel);
                Part("MuzzleRing", PrimitiveType.Cylinder, model, new Vector3(0f, 0.012f, 0.34f), new Vector3(90f, 0f, 0f), new Vector3(0.068f, 0.025f, 0.068f), muzzle);
                Part("Grip", PrimitiveType.Cube, model, new Vector3(0f, -0.115f, -0.09f), new Vector3(-15f, 0f, 0f), new Vector3(0.055f, 0.16f, 0.075f), grip);

                // 충돌·그림자는 없다 (카메라에 붙은 겉모습일 뿐이다)
                foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                var script = root.AddComponent<FirstPersonGun>();
                var so = new SerializedObject(script);
                so.FindProperty("zoom").objectReferenceValue = player.GetComponent<CameraZoom>();
                so.FindProperty("gun").objectReferenceValue = player.GetComponent<OneBulletGun>();
                so.FindProperty("health").objectReferenceValue = player.GetComponent<PlayerHealth>();
                so.FindProperty("aim").objectReferenceValue = player.GetComponent<AimController>();
                so.FindProperty("controller").objectReferenceValue = player.GetComponent<CharacterController>();
                so.FindProperty("model").objectReferenceValue = model;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 1인칭 총을 만들었어요 (카메라 아래 FirstPersonGun).");
            }
            AssetDatabase.SaveAssets();
        }

        static void Part(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 euler, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
