using LastBullet.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 미술관 움직임 느낌 (2026-11-06 사용자 요청: 걸을 때 시점이 살짝 흔들리고, 달릴 때 시야가 넓어진다).
    //   플레이어의 AimController에 "움직임 느낌"(movementFeel)을 켠다 (미술관만. 전투 맵은 조준이 흔들리지 않게 그대로).
    //   (작품 앞에 서면 액자가 빛나는 효과도 만들었지만 사용자가 별로라고 해서 지웠다.)
    // 메뉴: LastBullet > Phase 81 미술관 움직임 느낌 켜기
    public static class Phase81FrameGlowSetup
    {
        [MenuItem("LastBullet/Phase 81 미술관 움직임 느낌 켜기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var player = GameObject.FindWithTag("Player");
            var aim = player != null ? player.GetComponent<AimController>() : null;
            if (aim == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: Player의 AimController를 못 찾았어요.");
                return;
            }
            var so = new SerializedObject(aim);
            so.FindProperty("movementFeel").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] 1_Museum: 움직임 느낌(걸을 때 시점 흔들림, 달릴 때 시야 넓어짐)을 켰어요.");
        }
    }
}
