using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 1인칭 총의 겉모습을 사용자가 가져온 pistol1 모델로 바꾼다 (2026-10-31 사용자 요청).
    //   모델: Assets/Low Poly Guns/Models/Guns/pistol1/pistol1.fbx (에셋은 그대로 두고, 원본 머티리얼은 Standard라 URP에서 분홍색이 되므로 쓰지 않는다)
    //   머티리얼: Assets/Materials/FirstPersonGun_Pistol.mat (URP Lit + pistol1_diffuse.png + pistol1_normal.png)
    //   Main Camera/FirstPersonGun/Model 아래의 임시 도형을 지우고 Pistol을 넣는다. 크기·방향은 아래 상수에서 맞춘다.
    // 놓는 자리·반동·흔들림은 FirstPersonGun 컴포넌트 값에서 고친다. 다시 실행해도 같은 결과다.
    // (Phase 48을 다시 실행하면 임시 도형으로 돌아가므로, 그 뒤에는 이 메뉴를 다시 실행한다)
    // 메뉴: LastBullet > Phase 49 1인칭 총을 pistol1로 바꾸기
    public static class Phase49PistolModelSetup
    {
        const string ModelPath = "Assets/Low Poly Guns/Models/Guns/pistol1/pistol1.fbx";
        const string DiffusePath = "Assets/Low Poly Guns/Models/Guns/pistol1/pistol1_diffuse.png";
        const string NormalPath = "Assets/Low Poly Guns/Models/Guns/pistol1/pistol1_normal.png";
        const string MaterialPath = "Assets/Materials/FirstPersonGun_Pistol.mat";

        // 모델은 길이 1.4m짜리라 화면용으로 줄인다. 모델의 가운데(0, 0.184, 0.05)를 Model 원점에 맞춘다.
        const float Scale = 0.22f;
        static readonly Vector3 MeshCenter = new Vector3(0f, 0.184f, 0.05f);
        const float YawDegrees = 0f; // 총구가 반대로 보이면 180

        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 49 1인칭 총을 pistol1로 바꾸기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (prefab == null)
            {
                Debug.LogError($"[LastBullet] {ModelPath}를 찾지 못했어요");
                return;
            }
            var material = GetOrCreateMaterial();

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var camera = Camera.main;
                var gun = camera != null ? camera.transform.Find("FirstPersonGun") : null;
                var model = gun != null ? gun.Find("Model") : null;
                if (model == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: FirstPersonGun/Model이 없어 건너뜀 (먼저 Phase 48)");
                    continue;
                }

                for (int i = model.childCount - 1; i >= 0; i--)
                    Object.DestroyImmediate(model.GetChild(i).gameObject);
                model.localScale = Vector3.one;

                var pistol = (GameObject)PrefabUtility.InstantiatePrefab(prefab, model);
                pistol.name = "Pistol";
                var yaw = Quaternion.Euler(0f, YawDegrees, 0f);
                pistol.transform.localRotation = yaw;
                pistol.transform.localScale = Vector3.one * Scale;
                pistol.transform.localPosition = -(yaw * MeshCenter) * Scale;

                foreach (var renderer in pistol.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                // FirstPersonGun이 켜고 끌 렌더러 목록을 다시 모은다 (Awake가 실행 때 모으므로 따로 할 일은 없다)
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 1인칭 총을 pistol1 모델로 바꿨어요.");
            }
            AssetDatabase.SaveAssets();
        }

        static Material GetOrCreateMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
                return existing; // 사용자가 고친 것일 수 있으니 그대로 둔다

            // 법선 그림은 Normal map으로 가져와야 한다
            var normalImporter = (TextureImporter)AssetImporter.GetAtPath(NormalPath);
            if (normalImporter != null && normalImporter.textureType != TextureImporterType.NormalMap)
            {
                normalImporter.textureType = TextureImporterType.NormalMap;
                normalImporter.SaveAndReimport();
            }

            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(DiffusePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath));
            material.EnableKeyword("_NORMALMAP");
            material.SetFloat("_Smoothness", 0.35f);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }
    }
}
