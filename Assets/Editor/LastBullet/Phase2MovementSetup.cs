using LastBullet.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // Phase 2-3: Player에 이동·달리기·점프·회피 스크립트(PlayerMovement)를 붙인다.
    // 메뉴: LastBullet > Phase 2-3 이동·점프·회피 붙이기
    public static class Phase2MovementSetup
    {
        [MenuItem("LastBullet/Phase 2-3 이동·점프·회피 붙이기")]
        static void Create()
        {
            // 1_Museum 씬이 열려 있지 않으면 자동으로 연다.
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "1_Museum")
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            }

            var player = GameObject.Find("Player");
            if (player == null || player.GetComponent<ThirdPersonCamera>() == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 2-1, 2-2 메뉴를 먼저 실행하세요.", "확인");
                return;
            }
            if (player.GetComponent<PlayerMovement>() != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Player에 이미 PlayerMovement가 있어요.", "확인");
                return;
            }

            Undo.AddComponent<PlayerMovement>(player);

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] Phase 2-3: Player에 PlayerMovement를 붙이고 1_Museum 씬을 저장했어요.");
        }
    }
}
