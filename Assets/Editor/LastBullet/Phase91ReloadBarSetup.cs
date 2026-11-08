using System.IO;
using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 재장전 표시를 "재장전 62%" 글자에서 숫자 아래의 가는 진행 막대로 바꾼다 (2026-11-08 사용자 요청: 장전한다고 뜨는 글자를 고치고 싶다 → 진행 막대).
    //   그림 칸: Assets/UI/EnemyBar_Back.png(막대 바탕, 적 체력바와 같은 잉크 바탕을 같이 씀), Assets/UI/Reload_Fill.png(채움, 이미 있으면 덮어쓰지 않는다).
    //   씬 3개(숲·성 마을·사막): Canvas/AmmoDisplay 아래에 ReloadBar(바탕) + Fill(채움, 가로로 차오름)을 만들고 AmmoDisplay에 연결한다. 옛 글자(StatusText)는 끈다.
    // 다시 누르면 막대를 지우고 새로 만든다. 메뉴: LastBullet > Phase 91 재장전 막대 만들기
    public static class Phase91ReloadBarSetup
    {
        const string FillPng = "Assets/UI/Reload_Fill.png";

        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 91 재장전 막대 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            MakeFillPng();
            AssetDatabase.Refresh();
            var back = ArtSlots.LoadSprite("Assets/UI/EnemyBar_Back.png");
            var fill = ArtSlots.LoadSprite(FillPng);

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var canvas = ArtSlots.FindCanvas();
                var ammo = canvas != null ? canvas.transform.Find("AmmoDisplay") : null;
                if (ammo == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: AmmoDisplay가 없어 건너뜀");
                    continue;
                }
                var old = ammo.Find("ReloadBar");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                // 숫자(AmmoText, 아이콘 오른쪽 x 135부터) 바로 아래, 왼쪽 아래 기준
                var root = new GameObject("ReloadBar", typeof(RectTransform), typeof(Image));
                root.transform.SetParent(ammo, false);
                var rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0f);
                rect.anchoredPosition = new Vector2(138f, 4f);
                rect.sizeDelta = new Vector2(150f, 9f);
                var bg = root.GetComponent<Image>();
                bg.sprite = back;
                bg.color = Color.white;
                bg.raycastTarget = false;

                var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillGo.transform.SetParent(rect, false);
                var fillRect = (RectTransform)fillGo.transform;
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = new Vector2(1.5f, 1.5f);
                fillRect.offsetMax = new Vector2(-1.5f, -1.5f);
                var fillImage = fillGo.GetComponent<Image>();
                fillImage.sprite = fill;
                fillImage.color = Color.white;
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
                fillImage.fillOrigin = 0;
                fillImage.fillAmount = 0f;
                fillImage.raycastTarget = false;
                root.SetActive(false); // 평소에는 안 보인다

                var display = ammo.GetComponent<AmmoDisplay>();
                var so = new SerializedObject(display);
                so.FindProperty("reloadBarRoot").objectReferenceValue = root;
                so.FindProperty("reloadFill").objectReferenceValue = fillImage;
                so.ApplyModifiedPropertiesWithoutUndo();

                var status = ammo.Find("StatusText");
                if (status != null)
                    status.gameObject.SetActive(false);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 재장전 진행 막대를 만들었어요.");
            }
            AssetDatabase.SaveAssets();
        }

        // 따뜻한 호박색 알약 모양 채움 (64×8): 위쪽이 밝고 아래가 진하다
        static void MakeFillPng()
        {
            if (File.Exists(FillPng))
                return;
            const int W = 64, H = 8;
            var texture = new Texture2D(W, H, TextureFormat.RGBA32, false);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float qx = Mathf.Abs(x + 0.5f - W / 2f) - (W / 2f - 3.5f);
                    float qy = Mathf.Abs(y + 0.5f - H / 2f) - (H / 2f - 3.5f);
                    float d = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - 3.5f;
                    float a = Mathf.Clamp01(0.5f - d);
                    var c = Color.Lerp(new Color32(0xD4, 0x84, 0x1A, 0xFF), new Color32(0xFF, 0xD0, 0x6A, 0xFF), y / (float)H);
                    texture.SetPixel(x, y, new Color(c.r, c.g, c.b, a));
                }
            texture.Apply();
            File.WriteAllBytes(FillPng, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(FillPng);
            var importer = (TextureImporter)AssetImporter.GetAtPath(FillPng);
            if (importer != null)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }
    }
}
