using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 전투 맵 왼쪽 아래 상태창을 스케치 모양으로 바꾼다 (2026-10-31 사용자 요청).
    //   ┌──────┐
    //   │ 사진 │  (오른쪽 위는 비어 있다)
    //   │      ├────────────────┐
    //   │      │ 닉네임          │   닉네임 칸은 사진 아래쪽 높이에 맞춘다
    //   ├──────┴────────────────┤
    //   │ ▓▓▓▓▓▓▓▓░░░░░░░░░░░░░ │   체력바는 아래에 가로로 길게
    //   └───────────────────────┘
    //   - 배경: L자 모양 전용 그림 Assets/UI/Hud_Panel_L.png (440×218, 크기 고정이라 직접 그려서 바꿔 끼우면 된다)
    //   - "잡은 적" 줄은 없앴다 (HudStatus도 뺐다). 얻은 물감은 오른쪽 위 전투 가방에 나온다.
    // 미술관 상태창은 그대로다. 다시 실행해도 같은 결과다. 메뉴: LastBullet > Phase 44 전투 상태창 모양 바꾸기
    public static class Phase44BattleHudShapeSetup
    {
        const float Pad = 14f;
        const float Photo = 150f;
        const float Gap = 12f;
        const float PlateHeight = 50f;
        const float PanelWidth = 440f;

        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 44 전투 상태창 모양 바꾸기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var canvas = ArtSlots.FindCanvas();
                var hud = canvas != null ? canvas.transform.Find("HudProfile") : null;
                var plate = hud != null ? hud.Find("NamePlate") : null;
                if (plate == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: 상태창(HudProfile)을 찾지 못해 건너뜀");
                    continue;
                }

                // 배경: L자 그림 (크기 고정이라 늘리지 않는다)
                var background = hud.GetComponent<Image>();
                background.sprite = ArtSlots.LoadSprite("Assets/UI/Hud_Panel_L.png");
                background.type = Image.Type.Simple;
                background.preserveAspect = false;
                background.color = Color.white;

                // 닉네임 칸: 사진 오른쪽, 사진 아래쪽 높이
                var plateRect = (RectTransform)plate;
                plateRect.anchorMin = plateRect.anchorMax = plateRect.pivot = new Vector2(0f, 1f);
                float x = Pad + Photo + Gap;
                plateRect.anchoredPosition = new Vector2(x, -(Pad + Photo - PlateHeight));
                plateRect.sizeDelta = new Vector2(PanelWidth - x - Pad, PlateHeight);

                var info = plate.Find("Info");
                if (info != null)
                    Object.DestroyImmediate(info.gameObject);
                var status = hud.GetComponent<HudStatus>();
                if (status != null)
                    Object.DestroyImmediate(status);

                var nick = plate.Find("Nickname").GetComponent<TextMeshProUGUI>();
                var nickRect = nick.rectTransform;
                nickRect.anchorMin = Vector2.zero;
                nickRect.anchorMax = Vector2.one;
                nickRect.pivot = new Vector2(0.5f, 0.5f);
                nickRect.offsetMin = new Vector2(16f, 4f);
                nickRect.offsetMax = new Vector2(-16f, -4f);
                nick.alignment = TextAlignmentOptions.Left;
                nick.enableAutoSizing = true;
                nick.fontSizeMin = 18f;
                nick.fontSizeMax = 30f;

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 전투 상태창을 L자 모양(사진·닉네임·체력바)으로 바꿨어요.");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
