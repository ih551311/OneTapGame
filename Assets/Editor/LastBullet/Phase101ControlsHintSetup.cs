using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 전투 시작 조작 안내를 만든다 (2026-11-12: 조작 안내가 없다는 점검 결과). 숲·성 마을·사막의 Canvas/ControlsHint:
    //   화면 아래 가운데에 종이 판(FrameProgress_Panel.png, 9-slice) + 두 줄 글. 처음 세 번 들어올 때 8초 동안 보인다 (ControlsHint).
    // 다시 실행하면 새로 만든다. 메뉴: LastBullet > Phase 101 조작 안내 만들기
    public static class Phase101ControlsHintSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";
        const string PlatePath = "Assets/UI/FrameProgress_Panel.png";

        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 101 조작 안내 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var plate = ArtSlots.LoadSprite(PlatePath, 24f);
            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var canvas = ArtSlots.FindCanvas();
                if (canvas == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: Canvas가 없어 건너뜀");
                    continue;
                }
                var old = canvas.transform.Find("ControlsHint");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                var root = new GameObject("ControlsHint", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(ControlsHint));
                root.transform.SetParent(canvas.transform, false);
                var rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 220f);
                rect.sizeDelta = new Vector2(1500f, 130f);
                var image = root.GetComponent<Image>();
                image.sprite = plate;
                image.type = Image.Type.Sliced;
                image.color = Color.white;
                image.raycastTarget = false;
                root.GetComponent<CanvasGroup>().blocksRaycasts = false;

                var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(root.transform, false);
                var text = go.GetComponent<TextMeshProUGUI>();
                text.font = font != null ? font : TMP_Settings.defaultFontAsset;
                text.text = "이동 WASD · 달리기 Shift · 점프 Space · 구르기 Q · 쏘기 좌클릭 · 조준 우클릭 · 재장전 R\n무기 1 소총 / 2 바주카 / F 바꾸기 · 물감 폭탄 E · 시점 전환 휠 · 메뉴 ESC";
                text.fontSize = 30f;
                text.color = new Color32(0x3E, 0x28, 0x18, 0xFF);
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                var textRect = text.rectTransform;
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = new Vector2(30f, 12f);
                textRect.offsetMax = new Vector2(-30f, -12f);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 전투 시작 조작 안내를 만들었어요.");
            }
        }
    }
}
