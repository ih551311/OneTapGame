using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 미술관의 전투 장소 이동 장치(StageMap, 임시 상자)를 사용자가 가져온 포털 모델(Assets/3Dmake/Untitled.fbx)로 바꾼다 (2026-11-01 사용자 요청).
    //   StageMap 오브젝트는 그대로 둔다 (E키 상호작용 InteractDevice·StageSelectDevice와 장소 선택 팝업 연결이 여기에 있다).
    //   상자 모양(MeshFilter, MeshRenderer)만 없애고, 자식 Portal에 모델을 넣는다. 충돌(BoxCollider)은 모델(바닥 카펫 제외)에 맞춘다.
    //   바닥에 서도록 높이를 맞추고, 모델의 앞쪽(거울 쪽)이 플레이어 시작 자리를 향하도록 돌린다.
    // 겉모습을 바꿀 때는 StageMap 아래 Portal만 바꾸면 된다. 다시 실행하면 Portal을 새로 만든다.
    // 메뉴: LastBullet > Phase 53 미술관 장소 이동 장치를 포털로 바꾸기
    public static class Phase53PortalSetup
    {
        const string ModelPath = "Assets/3Dmake/Untitled.fbx";
        const float Size = 1.3f; // 전체 크기 배율 (캐릭터가 머리 큰 만화 체형이라 실제보다 크게)

        [MenuItem("LastBullet/Phase 53 미술관 장소 이동 장치를 포털로 바꾸기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var stage = GameObject.Find("StageMap");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (stage == null || model == null)
            {
                Debug.LogError("[LastBullet] StageMap이나 포털 모델이 없어요");
                return;
            }

            var filter = stage.GetComponent<MeshFilter>();
            if (filter != null)
                Object.DestroyImmediate(filter);
            var renderer = stage.GetComponent<MeshRenderer>();
            if (renderer != null)
                Object.DestroyImmediate(renderer);

            var position = stage.transform.position;
            stage.transform.position = new Vector3(position.x, 0f, position.z);
            stage.transform.localScale = Vector3.one * Size;

            var player = GameObject.FindWithTag("Player");
            float yaw = 0f;
            if (player != null)
            {
                Vector3 toPlayer = player.transform.position - stage.transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f)
                    yaw = Quaternion.LookRotation(toPlayer).eulerAngles.y;
            }
            stage.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var old = stage.transform.Find("Portal");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            var portal = (GameObject)PrefabUtility.InstantiatePrefab(model, stage.transform);
            portal.name = "Portal";
            portal.transform.localPosition = Vector3.zero;
            portal.transform.localRotation = model.transform.localRotation; // 모델 파일 자체 회전(X 270도)을 그대로 쓴다
            portal.transform.localScale = Vector3.one;

            // 거울 면은 3ds Max용 셰이더(PhysicalMaterial3DsMax)라 이 프로젝트(URP)에서 투명하게 보인다. URP 전용 은빛 거울 머티리얼로 바꾼다.
            // 직접 만든 그림·재질로 바꾸고 싶으면 Assets/Materials/PortalMirror.mat의 Base Map에 그림을 끌어 놓으면 된다 (이미 있으면 그대로 둔다).
            var mirrorMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PortalMirror.mat");
            if (mirrorMaterial == null)
            {
                mirrorMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mirrorMaterial.SetColor("_BaseColor", new Color(0.55f, 0.62f, 0.78f, 1f));
                mirrorMaterial.SetFloat("_Metallic", 1f);
                mirrorMaterial.SetFloat("_Smoothness", 0.95f);
                AssetDatabase.CreateAsset(mirrorMaterial, "Assets/Materials/PortalMirror.mat");
            }
            foreach (var r in portal.GetComponentsInChildren<Renderer>())
                if (r.name.StartsWith("Mirror_Surface"))
                    r.sharedMaterial = mirrorMaterial;

            // 이 오브젝트의 로컬 좌표에서 모델 범위를 구한다 (바닥 카펫은 충돌에서 뺀다)
            Bounds all = default, solid = default;
            bool firstAll = true, firstSolid = true;
            foreach (var r in portal.GetComponentsInChildren<Renderer>())
            {
                var local = ToLocal(stage.transform, r.bounds);
                Encapsulate(ref all, ref firstAll, local);
                if (!r.name.Contains("Carpet"))
                    Encapsulate(ref solid, ref firstSolid, local);
            }

            // 모델 다리(바닥)가 y=0에 오도록 올린다
            float lift = -all.min.y;
            portal.transform.localPosition = new Vector3(0f, lift, 0f);
            solid.center += new Vector3(0f, lift, 0f);

            var collider = stage.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.center = solid.center;
                collider.size = solid.size;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LastBullet] 장소 이동 장치를 포털로 바꿨어요 (방향 {yaw:0}도, 충돌 {solid.size.x:0.00}×{solid.size.y:0.00}×{solid.size.z:0.00}).");
        }

        static Bounds ToLocal(Transform space, Bounds world)
        {
            var result = new Bounds(space.InverseTransformPoint(world.center), Vector3.zero);
            for (int i = 0; i < 8; i++)
            {
                var corner = world.center + Vector3.Scale(world.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                result.Encapsulate(space.InverseTransformPoint(corner));
            }
            return result;
        }

        static void Encapsulate(ref Bounds bounds, ref bool first, Bounds other)
        {
            if (first)
            {
                bounds = other;
                first = false;
            }
            else
                bounds.Encapsulate(other);
        }
    }
}
