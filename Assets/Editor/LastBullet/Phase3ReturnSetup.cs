using System.IO;
using LastBullet.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // Phase 3-3: 떨어진 탄환에 E키 회수·15초 자동 복귀를 붙이고, 크로스헤어 둘레에 원형 게이지를 만든다.
    // 메뉴: LastBullet > Phase 3-3 탄환 회수와 자동 복귀 만들기
    public static class Phase3ReturnSetup
    {
        const string PrefabPath = "Assets/Prefabs/DroppedBullet.prefab";
        const string RingSpritePath = "Assets/UI/Ring.png";

        [MenuItem("LastBullet/Phase 3-3 탄환 회수와 자동 복귀 만들기")]
        static void Create()
        {
            // 1_Museum 씬이 열려 있지 않으면 자동으로 연다.
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "1_Museum")
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            }

            var player = GameObject.Find("Player");
            var gun = player != null ? player.GetComponent<OneBulletGun>() : null;
            var hud = ArtSlots.FindCanvas();
            if (gun == null || hud == null || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 3-2 메뉴를 먼저 실행하세요.", "확인");
                return;
            }
            if (hud.GetComponent<BulletIndicatorUI>() != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "HUD에 이미 BulletIndicatorUI가 있어요.", "확인");
                return;
            }

            // 1. 탄환 프리팹에 회수·자동 복귀 스크립트를 붙인다.
            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot.GetComponent<BulletPickup>() == null)
                prefabRoot.AddComponent<BulletPickup>();
            if (prefabRoot.GetComponent<BulletAutoReturn>() == null)
                prefabRoot.AddComponent<BulletAutoReturn>();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            // 2. 크로스헤어 둘레의 원형 게이지 (노란 고리, 남은 시간만큼 채워짐)
            var gaugeObject = new GameObject("ReturnGauge", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(gaugeObject, "Create ReturnGauge");
            gaugeObject.transform.SetParent(hud.transform, false);
            var rect = gaugeObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(56f, 56f);
            var gauge = gaugeObject.GetComponent<Image>();
            gauge.sprite = GetOrCreateRingSprite();
            gauge.type = Image.Type.Filled;
            gauge.fillMethod = Image.FillMethod.Radial360;
            gauge.fillOrigin = (int)Image.Origin360.Top;
            gauge.fillClockwise = false;
            gauge.color = new Color32(0xFA, 0xCC, 0x15, 0xFF); // 물감 노랑
            gauge.raycastTarget = false;
            gauge.enabled = false;

            // 3. HUD에 BulletIndicatorUI를 붙이고 연결한다.
            var indicator = Undo.AddComponent<BulletIndicatorUI>(hud);
            var so = new SerializedObject(indicator);
            so.FindProperty("gun").objectReferenceValue = gun;
            so.FindProperty("returnGauge").objectReferenceValue = gauge;
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = hud;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] Phase 3-3: 탄환 회수·자동 복귀와 원형 게이지를 만들고 1_Museum 씬을 저장했어요.");
        }

        // 128px 흰 고리 이미지를 만들어 스프라이트로 가져온다. 색은 Image에서 정한다.
        static Sprite GetOrCreateRingSprite()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RingSpritePath);
            if (sprite != null)
                return sprite;

            const int size = 128;
            const float thickness = 12f;
            float outer = size / 2f - 1f;
            float inner = outer - thickness;
            var center = new Vector2(size / 2f, size / 2f);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    // 가장자리를 1px 부드럽게
                    float alpha = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            File.WriteAllBytes(Path.Combine(Application.dataPath, "UI/Ring.png"), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(RingSpritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(RingSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(RingSpritePath);
        }
    }
}
