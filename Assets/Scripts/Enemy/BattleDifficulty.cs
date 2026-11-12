using UnityEngine;

namespace LastBullet.Enemy
{
    // 전투가 길어질수록 새로 나오는 적이 단단해진다 (2026-11-12, 기획 D단계: "난이도가 평평하다"는 점검 결과).
    //  - 전투 맵에 들어온 지 150초마다 한 단계, 한 단계마다 적 체력 +8%, 최대 5단계(+40%, 12분 반).
    //  - 새로 만들어지는 적(처음 배치, 60초 뒤 다시 나오는 적, 분열체)의 체력에 곱한다. 이미 있는 적은 그대로다. 보스(체력 500 초과)는 그대로다.
    //  - 물감·종이조각 보상은 그대로라서 오래 버틸수록 쉽게 얻기보다 조금 더 힘들게 얻는다. 맵을 나갔다 오면(씬 다시 불러오기) 처음부터다.
    //  - 값은 아래 상수에서 바꾼다. 아무 일도 없게 하려면 StepSeconds를 아주 크게 한다.
    public static class BattleDifficulty
    {
        public const float StepSeconds = 150f;
        public const float PerStep = 0.08f;
        public const int MaxSteps = 5;

        // 지금 단계 (0 ~ MaxSteps)
        public static int Step => Mathf.Clamp(Mathf.FloorToInt(Time.timeSinceLevelLoad / StepSeconds), 0, MaxSteps);

        public static float HealthMultiplier => 1f + PerStep * Step;
    }
}
