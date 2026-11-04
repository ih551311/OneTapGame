using System.IO;
using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 거울 포털로 전투 장소에 들어갈 때의 색 번짐 전환을 만든다 (2026-11-05 사용자 요청).
    //   그림 칸 (이미 있으면 덮어쓰지 않는다): Assets/UI/PaintBlob.png — 흰색 얼룩 (가장자리가 울퉁불퉁한 동그라미 + 튄 방울). 색은 코드가 맵의 물감 3색으로 입힌다.
    //   씬 4개(미술관·숲·성 마을·사막): Canvas/PaintTransition (화면 전체 크기) 아래에 얼룩 Image 40개(8×5 격자, 조금씩 어긋남)를 만들고 PaintTransition을 붙인다.
    //     평소에는 얼룩 크기가 0이고, 미술관에서 전투 장소를 고르면 번져서 화면을 덮은 뒤 씬을 옮기고, 도착한 맵에서는 줄어들며 맵이 드러난다.
    // 이미 만들었으면 지우고 다시 만든다. 메뉴: LastBullet > Phase 70 색 번짐 전환 만들기
    public static class Phase70PaintTransitionSetup
    {
        const string BlobPng = "Assets/UI/PaintBlob.png";
        const int Columns = 8, Rows = 5;
        const float BlobSize = 720f;

        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 70 색 번짐 전환 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            CreatePng();
            AssetDatabase.Refresh();
            var sprite = ArtSlots.LoadSprite(BlobPng);

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var canvas = ArtSlots.FindCanvas();
                if (canvas == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: Canvas가 없어 건너뜀");
                    continue;
                }
                var old = canvas.transform.Find("PaintTransition");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                var root = new GameObject("PaintTransition", typeof(RectTransform));
                root.transform.SetParent(canvas.transform, false);
                var rect = (RectTransform)root.transform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                root.transform.SetAsLastSibling(); // 다른 UI 위에 그린다

                var random = new System.Random(11);
                var images = new Image[Columns * Rows];
                for (int y = 0; y < Rows; y++)
                    for (int x = 0; x < Columns; x++)
                    {
                        var go = new GameObject($"Blob_{y}_{x}", typeof(RectTransform), typeof(Image));
                        go.transform.SetParent(rect, false);
                        var image = go.GetComponent<Image>();
                        image.sprite = sprite;
                        image.preserveAspect = true;
                        image.raycastTarget = true; // 번지는 동안 뒤의 버튼이 눌리지 않게
                        var r = (RectTransform)go.transform;
                        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                        float jitterX = (float)(random.NextDouble() - 0.5) * 120f, jitterY = (float)(random.NextDouble() - 0.5) * 120f;
                        float px = (x - (Columns - 1) / 2f) * (1920f / (Columns - 1)) * 0.98f + jitterX;
                        float py = (y - (Rows - 1) / 2f) * (1080f / (Rows - 1)) * 0.98f + jitterY;
                        r.anchoredPosition = new Vector2(px, py);
                        float size = BlobSize * (0.9f + (float)random.NextDouble() * 0.3f);
                        r.sizeDelta = new Vector2(size, size);
                        r.localRotation = Quaternion.Euler(0f, 0f, (float)random.NextDouble() * 360f);
                        r.localScale = Vector3.zero; // 평소에는 얼룩이 없다 (오브젝트는 켜 둔다)
                        images[y * Columns + x] = image;
                    }

                var transition = root.AddComponent<PaintTransition>();
                var so = new SerializedObject(transition);
                var array = so.FindProperty("blobs");
                array.arraySize = images.Length;
                for (int i = 0; i < images.Length; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = images[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 색 번짐 전환을 만들었어요.");
            }
            AssetDatabase.SaveAssets();
        }

        // 0 이하는 0, 1 이상은 1, 사이는 부드럽게 (Mathf.SmoothStep과 인자 뜻이 다르다)
        static float Smooth(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        // 가장자리가 울퉁불퉁한 얼룩 + 주위에 튄 방울 몇 개 (흰색)
        static void CreatePng()
        {
            if (File.Exists(BlobPng))
                return;
            const int N = 256;
            var texture = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var drops = new[] { (0.62f, 0.9f, 0.07f), (2.4f, 0.86f, 0.05f), (4.1f, 0.92f, 0.06f), (5.3f, 0.84f, 0.04f) };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - (N - 1) / 2f) / (N / 2f), dy = (y - (N - 1) / 2f) / (N / 2f);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float angle = Mathf.Atan2(dy, dx);
                    float edge = 0.66f + 0.07f * Mathf.Sin(5f * angle + 0.6f) + 0.05f * Mathf.Sin(9f * angle + 2.1f) + 0.03f * Mathf.Sin(14f * angle);
                    float a = 1f - Smooth(edge - 0.03f, edge + 0.01f, r);
                    foreach (var d in drops)
                    {
                        float ddx = dx - Mathf.Cos(d.Item1) * d.Item2 * 0.95f, ddy = dy - Mathf.Sin(d.Item1) * d.Item2 * 0.95f;
                        float dr = Mathf.Sqrt(ddx * ddx + ddy * ddy);
                        a = Mathf.Max(a, 1f - Smooth(d.Item3 - 0.015f, d.Item3 + 0.01f, dr));
                    }
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            texture.Apply();
            File.WriteAllBytes(BlobPng, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(BlobPng);
        }
    }
}
