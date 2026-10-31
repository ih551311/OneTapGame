using LastBullet.Inventory;
using LastBullet.Stage;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 상태창(HudProfile)의 이름 칸에 숫자를 넣는다 (2026-10-31 사용자 요청).
    //   미술관: 닉네임 / 그림 N장 / 종이 N장
    //   전투 맵: 닉네임 / 잡은 적 N / 이번 판에 얻은 물감(그 맵에서 나오는 3색: 아이콘 + 양)
    // 닉네임은 윗줄로 올려 작게 두고 숫자 줄들이 그 아래에 놓인다. 모두 TextMeshPro 한글 글꼴(font1 SDF)이고
    // 물감 아이콘은 Assets/UI/Icon_Paint_*.png (전투 가방과 같은 그림). 다시 실행하면 숫자 줄만 새로 만든다.
    // 먼저 Phase 33(프로필 패널)이 있어야 한다. 메뉴: LastBullet > Phase 34 상태창에 숫자 넣기
    public static class Phase34HudStatusSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";
        static readonly Color Cream = new Color32(0xEE, 0xE6, 0xDA, 0xFF);
        static readonly Color SoftCream = new Color32(0xD6, 0xCC, 0xBE, 0xFF);

        static readonly (string scene, bool battle)[] Scenes =
        {
            ("Assets/Scenes/1_Museum.unity", false),
            ("Assets/Scenes/2_Forest.unity", true),
            ("Assets/Scenes/3_CastleVillage.unity", true),
            ("Assets/Scenes/4_Desert.unity", true),
        };

        [MenuItem("LastBullet/Phase 34 상태창에 숫자 넣기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var (path, battle) in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var canvas = ArtSlots.FindCanvas();
                var panel = canvas != null ? canvas.transform.Find("HudProfile") : null;
                var plate = panel != null ? panel.Find("NamePlate") : null;
                if (plate == null)
                {
                    EditorUtility.DisplayDialog("LastBullet", "먼저 Phase 33 화면에 프로필 넣기를 실행하세요.", "확인");
                    return;
                }

                // 닉네임은 윗줄로 (작게, 왼쪽 맞춤)
                var nick = plate.Find("Nickname").GetComponent<TextMeshProUGUI>();
                TopLeft(nick.rectTransform, new Vector2(14f, 8f), new Vector2(222f, 40f));
                nick.alignment = TextAlignmentOptions.Left;
                nick.fontSizeMin = 18f;
                nick.fontSizeMax = 28f;

                var oldInfo = plate.Find("Info");
                if (oldInfo != null)
                    Object.DestroyImmediate(oldInfo.gameObject);
                var info = new GameObject("Info", typeof(RectTransform)).transform;
                info.SetParent(plate, false);
                var infoRect = (RectTransform)info;
                infoRect.anchorMin = Vector2.zero;
                infoRect.anchorMax = Vector2.one;
                infoRect.offsetMin = infoRect.offsetMax = Vector2.zero;

                var status = panel.GetComponent<HudStatus>();
                if (status != null)
                    Object.DestroyImmediate(status);
                status = panel.gameObject.AddComponent<HudStatus>();
                var so = new SerializedObject(status);

                if (!battle)
                {
                    var line1 = Line(info, "Paintings", new Vector2(14f, 56f), 28f);
                    var line2 = Line(info, "Paper", new Vector2(14f, 98f), 28f);
                    so.FindProperty("mode").enumValueIndex = (int)HudStatus.Mode.Museum;
                    so.FindProperty("line1").objectReferenceValue = line1;
                    so.FindProperty("line2").objectReferenceValue = line2;
                }
                else
                {
                    // 전투 맵의 상태창에는 "잡은 적"만 둔다. 얻은 물감은 오른쪽 위 전투 가방에 나온다 (2026-10-31 사용자 요청).
                    TopLeft(nick.rectTransform, new Vector2(14f, 26f), new Vector2(222f, 40f));
                    var line1 = Line(info, "Kills", new Vector2(14f, 84f), 28f);
                    so.FindProperty("mode").enumValueIndex = (int)HudStatus.Mode.Battle;
                    so.FindProperty("line1").objectReferenceValue = line1;
                    so.FindProperty("paintColors").arraySize = 0;
                    so.FindProperty("paintCounts").arraySize = 0;
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 상태창에 숫자를 넣었어요 ({(battle ? "전투" : "미술관")}).");
            }
            AssetDatabase.SaveAssets();
        }

        static TMP_Text Line(Transform parent, string name, Vector2 offset, float size)
        {
            return Text(parent, name, offset, new Vector2(222f, 36f), size, Cream);
        }

        static TMP_Text Text(Transform parent, string name, Vector2 offset, Vector2 size, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TopLeft((RectTransform)go.transform, offset, size);
            var text = go.GetComponent<TextMeshProUGUI>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Left;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.text = "0";
            return text;
        }

        // 왼쪽 위 모서리 기준: x는 오른쪽으로, y는 아래로 얼마나 떨어졌는지
        static void TopLeft(RectTransform rect, Vector2 offset, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(offset.x, -offset.y);
            rect.sizeDelta = size;
        }
    }
}
