using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // Phase 2-1: Hideout 씬에 바닥과 임시 플레이어(캡슐)를 만든다.
    // 메뉴: LastBullet > Phase 2-1 바닥과 플레이어 만들기
    public static class Phase2PlayerSetup
    {
        [MenuItem("LastBullet/Phase 2-1 바닥과 플레이어 만들기")]
        static void Create()
        {
            // Hideout 씬이 열려 있지 않으면 자동으로 연다.
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Hideout")
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                scene = EditorSceneManager.OpenScene("Assets/Scenes/Hideout.unity");
            }
            if (GameObject.Find("Player") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 Player가 있어요. 다시 만들려면 Ground와 Player를 지운 뒤 실행하세요.", "확인");
                return;
            }

            // 바닥: Plane은 기본 10m x 10m이므로 5배로 키워 50m x 50m
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            Undo.RegisterCreatedObjectUndo(ground, "Create Ground");

            // 플레이어: 위치(0,0,0)이 발바닥. 충돌은 CharacterController가 맡는다.
            var player = new GameObject("Player");
            player.tag = "Player";
            var controller = player.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0f, 1f, 0f);
            Undo.RegisterCreatedObjectUndo(player, "Create Player");

            // 겉모습: 캡슐. 충돌은 CharacterController가 하므로 캡슐의 콜라이더는 지운다.
            var body = CreateVisual(PrimitiveType.Capsule, "Body", player.transform);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);

            // 앞쪽 표시: 캐릭터가 어느 쪽을 보는지 알 수 있게 작은 상자를 얼굴 위치에 붙인다.
            var front = CreateVisual(PrimitiveType.Cube, "Front", player.transform);
            front.transform.localPosition = new Vector3(0f, 1.5f, 0.5f);
            front.transform.localScale = new Vector3(0.3f, 0.2f, 0.2f);

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] Phase 2-1: Ground와 Player를 만들고 Hideout 씬을 저장했어요.");
        }

        static GameObject CreateVisual(PrimitiveType type, string name, Transform parent)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
