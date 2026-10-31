using LastBullet.Combat;
using LastBullet.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // Phase 3-1: 총(임시 상자), 총구, 탄도선, OneBulletGun 스크립트, 테스트용 과녁을 만든다.
    // 메뉴: LastBullet > Phase 3-1 총과 발사 만들기
    public static class Phase3FireSetup
    {
        const string TracerMaterialPath = "Assets/Materials/Tracer.mat";

        [MenuItem("LastBullet/Phase 3-1 총과 발사 만들기")]
        static void Create()
        {
            // 1_Museum 씬이 열려 있지 않으면 자동으로 연다.
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "1_Museum")
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            }

            var player = GameObject.Find("Player");
            if (player == null || player.GetComponent<PlayerMovement>() == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 2 메뉴를 먼저 모두 실행하세요.", "확인");
                return;
            }
            if (player.GetComponent<OneBulletGun>() != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Player에 이미 OneBulletGun이 있어요.", "확인");
                return;
            }

            // 1. 탄도선과 맞은 자리 표시에 쓸 노란 머티리얼 (물감 노랑 #FACC15)
            var tracerMaterial = AssetDatabase.LoadAssetAtPath<Material>(TracerMaterialPath);
            if (tracerMaterial == null)
            {
                tracerMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                tracerMaterial.SetColor("_BaseColor", new Color32(0xFA, 0xCC, 0x15, 0xFF));
                AssetDatabase.CreateAsset(tracerMaterial, TracerMaterialPath);
            }

            // 2. 총(임시 상자): 오른손 위치. 겉모습만 있고 충돌은 없다.
            var gun = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Undo.RegisterCreatedObjectUndo(gun, "Create Gun");
            gun.name = "Gun";
            Object.DestroyImmediate(gun.GetComponent<Collider>());
            gun.transform.SetParent(player.transform, false);
            gun.transform.localPosition = new Vector3(0.35f, 1.25f, 0.35f);
            gun.transform.localScale = new Vector3(0.1f, 0.12f, 0.45f);

            // 3. 총구: 총 앞 끝. 크기가 바뀐 Gun의 자식으로 두면 계산이 헷갈리므로 Player 바로 아래에 둔다.
            var muzzle = new GameObject("Muzzle");
            Undo.RegisterCreatedObjectUndo(muzzle, "Create Muzzle");
            muzzle.transform.SetParent(player.transform, false);
            muzzle.transform.localPosition = new Vector3(0.35f, 1.25f, 0.6f);

            // 4. 탄도선: 발사 순간 총구에서 맞은 곳까지 보이는 노란 선
            var tracerObject = new GameObject("BulletTracer");
            Undo.RegisterCreatedObjectUndo(tracerObject, "Create BulletTracer");
            tracerObject.transform.SetParent(player.transform, false);
            var tracer = tracerObject.AddComponent<LineRenderer>();
            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
            tracer.startWidth = tracer.endWidth = 0.03f;
            tracer.sharedMaterial = tracerMaterial;
            tracer.shadowCastingMode = ShadowCastingMode.Off;
            tracer.receiveShadows = false;
            tracer.enabled = false;

            // 5. OneBulletGun을 Player에 붙이고 참조를 연결한다.
            var gunScript = Undo.AddComponent<OneBulletGun>(player);
            var so = new SerializedObject(gunScript);
            so.FindProperty("muzzle").objectReferenceValue = muzzle.transform;
            so.FindProperty("tracer").objectReferenceValue = tracer;
            so.FindProperty("impactMaterial").objectReferenceValue = tracerMaterial;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 6. 테스트용 과녁 (임시, 적이 생기면 지운다)
            var targets = new GameObject("TestTargets (temp)");
            Undo.RegisterCreatedObjectUndo(targets, "Create TestTargets");
            CreateTarget(targets.transform, "Target_Wall", new Vector3(0f, 1f, 8f), new Vector3(2f, 2f, 0.5f));
            CreateTarget(targets.transform, "Target_Box", new Vector3(-4f, 0.5f, 12f), Vector3.one);
            CreateTarget(targets.transform, "Target_Pillar", new Vector3(5f, 1.5f, 15f), new Vector3(1f, 3f, 1f));

            Selection.activeGameObject = player;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] Phase 3-1: 총, 총구, 탄도선, OneBulletGun, 테스트 과녁을 만들고 1_Museum 씬을 저장했어요.");
        }

        static void CreateTarget(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            var target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = name;
            target.transform.SetParent(parent, false);
            target.transform.position = position;
            target.transform.localScale = scale;
        }
    }
}
