using LastBullet.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 우클릭 조준(AimController)을 플레이어가 있는 모든 씬에 넣는다 (2026-10-31 사용자 요청).
    // 시야각·카메라 거리·마우스 감도 값은 Player의 AimController에서 바꿀 수 있다. 이미 있으면 건너뛴다.
    // 메뉴: LastBullet > Phase 30 우클릭 조준 넣기
    public static class Phase30AimSetup
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 30 우클릭 조준 넣기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = Object.FindFirstObjectByType<PlayerMovement>();
                var cam = Object.FindFirstObjectByType<CinemachineCamera>();
                if (player == null || cam == null || player.GetComponent<AimController>() != null)
                    continue;

                var aim = player.gameObject.AddComponent<AimController>();
                var so = new SerializedObject(aim);
                so.FindProperty("followCamera").objectReferenceValue = cam;
                so.FindProperty("follow").objectReferenceValue = cam.GetComponent<CinemachineThirdPersonFollow>();
                so.FindProperty("look").objectReferenceValue = player.GetComponent<ThirdPersonCamera>();
                so.FindProperty("health").objectReferenceValue = player.GetComponent<PlayerHealth>();
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 우클릭 조준을 넣었어요.");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
