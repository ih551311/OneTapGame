using UnityEngine;

namespace LastBullet.Combat
{
    // 바주카 재장전 시간표 (2026-11-08). 3인칭 왼팔(ThirdPersonBazookaPose)·1인칭 왼팔(BazookaArmSlide)·로켓 모델(BazookaWeapon)이 같이 쓴다.
    // 좌표는 바주카 모델 안(총구가 +Z, 위가 +Y, 모델 크기 1 기준)이다.
    //   0~25%   앞 손잡이를 놓고 총구 앞(로켓을 받을 자리)으로 손을 뻗는다
    //   25~40%  새 로켓이 손에 나타난다 (총구 앞 0.5m)
    //   40~80%  로켓을 쥔 채 총구 안으로 밀어 넣는다
    //   80~100% 앞 손잡이로 돌아온다
    public static class BazookaReloadTimeline
    {
        public const float StartOffset = 0.5f; // 로켓이 처음 보이는 자리 (끼워진 자리에서 앞으로 m)

        // 로켓이 끼워진 자리에서 앞으로 얼마나 떨어져 있는가 (0 = 끼워짐). 재장전 중이 아니면 0
        public static float RocketOffset(float progress)
        {
            if (progress < 0.4f)
                return StartOffset;
            if (progress < 0.8f)
                return StartOffset * (1f - Smooth((progress - 0.4f) / 0.4f));
            return 0f;
        }

        // 재장전 중 로켓이 보이는가 (25% 이후)
        public static bool RocketVisible(float progress)
        {
            return progress >= 0.25f;
        }

        // 왼손 자리. rest = 앞 손잡이 자리, muzzleZ = 발사관 앞 끝
        public static Vector3 LeftHand(float progress, Vector3 rest, float muzzleZ)
        {
            Vector3 grab = new Vector3(0f, rest.y + 0.05f, muzzleZ + 0.18f); // 끼워진 로켓의 몸통
            if (progress <= 0f)
                return rest;
            if (progress < 0.25f)
                return Vector3.Lerp(rest, grab + new Vector3(0f, 0f, StartOffset), Smooth(progress / 0.25f));
            if (progress < 0.8f)
                return grab + new Vector3(0f, 0f, RocketOffset(progress));
            return Vector3.Lerp(grab, rest, Smooth((progress - 0.8f) / 0.2f));
        }

        static float Smooth(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
