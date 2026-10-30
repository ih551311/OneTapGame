using LastBullet.Effects;
using LastBullet.Inventory;
using LastBullet.Stage;
using UnityEngine;

namespace LastBullet.Enemy
{
    // 적을 처치하면 재료를 떨어뜨린다 (기획서 2.5): 종이조각 1개 + 9색 중 랜덤 1색 물감 30.
    // (2026-10-30 사용자 요청으로 50 → 30. 적 프리팹에 저장된 값이 우선이므로 바꿀 때는 프리팹의 Paint Amount를 고친다.)
    // 물감 색은 지역마다 정해진 3색 중에서 고른다 (PaintZone). 지역이 없는 곳(숲)은 9색 중 랜덤.
    // 바닥에 떨어뜨리지 않고 바로 전투 가방(RunBag)에 들어간다.
    // 처치 연출: 머리에서 그 물감 색의 방울이 터지고 바닥에 자국이 남는다 (PaintBurstEffect).
    // EnemyHealth의 Died는 한 번만 불리므로 보상도 한 번만 지급된다.
    // 붙일 곳: 적 프리팹의 맨 위 오브젝트 (EnemyHealth와 함께)
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyDrop : MonoBehaviour
    {
        [SerializeField] int paperScraps = 1;
        [SerializeField] int paintAmount = 30;

        [Tooltip("처치 때 터지는 물감 연출 (Prefabs/PaintBurst). 비우면 연출 없음")]
        [SerializeField] PaintBurstEffect burstPrefab;

        [Tooltip("물감이 터지는 자리. 보통 Head")]
        [SerializeField] Transform burstPoint;

        // 만들어진 자리. 물감 색 지역(PaintZone)은 이 자리로 정한다 (적이 쫓아가며 지역을 벗어나도 색은 그대로)
        Vector3 spawnPosition;

        void Awake()
        {
            spawnPosition = transform.position;
            GetComponent<EnemyHealth>().Died += OnDied;
        }

        void OnDied(EnemyHealth _)
        {
            // 지역이 있으면 그 지역의 색 중에서, 없으면 9색 중 랜덤
            PaintColor color = PaintZone.PickColor(spawnPosition);

            // 전투 가방이 없는 씬(예: 테스트)에서는 재료를 주지 않고 연출만 한다
            if (RunBag.Current != null)
                RunBag.Current.Add(paperScraps, color, paintAmount);

            if (burstPrefab != null)
            {
                Vector3 head = burstPoint != null ? burstPoint.position : transform.position + Vector3.up * 1.8f;
                PaintBurstEffect.Spawn(burstPrefab, head, transform.position, PaintColors.Colors[(int)color]);
            }
        }
    }
}
