using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 지금까지 만든 UI와 오브젝트를 "내 그림 넣는 곳" 규칙에 맞춘다.
    // - UI: Assets/UI의 PNG를 그대로 보여 준다 (Image 색 = 흰색이라 그림 색이 그대로 나온다)
    // - 오브젝트: 오브젝트마다 전용 머티리얼(Assets/Materials)을 쓴다. Base Map에 그림을 넣으면 바뀐다.
    // 메뉴: LastBullet > 그림 넣는 곳 정리 (UI·오브젝트)
    public static class ArtSlotsSetup
    {
        [MenuItem("LastBullet/그림 넣는 곳 정리 (UI·오브젝트)")]
        static void Apply()
        {
            // Hideout 씬이 열려 있지 않으면 자동으로 연다.
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Hideout")
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                scene = EditorSceneManager.OpenScene("Assets/Scenes/Hideout.unity");
            }

            var hud = GameObject.Find("HUD");
            var player = GameObject.Find("Player");
            if (hud == null || player == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 2~3 메뉴를 먼저 실행하세요.", "확인");
                return;
            }

            // 1. UI: 크로스헤어를 그림 한 장으로 다시 만들고, 게이지·화살표는 그림 색 그대로
            var oldCrosshair = hud.transform.Find("Crosshair");
            if (oldCrosshair != null)
                Undo.DestroyObjectImmediate(oldCrosshair.gameObject);
            Phase2CameraSetup.BuildCrosshair(hud.transform);
            SetUiImage(hud.transform.Find("ReturnGauge"), "Assets/UI/Ring.png");
            SetUiImage(hud.transform.Find("BulletArrow"), "Assets/UI/Arrow.png");

            // 2. 오브젝트: 전용 머티리얼
            var playerMaterial = ArtSlots.GetOrCreateMaterial("Player", new Color32(0xE5, 0xE5, 0xE5, 0xFF));
            SetMaterial(player.transform.Find("Body"), playerMaterial);
            SetMaterial(player.transform.Find("Front"), playerMaterial);
            SetMaterial(player.transform.Find("Gun"), ArtSlots.GetOrCreateMaterial("Gun", new Color32(0x40, 0x40, 0x40, 0xFF)));
            var ground = GameObject.Find("Ground");
            if (ground != null)
                SetMaterial(ground.transform, ArtSlots.GetOrCreateMaterial("Ground", new Color32(0xD4, 0xD4, 0xD4, 0xFF)));

            // 프리팹: 바꾸면 씬에 놓인 것과 앞으로 생기는 것이 모두 바뀐다
            var statueMaterial = ArtSlots.GetOrCreateMaterial("Enemy_Charger", new Color32(0x9C, 0xA3, 0xAF, 0xFF)); // 회색 조각상
            EditPrefab("Assets/Prefabs/Enemy_Charger.prefab", root =>
            {
                SetMaterial(root.transform.Find("Body"), statueMaterial);
                SetMaterial(root.transform.Find("Head"), statueMaterial);
            });
            var bulletMaterial = ArtSlots.GetOrCreateMaterial("Bullet", new Color32(0xFA, 0xCC, 0x15, 0xFF));
            EditPrefab("Assets/Prefabs/DroppedBullet.prefab", root => SetMaterial(root.transform.Find("Model"), bulletMaterial));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] UI는 Assets/UI 그림을, 오브젝트는 전용 머티리얼을 쓰도록 정리하고 Hideout 씬을 저장했어요. 목록: docs/ART_SLOTS.md");
        }

        static void SetUiImage(Transform target, string spritePath)
        {
            if (target == null)
                return;
            var image = target.GetComponent<Image>();
            Undo.RecordObject(image, "Set UI Image");
            image.sprite = ArtSlots.LoadSprite(spritePath);
            image.color = Color.white; // 흰색 = 그림 색 그대로
            image.preserveAspect = true;
            var outline = target.GetComponent<Outline>();
            if (outline != null)
                Undo.DestroyObjectImmediate(outline); // 테두리는 그림 안에 그린다
        }

        static void SetMaterial(Transform target, Material material)
        {
            if (target == null)
                return;
            var renderer = target.GetComponent<Renderer>();
            Undo.RecordObject(renderer, "Set Material");
            renderer.sharedMaterial = material;
        }

        static void EditPrefab(string path, System.Action<GameObject> edit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            edit(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
