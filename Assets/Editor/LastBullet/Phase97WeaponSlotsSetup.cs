using System.IO;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 무기 슬롯 표시를 만든다 (2026-11-11 사용자 요청: 지금 어떤 무기가 있는지 화면에서 보이게).
    //   Canvas/WeaponSlots: 전투 화면 왼쪽 아래, 탄약 숫자 위에 칸 두 개(1 소총, 2 바주카). 칸 안에 무기 그림 + 키 번호. 들고 있는 칸은 밝고 테두리 그림이 바뀐다.
    //   그림: Assets/UI/Slot_Back.png (보통, 96×64, 9-slice 모서리 20), Slot_Selected.png (들고 있음, 같은 크기). 없을 때만 만든다 — 같은 이름으로 덮어써서 바꾼다.
    //   무기 그림은 Weapon_Icon.png(소총), Weapon_Bazooka.png(바주카)를 그대로 쓴다.
    // 씬 3개(숲·성 마을·사막). 먼저 Phase 93(바주카)·92(키 번호 판으로 쓰는 Skill_Cooldown.png)가 있어야 한다. 다시 실행하면 새로 만든다. 메뉴: LastBullet > Phase 97 무기 슬롯 표시 만들기
    public static class Phase97WeaponSlotsSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";
        const string BackPng = "Assets/UI/Slot_Back.png";
        const string SelectedPng = "Assets/UI/Slot_Selected.png";
        const string BadgePng = "Assets/UI/Skill_Cooldown.png"; // 흰색 원 (색은 코드가 입힌다, E 스킬 키 번호 판과 같은 것)

        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 97 무기 슬롯 표시 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            MakeSlotPng(BackPng, false);
            MakeSlotPng(SelectedPng, true);
            AssetDatabase.Refresh();
            var back = ArtSlots.LoadSprite(BackPng, 20f);
            var selected = ArtSlots.LoadSprite(SelectedPng, 20f);
            var badge = ArtSlots.LoadSprite(BadgePng);
            var rifleIcon = ArtSlots.LoadSprite("Assets/UI/Weapon_Icon.png");
            var bazookaIcon = ArtSlots.LoadSprite("Assets/UI/Weapon_Bazooka.png");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (back == null || selected == null || rifleIcon == null || bazookaIcon == null || badge == null)
            {
                Debug.LogWarning("[LastBullet] 슬롯 그림·무기 그림·번호 판 그림 중 없는 것이 있어요 (먼저 Phase 92·93).");
                return;
            }

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var canvas = ArtSlots.FindCanvas();
                if (canvas == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: Canvas가 없어 건너뜀");
                    continue;
                }
                var old = canvas.transform.Find("WeaponSlots");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                // 탄약 칸(x 500~860, y 36~146) 바로 위에 칸 두 개. 위에서부터 보이게 맨 앞쪽(마지막 자식)에 둔다.
                var root = new GameObject("WeaponSlots", typeof(RectTransform));
                root.transform.SetParent(canvas.transform, false);
                var rootRect = (RectTransform)root.transform;
                // 2026-11-11 사용자 요청: 탄약 표시(총 그림 + 숫자, x 500~) 바로 오른쪽에, 세로는 탄약 표시 가운데에 맞춘다.
                rootRect.anchorMin = rootRect.anchorMax = new Vector2(0f, 0f);
                rootRect.pivot = new Vector2(0f, 0f);
                rootRect.anchoredPosition = new Vector2(800f, 58f);
                rootRect.sizeDelta = new Vector2(230f, 66f);

                var slots = new WeaponSlotsUI.Slot[2];
                var icons = new[] { rifleIcon, bazookaIcon };
                for (int i = 0; i < 2; i++)
                    slots[i] = MakeSlot(rootRect, i, icons[i], back, badge, font);

                var ui = root.AddComponent<WeaponSlotsUI>();
                var so = new SerializedObject(ui);
                var array = so.FindProperty("slots");
                array.arraySize = 2;
                for (int i = 0; i < 2; i++)
                {
                    var element = array.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("root").objectReferenceValue = slots[i].root;
                    element.FindPropertyRelative("back").objectReferenceValue = slots[i].back;
                    element.FindPropertyRelative("icon").objectReferenceValue = slots[i].icon;
                }
                so.FindProperty("normalBack").objectReferenceValue = back;
                so.FindProperty("selectedBack").objectReferenceValue = selected;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 무기 슬롯 표시를 만들었어요 (1 소총 · 2 바주카).");
            }
            AssetDatabase.SaveAssets();
        }

        static WeaponSlotsUI.Slot MakeSlot(RectTransform parent, int index, Sprite icon, Sprite back, Sprite badge, TMP_FontAsset font)
        {
            var go = new GameObject("Slot_" + (index + 1), typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(104f, 64f);
            rect.anchoredPosition = new Vector2(index * 118f, 0f);
            rect.localScale = Vector3.one;

            var backImage = NewImage("Back", rect, back, Color.white);
            backImage.type = Image.Type.Sliced;
            Stretch(backImage.rectTransform);

            var iconImage = NewImage("Icon", rect, icon, Color.white);
            iconImage.preserveAspect = true;
            var iconRect = iconImage.rectTransform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(10f, 8f);
            iconRect.offsetMax = new Vector2(-10f, -8f);

            // 키 번호: 왼쪽 위 모서리의 작은 둥근 판 + 숫자
            var key = new GameObject("KeyLabel", typeof(RectTransform));
            key.transform.SetParent(rect, false);
            var keyRect = (RectTransform)key.transform;
            keyRect.anchorMin = keyRect.anchorMax = keyRect.pivot = new Vector2(0f, 1f);
            keyRect.anchoredPosition = new Vector2(-8f, 8f);
            keyRect.sizeDelta = new Vector2(30f, 30f);
            var badgeImage = NewImage("Badge", keyRect, badge, new Color32(0x3E, 0x28, 0x18, 0xFF));
            Stretch(badgeImage.rectTransform);
            var text = new GameObject("Number", typeof(RectTransform), typeof(TextMeshProUGUI));
            text.transform.SetParent(keyRect, false);
            var tmp = text.GetComponent<TextMeshProUGUI>();
            tmp.font = font != null ? font : TMP_Settings.defaultFontAsset;
            tmp.text = (index + 1).ToString();
            tmp.fontSize = 22f;
            tmp.color = new Color32(0xF4, 0xEA, 0xD2, 0xFF);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            Stretch(tmp.rectTransform);

            return new WeaponSlotsUI.Slot { root = rect, back = backImage, icon = iconImage };
        }

        static Image NewImage(string name, RectTransform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        // ── 그림: 종이 느낌의 둥근 칸 (잉크 테두리). selected면 더 밝은 종이 + 호박색 테두리 ──
        static void MakeSlotPng(string path, bool selected)
        {
            if (File.Exists(path))
                return;
            const int W = 96, H = 64;
            var texture = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var ink = new Color32(0x3E, 0x28, 0x18, 0xFF);
            var amber = new Color32(0xF2, 0xB1, 0x34, 0xFF);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    // 둥근 사각형 거리 (안쪽이 음수). 가장자리가 손으로 그린 듯 살짝 일렁인다.
                    float d = RoundRect(px, py, W * 0.5f, H * 0.5f, W * 0.5f - 3f, H * 0.5f - 3f, 12f) + (Noise(px * 0.09f, py * 0.09f) - 0.5f) * 1.4f;
                    var col = new Color(0f, 0f, 0f, 0f);
                    if (selected && d < 3.2f)
                        col = Over(col, new Color(amber.r / 255f, amber.g / 255f, amber.b / 255f, Mathf.Clamp01(3.2f - d) * 0.95f)); // 바깥 호박색 띠
                    if (d < 0.9f)
                        col = Over(col, new Color(ink.r / 255f, ink.g / 255f, ink.b / 255f, Mathf.Clamp01(0.9f - d)));
                    if (d < -2.2f)
                    {
                        var top = selected ? new Color32(0xFF, 0xF6, 0xD2, 0xFF) : new Color32(0xE2, 0xD0, 0xA6, 0xFF);
                        var bottom = selected ? new Color32(0xF7, 0xE2, 0xA8, 0xFF) : new Color32(0xCF, 0xBB, 0x8E, 0xFF);
                        var paper = Color.Lerp(bottom, top, py / H);
                        float n = (Noise(px * 0.5f, py * 0.5f) - 0.5f) * 0.05f;
                        paper = new Color(paper.r + n, paper.g + n, paper.b + n, 1f);
                        col = Over(col, new Color(paper.r, paper.g, paper.b, Mathf.Clamp01(-2.2f - d)));
                    }
                    texture.SetPixel(x, y, col);
                }
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
        }

        static float RoundRect(float px, float py, float cx, float cy, float halfW, float halfH, float radius)
        {
            float qx = Mathf.Abs(px - cx) - (halfW - radius);
            float qy = Mathf.Abs(py - cy) - (halfH - radius);
            return Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        static Color Over(Color dst, Color src)
        {
            float oa = src.a + dst.a * (1f - src.a);
            if (oa <= 0f)
                return Color.clear;
            var rgb = (new Vector3(src.r, src.g, src.b) * src.a + new Vector3(dst.r, dst.g, dst.b) * dst.a * (1f - src.a)) / oa;
            return new Color(rgb.x, rgb.y, rgb.z, oa);
        }

        static float Noise(float x, float y)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            return Mathf.Lerp(Mathf.Lerp(Hash(xi, yi), Hash(xi + 1, yi), fx), Mathf.Lerp(Hash(xi, yi + 1), Hash(xi + 1, yi + 1), fx), fy);
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
