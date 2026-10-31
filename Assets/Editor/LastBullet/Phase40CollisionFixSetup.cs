using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 숲·성 마을의 충돌(Collider)을 눈에 보이는 모양에 맞게 다시 만든다 (2026-10-31 사용자 요청: 총이 허공에 맞거나, 오브젝트에 걸리거나, 통과하는 곳 검토).
    // 원인: 예전에 상자·캡슐 충돌을 붙일 때 돌려 놓은(회전한) 오브젝트의 크기를 잘못 재서, 눈에 안 보이는 큰 상자가 천막(성 마을 14m)·통나무·마차·바위 둘레에 생겼다.
    //       총알은 그 상자에 맞고 플레이어는 허공에서 걸렸다. 반대로 나무 줄기 위쪽·등불 팔·울타리 이음매는 충돌이 없어 총알이 통과했다.
    // 고치는 방법 (움직이지 않는 장식에만):
    //   - 상자 충돌: 울타리는 실제 크기의 얇은 상자(이음매가 벌어지지 않게 조금 늘림)로, 나머지(우물·마차·천막·통·상자·의자·탁자·벤치·바위·통나무·집 등)는
    //     각 부분의 모양을 따라가는 충돌(MeshCollider)로 바꾼다. 아주 작은 조각(0.25m 미만)은 걸림을 줄이려고 뺀다.
    //   - 성 마을 나무: 줄기 모양을 따라가는 충돌로 (줄기 전체 높이), 등불은 기둥과 팔·등 모양 충돌로 바꾼다.
    //   - 숲 작은 바위, 성 마을 돌(Stone4), 사막 바위(Rock_04)는 충돌이 없어 통과됐으므로 모양 충돌을 붙인다. 풀·꽃·덤불은 일부러 통과되게 둔다.
    //   - 바꾼 뒤 적이 걷는 길(NavMesh)을 다시 굽는다.
    // 다시 실행해도 안전하다 (이미 바뀐 것은 건너뛴다). 메뉴: LastBullet > Phase 40 충돌 다듬기
    public static class Phase40CollisionFixSetup
    {
        const float MinPartSize = 0.25f; // 이보다 작은 조각에는 충돌을 안 붙인다 (잔걸림 방지)

        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity", // 작은 바위(Rock_04)에 충돌이 없었다
        };

        [MenuItem("LastBullet/Phase 40 충돌 다듬기")]
        static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                int boxToMesh = 0, boxExact = 0, trees = 0, lamps = 0, smallStones = 0, parts = 0;

                foreach (var box in Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var renderers = Renderers(box.gameObject);
                    if (box.isTrigger || renderers.Count == 0 || box.GetComponentInParent<Canvas>() != null)
                        continue; // 눈에 안 보이는 경계벽(Wall_*)은 그대로
                    if (box.name.ToLower().Contains("fence"))
                    {
                        SetExactBox(box, renderers);
                        boxExact++;
                        continue;
                    }
                    // 상자와 모양이 같은 오브젝트에 있으면(숲 바위·통나무 등) 상자를 먼저 떼야 모양 충돌을 붙일 수 있다
                    var owner = box.gameObject;
                    Object.DestroyImmediate(box);
                    int added = AddMeshColliders(owner, MinPartSize);
                    if (added > 0)
                    {
                        boxToMesh++;
                        parts += added;
                    }
                    else
                        SetExactBox(owner.AddComponent<BoxCollider>(), renderers); // 전부 작은 조각이면 실제 크기 상자
                }

                foreach (var capsule in Object.FindObjectsByType<CapsuleCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var renderers = Renderers(capsule.gameObject);
                    if (capsule.isTrigger || renderers.Count == 0)
                        continue;
                    string n = capsule.name.ToLower();
                    if (n == "lamp")
                    {
                        int added = AddMeshColliders(capsule.gameObject, MinPartSize);
                        if (added > 0)
                        {
                            Object.DestroyImmediate(capsule);
                            lamps++;
                            parts += added;
                        }
                    }
                    else if (n.StartsWith("tree") && renderers.Count > 1) // 성 마을 나무: 줄기·가지·잎이 따로 있는 모양
                    {
                        int added = AddTrunkColliders(capsule.gameObject, renderers);
                        if (added > 0)
                        {
                            Object.DestroyImmediate(capsule);
                            trees++;
                            parts += added;
                        }
                    }
                }

                // 충돌이 없던 작은 바위·돌
                foreach (var filter in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    string n = filter.gameObject.name;
                    bool stone = n.StartsWith("rpgpp_lt_rock_small") || n == "Stone4" || n == "Rock_04";
                    if (!stone || filter.sharedMesh == null || HasColliderAbove(filter.transform))
                        continue;
                    AddMeshCollider(filter);
                    // 적이 이 돌 위로 걷지 않게 걷는 길에서도 뺀다 (돌 모양이 완만하면 길이 돌 위로 이어진다)
                    if (filter.GetComponent<NavMeshModifier>() == null)
                    {
                        var modifier = filter.gameObject.AddComponent<NavMeshModifier>();
                        modifier.overrideArea = true;
                        modifier.area = 1; // Not Walkable
                    }
                    smallStones++;
                }

                // 걷는 길 다시 굽기 (눈에 안 보이는 큰 상자가 없어졌으니 그 자리도 걸을 수 있게)
                int surfaces = 0;
                foreach (var surface in Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    surface.BuildNavMesh();
                    surfaces++;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 상자 → 모양 충돌 {boxToMesh}개, 울타리 상자 {boxExact}개, 나무 줄기 {trees}개, 등불 {lamps}개, 작은 돌 {smallStones}개 (충돌 조각 {parts}개), NavMesh {surfaces}개 다시 굽기");
            }
            AssetDatabase.SaveAssets();
        }

        static List<MeshRenderer> Renderers(GameObject go)
        {
            var list = new List<MeshRenderer>(go.GetComponentsInChildren<MeshRenderer>(true));
            return list;
        }

        static bool HasColliderAbove(Transform t)
        {
            for (var a = t; a != null; a = a.parent)
            {
                var c = a.GetComponent<Collider>();
                if (c != null && !c.isTrigger)
                    return true;
            }
            return false;
        }

        static void AddMeshCollider(MeshFilter filter)
        {
            var mc = filter.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = filter.sharedMesh;
        }

        // 자식 모양(MeshFilter)마다 모양을 따라가는 충돌을 붙인다. 너무 작은 조각은 뺀다. 붙인 개수를 돌려준다.
        static int AddMeshColliders(GameObject root, float minSize)
        {
            int count = 0;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>();
                if (renderer == null || filter.sharedMesh == null || filter.GetComponent<Collider>() != null)
                    continue;
                var size = renderer.bounds.size;
                if (Mathf.Max(size.x, Mathf.Max(size.y, size.z)) < minSize)
                    continue;
                AddMeshCollider(filter);
                count++;
            }
            return count;
        }

        // 성 마을 나무: 땅에 닿아 있고 가는(너비 2.4m 이하) 긴 조각 = 줄기. 잎·가지 덩어리는 제외한다.
        static int AddTrunkColliders(GameObject root, List<MeshRenderer> renderers)
        {
            float groundY = float.MaxValue;
            foreach (var r in renderers)
                groundY = Mathf.Min(groundY, r.bounds.min.y);

            int count = 0;
            foreach (var r in renderers)
            {
                var filter = r.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null || filter.GetComponent<Collider>() != null)
                    continue;
                var size = r.bounds.size;
                bool touchesGround = r.bounds.min.y <= groundY + 0.8f;
                bool slim = Mathf.Max(size.x, size.z) <= 2.4f && size.y >= 1.2f;
                if (touchesGround && slim)
                {
                    AddMeshCollider(filter);
                    count++;
                }
            }
            return count;
        }

        // 오브젝트의 로컬 좌표에서 모양 전체를 감싸는 실제 크기의 상자 (회전해도 틀리지 않는다)
        static void SetExactBox(BoxCollider box, List<MeshRenderer> renderers)
        {
            var root = box.transform;
            Bounds local = default;
            bool first = true;
            foreach (var r in renderers)
            {
                var filter = r.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;
                var mb = filter.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var p = root.InverseTransformPoint(filter.transform.TransformPoint(corner));
                    if (first) { local = new Bounds(p, Vector3.zero); first = false; }
                    else local.Encapsulate(p);
                }
            }
            if (first)
                return;

            var scale = root.lossyScale;
            var size = local.size;
            // 울타리: 얇아서 뚫고 지나가거나 총알이 벌어진 틈으로 새지 않게 두께를 0.3m 이상, 길이는 이음매가 겹치게 0.1m 늘린다
            bool lengthX = size.x * scale.x >= size.z * scale.z;
            if (lengthX)
            {
                size.x += 0.1f / Mathf.Max(0.0001f, scale.x);
                size.z = Mathf.Max(size.z, 0.3f / Mathf.Max(0.0001f, scale.z));
            }
            else
            {
                size.z += 0.1f / Mathf.Max(0.0001f, scale.z);
                size.x = Mathf.Max(size.x, 0.3f / Mathf.Max(0.0001f, scale.x));
            }
            box.center = local.center;
            box.size = size;
        }
    }
}
