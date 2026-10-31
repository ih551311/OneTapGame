using UnityEngine;
using UnityEngine.AI;

namespace LastBullet.Enemy
{
    // 분열 적 (2026-10-31 사용자 요청): 처치되면 그 자리에서 작은 적 여러 마리로 나뉜다.
    // 작은 적(Enemy_SplitterMini)은 더 빠르고 머리가 작다. 작은 적은 다시 나뉘지 않는다 (miniPrefab을 비워 둔다).
    // 작은 적도 EnemyHealth·EnemyDrop을 가진 보통 적이다. 몸 색(= 잡으면 주는 물감 색)은 원래 적의 색을 이어받는다.
    // 몸샷으로 잡든 헤드샷으로 잡든 똑같이 나뉜다.
    // 붙일 곳: 분열 적 프리팹의 맨 위 오브젝트 (EnemyHealth, EnemyDrop과 함께. 메뉴 LastBullet > Phase 46 분열 적 만들기)
    [RequireComponent(typeof(EnemyHealth), typeof(EnemyDrop))]
    public class EnemySplitter : MonoBehaviour
    {
        [Tooltip("처치되면 나올 작은 적 (Prefabs/Enemy_SplitterMini). 비우면 나뉘지 않는다")]
        [SerializeField] GameObject miniPrefab;

        [Tooltip("나오는 마리 수 (기본값)")]
        [SerializeField] int count = 2;

        [Tooltip("죽은 자리에서 얼마나 떨어져 나오는지 (m, 기본값)")]
        [SerializeField] float spread = 1.2f;

        void Awake()
        {
            GetComponent<EnemyHealth>().Died += OnDied;
        }

        void OnDied(EnemyHealth _)
        {
            if (miniPrefab == null)
                return;

            var drop = GetComponent<EnemyDrop>();
            float startAngle = Random.value * 360f;
            for (int i = 0; i < count; i++)
            {
                // 둥글게 나란히 퍼진다. 걸을 수 있는 자리(NavMesh)로 맞춘다.
                float angle = (startAngle + 360f * i / count) * Mathf.Deg2Rad;
                Vector3 position = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * spread;
                if (NavMesh.SamplePosition(position, out var hit, 3f, NavMesh.AllAreas))
                    position = hit.position;
                else
                    position = transform.position;

                // 부모는 이 적이 속한 Enemies 폴더 (이 적은 곧 사라지므로 이 적 아래에 두지 않는다)
                var mini = Instantiate(miniPrefab, position, transform.rotation, transform.parent);
                mini.name = name + "_Mini" + (i + 1);
                var miniDrop = mini.GetComponent<EnemyDrop>();
                if (miniDrop != null)
                    miniDrop.InheritColor(drop.DropColor);
            }
            Debug.Log($"[EnemySplitter] {name} 처치 → 작은 적 {count}마리로 나뉨");
        }
    }
}
