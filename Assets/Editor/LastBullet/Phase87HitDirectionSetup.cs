using System.IO;
using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 맞은 방향 표시를 만든다 (2026-11-08 사용자가 고른 추천 작업: 피격 연출).
    //   그림 칸 (이미 있으면 덮어쓰지 않는다): Assets/UI/HitDirection.png — 위쪽을 향한 붉은 호(약 50도 폭) 한 개가 들어 있는 정사각형 그림 (512×512, 가운데가 조준점).
    //   씬 3개(숲·성 마을·사막): Canvas/HitDirection (조준점 가운데 800×800) 아래에 호 이미지 4개(Arc_0~3)를 만들고 HitDirectionIndicator를 붙인다.
    //   평소에는 모두 투명하고, 맞은 방향 쪽에 호가 나타났다 1.1초 동안 사라진다.
    // 다시 누르면 지우고 새로 만든다. 메뉴: LastBullet > Phase 87 맞은 방향 표시 만들기
    public static class Phase87HitDirectionSetup
    {
        const string Png = "Assets/UI/HitDirection.png";

        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 87 맞은 방향 표시 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            MakePng();
            AssetDatabase.Refresh();
            var sprite = ArtSlots.LoadSprite(Png);

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var canvas = ArtSlots.FindCanvas();
                if (canvas == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: Canvas가 없어 건너뜀");
                    continue;
                }
                var old = canvas.transform.Find("HitDirection");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                var root = new GameObject("HitDirection", typeof(RectTransform));
                root.transform.SetParent(canvas.transform, false);
                var rootRect = (RectTransform)root.transform;
                rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = new Vector2(800f, 800f);
                // 상태창·결과 창 같은 다른 UI보다 뒤에 둔다 (조준점 바로 위쯤)
                var crosshair = canvas.transform.Find("Crosshair");
                root.transform.SetSiblingIndex(crosshair != null ? crosshair.GetSiblingIndex() : 0);

                var arcs = new Image[4];
                for (int i = 0; i < arcs.Length; i++)
                {
                    var go = new GameObject($"Arc_{i}", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(rootRect, false);
                    var rect = (RectTransform)go.transform;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    var image = go.GetComponent<Image>();
                    image.sprite = sprite;
                    image.color = new Color(1f, 1f, 1f, 0f); // 흰색 = 그림 색 그대로, 처음엔 투명
                    image.preserveAspect = true;
                    image.raycastTarget = false;
                    arcs[i] = image;
                }

                var indicator = root.AddComponent<HitDirectionIndicator>();
                var so = new SerializedObject(indicator);
                var array = so.FindProperty("arcs");
                array.arraySize = arcs.Length;
                for (int i = 0; i < arcs.Length; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = arcs[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 맞은 방향 표시를 만들었어요.");
            }
            AssetDatabase.SaveAssets();
        }

        // 위쪽을 향한 붉은 호: 중심에서 반지름 0.78~0.93 사이의 띠, 위쪽 ±26도 (끝으로 갈수록 투명)
        static void MakePng()
        {
            if (File.Exists(Png))
                return;
            const int N = 512;
            var texture = new Texture2D(N, N, TextureFormat.RGBA32, false);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = ((x + 0.5f) / N * 2f - 1f), dy = ((y + 0.5f) / N * 2f - 1f);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Abs(Mathf.Atan2(dx, dy) * Mathf.Rad2Deg); // 위쪽(+y)이 0도
                    float band = Mathf.Clamp01(1f - Mathf.Abs(r - 0.855f) / 0.075f);       // 띠 가운데가 진하다
                    float wedge = Mathf.Clamp01((26f - angle) / 10f);                      // 양 끝 10도는 서서히 사라진다
                    float a = band * band * (3f - 2f * band) * wedge;
                    texture.SetPixel(x, y, new Color(0.95f, 0.12f, 0.1f, a));
                }
            texture.Apply();
            File.WriteAllBytes(Png, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(Png);
            var importer = (TextureImporter)AssetImporter.GetAtPath(Png);
            if (importer != null)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }
    }
}
