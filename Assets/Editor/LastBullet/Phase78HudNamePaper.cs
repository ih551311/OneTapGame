using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 화면 왼쪽 위·아래 프로필 패널의 이름 칸(NamePlate)을 종이 느낌으로 바꾼다 (2026-11-06 사용자 요청).
    //   - Assets/UI/Hud_Name.png 를 약간 진한 베이지 종이 쪽지(손으로 오린 가장자리)로 다시 그린다 (유니티 C#으로 생성, 압축 끔).
    //   - 이름 칸 위 글자(닉네임·그림 수·종이 수)를 어두운 갈색으로 바꾼다 (4개 씬).
    // 메뉴: LastBullet > Phase 78 이름 칸 종이로 (그림을 직접 그려 덮어쓰려면 PNG만 바꾸면 된다. 이 메뉴는 그림도 덮어쓴다)
    public static class Phase78HudNamePaper
    {
        const string PngPath = "Assets/UI/Hud_Name.png";
        const int Size = 96;
        public static readonly Color32 TextColor = new Color32(0x4A, 0x31, 0x1E, 0xFF); // 어두운 갈색

        [MenuItem("LastBullet/Phase 78 이름 칸 종이로")]
        static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            WritePng();

            foreach (var path in new[] { "Assets/Scenes/1_Museum.unity", "Assets/Scenes/2_Forest.unity", "Assets/Scenes/3_CastleVillage.unity", "Assets/Scenes/4_Desert.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path);
                var canvas = ArtSlots.FindCanvas();
                var plate = canvas != null ? canvas.transform.Find("HudProfile/NamePlate") : null;
                if (plate == null)
                {
                    Debug.LogWarning($"[LastBullet] {path}: HudProfile/NamePlate 가 없어 건너뜀.");
                    continue;
                }
                int n = 0;
                foreach (var text in plate.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.color = TextColor;
                    EditorUtility.SetDirty(text);
                    n++;
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {path}: 이름 칸 글자 {n}개를 어두운 갈색으로 바꿨어요.");
            }
            AssetDatabase.SaveAssets();
        }

        static void WritePng()
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var paper = new Color(0.92f, 0.84f, 0.68f);      // 베이지 종이
            var edge = new Color(0.70f, 0.57f, 0.39f);       // 가장자리 짙은 갈색
            float half = Size * 0.5f;
            const float radius = 11f;
            const float inset = 2f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // 둥근 사각형까지의 부호 있는 거리 (안쪽이 +)
                    float dx = Mathf.Abs(x + 0.5f - half) - (half - inset - radius);
                    float dy = Mathf.Abs(y + 0.5f - half) - (half - inset - radius);
                    float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
                    float inside = Mathf.Min(Mathf.Max(dx, dy), 0f);
                    float d = radius - (outside + inside);

                    // 손으로 오린 듯 가장자리를 살짝 흔든다
                    float wobble = (Noise(x * 0.18f, y * 0.18f) - 0.5f) * 3.0f + (Noise(x * 0.6f + 9f, y * 0.6f + 4f) - 0.5f) * 1.0f;
                    float edgeDist = d + wobble;
                    float alpha = Mathf.Clamp01(edgeDist + 0.5f);

                    // 종이 결(가는 섬유 + 얼룩)
                    float grain = (Noise(x * 0.9f, y * 0.25f) - 0.5f) * 0.05f + (Noise(x * 0.12f, y * 0.12f) - 0.5f) * 0.05f;
                    var color = paper + new Color(grain, grain, grain * 0.8f, 0f);
                    // 가장자리 가까이 짙게
                    float rim = Mathf.Clamp01(1f - edgeDist / 5f);
                    color = Color.Lerp(color, edge, rim * rim * 0.9f);
                    color.a = alpha;
                    tex.SetPixel(x, y, color);
                }
            }
            tex.Apply();
            System.IO.File.WriteAllBytes(PngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(PngPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(PngPath);
            importer.textureCompression = TextureImporterCompression.Uncompressed; // 작은 UI 그림이 깨져 보이지 않게
            importer.SaveAndReimport();
        }

        // 격자 값 잡음 (0~1)
        static float Noise(float x, float y)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(xi, yi), b = Hash(xi + 1, yi), c = Hash(xi, yi + 1), e = Hash(xi + 1, yi + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, e, fx), fy);
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }
    }
}
