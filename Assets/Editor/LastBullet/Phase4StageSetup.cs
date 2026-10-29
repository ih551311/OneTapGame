using System.Linq;
using LastBullet.Player;
using LastBullet.Stage;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // Phase 4-4: 훈련장 씬(Stage_Training)을 만든다.
    // - Hideout을 복사해서 시작하고, 적 3마리를 훈련장으로 옮긴다 (Hideout은 미술관이라 적이 없다).
    // - StageManager: 적을 모두 처치하면 클리어 → 미술관으로 복귀. 훈련장에서 죽으면 미술관으로 복귀.
    // - 모든 UI는 Canvas 한 곳에 둔다 (HUD → Canvas 이름 변경).
    // 메뉴: LastBullet > Phase 4-4 훈련장 만들기
    public static class Phase4StageSetup
    {
        const string HideoutPath = "Assets/Scenes/Hideout.unity";
        const string StagePath = "Assets/Scenes/Stage_Training.unity";

        [MenuItem("LastBullet/Phase 4-4 훈련장 만들기")]
        static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(StagePath) != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 Stage_Training 씬이 있어요.", "확인");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            // 1. 미술관(Hideout): UI 캔버스 이름 정리, 적 제거
            var hideout = EditorSceneManager.OpenScene(HideoutPath);
            RenameCanvas();
            var hideoutEnemies = GameObject.Find("TestEnemies (임시)");
            if (hideoutEnemies == null || GameObject.Find("Player")?.GetComponent<PlayerHealth>() == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 4-3 메뉴를 먼저 실행하세요.", "확인");
                return;
            }
            EditorSceneManager.SaveScene(hideout); // 적이 있는 상태로 먼저 저장해서 복사본에 적이 남게 한다
            AssetDatabase.CopyAsset(HideoutPath, StagePath);
            Object.DestroyImmediate(hideoutEnemies);
            EditorSceneManager.SaveScene(hideout);

            // 2. 훈련장: 복사본을 열어서 설정
            var stage = EditorSceneManager.OpenScene(StagePath);

            var enemies = GameObject.Find("TestEnemies (임시)");
            enemies.name = "Enemies";
            // 기획서 2.6: 훈련장 = 돌진형 3마리. 플레이어 앞쪽에 간격을 두고 배치
            Vector3[] positions = { new Vector3(-6f, 0f, 14f), new Vector3(0f, 0f, 20f), new Vector3(6f, 0f, 14f) };
            for (int i = 0; i < enemies.transform.childCount && i < positions.Length; i++)
            {
                var enemy = enemies.transform.GetChild(i);
                enemy.position = positions[i];
                enemy.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // 죽으면 미술관으로
            var playerHealth = new SerializedObject(GameObject.Find("Player").GetComponent<PlayerHealth>());
            playerHealth.FindProperty("sceneAfterDeath").stringValue = "Hideout";
            playerHealth.ApplyModifiedPropertiesWithoutUndo();

            // 훈련장 전용 NavMesh
            var surface = GameObject.Find("NavMesh").GetComponent<NavMeshSurface>();
            surface.BuildNavMesh();
            if (!AssetDatabase.IsValidFolder("Assets/Scenes/Stage_Training"))
                AssetDatabase.CreateFolder("Assets/Scenes", "Stage_Training");
            AssetDatabase.CreateAsset(surface.navMeshData, "Assets/Scenes/Stage_Training/NavMesh-Stage_Training.asset");

            // 클리어 그림 (Canvas 가운데, 평소에는 꺼짐)
            var canvas = ArtSlots.FindCanvas();
            var clear = new GameObject("StageClear", typeof(RectTransform), typeof(Image));
            clear.transform.SetParent(canvas.transform, false);
            var rect = clear.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 120f);
            rect.sizeDelta = new Vector2(800f, 200f);
            var image = clear.GetComponent<Image>();
            image.sprite = ArtSlots.LoadSprite("Assets/UI/StageClear.png");
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            clear.SetActive(false);

            // 스테이지 진행 관리
            var manager = new GameObject("StageManager").AddComponent<StageManager>();
            var so = new SerializedObject(manager);
            so.FindProperty("clearMessage").objectReferenceValue = clear;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(stage);

            // 3. 빌드 목록에 훈련장 추가
            if (!EditorBuildSettings.scenes.Any(s => s.path == StagePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes.Append(new EditorBuildSettingsScene(StagePath, true)).ToArray();

            AssetDatabase.SaveAssets();
            Selection.activeGameObject = manager.gameObject;
            Debug.Log("[LastBullet] Phase 4-4: Stage_Training 씬(돌진형 3마리, StageManager)을 만들고 Hideout에서 적을 뺐어요. 지금 열린 씬은 Stage_Training이에요.");
        }

        // 예전 이름 HUD를 Canvas로 바꾼다
        static void RenameCanvas()
        {
            var hud = GameObject.Find("HUD");
            if (hud != null)
                hud.name = "Canvas";
        }
    }
}
