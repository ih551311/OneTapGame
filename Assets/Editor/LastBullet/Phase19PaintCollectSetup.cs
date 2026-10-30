using LastBullet.Inventory;
using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 숲·성 마을·사막 씬에 물감 획득 연출(PaintCollectEffect)을 넣는다 (2026-10-31 사용자 요청).
    // Canvas의 전투 가방 패널(RunBagPanel) 바로 위에 화면 전체 크기 오브젝트 PaintCollect를 만들고,
    // 방울 틀(Drop)과 9색 방울 그림(Assets/UI/PaintDrop_*.png), 도착 지점(가방의 물감 아이콘 9개)을 연결한다.
    // 이미 있으면 건너뛴다. 메뉴: LastBullet > Phase 19 물감 획득 연출 넣기
    public static class Phase19PaintCollectSetup
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 19 물감 획득 연출 넣기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            string[] names = System.Enum.GetNames(typeof(PaintColor));
            foreach (var scenePath in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(scenePath);
                var canvas = ArtSlots.FindCanvas();
                var bagPanel = canvas != null ? canvas.transform.Find("RunBagPanel") : null;
                if (bagPanel == null || canvas.transform.Find("PaintCollect") != null)
                    continue;

                var root = new GameObject("PaintCollect", typeof(RectTransform));
                root.transform.SetParent(canvas.transform, false);
                root.transform.SetSiblingIndex(bagPanel.GetSiblingIndex() + 1); // 가방 패널 위, 팝업·일시정지 메뉴 아래
                Phase4StageSelectSetup.Stretch(root.GetComponent<RectTransform>());

                var template = Phase4StageSelectSetup.CreateImage("Drop", root.transform, "Assets/UI/PaintDrop_Red.png");
                template.GetComponent<RectTransform>().anchorMin = template.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
                template.SetActive(false);

                var script = root.AddComponent<PaintCollectEffect>();
                var so = new SerializedObject(script);
                so.FindProperty("dropTemplate").objectReferenceValue = template.GetComponent<Image>();
                var sprites = so.FindProperty("dropSprites");
                var targets = so.FindProperty("targets");
                sprites.arraySize = targets.arraySize = names.Length;
                for (int i = 0; i < names.Length; i++)
                {
                    sprites.GetArrayElementAtIndex(i).objectReferenceValue = ArtSlots.LoadSprite($"Assets/UI/PaintDrop_{names[i]}.png");
                    targets.GetArrayElementAtIndex(i).objectReferenceValue = bagPanel.Find($"Icon_Paint_{names[i]}");
                }
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 물감 획득 연출(PaintCollect)을 만들었어요.");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
