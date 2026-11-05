using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 걷기·가만히 있기 동작의 빠르기를 고른다 (2026-11-06 사용자 요청: "걸을 때 어색하고, 가만히 있을 때 위아래로 너무 빨리 왔다갔다").
    //   - 대기(Wait01)는 길이가 0.33초짜리 반복이라 1초에 3번 들썩였다 → 4분의 1 속도(약 1.3초에 한 번, 숨 쉬듯).
    //   - 걷기·달리기 동작은 모델의 자연스러운 속도(걷기 약 2.4m/s, 달리기 약 4.8m/s)보다 실제 이동(5.05, 7.85m/s)이 훨씬 빨라서 발이 땅에서 미끄러졌다.
    //     발이 더 빨리 구르게 하되 너무 허둥대지 않을 정도로만 올린다 (걷기 1.3배, 달리기 1.15배).
    //   Assets/Animations/PlayerCharacter.controller 의 각 상태 Speed 값이라서, 애니메이터 창에서 직접 고쳐도 된다.
    // 메뉴: LastBullet > Phase 79 걷기·대기 동작 속도 맞추기
    public static class Phase79LocomotionTune
    {
        public const float IdleSpeed = 0.25f;
        public const float WalkSpeed = 1.3f;
        public const float RunSpeed = 1.15f;

        [MenuItem("LastBullet/Phase 79 걷기·대기 동작 속도 맞추기")]
        static void Run()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Animations/PlayerCharacter.controller");
            if (controller == null)
            {
                Debug.LogWarning("[LastBullet] PlayerCharacter.controller 가 없어요.");
                return;
            }
            foreach (var child in controller.layers[0].stateMachine.states)
            {
                var state = child.state;
                if (state.name == "Idle") state.speed = IdleSpeed;
                else if (state.name == "Walk") state.speed = WalkSpeed;
                else if (state.name == "Run") state.speed = RunSpeed;
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] 동작 빠르기: 대기 {IdleSpeed}배, 걷기 {WalkSpeed}배, 달리기 {RunSpeed}배로 맞췄어요.");
        }
    }
}
