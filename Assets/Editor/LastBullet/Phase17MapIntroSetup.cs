using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 숲·성 마을·사막 씬의 Canvas에 "맵 입장 안내"(MapIntro)를 만든다 (2026-10-31 사용자 요청).
    // 화면 위쪽 가운데: 창 배경 + 맵 이름·낮/밤 그림 + "얻는 물감 ● ● ●" 그림. 이미 있으면 건너뛴다.
    // 그림은 모두 Assets/UI PNG라서 직접 그린 그림으로 바꿀 수 있다. 위치·크기는 Scene 창에서 옮기면 된다.
    // 메뉴: LastBullet > Phase 17 맵 입장 안내 넣기
    public static class Phase17MapIntroSetup
    {
        static readonly (string scene, string key)[] Maps =
        {
            ("Assets/Scenes/2_Forest.unity", "Forest"),
            ("Assets/Scenes/3_CastleVillage.unity", "Castle"),
            ("Assets/Scenes/4_Desert.unity", "Desert"),
        };

        [MenuItem("LastBullet/Phase 17 맵 입장 안내 넣기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var (scenePath, key) in Maps)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var canvas = ArtSlots.FindCanvas();
                if (canvas == null || canvas.transform.Find("MapIntro") != null)
                    continue;

                var root = new GameObject("MapIntro", typeof(RectTransform), typeof(CanvasGroup));
                root.transform.SetParent(canvas.transform, false);
                root.transform.SetAsFirstSibling(); // 다른 창·일시정지 메뉴보다 뒤에 그려지게
                Phase4StageSelectSetup.SetRect(root, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(800f, 210f));
                var group = root.GetComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                var panel = Phase4StageSelectSetup.CreateImage("Panel", root.transform, "Assets/UI/Popup_Panel.png", Phase4StageSelectSetup.PanelBorder);
                panel.GetComponent<Image>().type = Image.Type.Sliced;
                panel.GetComponent<Image>().preserveAspect = false;
                Phase4StageSelectSetup.Stretch(panel.GetComponent<RectTransform>());

                var title = Phase4StageSelectSetup.CreateImage("Title", root.transform, $"Assets/UI/StageTitle_{key}_Day.png");
                Phase4StageSelectSetup.SetRect(title, new Vector2(0.5f, 0.5f), new Vector2(0f, 35f), new Vector2(640f, 96f));
                var colors = Phase4StageSelectSetup.CreateImage("Colors", root.transform, $"Assets/UI/StageColors_{key}.png");
                Phase4StageSelectSetup.SetRect(colors, new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(720f, 64f));

                var script = root.AddComponent<MapIntroBanner>();
                var so = new SerializedObject(script);
                so.FindProperty("titleImage").objectReferenceValue = title.GetComponent<Image>();
                so.FindProperty("daySprite").objectReferenceValue = ArtSlots.LoadSprite($"Assets/UI/StageTitle_{key}_Day.png");
                so.FindProperty("nightSprite").objectReferenceValue = ArtSlots.LoadSprite($"Assets/UI/StageTitle_{key}_Night.png");
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 맵 입장 안내(MapIntro)를 만들었어요.");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
