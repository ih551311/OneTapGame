using LastBullet.Inventory;
using LastBullet.Stage;
using UnityEngine;

namespace LastBullet.Enemy
{
    // 적을 처치하면 재료를 떨어뜨린다 (기획서 2.5): 종이조각 1개 + 9색 중 랜덤 1색 물감 50.
    // 바닥에 떨어뜨리지 않고 바로 전투 가방(RunBag)에 들어간다.
    // EnemyHealth의 Died는 한 번만 불리므로 보상도 한 번만 지급된다.
    // 붙일 곳: 적 프리팹의 맨 위 오브젝트 (EnemyHealth와 함께)
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyDrop : MonoBehaviour
    {
        [SerializeField] int paperScraps = 1;
        [SerializeField] int paintAmount = 50;

        void Awake()
        {
            GetComponent<EnemyHealth>().Died += OnDied;
        }

        void OnDied(EnemyHealth _)
        {
            if (RunBag.Current == null)
                return; // 전투 가방이 없는 씬 (예: 테스트)
            RunBag.Current.Add(paperScraps, PaintColors.Random(), paintAmount);
        }
    }
}
