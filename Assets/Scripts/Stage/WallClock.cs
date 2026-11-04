using System;
using UnityEngine;

namespace LastBullet.Stage
{
    // 미술관 벽시계 (2026-11-05 사용자 요청: 벽시계 소리). 컴퓨터의 실제 시각대로 시침·분침·초침이 돈다.
    //  - 초침은 1초마다 똑딱 하고 한 칸씩 움직이고, 그 순간 소리도 난다 (MuseumAmbience가 같은 때에 똑 · 딱을 번갈아 낸다).
    //  - 시계 앞(보는 쪽)은 -Z 방향이다. 바늘은 시계 로컬 +Y(12시 방향)를 가리키게 만들어 두고, 시계 앞에서 볼 때 시계 방향으로 돌린다.
    // 붙일 곳: 미술관 WallClock 오브젝트 (HourPivot·MinutePivot·SecondPivot 연결. 메뉴 LastBullet > Phase 71 벽시계와 삐걱 소리 만들기)
    public class WallClock : MonoBehaviour
    {
        [SerializeField] Transform hourPivot;
        [SerializeField] Transform minutePivot;
        [SerializeField] Transform secondPivot;

        // 지금 몇 초인지 (똑딱 소리가 같은 때에 나도록 다른 스크립트가 읽는다)
        public int Second { get; private set; } = -1;

        void Update()
        {
            var now = DateTime.Now;
            float hour = ((now.Hour % 12) + now.Minute / 60f) * 30f;
            float minute = (now.Minute + now.Second / 60f) * 6f;
            float second = now.Second * 6f; // 한 칸씩 똑딱
            Set(hourPivot, hour);
            Set(minutePivot, minute);
            Set(secondPivot, second);
            Second = now.Second;
        }

        // 시계 앞에서 볼 때 시계 방향 = 로컬 +Z축 양의 회전 (유니티는 왼손 좌표계)
        static void Set(Transform pivot, float degrees)
        {
            if (pivot != null)
                pivot.localRotation = Quaternion.Euler(0f, 0f, degrees);
        }
    }
}
