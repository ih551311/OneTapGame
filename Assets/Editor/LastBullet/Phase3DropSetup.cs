using LastBullet.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // Phase 3-2: 떨어진 탄환 프리팹을 만들어 총에 연결하고, 명중 테스트용 허수아비(Hitbox)를 세운다.
    // 메뉴: LastBullet > Phase 3-2 탄환 떨어뜨리기와 허수아비 만들기
    public static class Phase3DropSetup
    {
        const string PrefabPath = "Assets/Prefabs/DroppedBullet.prefab";
        const string TracerMaterialPath = "Assets/Materials/Tracer.mat";

        [MenuItem("LastBullet/Phase 3-2 탄환 떨어뜨리기와 허수아비 만들기")]
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

            var player = GameObject.Find("Player");
            var gun = player != null ? player.GetComponent<OneBulletGun>() : null;
            var targets = GameObject.Find("TestTargets (임시)");
            if (gun == null || targets == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 3-1 메뉴를 먼저 실행하세요.", "확인");
                return;
            }
            if (targets.transform.Find("Target_Dummy") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "이미 Target_Dummy가 있어요.", "확인");
                return;
            }

            // 1. 떨어진 탄환 프리팹: 바닥에 누운 노란 캡슐 (충돌 없음, 회수는 거리로 판정)
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                var root = new GameObject("DroppedBullet");
                var model = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                model.name = "Model";
                Object.DestroyImmediate(model.GetComponent<Collider>());
                model.transform.SetParent(root.transform, false);
                model.transform.localPosition = new Vector3(0f, 0.06f, 0f);
                model.transform.localRotation = Quaternion.Euler(0f, 0f, 90f); // 눕힌다
                model.transform.localScale = new Vector3(0.12f, 0.15f, 0.12f);
                model.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(TracerMaterialPath);
                prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Object.DestroyImmediate(root);
            }

            // 2. 총에 프리팹 연결
            var so = new SerializedObject(gun);
            so.FindProperty("droppedBulletPrefab").objectReferenceValue = prefab;
            so.ApplyModifiedProperties();

            // 3. 허수아비: 몸(Body)과 머리(Head)에 Hitbox. 맞히면 탄환을 바로 다시 쓸 수 있다.
            var dummy = new GameObject("Target_Dummy");
            Undo.RegisterCreatedObjectUndo(dummy, "Create Target_Dummy");
            dummy.transform.SetParent(targets.transform, false);
            dummy.transform.position = new Vector3(3f, 0f, 6f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(dummy.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.AddComponent<Hitbox>(); // 기본값 Body

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Head";
            head.transform.SetParent(dummy.transform, false);
            head.transform.localPosition = new Vector3(0f, 2.35f, 0f);
            head.transform.localScale = Vector3.one * 0.7f; // 기획서 2.11: 머리를 몸보다 약간 크게
            var headBox = new SerializedObject(head.AddComponent<Hitbox>());
            headBox.FindProperty("part").enumValueIndex = (int)HitPart.Head;
            headBox.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = dummy;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] Phase 3-2: DroppedBullet 프리팹을 총에 연결하고 Target_Dummy를 만들고 Hideout 씬을 저장했어요.");
        }
    }
}
