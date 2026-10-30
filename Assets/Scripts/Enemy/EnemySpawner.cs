using UnityEngine;
using UnityEngine.AI;

namespace LastBullet.Enemy
{
    // 적 스포너: 스테이지가 시작될 때 자기 자리에서 적 한 마리를 만들어 낸다 (2026-10-30 사용자 요청: 적마다 스포너).
    // 스포너 하나 = 적 한 마리. 적의 종류는 Enemy Prefab 칸으로 정한다 (돌진형 / 방어형 / 원거리형 ...).
    // 위치와 방향은 이 오브젝트의 Transform이다. Scene 창에서 옮기거나 돌리면 적도 그 자리에서 나온다.
    // 적은 스테이지가 시작될 때 만들어지므로, StageManager(적을 모두 처치하면 클리어)보다 먼저 실행된다.
    // 붙일 곳: 숲 씬의 Spawners 아래 Spawner_... 오브젝트
    [DefaultExecutionOrder(-100)]
    public class EnemySpawner : MonoBehaviour
    {
        [Tooltip("여기서 나올 적 (Prefabs/Enemy_Charger, Enemy_Shield, Enemy_Thrower)")]
        [SerializeField] GameObject enemyPrefab;

        [Tooltip("만들어진 적을 담아 둘 부모 오브젝트 (보통 Enemies). 비우면 씬 맨 위에 둔다")]
        [SerializeField] Transform enemiesParent;

        [Tooltip("켜면 적이 시작할 때 플레이어 쪽을 바라본다. 끄면 이 오브젝트의 방향을 따른다")]
        [SerializeField] bool faceTowardsPlayer = true;

        public GameObject Spawned { get; private set; }

        void Start()
        {
            if (enemyPrefab == null)
            {
                Debug.LogWarning($"[EnemySpawner] {name}: 적 프리팹이 비어 있어요");
                return;
            }

            // NavMesh(적이 걷는 길) 위로 자리를 맞춘다. 길이 없는 곳이면 그 자리에 그대로 둔다.
            Vector3 position = transform.position;
            if (NavMesh.SamplePosition(position, out var hit, 8f, NavMesh.AllAreas))
                position = hit.position;

            Quaternion rotation = transform.rotation;
            var player = GameObject.FindWithTag("Player");
            if (faceTowardsPlayer && player != null)
            {
                Vector3 toPlayer = player.transform.position - position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f)
                    rotation = Quaternion.LookRotation(toPlayer);
            }

            Spawned = Instantiate(enemyPrefab, position, rotation, enemiesParent);
            // Spawner_Charger_01 → Enemy_Charger_01 (Hierarchy에서 어느 스포너의 적인지 알아보기 쉽게)
            Spawned.name = name.StartsWith("Spawner_") ? "Enemy_" + name.Substring("Spawner_".Length) : $"{enemyPrefab.name}_{name}";
        }

        // Scene 창에서 스포너가 어디인지, 어떤 적인지 보이게 한다 (게임 화면에는 안 나온다)
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.1f, 0.7f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, transform.position + Vector3.up * 2.3f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.9f, transform.position + Vector3.up * 0.9f + transform.forward * 1.2f);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2.6f, enemyPrefab != null ? enemyPrefab.name : "(적 없음)");
#endif
        }
    }
}
