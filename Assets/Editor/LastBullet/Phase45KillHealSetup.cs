using LastBullet.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 버튼(F키)으로 쓰는 힐 스킬을 없애고, 적을 처치하면 체력이 조금씩 차게 한다 (2026-10-31 사용자 요청).
    //   - Player: 옛 HealSkill(지운 스크립트라 "없음" 상태로 남은 것)을 치우고 KillHeal을 붙인다 (헤드샷 +10, 몸샷 +5)
    //   - Canvas/HealSkill (오른쪽 아래 스킬 칸)을 지운다
    //   - 마지막에 Phase 37(소리)을 다시 실행해 HealEffect를 KillHeal에 연결한다
    // 값은 Player의 KillHeal에서 고친다. 다시 실행해도 같은 결과다. 메뉴: LastBullet > Phase 45 처치 회복 넣기
    public static class Phase45KillHealSetup
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 45 처치 회복 넣기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = GameObject.FindWithTag("Player");
                var canvas = ArtSlots.FindCanvas();
                if (player == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: Player가 없어 건너뜀");
                    continue;
                }

                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(player);
                if (player.GetComponent<KillHeal>() == null)
                    player.AddComponent<KillHeal>();

                if (canvas != null)
                {
                    var icon = canvas.transform.Find("HealSkill");
                    if (icon != null)
                        Object.DestroyImmediate(icon.gameObject);
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 처치 회복을 붙이고 힐 스킬 칸을 지웠어요 (없음 스크립트 {removed}개 정리).");
            }
            Phase37HealEffectSetup.Create();
        }
    }
}
