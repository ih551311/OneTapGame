using LastBullet.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 미술관에서 총을 안 든 모습으로 걷게 한다 (2026-11-04 사용자 요청: "아예 총을 안 들고 있는 모션으로").
    //   캐릭터의 동작 파일이 전부 총 든 자세라서, 1_Museum의 Player에 UnarmedArmPose를 붙여 팔을 몸 옆으로 늘어뜨리고 흔들게 한다.
    //   전투 맵은 그대로다. 메뉴: LastBullet > Phase 66 미술관 맨손 걷기
    public static class Phase66MuseumUnarmedSetup
    {
        [MenuItem("LastBullet/Phase 66 미술관 맨손 걷기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: Player가 없어 건너뜀");
                return;
            }
            var pose = player.GetComponent<UnarmedArmPose>();
            if (pose == null)
                pose = player.AddComponent<UnarmedArmPose>();
            var so = new SerializedObject(pose);
            so.FindProperty("body").objectReferenceValue = player.transform;
            so.FindProperty("rightUpper").objectReferenceValue = Find(player, "R.Arm");
            so.FindProperty("rightMid").objectReferenceValue = Find(player, "R.ForeArm");
            so.FindProperty("rightHand").objectReferenceValue = Find(player, "R.Hand");
            so.FindProperty("leftUpper").objectReferenceValue = Find(player, "L.Arm");
            so.FindProperty("leftMid").objectReferenceValue = Find(player, "L.ForeArm");
            so.FindProperty("leftHand").objectReferenceValue = Find(player, "L.Hand");
            so.FindProperty("controller").objectReferenceValue = player.GetComponent<CharacterController>();
            so.FindProperty("sideOffset").floatValue = 0.17f;   // 치마 밖으로 손이 나오게
            so.FindProperty("forwardOffset").floatValue = 0.05f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] 1_Museum: 맨손으로 걷는 팔을 붙였어요.");
        }

        static Transform Find(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name)
                    return t;
            return null;
        }
    }
}
