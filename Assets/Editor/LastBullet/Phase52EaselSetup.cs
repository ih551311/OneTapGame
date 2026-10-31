using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 미술관의 그림 그리는 곳(PaintWorkbench, 임시 상자)을 사용자가 가져온 이젤 모델(Assets/3Dcanvas/canvas.fbx)로 바꾼다 (2026-11-01 사용자 요청).
    //   PaintWorkbench 오브젝트는 그대로 둔다 (E키 상호작용 InteractDevice와 팝업 연결이 여기에 있다).
    //   상자 모양(MeshFilter, MeshRenderer)만 없애고, 자식 Easel에 이젤 모델을 넣는다. 충돌(BoxCollider)은 이젤 크기에 맞춘다.
    //   모델의 피벗은 몸통 가운데라서 높이를 0.80 올려 다리가 바닥에 닿게 한다. 그림 쪽(+Z)이 플레이어 시작 자리를 향하도록 돌린다.
    // 겉모습을 바꿀 때는 PaintWorkbench 아래 Easel만 바꾸면 된다. 다시 실행하면 Easel을 새로 만든다. 메뉴: LastBullet > Phase 52 미술관 그림 작업대를 이젤로 바꾸기
    public static class Phase52EaselSetup
    {
        const string ModelPath = "Assets/3Dcanvas/canvas.fbx";
        const float LiftToFloor = 0.80f;                              // 모델 피벗(가운데)에서 바닥까지
        static readonly Vector3 ColliderSize = new Vector3(1.08f, 1.61f, 0.63f);

        [MenuItem("LastBullet/Phase 52 미술관 그림 작업대를 이젤로 바꾸기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var bench = GameObject.Find("PaintWorkbench");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (bench == null || model == null)
            {
                Debug.LogError("[LastBullet] PaintWorkbench나 이젤 모델이 없어요");
                return;
            }

            // 상자 모양 제거 (충돌·상호작용은 유지)
            var filter = bench.GetComponent<MeshFilter>();
            if (filter != null)
                Object.DestroyImmediate(filter);
            var renderer = bench.GetComponent<MeshRenderer>();
            if (renderer != null)
                Object.DestroyImmediate(renderer);

            // 위치·크기: 바닥 기준 (상자는 높이 1m 중심이 0.5였다)
            var position = bench.transform.position;
            bench.transform.position = new Vector3(position.x, 0f, position.z);
            bench.transform.localScale = Vector3.one;

            // 그림 쪽이 플레이어 시작 자리를 향하게
            var player = GameObject.FindWithTag("Player");
            float yaw = 0f;
            if (player != null)
            {
                Vector3 toPlayer = player.transform.position - bench.transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f)
                    yaw = Quaternion.LookRotation(toPlayer).eulerAngles.y;
            }
            bench.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var collider = bench.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.size = ColliderSize;
                collider.center = new Vector3(0f, ColliderSize.y * 0.5f, 0f);
            }

            var old = bench.transform.Find("Easel");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            var easel = (GameObject)PrefabUtility.InstantiatePrefab(model, bench.transform);
            easel.name = "Easel";
            easel.transform.localPosition = new Vector3(0f, LiftToFloor, 0f);
            easel.transform.localRotation = model.transform.localRotation; // 모델 파일 자체가 눕혀져(X 270도) 있어서 그 회전을 그대로 쓴다
            easel.transform.localScale = Vector3.one;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LastBullet] 그림 작업대를 이젤로 바꿨어요 (방향 {yaw:0}도).");
        }
    }
}
