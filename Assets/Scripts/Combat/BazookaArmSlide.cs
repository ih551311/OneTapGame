using UnityEngine;

namespace LastBullet.Combat
{
    // 1인칭 바주카의 왼팔 재장전 움직임 (2026-11-08 만듦, 2026-11-09 사용자 지적 "장전할 때 손이 이상하다"로 다시 만듦).
    // 1인칭 팔은 한 덩어리 그림(메시)이라 관절을 구부릴 수 없다. 팔을 통째로 옮기거나 돌리면 어색해서(굵은 팔 끝이 화면에 들어온다),
    // 메뉴 Phase 93이 3인칭 캐릭터의 진짜 바주카 자세(ThirdPersonBazookaPose, 뼈 IK)에서 왼팔 모양을 세 번 뽑아 둔다:
    //   기본(앞 손잡이) · Reach(로켓을 잡으러 뻗음, 재장전 30%) · Push(로켓을 밀어 넣음, 80%)
    // 이 오브젝트의 SkinnedMeshRenderer에 Reach·Push 블렌드 셰이프(모양 섞기)로 들어 있고, 여기서 재장전 진행도에 맞춰 섞는다 (BazookaReloadTimeline.ArmBlend).
    // 그래서 1인칭 왼손도 3인칭처럼 앞 손잡이를 놓고 로켓을 잡아 끼운 뒤 돌아온다.
    // 팔 메시: Assets/Models/FirstPerson/BazookaArm_L.asset (캐릭터·자세가 바뀌면 메뉴 Phase 93을 다시 실행한다)
    // 붙일 곳: BazookaHolder 아래의 LeftArm (메뉴 LastBullet > Phase 93 바주카포 만들기)
    public class BazookaArmSlide : MonoBehaviour
    {
        [SerializeField] AutoGun gun;
        [SerializeField] SkinnedMeshRenderer arm;

        int reachIndex = -1, pushIndex = -1;

        void Awake()
        {
            if (arm == null)
                arm = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (arm != null && arm.sharedMesh != null)
            {
                reachIndex = arm.sharedMesh.GetBlendShapeIndex("Reach");
                pushIndex = arm.sharedMesh.GetBlendShapeIndex("Push");
                arm.updateWhenOffscreen = true; // 블렌드 셰이프로 모양이 바뀌어도 화면 밖으로 판정되어 사라지지 않게
            }
        }

        void LateUpdate()
        {
            if (arm == null)
                return;
            float p = gun != null && gun.IsReloading ? gun.ReloadProgress : 0f;
            BazookaReloadTimeline.ArmBlend(p, out float reach, out float push);
            if (reachIndex >= 0)
                arm.SetBlendShapeWeight(reachIndex, reach * 100f);
            if (pushIndex >= 0)
                arm.SetBlendShapeWeight(pushIndex, push * 100f);
        }
    }
}
