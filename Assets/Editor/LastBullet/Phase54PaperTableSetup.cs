using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 미술관의 종이 합성대(PaperCraftTable, 임시 상자)를 사용자가 가져온 종이 복원 작업대 모델(Assets/3Dmake/paper.fbx)로 바꾼다 (2026-11-01 사용자 요청).
    //   PaperCraftTable 오브젝트는 그대로 둔다 (E키 상호작용 InteractDevice와 합성 팝업 연결이 여기에 있다).
    //   상자 모양(MeshFilter, MeshRenderer)만 없애고, 자식 Press에 모델을 넣는다. 충돌(BoxCollider)은 모델(바닥에 깐 천 제외)에 맞춘다.
    //   바닥에 서도록 높이를 맞추고, 작업하는 앞쪽이 플레이어 시작 자리를 향하도록 돌린다 (FrontYaw로 앞면 보정).
    // 겉모습을 바꿀 때는 PaperCraftTable 아래 Press만 바꾸면 된다. 다시 실행하면 Press를 새로 만든다.
    // 메뉴: LastBullet > Phase 54 미술관 종이 합성대를 작업대 모델로 바꾸기
    public static class Phase54PaperTableSetup
    {
        const string ModelPath = "Assets/3Dmake/paper.fbx";
        const float Size = 1.3f;     // 전체 크기 배율 (캐릭터가 머리 큰 만화 체형이라 실제보다 크게)
        const float FrontYaw = 0f;   // 모델의 앞면이 플레이어 쪽이 아니면 이 값(도)을 더한다

        [MenuItem("LastBullet/Phase 54 미술관 종이 합성대를 작업대 모델로 바꾸기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var table = GameObject.Find("PaperCraftTable");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (table == null || model == null)
            {
                Debug.LogError("[LastBullet] PaperCraftTable이나 작업대 모델이 없어요");
                return;
            }

            var filter = table.GetComponent<MeshFilter>();
            if (filter != null)
                Object.DestroyImmediate(filter);
            var renderer = table.GetComponent<MeshRenderer>();
            if (renderer != null)
                Object.DestroyImmediate(renderer);

            var position = table.transform.position;
            table.transform.position = new Vector3(position.x, 0f, position.z);
            table.transform.localScale = Vector3.one * Size;

            var player = GameObject.FindWithTag("Player");
            float yaw = 0f;
            if (player != null)
            {
                Vector3 toPlayer = player.transform.position - table.transform.position;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.01f)
                    yaw = Quaternion.LookRotation(toPlayer).eulerAngles.y;
            }
            // 범위는 회전 없이(축에 맞춰) 재야 충돌 상자가 부풀지 않는다. 회전은 마지막에 준다.
            table.transform.rotation = Quaternion.identity;

            var old = table.transform.Find("Press");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            var press = (GameObject)PrefabUtility.InstantiatePrefab(model, table.transform);
            press.name = "Press";
            press.transform.localPosition = Vector3.zero;
            press.transform.localRotation = model.transform.localRotation; // 모델 파일 자체 회전(X 270도)을 그대로 쓴다
            press.transform.localScale = Vector3.one;

            // 이 오브젝트의 로컬 좌표에서 모델 범위를 구한다 (바닥 천은 충돌에서 뺀다)
            Bounds all = default, solid = default;
            bool firstAll = true, firstSolid = true;
            foreach (var r in press.GetComponentsInChildren<Renderer>())
            {
                var local = ToLocal(table.transform, r.bounds);
                Encapsulate(ref all, ref firstAll, local);
                if (!r.name.Contains("Work_Mat"))
                    Encapsulate(ref solid, ref firstSolid, local);
            }

            // 모델 다리(바닥)가 y=0에 오도록 올린다 (바닥 천은 얇아서 높이에 거의 영향이 없다)
            float lift = -solid.min.y;
            press.transform.localPosition = new Vector3(0f, lift, 0f);
            solid.center += new Vector3(0f, lift, 0f);

            var collider = table.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.center = solid.center;
                collider.size = solid.size;
            }

            table.transform.rotation = Quaternion.Euler(0f, yaw + FrontYaw, 0f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LastBullet] 종이 합성대를 작업대 모델로 바꿨어요 (방향 {yaw + FrontYaw:0}도, 충돌 {solid.size.x:0.00}×{solid.size.y:0.00}×{solid.size.z:0.00}).");
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
