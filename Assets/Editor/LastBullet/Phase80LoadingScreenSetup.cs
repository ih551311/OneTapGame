using System.IO;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 미술관에서 전투 장소를 고를 때 뜨는 로딩 화면을 만든다 (2026-11-06 사용자 요청).
    //   그림 칸 (이미 있으면 덮어쓰지 않는다): Assets/UI/Loading_Bg.png(배경) · Loading_Frame.png(내 그림 액자, 9-slice) ·
    //     Loading_JokePanel.png(아재개그 종이 판, 9-slice) · Loading_BarBg.png(막대 바닥, 9-slice) · Loading_BarFill.png(막대 채움)
    //   씬 1_Museum: Canvas/LoadingScreen (화면 전체 크기, 맨 위) 아래에 배경, 내 그림 액자, 맵 이름, 얻는 물감 3색, 아재개그 판, 불러오는 막대를 만든다.
    //   StageSelectPopup이 LoadingScreen.Load(씬 이름)을 부른다.
    // 이미 만들었으면 지우고 다시 만든다. 메뉴: LastBullet > Phase 80 로딩 화면 만들기
    public static class Phase80LoadingScreenSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";
        static readonly Color Cream = new Color32(0xEE, 0xE6, 0xDA, 0xFF);
        static readonly Color CreamDim = new Color32(0xC9, 0xB9, 0xA0, 0xFF);
        static readonly Color Brown = new Color32(0x4A, 0x31, 0x1E, 0xFF);
        static readonly Color Accent = new Color32(0x9A, 0x3B, 0x1C, 0xFF);

        [MenuItem("LastBullet/Phase 80 로딩 화면 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            MakePngs();
            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var canvas = ArtSlots.FindCanvas();
            if (canvas == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: Canvas가 없어 건너뜀");
                return;
            }
            var old = canvas.transform.Find("LoadingScreen");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            var root = NewRect("LoadingScreen", canvas.transform);
            Stretch(root);
            root.SetAsLastSibling(); // 다른 UI와 색 번짐 전환 위에 그린다

            // 배경 (화면 전체)
            var bg = NewImage("Background", root, "Assets/UI/Loading_Bg.png", 0f);
            Stretch(bg.rectTransform);
            bg.preserveAspect = false;

            // 내 그림 액자 (살짝 기울여 벽에서 내려 놓은 사진처럼)
            var frame = NewImage("PaintingFrame", root, "Assets/UI/Loading_Frame.png", 20f);
            frame.type = Image.Type.Sliced;
            Place(frame.rectTransform, new Vector2(-520f, 160f), new Vector2(520f, 560f));
            frame.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -3f);
            var painting = NewImage("Painting", frame.rectTransform, "Assets/UI/Profile_Empty.png", 0f);
            Place(painting.rectTransform, new Vector2(0f, 32f), new Vector2(440f, 440f));
            var label = NewText("PaintingLabel", frame.rectTransform, font, "내 그림", 34f, Brown, TextAlignmentOptions.Center);
            Place(label.rectTransform, new Vector2(0f, -232f), new Vector2(440f, 56f));

            // 맵 이름
            var mapName = NewText("MapName", root, font, "숲", 120f, Cream, TextAlignmentOptions.Center);
            Place(mapName.rectTransform, new Vector2(330f, 250f), new Vector2(900f, 170f));

            // 얻는 물감 (작게)
            var colorTitle = NewText("ColorsTitle", root, font, "여기서 얻는 물감", 32f, CreamDim, TextAlignmentOptions.Center);
            Place(colorTitle.rectTransform, new Vector2(330f, 120f), new Vector2(700f, 50f));
            var icons = new Image[3];
            var names = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                float x = 330f + (i - 1) * 230f;
                icons[i] = NewImage($"ColorIcon_{i}", root, "Assets/UI/Icon_Paint_Red.png", 0f);
                Place(icons[i].rectTransform, new Vector2(x, 30f), new Vector2(96f, 96f));
                names[i] = NewText($"ColorName_{i}", root, font, "빨강", 34f, Cream, TextAlignmentOptions.Center);
                Place(names[i].rectTransform, new Vector2(x, -50f), new Vector2(200f, 50f));
            }

            // 아재개그 판
            var jokePanel = NewImage("JokePanel", root, "Assets/UI/Loading_JokePanel.png", 24f);
            jokePanel.type = Image.Type.Sliced;
            Place(jokePanel.rectTransform, new Vector2(0f, -300f), new Vector2(1500f, 250f));
            var question = NewText("JokeQuestion", jokePanel.rectTransform, font, "Q. 아재개그", 46f, Brown, TextAlignmentOptions.Center);
            Place(question.rectTransform, new Vector2(0f, 45f), new Vector2(1380f, 90f));
            var answer = NewText("JokeAnswer", jokePanel.rectTransform, font, "A. 답", 58f, Accent, TextAlignmentOptions.Center);
            Place(answer.rectTransform, new Vector2(0f, -45f), new Vector2(1380f, 100f));
            answer.gameObject.SetActive(false);

            // 불러오는 막대
            var loadingText = NewText("LoadingText", root, font, "불러오는 중...", 30f, CreamDim, TextAlignmentOptions.Left);
            Place(loadingText.rectTransform, new Vector2(-380f, -470f), new Vector2(500f, 44f));
            var barBg = NewImage("BarBackground", root, "Assets/UI/Loading_BarBg.png", 12f);
            barBg.type = Image.Type.Sliced;
            Place(barBg.rectTransform, new Vector2(0f, -510f), new Vector2(1500f, 36f));
            var fill = NewImage("BarFill", barBg.rectTransform, "Assets/UI/Loading_BarFill.png", 0f);
            fill.preserveAspect = false;
            Stretch(fill.rectTransform);
            fill.rectTransform.offsetMin = new Vector2(5f, 5f);
            fill.rectTransform.offsetMax = new Vector2(-5f, -5f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;

            var component = root.gameObject.AddComponent<LoadingScreen>();
            var so = new SerializedObject(component);
            so.FindProperty("paintingFrame").objectReferenceValue = frame.gameObject;
            so.FindProperty("paintingImage").objectReferenceValue = painting;
            so.FindProperty("emptyPainting").objectReferenceValue = ArtSlots.LoadSprite("Assets/UI/Profile_Empty.png");
            so.FindProperty("mapName").objectReferenceValue = mapName;
            SetArray(so.FindProperty("colorIcons"), icons);
            SetArray(so.FindProperty("colorNames"), names);
            so.FindProperty("jokeQuestion").objectReferenceValue = question;
            so.FindProperty("jokeAnswer").objectReferenceValue = answer;
            so.FindProperty("barFill").objectReferenceValue = fill;
            so.FindProperty("loadingText").objectReferenceValue = loadingText;
            string[] colorNames9 = { "Red", "Orange", "Yellow", "Green", "Blue", "Purple", "Pink", "White", "Black" };
            var iconArray = so.FindProperty("paintIcons");
            iconArray.arraySize = colorNames9.Length;
            for (int i = 0; i < colorNames9.Length; i++)
                iconArray.GetArrayElementAtIndex(i).objectReferenceValue = ArtSlots.LoadSprite($"Assets/UI/Icon_Paint_{colorNames9[i]}.png");
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] 1_Museum: 로딩 화면(Canvas/LoadingScreen)을 만들었어요.");
        }

        // ── UI 만들기 도우미 ──
        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static Image NewImage(string name, Transform parent, string spritePath, float border)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = ArtSlots.LoadSprite(spritePath, border);
            image.color = Color.white; // 그림 색 그대로
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        static TMP_Text NewText(string name, Transform parent, TMP_FontAsset font, string value, float size, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        // 화면 한가운데 기준 위치와 크기
        static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static void SetArray(SerializedProperty array, Object[] items)
        {
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        // ── 임시 그림 만들기 (이미 있으면 사용자가 바꾼 것이라 그대로 둔다) ──
        static void MakePngs()
        {
            Make("Assets/UI/Loading_Bg.png", 512, 288, Background);
            Make("Assets/UI/Loading_Frame.png", 128, 128, Frame);
            if (!File.Exists("Assets/UI/Loading_JokePanel.png") && File.Exists("Assets/UI/Hud_Panel.png"))
                File.Copy("Assets/UI/Hud_Panel.png", "Assets/UI/Loading_JokePanel.png"); // 종이 패널과 같은 느낌
            Make("Assets/UI/Loading_BarBg.png", 128, 36, BarBackground);
            Make("Assets/UI/Loading_BarFill.png", 256, 36, BarFillColor);
        }

        static void Make(string path, int width, int height, System.Func<int, int, int, int, Color> pixel)
        {
            if (File.Exists(path))
                return;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    texture.SetPixel(x, y, pixel(x, y, width, height));
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        // 어두운 갈색 종이 + 가장자리 어둡게(비네팅) + 종이 결 + 은은한 물감 번짐 얼룩
        static Color Background(int x, int y, int w, int h)
        {
            float u = x / (float)w, v = y / (float)h;
            float dx = (u - 0.5f) * 1.6f, dy = (v - 0.5f) * 1.1f;
            float vignette = 1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy)) * 0.75f;
            var baseColor = new Color(0.20f, 0.14f, 0.10f);
            var color = baseColor * (0.55f + 0.7f * vignette);
            float grain = (Noise(x * 0.8f, y * 0.8f) - 0.5f) * 0.05f + (Noise(x * 0.07f, y * 0.07f) - 0.5f) * 0.06f;
            color += new Color(grain, grain, grain * 0.8f, 0f);
            // 은은한 물감 얼룩 (붉은 기·푸른 기)
            float warm = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.18f, 0.8f)) * 3.2f);
            float cool = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.85f, 0.25f)) * 3.2f);
            color += new Color(0.12f, 0.03f, 0.0f, 0f) * warm * warm + new Color(0.0f, 0.04f, 0.10f, 0f) * cool * cool;
            color.a = 1f;
            return color;
        }

        // 크림색 종이 액자 (둥근 모서리, 가장자리 짙게, 그림자 없음)
        static Color Frame(int x, int y, int w, int h)
        {
            float half = w * 0.5f;
            float dx = Mathf.Abs(x + 0.5f - half) - (half - 3f - 14f);
            float dy = Mathf.Abs(y + 0.5f - half) - (half - 3f - 14f);
            float outside = Mathf.Sqrt(Mathf.Max(dx, 0f) * Mathf.Max(dx, 0f) + Mathf.Max(dy, 0f) * Mathf.Max(dy, 0f));
            float d = 14f - (outside + Mathf.Min(Mathf.Max(dx, dy), 0f));
            float edgeDist = d + (Noise(x * 0.2f, y * 0.2f) - 0.5f) * 2.2f;
            float alpha = Mathf.Clamp01(edgeDist + 0.5f);
            var paper = new Color(0.96f, 0.92f, 0.82f);
            var edge = new Color(0.75f, 0.63f, 0.45f);
            float grain = (Noise(x * 0.9f, y * 0.3f) - 0.5f) * 0.04f;
            var color = paper + new Color(grain, grain, grain, 0f);
            float rim = Mathf.Clamp01(1f - edgeDist / 5f);
            color = Color.Lerp(color, edge, rim * rim);
            color.a = alpha;
            return color;
        }

        // 막대 바닥: 어두운 가죽색 둥근 막대 + 안쪽 테두리
        static Color BarBackground(int x, int y, int w, int h)
        {
            float r = h * 0.5f;
            float cx = Mathf.Clamp(x + 0.5f, r, w - r);
            float d = r - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, r));
            float alpha = Mathf.Clamp01(d + 0.5f);
            var dark = new Color(0.11f, 0.08f, 0.06f);
            var rim = new Color(0.55f, 0.42f, 0.28f);
            var color = Color.Lerp(dark, rim, Mathf.Clamp01(1f - d / 2.5f));
            color.a = alpha;
            return color;
        }

        // 막대 채움: 따뜻한 주황 + 비스듬한 줄무늬 (첨부 그림의 막대 느낌)
        static Color BarFillColor(int x, int y, int w, int h)
        {
            float stripe = Mathf.Repeat((x + y) / 14f, 1f);
            var a = new Color(0.98f, 0.62f, 0.12f);
            var b = new Color(0.90f, 0.45f, 0.08f);
            var color = Color.Lerp(a, b, stripe < 0.5f ? 0f : 1f);
            float shade = 1f - Mathf.Abs(y / (float)h - 0.5f) * 0.4f;
            color *= shade;
            color.a = 1f;
            return color;
        }

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
