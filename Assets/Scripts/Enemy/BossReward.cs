using System.Collections;
using LastBullet.Effects;
using LastBullet.Inventory;
using LastBullet.Stage;
using LastBullet.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastBullet.Enemy
{
    // 보스를 처치하면 물감을 많이 준다 (2026-11-06 사용자 결정: "물감을 많이 준다").
    //  - 그 맵에서 나오는 물감 3색을 모두 paintPerColor씩, 종이조각을 paperScraps개 준다 (전투 가방에 바로 들어간다).
    //  - 일반 적의 보상(EnemyDrop)은 보스 프리팹에서 0으로 해 두었다.
    // 붙일 곳: 보스 프리팹(Enemy_Boss)의 맨 위 오브젝트 (EnemyHealth와 함께)
    [RequireComponent(typeof(EnemyHealth))]
    public class BossReward : MonoBehaviour
    {
        [Tooltip("물감 한 색마다 주는 양 (일반 적은 한 마리에 30)")]
        [SerializeField] int paintPerColor = 150;

        [SerializeField] int paperScraps = 5;

        void Awake()
        {
            GetComponent<EnemyHealth>().Died += OnDied;
        }

        void OnDied(EnemyHealth _)
        {
            var colors = StageColors.For(SceneManager.GetActiveScene().name);
            if (colors == null || RunBag.Current == null)
                return;

            Vector3 head = transform.position + Vector3.up * 4f;
            bool first = true;
            foreach (var color in colors)
            {
                RunBag.Current.Add(first ? paperScraps : 0, color, paintPerColor);
                first = false;
                PaintCollectEffect.Play(color, head);
            }
            Debug.Log($"[BossReward] 보스 처치: 물감 {colors.Count}색 × {paintPerColor}, 종이조각 {paperScraps}");
        }
    }
}
