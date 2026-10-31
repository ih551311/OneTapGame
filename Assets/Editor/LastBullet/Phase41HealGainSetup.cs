using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 힐로 체력이 찰 때, 체력바에서 늘어난 만큼을 다른 색(연두)으로 1초쯤 보여 주게 한다 (2026-10-31 사용자 요청).
    // 화면 가장자리 번쩍(HealFlash)은 없앤다.
    //   체력바 Fill 바로 뒤에 HealGain(Filled 이미지, 그림 Assets/UI/HealthBar_Gain.png)을 만들고 BattleUI에 연결한다.
    //   Fill과 같은 자리·크기라서 체력바 위치를 옮겨도 따라간다.
    // 다시 실행하면 HealGain을 새로 만든다. 메뉴: LastBullet > Phase 41 체력바 회복분 표시 넣기
    public static class Phase41HealGainSetup
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 41 체력바 회복분 표시 넣기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var canvas = ArtSlots.FindCanvas();
                var battleUI = Object.FindFirstObjectByType<BattleUI>(FindObjectsInactive.Include);
                var bar = FindBar(canvas);
                var fill = bar != null ? bar.Find("Fill") : null;
                if (battleUI == null || fill == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: 체력바나 BattleUI를 찾지 못해 건너뜀");
                    continue;
                }

                // 화면 가장자리 번쩍 제거
                var feedback = canvas.transform.Find("HitFeedback");
                var flash = feedback != null ? feedback.Find("HealFlash") : null;
                if (flash != null)
                    Object.DestroyImmediate(flash.gameObject);

                var old = bar.Find("HealGain");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                var gain = Phase4StageSelectSetup.CreateImage("HealGain", bar, "Assets/UI/HealthBar_Gain.png");
                var rect = (RectTransform)gain.transform;
                var fillRect = (RectTransform)fill;
                rect.anchorMin = fillRect.anchorMin;
                rect.anchorMax = fillRect.anchorMax;
                rect.pivot = fillRect.pivot;
                rect.offsetMin = fillRect.offsetMin;
                rect.offsetMax = fillRect.offsetMax;
                gain.transform.SetSiblingIndex(fill.GetSiblingIndex()); // Fill 바로 뒤(아래)에 그려진다

                var image = gain.GetComponent<Image>();
                image.type = Image.Type.Filled;
                image.fillMethod = Image.FillMethod.Horizontal;
                image.fillOrigin = (int)Image.OriginHorizontal.Left;
                image.preserveAspect = false;
                image.fillAmount = 1f;
                image.enabled = false; // 힐할 때만 보인다

                var so = new SerializedObject(battleUI);
                so.FindProperty("healGainFill").objectReferenceValue = image;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 체력바 회복분 표시(HealGain)를 만들고 가장자리 번쩍을 없앴어요.");
            }
            AssetDatabase.SaveAssets();
        }

        // 체력바는 상태창(HudProfile) 안에 있다 (Phase 33). 예전 위치(Canvas 바로 아래)도 찾는다.
        static Transform FindBar(GameObject canvas)
        {
            if (canvas == null)
                return null;
            var hud = canvas.transform.Find("HudProfile");
            var bar = hud != null ? hud.Find("HealthBar") : null;
            return bar != null ? bar : canvas.transform.Find("HealthBar");
        }
    }
}
