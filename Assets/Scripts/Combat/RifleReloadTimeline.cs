using UnityEngine;

namespace LastBullet.Combat
{
    // 소총 재장전의 왼손 움직임 시간표 (2026-11-04 사용자 요청: 재장전 모션이 어색하다).
    // 재장전 진행도 p(0~1)에 따라 왼손이 총(총 모델 안 좌표, 총구 +Z, 위 +Y)에서 있어야 할 자리를 돌려준다:
    //   0.00~0.10  총열 덮개 → 탄창을 잡으러 간다
    //   0.10~0.25  탄창을 아래로 뽑는다            (탄창도 같이 내려간다: MagazineReloadMotion)
    //   0.25~0.45  허리 쪽으로 가서 새 탄창을 꺼낸다 (탄창은 안 보임)
    //   0.45~0.70  새 탄창을 들고 올라와 끼운다     (탄창도 같이 올라온다)
    //   0.70~0.80  장전 손잡이 쪽으로 간다
    //   0.80~0.88  손잡이를 뒤로 당긴다 (장전)
    //   0.88~1.00  총열 덮개로 돌아온다
    // 3인칭 캐릭터의 왼팔 IK(ThirdPersonGunPose)와 1인칭 왼팔(ArmReloadSlide)이 같은 시간표를 쓴다.
    public static class RifleReloadTimeline
    {
        public const float MagazineTravel = 0.22f;   // 탄창이 빠지는 거리 (총 모델 단위)

        // 탄창 위치 오프셋 (0 = 끼워진 자리, 1 = 뽑힌 자리) 과 보이는지
        public static float MagazineOffset(float p, out bool visible)
        {
            visible = true;
            if (p < 0.05f || p >= 0.70f)
                return 0f;
            if (p < 0.25f)
                return Ease((p - 0.05f) / 0.20f);
            if (p < 0.45f)
            {
                visible = false;
                return 1f;
            }
            return 1.3f * (1f - Ease((p - 0.45f) / 0.25f));
        }

        // 왼손이 있어야 할 자리 (총 모델 안 좌표)
        //  rest: 평소 받치는 자리(총열 덮개), magazine: 탄창 자리, charge: 장전 손잡이 자리
        public static Vector3 LeftHand(float p, Vector3 rest, Vector3 magazine, Vector3 charge)
        {
            Vector3 down = Vector3.down * MagazineTravel;
            Vector3 pulled = magazine + down;
            Vector3 belt = magazine + Vector3.down * 0.29f + new Vector3(-0.03f, 0f, -0.05f);
            Vector3 insert = magazine + down * 1.3f;

            if (p <= 0f)
                return rest;
            if (p < 0.10f)
                return Vector3.Lerp(rest, magazine, Ease(p / 0.10f));
            if (p < 0.25f)
                return magazine + down * Ease((p - 0.10f) / 0.15f);
            if (p < 0.45f)
                return Vector3.Lerp(pulled, belt, Ease((p - 0.25f) / 0.20f));
            if (p < 0.70f)
                return Vector3.Lerp(insert, magazine, Ease((p - 0.45f) / 0.25f));
            if (p < 0.80f)
                return Vector3.Lerp(magazine, charge, Ease((p - 0.70f) / 0.10f));
            if (p < 0.88f)
                return charge + Vector3.back * 0.09f * Mathf.Sin((p - 0.80f) / 0.08f * Mathf.PI);
            return Vector3.Lerp(charge, rest, Ease((p - 0.88f) / 0.12f));
        }

        static float Ease(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }
    }
}
