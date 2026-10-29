using System.Linq;
using LastBullet.Player;
using LastBullet.UI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // Phase 4-3: 플레이어 체력·사망과 체력바를 만든다.
    // 테스트용 과녁(TestTargets)을 지우고 NavMesh를 다시 굽는다. 1_Museum을 빌드 목록에 넣는다(사망 후 다시 시작용).
    // 메뉴: LastBullet > Phase 4-3 플레이어 체력과 체력바 만들기
    public static class Phase4HealthSetup
    {
        const string ScenePath = "Assets/Scenes/1_Museum.unity";

        [MenuItem("LastBullet/Phase 4-3 플레이어 체력과 체력바 만들기")]
        static void Create()
        {
            // 1_Museum 씬이 열려 있지 않으면 자동으로 연다.
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "1_Museum")
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                scene = EditorSceneManager.OpenScene(ScenePath);
            }

            var player = GameObject.Find("Player");
            var hud = ArtSlots.FindCanvas();
            if (player == null || hud == null || GameObject.Find("NavMesh") == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 4-2 메뉴를 먼저 실행하세요.", "확인");
                return;
            }
            if (player.GetComponent<PlayerHealth>() != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Player에 이미 PlayerHealth가 있어요.", "확인");
                return;
            }

            // 1. 테스트용 과녁 삭제 (사용자 요청: 적만 남긴다)
            var targets = GameObject.Find("TestTargets (임시)");
            if (targets != null)
                Undo.DestroyObjectImmediate(targets);

            // 2. 과녁이 사라졌으니 NavMesh를 다시 굽는다 (같은 파일에 덮어쓴다)
            var surface = GameObject.Find("NavMesh").GetComponent<NavMeshSurface>();
            var oldData = surface.navMeshData;
            string navMeshPath = oldData != null ? AssetDatabase.GetAssetPath(oldData) : "Assets/Scenes/1_Museum/NavMesh-1_Museum.asset";
            surface.BuildNavMesh();
            if (oldData != null)
                AssetDatabase.DeleteAsset(navMeshPath);
            AssetDatabase.CreateAsset(surface.navMeshData, navMeshPath);

            // 3. 플레이어 체력
            Undo.AddComponent<PlayerHealth>(player);

            // 4. 체력바: 왼쪽 아래, 배경 + 채움 (그림은 Assets/UI에서 바꿀 수 있다)
            var bar = new GameObject("HealthBar", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(bar, "Create HealthBar");
            bar.transform.SetParent(hud.transform, false);
            var barRect = bar.GetComponent<RectTransform>();
            barRect.anchorMin = barRect.anchorMax = barRect.pivot = Vector2.zero;
            barRect.anchoredPosition = new Vector2(40f, 40f);
            barRect.sizeDelta = new Vector2(400f, 40f); // 1920x1080 기준 px
            var back = bar.GetComponent<Image>();
            back.sprite = ArtSlots.LoadSprite("Assets/UI/HealthBar_Back.png");
            back.color = Color.white;
            back.raycastTarget = false;

            var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(bar.transform, false);
            var fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(4f, 4f); // 테두리 안쪽
            fillRect.offsetMax = new Vector2(-4f, -4f);
            var fill = fillObject.GetComponent<Image>();
            fill.sprite = ArtSlots.LoadSprite("Assets/UI/HealthBar_Fill.png");
            fill.color = Color.white;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.raycastTarget = false;

            var battleUI = Undo.AddComponent<BattleUI>(hud);
            var so = new SerializedObject(battleUI);
            so.FindProperty("playerHealth").objectReferenceValue = player.GetComponent<PlayerHealth>();
            so.FindProperty("healthFill").objectReferenceValue = fill;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 5. 빌드 목록에 1_Museum 추가 (씬을 다시 불러오려면 필요하다)
            if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
            {
                var scenes = EditorBuildSettings.scenes.ToList();
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] Phase 4-3: 과녁 삭제, NavMesh 다시 굽기, PlayerHealth와 체력바를 만들고 1_Museum 씬을 저장했어요.");
        }
    }
}
