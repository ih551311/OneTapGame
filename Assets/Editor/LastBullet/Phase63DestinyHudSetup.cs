using System.IO;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 데스티니식 전투 화면 만들기 (2026-11-04 사용자 결정 — B단계).
    //   적 프리팹: 체력(maxHealth)과 이름(displayName)을 정하고, 맞아도 멈칫하지 않게 hitStunTime을 0으로 한다.
    //   그림 칸(Assets/UI): HitMarker.png(힛마커 ×), EnemyBar_Back.png / EnemyBar_Fill.png(적 체력바), Weapon_Icon.png(총 아이콘).
    //                       이미 있는 그림은 덮어쓰지 않는다 (사용자가 그린 것을 지키려고).
    //   Canvas(4개 씬): HitMarker, DamageNumbers, EnemyHealthBars를 만들고, 조준점(Crosshair)에 퍼짐(ReticleSpread)을 붙이고,
    //                   탄약 표시(AmmoDisplay)를 왼쪽 아래(프로필 옆)로 옮겨 총 아이콘 + 큰 숫자 + ∞ 모양으로 다시 만든다.
    // 이미 만들었으면 지우고 다시 만든다. 메뉴: LastBullet > Phase 63 데스티니식 전투 화면 만들기
    public static class Phase63DestinyHudSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";

        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        // 프리팹, 이름, 체력
        static readonly (string path, string name, float hp)[] Enemies =
        {
            ("Assets/Prefabs/Enemy_Charger.prefab", "돌진병", 60f),
            ("Assets/Prefabs/Enemy_Thrower.prefab", "투척병", 40f),
            ("Assets/Prefabs/Enemy_Splitter.prefab", "분열체", 140f),
            ("Assets/Prefabs/Enemy_SplitterMini.prefab", "작은 분열체", 30f),
        };

        [MenuItem("LastBullet/Phase 63 데스티니식 전투 화면 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            SetupEnemyPrefabs();
            CreatePngs();
            AssetDatabase.Refresh();

            var hitSprite = ArtSlots.LoadSprite("Assets/UI/HitMarker.png");
            var backSprite = ArtSlots.LoadSprite("Assets/UI/EnemyBar_Back.png");
            var fillSprite = ArtSlots.LoadSprite("Assets/UI/EnemyBar_Fill.png");
            var iconSprite = ArtSlots.LoadSprite("Assets/UI/Weapon_Icon.png");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var canvas = ArtSlots.FindCanvas();
                if (canvas == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: Canvas가 없어 건너뜀");
                    continue;
                }
                var parent = canvas.transform;

                // 힛마커 (조준점 가운데)
                Remove(parent, "HitMarker");
                var hit = NewImage("HitMarker", parent, hitSprite);
                var hitRect = (RectTransform)hit.transform;
                hitRect.anchorMin = hitRect.anchorMax = hitRect.pivot = new Vector2(0.5f, 0.5f);
                hitRect.anchoredPosition = Vector2.zero;
                hitRect.sizeDelta = new Vector2(64f, 64f);
                hit.enabled = false;
                var marker = hit.gameObject.AddComponent<HitMarker>();
                Set(marker, "image", hit);

                // 데미지 숫자, 적 체력바 (화면 전체 크기의 빈 오브젝트)
                Remove(parent, "DamageNumbers");
                var numbers = NewFullScreen("DamageNumbers", parent);
                Set(numbers.AddComponent<DamageNumbers>(), "font", font);

                Remove(parent, "EnemyHealthBars");
                var bars = NewFullScreen("EnemyHealthBars", parent);
                var barsScript = bars.AddComponent<EnemyHealthBars>();
                Set(barsScript, "font", font);
                Set(barsScript, "backSprite", backSprite);
                Set(barsScript, "fillSprite", fillSprite);

                // 조준점 퍼짐
                var crosshair = parent.Find("Crosshair");
                if (crosshair != null && crosshair.GetComponent<ReticleSpread>() == null)
                    crosshair.gameObject.AddComponent<ReticleSpread>();

                // 탄약 표시: 왼쪽 아래
                BuildAmmoDisplay(parent, iconSprite, font);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 데스티니식 전투 화면을 만들었어요.");
            }
            AssetDatabase.SaveAssets();
        }

        static void SetupEnemyPrefabs()
        {
            foreach (var e in Enemies)
            {
                var root = PrefabUtility.LoadPrefabContents(e.path);
                var health = root.GetComponent<LastBullet.Enemy.EnemyHealth>();
                if (health != null)
                {
                    var so = new SerializedObject(health);
                    so.FindProperty("maxHealth").floatValue = e.hp;
                    so.FindProperty("displayName").stringValue = e.name;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                    Debug.LogWarning($"[LastBullet] {e.path}에 EnemyHealth가 없어요.");

                foreach (var ai in root.GetComponents<MonoBehaviour>())
                {
                    var so = new SerializedObject(ai);
                    var stun = so.FindProperty("hitStunTime");
                    if (stun != null)
                    {
                        stun.floatValue = 0f;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, e.path);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------- PNG (이미 있으면 건너뜀) ----------

        static void CreatePngs()
        {
            WritePng("Assets/UI/HitMarker.png", 64, 64, (x, y) =>
            {
                float dx = Mathf.Abs(x - 31.5f), dy = Mathf.Abs(y - 31.5f);
                float far = Mathf.Max(dx, dy);
                bool diagonal = Mathf.Abs(dx - dy) <= 1.8f && far >= 9f && far <= 26f;
                return diagonal ? Color.white : Color.clear;
            });
            WritePng("Assets/UI/EnemyBar_Back.png", 64, 8, (x, y) =>
            {
                bool edge = x == 0 || y == 0 || x == 63 || y == 7;
                return edge ? new Color(0f, 0f, 0f, 0.9f) : new Color(0.05f, 0.05f, 0.05f, 0.65f);
            });
            WritePng("Assets/UI/EnemyBar_Fill.png", 64, 8, (x, y) =>
            {
                bool edge = x == 0 || y == 0 || x == 63 || y == 7;
                return edge ? Color.clear : Color.Lerp(new Color(0.75f, 0.12f, 0.12f), new Color(0.95f, 0.3f, 0.25f), y / 7f);
            });
            // 총 아이콘: 소총 옆모습 (간단한 도형)
            WritePng("Assets/UI/Weapon_Icon.png", 128, 48, (x, y) =>
            {
                bool body = x >= 28 && x <= 100 && y >= 22 && y <= 32;
                bool barrel = x >= 100 && x <= 124 && y >= 26 && y <= 30;
                bool stock = x >= 6 && x <= 30 && y >= 18 && y <= 32;
                bool grip = x >= 44 && x <= 52 && y >= 8 && y <= 22;
                bool magazine = x >= 62 && x <= 74 && y >= 6 && y <= 22;
                bool sight = x >= 80 && x <= 90 && y >= 32 && y <= 36;
                return body || barrel || stock || grip || magazine || sight ? Color.white : Color.clear;
            });
        }

        static void WritePng(string path, int width, int height, System.Func<int, int, Color> pixel)
        {
            if (File.Exists(path))
                return;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    texture.SetPixel(x, y, pixel(x, y));
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
        }

        // ---------- 캔버스 오브젝트 ----------

        static void BuildAmmoDisplay(Transform canvas, Sprite icon, TMP_FontAsset font)
        {
            Remove(canvas, "AmmoDisplay");
            var root = new GameObject("AmmoDisplay", typeof(RectTransform));
            root.transform.SetParent(canvas, false);
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(500f, 36f);
            rect.sizeDelta = new Vector2(360f, 110f);

            var iconImage = NewImage("WeaponIcon", root.transform, icon);
            iconImage.color = new Color(1f, 1f, 1f, 0.9f);
            var iconRect = (RectTransform)iconImage.transform;
            iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 0f);
            iconRect.sizeDelta = new Vector2(120f, 45f);

            var ammo = NewText("AmmoText", root.transform, font, 76f, TextAlignmentOptions.MidlineLeft, new Color(0.95f, 0.95f, 0.95f));
            var ammoRect = ammo.rectTransform;
            ammoRect.anchorMin = new Vector2(0f, 0f);
            ammoRect.anchorMax = new Vector2(1f, 1f);
            ammoRect.offsetMin = new Vector2(135f, 0f);
            ammoRect.offsetMax = Vector2.zero;
            ammo.text = "30";

            var status = NewText("StatusText", root.transform, font, 22f, TextAlignmentOptions.TopLeft, new Color(1f, 0.85f, 0.4f));
            var statusRect = status.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0f, 0f);
            statusRect.anchoredPosition = new Vector2(135f, 0f);
            statusRect.sizeDelta = new Vector2(0f, 30f);
            status.text = "";
            status.enabled = false;

            var display = root.AddComponent<AmmoDisplay>();
            Set(display, "ammoText", ammo);
            Set(display, "statusText", status);
        }

        static void Remove(Transform parent, string name)
        {
            var old = parent.Find(name);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
        }

        static Image NewImage(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white; // 흰색 = 그림 색 그대로
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        static GameObject NewFullScreen(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return go;
        }

        static TextMeshProUGUI NewText(string name, Transform parent, TMP_FontAsset font, float size, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
