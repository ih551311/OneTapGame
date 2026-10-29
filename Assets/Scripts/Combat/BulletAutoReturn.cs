using UnityEngine;

namespace LastBullet.Combat
{
    // 떨어진 탄환은 위치와 관계없이 15초 뒤 자동으로 돌아온다 (기획서 2.4-7).
    // 벽 안이나 맵 밖에 떨어져도 이 타이머가 있어서 탄환을 잃지 않는다.
    // 붙일 곳: Prefabs/DroppedBullet
    public class BulletAutoReturn : MonoBehaviour
    {
        [SerializeField] float returnTime = 15f;

        OneBulletGun gun;
        float dropTime;

        public void Init(OneBulletGun owner)
        {
            gun = owner;
            dropTime = Time.time;
        }

        // 남은 시간 비율. 1 = 방금 떨어짐, 0 = 돌아올 때. 원형 게이지가 쓴다.
        public float RemainingRatio => Mathf.Clamp01(1f - (Time.time - dropTime) / returnTime);

        void Update()
        {
            if (gun != null && Time.time - dropTime >= returnTime)
                gun.ReturnBullet("15초 자동 복귀");
        }
    }
}
