using System.IO;
using LastBullet.Stage;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 첫 입장 길 안내를 만든다 (2026-11-11 사용자 요청: 튜토리얼 느낌으로 부모님 편지를 보고, 할아버지 일기장 쪽으로 이동하라고 바닥에 표시).
    //   그림: Assets/UI/Guide_Arrow.png (호박색 겹화살표 128×128, 위쪽이 가는 방향), Guide_Ring.png (호박색 고리 256×256) — 없을 때만 만든다. 같은 이름으로 덮어써서 바꾼다.
    //   머티리얼: Assets/Materials/FloorGuide_Arrow.mat, FloorGuide_Ring.mat (URP Unlit 투명, 그림 + 색은 코드가 입힌다).
    //   오브젝트: 씬 맨 위 TutorialGuide(스크립트), Canvas/GuideHint(화면 위 안내 글: 종이 판 + 글자).
    //   결과: 편지를 읽고 닫으면 바닥에 화살표가 일기장 책상 쪽으로 흐르고 책상 앞에 고리가 뜨며, 일기장을 열면 사라진다.
    // 먼저 Phase 76(일기장 책상)·98(부모님 편지)이 있어야 한다. 다시 실행하면 새로 만든다. 메뉴: LastBullet > Phase 99 첫 입장 길 안내 만들기
    public static class Phase99TutorialGuideSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";
        const string ArrowPng = "Assets/UI/Guide_Arrow.png";
        const string RingPng = "Assets/UI/Guide_Ring.png";
        const string PlatePath = "Assets/UI/FrameProgress_Panel.png";

        [MenuItem("LastBullet/Phase 99 첫 입장 길 안내 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var canvas = ArtSlots.FindCanvas();
            var desk = GameObject.Find("GrandpaDesk");
            var diary = canvas != null ? canvas.transform.Find("DiaryPopup") : null;
            var mailbox = GameObject.Find("Mailbox");
            var letter = canvas != null ? canvas.transform.Find("StoryPopup") : null;
            if (canvas == null || desk == null || diary == null || mailbox == null || letter == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: 일기장 책상(GrandpaDesk)·일기장 창(DiaryPopup)·우체통(Mailbox)·편지 창(StoryPopup)·Canvas 중 없는 것이 있어 건너뜀 (먼저 Phase 76·100).");
                return;
            }

            MakeArrowPng();
            MakeRingPng();
            AssetDatabase.Refresh();
            var arrowMaterial = GetMaterial("FloorGuide_Arrow", ArrowPng);
            var ringMaterial = GetMaterial("FloorGuide_Ring", RingPng);

            var old = GameObject.Find("TutorialGuide");
            if (old != null)
                Object.DestroyImmediate(old);
            var oldHint = canvas.transform.Find("GuideHint");
            if (oldHint != null)
                Object.DestroyImmediate(oldHint.gameObject);

            // 화면 위 안내 글: 종이 판(진행도 표시와 같은 판 그림) + 글자
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var hint = new GameObject("GuideHint", typeof(RectTransform), typeof(Image));
            hint.transform.SetParent(canvas.transform, false);
            var hintRect = (RectTransform)hint.transform;
            hintRect.anchorMin = hintRect.anchorMax = hintRect.pivot = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, -40f);
            hintRect.sizeDelta = new Vector2(1240f, 84f); // 가장 긴 안내 글(종이 합성대 단계)이 한 줄에 들어가게
            var plate = hint.GetComponent<Image>();
            plate.sprite = ArtSlots.LoadSprite(PlatePath, 24f);
            plate.type = Image.Type.Sliced;
            plate.color = Color.white;
            plate.raycastTarget = false;
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(hint.transform, false);
            var text = textGo.GetComponent<TextMeshProUGUI>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.text = "바닥의 화살표를 따라 할아버지의 일기장으로 가 보세요";
            text.fontSize = 34f;
            text.color = new Color32(0x3E, 0x28, 0x18, 0xFF);
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            hint.SetActive(false);

            // ── 단계 목록: 우체통 → 일기장 → 지도 탁자 → 종이 합성대 → 그림 작업대 → 액자 ──
            // 서 있을 자리: 우체통은 앞쪽 1.3m, 일기장 책상은 전시장 쪽(월드 +X) 2m, 나머지는 입구 쪽(월드 -Z) 1.8m.
            var steps = new System.Collections.Generic.List<(string id, PopupBase popup, Vector3 stand, string hint, TutorialGuide.StartCondition when)>();
            steps.Add(("mailbox", letter.GetComponent<PopupBase>(), mailbox.transform.position + mailbox.transform.forward * 1.3f,
                "바닥의 화살표를 따라 우체통으로 가 보세요", TutorialGuide.StartCondition.Always));
            steps.Add(("diary", diary.GetComponent<PopupBase>(), desk.transform.position + new Vector3(2f, 0f, 0f),
                "이번에는 할아버지의 일기장으로 가 보세요", TutorialGuide.StartCondition.Always));
            AddDeviceStep(steps, "map", "StageMap", "지도 탁자에서 거울 너머로 갈 곳을 골라 보세요", TutorialGuide.StartCondition.Always);
            AddDeviceStep(steps, "craft", "PaperCraftTable", "종이조각이 모였어요. 종이 합성대에서 그림 종이를 만들어 보세요", TutorialGuide.StartCondition.HasScrapsForPaper);
            AddDeviceStep(steps, "paint", "PaintWorkbench", "그림 작업대에서 그림을 그려 보세요", TutorialGuide.StartCondition.HasPaper);
            var galleryManager = Object.FindFirstObjectByType<GalleryManager>();
            var frame5 = GameObject.Find("PictureFrame_05");
            if (galleryManager != null && frame5 != null)
            {
                var picker = new SerializedObject(galleryManager).FindProperty("picker").objectReferenceValue as PopupBase;
                steps.Add(("frame", picker, frame5.transform.position + new Vector3(0f, 0f, -1.8f),
                    "다 그렸다면 액자 앞에서 E로 걸어 보세요", TutorialGuide.StartCondition.HasPainting));
            }

            var go = new GameObject("TutorialGuide");
            var guide = go.AddComponent<TutorialGuide>();
            var so = new SerializedObject(guide);
            var array = so.FindProperty("steps");
            array.arraySize = steps.Count;
            for (int i = 0; i < steps.Count; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("id").stringValue = steps[i].id;
                element.FindPropertyRelative("popup").objectReferenceValue = steps[i].popup;
                element.FindPropertyRelative("stand").vector3Value = steps[i].stand;
                element.FindPropertyRelative("hint").stringValue = steps[i].hint;
                element.FindPropertyRelative("startWhen").enumValueIndex = (int)steps[i].when;
            }
            so.FindProperty("arrowMaterial").objectReferenceValue = arrowMaterial;
            so.FindProperty("ringMaterial").objectReferenceValue = ringMaterial;
            so.FindProperty("hint").objectReferenceValue = hint;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] 1_Museum: 길 안내({steps.Count}단계: 우체통 → 일기장 → 지도 탁자 → 종이 합성대 → 그림 작업대 → 액자)를 만들었어요.");
        }

        // 미술관의 장치(InteractDevice) 이름으로 창과 위치를 찾아 단계에 더한다
        static void AddDeviceStep(System.Collections.Generic.List<(string id, PopupBase popup, Vector3 stand, string hint, TutorialGuide.StartCondition when)> steps,
            string id, string objectName, string hintText, TutorialGuide.StartCondition when)
        {
            var device = GameObject.Find(objectName);
            if (device == null)
            {
                Debug.LogWarning($"[LastBullet] 길 안내: {objectName} 오브젝트가 없어 '{id}' 단계를 건너뜀");
                return;
            }
            var popup = new SerializedObject(device.GetComponent<InteractDevice>()).FindProperty("popup").objectReferenceValue as PopupBase;
            steps.Add((id, popup, device.transform.position + new Vector3(0f, 0f, -1.8f), hintText, when));
        }

        static Material GetMaterial(string name, string pngPath)
        {
            string path = $"Assets/Materials/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent + 6;
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath));
            material.SetColor("_BaseColor", new Color(0.95f, 0.69f, 0.2f, 1f));
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ── 그림 ──
        // 겹화살표 ">>" (위를 가리킨다): 굵은 선분 두 쌍. 가장자리는 부드럽게, 안쪽은 흰색(색은 코드가 입힌다).
        static void MakeArrowPng()
        {
            if (File.Exists(ArrowPng))
                return;
            const int N = 128;
            var texture = new Texture2D(N, N, TextureFormat.RGBA32, false);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = 999f;
                    foreach (float apexY in new[] { 108f, 70f })
                    {
                        d = Mathf.Min(d, SegmentDistance(p, new Vector2(64f, apexY), new Vector2(18f, apexY - 46f)) - 9f);
                        d = Mathf.Min(d, SegmentDistance(p, new Vector2(64f, apexY), new Vector2(110f, apexY - 46f)) - 9f);
                    }
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d)));
                }
            texture.Apply();
            SavePng(texture, ArrowPng);
        }

        // 부드러운 고리: 가운데가 비고 가장자리가 옅어진다
        static void MakeRingPng()
        {
            if (File.Exists(RingPng))
                return;
            const int N = 256;
            var texture = new Texture2D(N, N, TextureFormat.RGBA32, false);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float ring = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.74f, 0.88f, r)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.9f, 0.98f, r)));
                    float glow = Mathf.Clamp01(1f - r) * 0.22f; // 안쪽을 아주 옅게 채운다
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(ring + glow)));
                }
            texture.Apply();
            SavePng(texture, RingPng);
        }

        static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).magnitude;
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
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }
    }
}
