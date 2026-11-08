using System.IO;
using LastBullet.Combat;
using LastBullet.Effects;
using LastBullet.Player;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // E키 스킬 "물감 폭탄"을 만들어 전투 맵 3개에 붙인다 (2026-11-08 사용자 결정: 조준한 곳에 물감을 던져 터뜨리고, 5m 구역 안의 적은 느려지고 지속 피해, 맵 물감 색, 쿨다운 15초).
    //   그림 칸 (이미 있으면 덮어쓰지 않는다): Assets/UI/Skill_PaintBomb.png(아이콘 96×96), Skill_Cooldown.png(쿨다운 막용 흰 원 96×96)
    //   소리 칸 (이미 있으면 덮어쓰지 않는다): Assets/Audio/Combat/Skill_Throw.wav(던지기), Skill_Splat.wav(터지기) — 코드로 합성한 임시 소리
    //   머티리얼: Assets/Materials/PaintBomb.mat (날아가는 덩어리, 색은 코드가 입힌다). 구역 바닥은 Phase 90의 FloorPaint_*.mat 9개를 쓴다 (먼저 Phase 90을 눌러 두었어야 한다).
    //   씬 3개(숲·성 마을·사막): Player에 PaintBombSkill을 붙이고 위 것들을 연결, Canvas/SkillE(아이콘 + 쿨다운 막 + 남은 초 + E 표시, 탄약 오른쪽)를 만든다.
    // 다시 누르면 스킬 컴포넌트와 SkillE를 지우고 새로 만든다 (값을 바꿨다면 인스펙터 값은 기본으로 돌아간다). 메뉴: LastBullet > Phase 92 물감 폭탄 스킬 만들기
    public static class Phase92PaintBombSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";
        const string IconPng = "Assets/UI/Skill_PaintBomb.png";
        const string CooldownPng = "Assets/UI/Skill_Cooldown.png";
        const int SampleRate = 44100;
        static readonly string[] ColorNames = { "Red", "Orange", "Yellow", "Green", "Blue", "Purple", "Pink", "White", "Black" };

        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 92 물감 폭탄 스킬 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            MakeIcon();
            MakeCooldownCircle();
            AssetDatabase.Refresh();
            var iconSprite = ArtSlots.LoadSprite(IconPng);
            var circleSprite = ArtSlots.LoadSprite(CooldownPng);
            var throwSound = EnsureSound("Skill_Throw", MakeThrow);
            var splatSound = EnsureSound("Skill_Splat", MakeSplat);

            var bombMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PaintBomb.mat");
            if (bombMaterial == null)
            {
                bombMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                bombMaterial.SetColor("_BaseColor", Color.white);
                AssetDatabase.CreateAsset(bombMaterial, "Assets/Materials/PaintBomb.mat");
            }
            var puddleMaterials = new Material[9];
            for (int i = 0; i < 9; i++)
            {
                puddleMaterials[i] = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/FloorPaint_{ColorNames[i]}.mat");
                if (puddleMaterials[i] == null)
                {
                    Debug.LogWarning("[LastBullet] FloorPaint 머티리얼이 없어요. 먼저 메뉴 Phase 90을 눌러 주세요.");
                    return;
                }
            }
            var burstPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PaintBurst.prefab")?.GetComponent<PaintBurstEffect>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = GameObject.FindWithTag("Player");
                var canvas = ArtSlots.FindCanvas();
                if (player == null || canvas == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: Player나 Canvas가 없어 건너뜀");
                    continue;
                }

                // 스킬 컴포넌트
                var old = player.GetComponent<PaintBombSkill>();
                if (old != null)
                    Object.DestroyImmediate(old);
                var skill = player.AddComponent<PaintBombSkill>();
                var so = new SerializedObject(skill);
                var array = so.FindProperty("puddleMaterials");
                array.arraySize = 9;
                for (int i = 0; i < 9; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = puddleMaterials[i];
                so.FindProperty("bombMaterial").objectReferenceValue = bombMaterial;
                so.FindProperty("burstPrefab").objectReferenceValue = burstPrefab;
                so.FindProperty("throwSound").objectReferenceValue = throwSound;
                so.FindProperty("splatSound").objectReferenceValue = splatSound;
                so.ApplyModifiedPropertiesWithoutUndo();

                // 쿨다운 표시
                var oldUi = canvas.transform.Find("SkillE");
                if (oldUi != null)
                    Object.DestroyImmediate(oldUi.gameObject);
                var root = new GameObject("SkillE", typeof(RectTransform));
                root.transform.SetParent(canvas.transform, false);
                var rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0f);
                rect.anchoredPosition = new Vector2(790f, 40f); // 탄약 숫자 바로 옆 (2026-11-08 사용자 요청으로 왼쪽으로 옮김)
                rect.sizeDelta = new Vector2(84f, 84f);

                var icon = NewImage("Icon", rect, iconSprite, Color.white);
                var overlay = NewImage("Cooldown", rect, circleSprite, new Color(0f, 0f, 0f, 0.62f));
                overlay.type = Image.Type.Filled;
                overlay.fillMethod = Image.FillMethod.Radial360;
                overlay.fillOrigin = 2;       // 위쪽에서 시작
                overlay.fillClockwise = false; // 남은 만큼 어두운 막이 시계 방향으로 걷힌다
                overlay.fillAmount = 0f;

                var seconds = NewText("Seconds", rect, font, "15", 40f, new Color32(0xF4, 0xEA, 0xD2, 0xFF));
                seconds.rectTransform.anchorMin = Vector2.zero;
                seconds.rectTransform.anchorMax = Vector2.one;
                seconds.rectTransform.offsetMin = seconds.rectTransform.offsetMax = Vector2.zero;
                seconds.enableAutoSizing = false;
                seconds.enabled = false;

                // E 키 표시: 오른쪽 아래의 작은 둥근 판 + 글자
                var key = new GameObject("KeyLabel", typeof(RectTransform));
                key.transform.SetParent(rect, false);
                var keyRect = (RectTransform)key.transform;
                keyRect.anchorMin = keyRect.anchorMax = keyRect.pivot = new Vector2(1f, 0f);
                keyRect.anchoredPosition = new Vector2(8f, -6f);
                keyRect.sizeDelta = new Vector2(34f, 34f);
                var badge = NewImage("Badge", keyRect, circleSprite, new Color32(0x3E, 0x28, 0x18, 0xFF));
                Stretch(badge.rectTransform);
                var keyText = NewText("E", keyRect, font, "E", 24f, new Color32(0xF4, 0xEA, 0xD2, 0xFF));
                Stretch(keyText.rectTransform);

                var ui = root.AddComponent<SkillCooldownUI>();
                var uso = new SerializedObject(ui);
                uso.FindProperty("icon").objectReferenceValue = icon;
                uso.FindProperty("overlay").objectReferenceValue = overlay;
                uso.FindProperty("secondsText").objectReferenceValue = seconds;
                uso.FindProperty("keyLabel").objectReferenceValue = key;
                uso.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 물감 폭탄 스킬과 쿨다운 표시를 만들었어요.");
            }
            AssetDatabase.SaveAssets();
        }

        // ── UI 도우미 ──
        static Image NewImage(string name, RectTransform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            image.raycastTarget = false;
            Stretch(image.rectTransform);
            return image;
        }

        static TextMeshProUGUI NewText(string name, RectTransform parent, TMP_FontAsset font, string value, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            return text;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        // ── 그림 ──
        // 아이콘: 크림색 둥근 판 + 잉크 테두리 + 색색이 터진 물감 (빨강·노랑·파랑 방울 세 개와 튄 점들)
        static void MakeIcon()
        {
            if (File.Exists(IconPng))
                return;
            const int N = 96;
            var texture = new Texture2D(N, N, TextureFormat.RGBA32, false);
            var blobs = new[]
            {
                (x: 40f, y: 54f, r: 16f, c: new Color32(0xD0, 0x3A, 0x2E, 0xFF)),
                (x: 58f, y: 48f, r: 15f, c: new Color32(0xFA, 0xCC, 0x15, 0xFF)),
                (x: 47f, y: 34f, r: 14f, c: new Color32(0x3B, 0x82, 0xF6, 0xFF)),
            };
            var drops = new[] { (x: 24f, y: 30f, r: 3f, i: 0), (x: 70f, y: 66f, r: 3.2f, i: 1), (x: 70f, y: 30f, r: 2.5f, i: 2), (x: 28f, y: 68f, r: 2.6f, i: 2) };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float dc = Mathf.Sqrt((px - 48f) * (px - 48f) + (py - 48f) * (py - 48f));
                    float wob = (Noise(px * 0.12f, py * 0.12f) - 0.5f) * 1.2f;
                    float edge = dc + wob;
                    var col = new Color(0f, 0f, 0f, 0f);
                    if (edge < 46f)
                    {
                        float aOut = Mathf.Clamp01(46f - edge);                       // 바깥 잉크 테두리
                        col = new Color(0x3E / 255f, 0x28 / 255f, 0x18 / 255f, aOut);
                        if (edge < 42.8f)
                        {
                            float k = Mathf.Clamp01(42.8f - edge);
                            var cream = Color.Lerp(new Color32(0xE8, 0xD6, 0xAE, 0xFF), new Color32(0xFB, 0xF3, 0xDC, 0xFF), py / N);
                            float n = (Noise(px * 0.5f, py * 0.5f) - 0.5f) * 0.05f;
                            cream = new Color(cream.r + n, cream.g + n, cream.b + n, 1f);
                            col = Color.Lerp(col, cream, k);
                            col.a = 1f;
                        }
                        // 물감 방울 (잉크 윤곽선 + 색)
                        foreach (var b in blobs)
                        {
                            float d = Mathf.Sqrt((px - b.x) * (px - b.x) + (py - b.y) * (py - b.y)) + (Noise(px * 0.2f + b.x, py * 0.2f) - 0.5f) * 3f - b.r;
                            if (d < 1.4f)
                                col = Over(col, new Color(0x3E / 255f, 0x28 / 255f, 0x18 / 255f, Mathf.Clamp01(1.4f - d)));
                            if (d < 0f)
                                col = Over(col, new Color(b.c.r / 255f, b.c.g / 255f, b.c.b / 255f, Mathf.Clamp01(-d)));
                        }
                        foreach (var d in drops)
                        {
                            float dd = Mathf.Sqrt((px - d.x) * (px - d.x) + (py - d.y) * (py - d.y)) - d.r;
                            var c = blobs[d.i].c;
                            if (dd < 0.8f)
                                col = Over(col, new Color(c.r / 255f, c.g / 255f, c.b / 255f, Mathf.Clamp01(0.8f - dd)));
                        }
                        // 반짝 한 점
                        float hl = Mathf.Sqrt((px - 36f) * (px - 36f) + (py - 60f) * (py - 60f)) - 3f;
                        if (hl < 0.8f)
                            col = Over(col, new Color(1f, 1f, 1f, 0.7f * Mathf.Clamp01(0.8f - hl)));
                    }
                    texture.SetPixel(x, y, col);
                }
            texture.Apply();
            SavePng(texture, IconPng);
        }

        static Color Over(Color dst, Color src)
        {
            float oa = src.a + dst.a * (1f - src.a);
            if (oa <= 0f)
                return Color.clear;
            var rgb = (new Vector3(src.r, src.g, src.b) * src.a + new Vector3(dst.r, dst.g, dst.b) * dst.a * (1f - src.a)) / oa;
            return new Color(rgb.x, rgb.y, rgb.z, oa);
        }

        static void MakeCooldownCircle()
        {
            if (File.Exists(CooldownPng))
                return;
            const int N = 96;
            var texture = new Texture2D(N, N, TextureFormat.RGBA32, false);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - 48f) * (x + 0.5f - 48f) + (y + 0.5f - 48f) * (y + 0.5f - 48f)) - 46f;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d)));
                }
            texture.Apply();
            SavePng(texture, CooldownPng);
        }

        static void SavePng(Texture2D texture, string path)
        {
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

        // ── 소리 ──
        static AudioClip EnsureSound(string name, System.Func<float[]> make)
        {
            string path = $"Assets/Audio/Combat/{name}.wav";
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null)
            {
                WriteWav(path, make());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // 휙 날아가는 소리: 거른 잡음의 높이가 올라갔다 내려온다 (0.3초)
        static float[] MakeThrow()
        {
            var rnd = new System.Random(31);
            int n = (int)(SampleRate * 0.3f);
            var data = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float k = t / 0.3f;
                float cutoff = 0.04f + 0.3f * Mathf.Sin(k * Mathf.PI);
                lp += (((float)rnd.NextDouble() * 2f - 1f) - lp) * cutoff;
                float env = Mathf.Sin(k * Mathf.PI);
                data[i] = lp * env;
            }
            return Normalize(data, 0.55f);
        }

        // 물감이 철퍽 터지는 소리: 낮은 "퐁" + 거른 잡음 + 방울 몇 개의 "톡톡" (0.6초)
        static float[] MakeSplat()
        {
            var rnd = new System.Random(32);
            int n = (int)(SampleRate * 0.6f);
            var data = new float[n];
            float lp = 0f, phase = 0f;
            float[] bloops = { 0.07f, 0.16f, 0.26f };
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float freq = 380f * Mathf.Exp(-t * 9f) + 90f;
                phase += freq / SampleRate;
                float body = Mathf.Sin(6.283f * phase) * Mathf.Exp(-t * 10f);
                lp += (((float)rnd.NextDouble() * 2f - 1f) - lp) * 0.2f;
                float splat = lp * Mathf.Exp(-t * 11f) * (t > 0.012f ? 1f : t / 0.012f);
                float drops = 0f;
                foreach (var b in bloops)
                {
                    float c = t - b;
                    if (c > 0f)
                        drops += Mathf.Sin(6.283f * (600f + 800f * b) * c) * Mathf.Exp(-c * 45f) * 0.35f;
                }
                data[i] = body * 0.8f + splat * 0.9f + drops;
            }
            return Normalize(data, 0.85f);
        }

        static float[] Normalize(float[] data, float peak)
        {
            float max = 0.0001f;
            foreach (var v in data)
                max = Mathf.Max(max, Mathf.Abs(v));
            for (int i = 0; i < data.Length; i++)
                data[i] = data[i] / max * peak;
            return data;
        }

        static void WriteWav(string path, float[] data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(stream))
            {
                int bytes = data.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(SampleRate);
                w.Write(SampleRate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(bytes);
                foreach (var v in data)
                    w.Write((short)(Mathf.Clamp(v, -1f, 1f) * 32767f));
            }
        }
    }
}
