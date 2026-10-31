using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace LastBullet.Enemy
{
    // 적 스포너: 전투 씬이 시작될 때 자기 자리에서 적 한 마리를 만들고, 그 적이 죽으면 시간이 지난 뒤 다시 만든다.
    // (2026-10-30 사용자 결정: 스테이지 클리어 없이 자유롭게 물감을 파밍한다. 그래서 적은 계속 다시 나온다.)
    // 스포너 하나 = 적 한 마리. 적의 종류는 Enemy Prefab 칸으로 정한다 (돌진형 / 방어형 / 원거리형 ...).
    // 위치와 방향은 이 오브젝트의 Transform이다. Scene 창에서 옮기거나 돌리면 적도 그 자리에서 나온다.
    // 다시 나올 때 플레이어가 가까이 있으면(눈앞에서 갑자기 생기지 않도록) 멀어질 때까지 기다린다.
    // 붙일 곳: 숲 씬의 Spawners 아래 Spawner_... 오브젝트
    public class EnemySpawner : MonoBehaviour
    {
        [Tooltip("여기서 나올 적 (Prefabs/Enemy_Charger, Enemy_Shield, Enemy_Thrower)")]
        [SerializeField] GameObject enemyPrefab;

        [Tooltip("만들어진 적을 담아 둘 부모 오브젝트 (보통 Enemies). 비우면 씬 맨 위에 둔다")]
        [SerializeField] Transform enemiesParent;

        [Tooltip("켜면 적이 시작할 때 플레이어 쪽을 바라본다. 끄면 이 오브젝트의 방향을 따른다")]
        [SerializeField] bool faceTowardsPlayer = true;

        [Tooltip("적이 죽은 뒤 다시 나오게 할지")]
        [SerializeField] bool respawn = true;

        [Tooltip("적이 죽은 뒤 다시 나오기까지 걸리는 시간 (초, 기본값)")]
        [SerializeField] float respawnDelay = 20f; // 2026-10-31: 몸샷도 처치돼서 적이 빨리 줄기에 60 → 20

        [Tooltip("다시 나올 때 플레이어가 이 거리 안에 있으면 멀어질 때까지 기다린다 (m, 기본값)")]
        [SerializeField] float respawnSafeDistance = 20f;

        public GameObject Spawned { get; private set; }

        Transform player;

        void Start()
        {
            var playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
            Spawn();
        }

        void Spawn()
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
            if (faceTowardsPlayer && player != null)
            {
                Vector3 toPlayer = player.position - position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f)
                    rotation = Quaternion.LookRotation(toPlayer);
            }

            Spawned = Instantiate(enemyPrefab, position, rotation, enemiesParent);
            // Spawner_Charger_01 → Enemy_Charger_01 (Hierarchy에서 어느 스포너의 적인지 알아보기 쉽게)
            Spawned.name = name.StartsWith("Spawner_") ? "Enemy_" + name.Substring("Spawner_".Length) : $"{enemyPrefab.name}_{name}";

            if (respawn)
            {
                var health = Spawned.GetComponent<EnemyHealth>();
                if (health != null)
                    health.Died += _ => StartCoroutine(RespawnAfterDelay());
            }
        }

        IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(respawnDelay);

            // 플레이어가 가까이 있으면 멀어질 때까지 기다린다
            while (player != null && FlatDistance(player.position) < respawnSafeDistance)
                yield return new WaitForSeconds(1f);

            Spawn();
        }

        float FlatDistance(Vector3 target)
        {
            Vector3 offset = target - transform.position;
            offset.y = 0f;
            return offset.magnitude;
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
