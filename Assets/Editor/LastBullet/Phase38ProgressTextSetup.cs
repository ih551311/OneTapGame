using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 미술관 화면 위 가운데 "서로 다른 작품 0 / 9" 표시의 그림과 글자를 나눈다 (2026-10-31 사용자 요청).
    //   배경: 전용 그림 Assets/UI/FrameProgress_Panel.png (9-slice, 모서리 24px). 이 그림만 바꾸면 된다.
    //   글자 "서로 다른 작품"과 숫자 "0 / 9": 둘 다 TextMeshPro 한글 글꼴(font1 SDF). 글자가 그림 안에 들어 있지 않다.
    // 예전의 글자 그림(FrameProgress_Label.png)은 쓰지 않아서 지운다. 다시 실행해도 안전하다.
    // 메뉴: LastBullet > Phase 38 진행도 글자 분리
    public static class Phase38ProgressTextSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";
        const string PanelPath = "Assets/UI/FrameProgress_Panel.png";

        [MenuItem("LastBullet/Phase 38 진행도 글자 분리")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var canvas = ArtSlots.FindCanvas();
            var progress = canvas != null ? canvas.transform.Find("FrameProgress") : null;
            if (progress == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "FrameProgress를 찾지 못했어요.", "확인");
                return;
            }

            // 배경: 전용 그림
            var background = progress.GetComponent<Image>();
            background.sprite = ArtSlots.LoadSprite(PanelPath, 24f);
            background.type = Image.Type.Sliced;
            background.color = Color.white;

            // 글자 그림은 없애고 TMP 글자로
            var oldLabel = progress.Find("Label");
            if (oldLabel != null)
                Object.DestroyImmediate(oldLabel.gameObject);

            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var label = Text(progress, "Label", font, new Vector2(-70f, 0f), new Vector2(360f, 64f), 34f);
            label.text = "서로 다른 작품";

            var count = progress.Find("Count").GetComponent<TextMeshProUGUI>();
            count.font = font != null ? font : TMP_Settings.defaultFontAsset;
            count.fontStyle = FontStyles.Normal;
            count.fontSize = 44f;
            count.color = Color.white;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.DeleteAsset("Assets/UI/FrameProgress_Label.png");
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] Phase 38: 진행도 표시를 배경 그림 + TMP 글자로 나눴어요.");
        }

        static TextMeshProUGUI Text(Transform parent, string name, TMP_FontAsset font, Vector2 position, Vector2 size, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            Phase4StageSelectSetup.SetRect(go, new Vector2(0.5f, 0.5f), position, size);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Normal;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }
    }
}
