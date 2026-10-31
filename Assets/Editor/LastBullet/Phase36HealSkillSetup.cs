using LastBullet.Player;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 전투 맵(숲·성 마을·사막)에 힐 스킬을 만든다 (2026-10-31 사용자 요청).
    //   Player에 HealSkill (F키, 체력 30 회복, 대기 30초)
    //   Canvas/HealSkill: 화면 오른쪽 아래 칸 (Assets/UI/Skill_Heal.png) + 대기 막(Skill_Heal_Cool.png, 시계 방향으로 걷힘) + 남은 초 + 키 글자 "F"
    // 다시 실행하면 칸을 지우고 새로 만든다. 값(회복량, 대기시간)은 Player의 HealSkill에서 고친다.
    // 메뉴: LastBullet > Phase 36 힐 스킬 만들기
    public static class Phase36HealSkillSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";

        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 36 힐 스킬 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
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
                if (player.GetComponent<HealSkill>() == null)
                    player.AddComponent<HealSkill>();

                var old = canvas.transform.Find("HealSkill");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                var root = Phase4StageSelectSetup.CreateImage("HealSkill", canvas.transform, "Assets/UI/Skill_Heal.png");
                Phase4StageSelectSetup.SetRect(root, new Vector2(1f, 0f), new Vector2(-90f, 90f), new Vector2(100f, 100f));

                var cool = Phase4StageSelectSetup.CreateImage("Cooldown", root.transform, "Assets/UI/Skill_Heal_Cool.png");
                Phase4StageSelectSetup.Stretch(cool.GetComponent<RectTransform>());
                var fill = cool.GetComponent<Image>();
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Radial360;
                fill.fillOrigin = (int)Image.Origin360.Top;
                fill.fillClockwise = false; // 남은 만큼만 어둡게 남는다
                fill.fillAmount = 0f;

                var seconds = Text("Seconds", root.transform, 40f, new Color32(0xFF, 0xFF, 0xFF, 0xFF), TextAlignmentOptions.Center);
                Phase4StageSelectSetup.Stretch(seconds.rectTransform);

                var key = Text("Key", root.transform, 22f, new Color32(0xEE, 0xE6, 0xDA, 0xFF), TextAlignmentOptions.BottomRight);
                Phase4StageSelectSetup.Stretch(key.rectTransform);
                key.rectTransform.offsetMin = new Vector2(6f, 4f);
                key.rectTransform.offsetMax = new Vector2(-8f, -4f);
                key.text = "F";

                var icon = root.AddComponent<HealSkillIcon>();
                var so = new SerializedObject(icon);
                so.FindProperty("cooldownFill").objectReferenceValue = fill;
                so.FindProperty("secondsText").objectReferenceValue = seconds;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 힐 스킬을 만들었어요 (F키).");
            }
            AssetDatabase.SaveAssets();
        }

        static TextMeshProUGUI Text(string name, Transform parent, float size, Color color, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.raycastTarget = false;
            text.text = "";
            return text;
        }
    }
}
