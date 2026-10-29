using System;
using System.Collections;
using System.Collections.Generic;
using LastBullet.Enemy;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastBullet.Stage
{
    // 스테이지 진행 (기획서 2.6). 씬에 있는 적을 모두 처치하면 클리어.
    // 클리어하면 "클리어" 그림을 띄우고 잠시 뒤 미술관(Hideout)으로 돌아간다.
    // Phase 5에서 더할 부분: 클리어 시 전투 가방 정산(RunBag.Settle), 다음 스테이지 해금.
    // 붙일 곳: 스테이지 씬의 StageManager 오브젝트
    public class StageManager : MonoBehaviour
    {
        [Tooltip("클리어 후 돌아갈 씬")]
        [SerializeField] string returnScene = "Hideout";

        [SerializeField] float returnDelay = 3f;

        [Tooltip("클리어 때 켜는 UI (Assets/UI/StageClear.png)")]
        [SerializeField] GameObject clearMessage;

        public bool IsCleared { get; private set; }
        public int RemainingEnemies => alive.Count;
        public event Action Cleared;

        readonly List<EnemyHealth> alive = new List<EnemyHealth>();

        void Start()
        {
            // 씬에 놓인 적을 모두 찾아 처치를 지켜본다
            foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None))
            {
                alive.Add(enemy);
                enemy.Died += OnEnemyDied;
            }
            if (clearMessage != null)
                clearMessage.SetActive(false);
            Debug.Log($"[StageManager] 시작: 적 {alive.Count}마리");
        }

        void OnEnemyDied(EnemyHealth enemy)
        {
            alive.Remove(enemy);
            Debug.Log($"[StageManager] 남은 적 {alive.Count}마리");
            if (alive.Count == 0 && !IsCleared)
                Clear();
        }

        void Clear()
        {
            IsCleared = true;
            Debug.Log("[StageManager] 클리어!");
            if (clearMessage != null)
                clearMessage.SetActive(true);
            Cleared?.Invoke();
            StartCoroutine(ReturnAfterDelay());
        }

        IEnumerator ReturnAfterDelay()
        {
            yield return new WaitForSeconds(returnDelay);
            SceneManager.LoadScene(returnScene);
        }
    }
}
