using LastBullet.Inventory;
using LastBullet.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 전투 맵의 오른쪽 위 전투 가방(RunBagPanel)에 그 맵에서 얻는 물감 3색만 보이게 한다 (2026-10-31 사용자 요청).
    // 미술관은 9색 전부 그대로다. 어느 색이 그 맵의 색인지는 StageColors 표를 따른다 (숲 빨강·주황·노랑 / 성 마을 초록·파랑·남색 / 사막 보라·하양·검정).
    // 다른 색의 아이콘·숫자 오브젝트는 지우지 않고 꺼 둔다 (RunBagUI가 9개 모두에 연결되어 있다). 3색은 왼쪽부터 간격을 맞춰 다시 놓고 패널 폭을 줄인다.
    // 다시 실행해도 같은 결과다. 메뉴: LastBullet > Phase 43 전투 가방에 맵 물감만 보이기
    public static class Phase43BagColorsSetup
    {
        const float FirstX = 40f;
        const float Spacing = 56f;
        const float PanelPadding = 28f;
        const float IconSize = 40f;

        static readonly (string scene, string map)[] Maps =
        {
            ("Assets/Scenes/2_Forest.unity", "2_Forest"),
            ("Assets/Scenes/3_CastleVillage.unity", "3_CastleVillage"),
            ("Assets/Scenes/4_Desert.unity", "4_Desert"),
        };

        [MenuItem("LastBullet/Phase 43 전투 가방에 맵 물감만 보이기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var (path, map) in Maps)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var canvas = ArtSlots.FindCanvas();
                var panel = canvas != null ? canvas.transform.Find("RunBagPanel") : null;
                var colors = StageColors.For(map);
                if (panel == null || colors == null || colors.Count == 0)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: RunBagPanel이나 맵 물감 표가 없어 건너뜀");
                    continue;
                }

                int shown = 0;
                for (int c = 0; c < PaintColors.Count; c++)
                {
                    var color = (PaintColor)c;
                    var icon = panel.Find($"Icon_Paint_{color}");
                    var count = panel.Find($"PaintCount_{color}");
                    int index = IndexOf(colors, color);
                    bool visible = index >= 0;
                    if (icon != null) icon.gameObject.SetActive(visible);
                    if (count != null) count.gameObject.SetActive(visible);
                    if (!visible)
                        continue;
                    float x = FirstX + index * Spacing;
                    Move(icon, x);
                    Move(count, x);
                    shown++;
                }

                // 패널 폭: 마지막 아이콘까지 + 여백 (종이조각 줄도 들어가는 최소 폭)
                var rect = (RectTransform)panel;
                float width = Mathf.Max(FirstX + (shown - 1) * Spacing + IconSize * 0.5f + PanelPadding, 200f);
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 전투 가방에 맵 물감 {shown}색만 보이게 했어요.");
            }
            AssetDatabase.SaveAssets();
        }

        static int IndexOf(System.Collections.Generic.IReadOnlyList<PaintColor> colors, PaintColor color)
        {
            for (int i = 0; i < colors.Count; i++)
                if (colors[i] == color)
                    return i;
            return -1;
        }

        static void Move(Transform t, float x)
        {
            if (t == null)
                return;
            var rect = (RectTransform)t;
            var p = rect.anchoredPosition;
            rect.anchoredPosition = new Vector2(x, p.y);
        }
    }
}
