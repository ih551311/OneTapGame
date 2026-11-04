using LastBullet.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 전투 맵(숲·성 마을·사막)을 1인칭으로 시작하게 한다 (2026-11-05 사용자 요청: 1인칭 기본).
    //   Player의 CameraZoom > Start First Person을 켠다. 마우스 휠로 3인칭으로 바꿀 수 있고, 바꾼 뒤에는 전투 맵끼리 그 선택을 기억한다.
    //   미술관·타이틀은 그대로(3인칭 시작). 메뉴: LastBullet > Phase 68 전투 맵 1인칭 기본
    public static class Phase68BattleFirstPersonSetup
    {
        static readonly string[] BattleScenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 68 전투 맵 1인칭 기본")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var path in BattleScenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = GameObject.FindWithTag("Player");
                var zoom = player != null ? player.GetComponent<CameraZoom>() : null;
                if (zoom == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: CameraZoom이 없어 건너뜀");
                    continue;
                }
                var so = new SerializedObject(zoom);
                so.FindProperty("startFirstPerson").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 1인칭으로 시작하게 했어요.");
            }
        }
    }
}
