using LastBullet.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // Player에 카메라 줌(CameraZoom)을 붙인다 (2026-10-31 사용자 요청: 마우스 휠로 3인칭 ↔ 1인칭).
    // 미술관·숲·성 마을·사막. 카메라 거리 계산은 AimController가 하므로 AimController가 있는 씬에서만 효과가 있다.
    // 이미 있으면 건너뛴다. 메뉴: LastBullet > Phase 42 카메라 줌 넣기
    public static class Phase42CameraZoomSetup
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 42 카메라 줌 넣기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = Object.FindFirstObjectByType<PlayerMovement>();
                if (player == null)
                    continue;
                if (player.GetComponent<AimController>() == null)
                    Debug.LogWarning($"[LastBullet] {scene.name}: AimController가 없어서 줌이 카메라에 적용되지 않아요 (Phase 30 필요)");
                if (player.GetComponent<CameraZoom>() == null)
                    player.gameObject.AddComponent<CameraZoom>();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 카메라 줌(CameraZoom)을 넣었어요.");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
