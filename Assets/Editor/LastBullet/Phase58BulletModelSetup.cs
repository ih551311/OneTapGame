using UnityEditor;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 바닥에 떨어진 탄환(Prefabs/DroppedBullet)의 겉모습을 사용자가 가져온 탄환 모델(Assets/3Dmake/BULIT.fbx)로 바꾼다 (2026-11-02 사용자 요청).
    //   기능(E키 회수, 7초 뒤 자동 복귀)은 맨 위 오브젝트의 BulletPickup·BulletAutoReturn이 그대로 맡는다.
    //   Model(겉모습 자리) 아래에 Bullet(FBX)을 넣는다. 모델은 실제 크기(길이 4.6cm)라 바닥에서 안 보여서 Scale배로 키우고,
    //   옆으로 눕혀 바닥 위에 놓는다. 멀리서 찾기 쉽도록 빛기둥(LightPillar)은 그대로 둔다.
    // 다시 실행하면 새로 맞춘다. 메뉴: LastBullet > Phase 58 탄환 모델 바꾸기
    public static class Phase58BulletModelSetup
    {
        const string PrefabPath = "Assets/Prefabs/DroppedBullet.prefab";
        const string ModelPath = "Assets/3Dmake/BULIT.fbx";
        const float Scale = 6f;            // 실제 4.6cm → 약 28cm (바닥에서 눈에 띄게)
        const float RestHeight = -0.015f;  // 탄환이 놓이는 높이. 프리팹 맨 위는 바닥 위 5cm에 놓이므로, 몸통 반지름(약 3.6cm)만큼 바닥에 닿게 낮춘다

        [MenuItem("LastBullet/Phase 58 탄환 모델 바꾸기")]
        static void Create()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogError($"[LastBullet] {ModelPath}를 찾지 못했어요");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(PrefabPath);

            // 겉모습 자리(Model)를 비우고 새로 만든다 (옛 캡슐 도형 제거)
            var old = root.transform.Find("Model");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            var holder = new GameObject("Model").transform;
            holder.SetParent(root.transform, false);

            var bullet = (GameObject)PrefabUtility.InstantiatePrefab(model, holder);
            bullet.name = "Bullet";
            bullet.transform.localScale = Vector3.one * Scale;
            // 파일 자체 회전(X 270도)으로 길이 방향이 위(Y)다. 옆으로 눕힌다 (Z 90도).
            bullet.transform.localRotation = Quaternion.Euler(0f, 0f, 90f) * model.transform.localRotation;
            bullet.transform.localPosition = Vector3.zero;

            // 모델의 가운데가 Model의 가운데에 오고, 몸통이 바닥에 닿도록 맞춘다
            var bounds = default(Bounds);
            bool first = true;
            foreach (var r in bullet.GetComponentsInChildren<Renderer>())
            {
                if (first) { bounds = r.bounds; first = false; }
                else bounds.Encapsulate(r.bounds);
            }
            holder.localPosition = new Vector3(0f, 0f, 0f);
            Vector3 center = holder.InverseTransformPoint(bounds.center);
            bullet.transform.localPosition = -center;
            holder.localPosition = new Vector3(0f, RestHeight, 0f);
            holder.localRotation = Quaternion.Euler(0f, 25f, 0f); // 살짝 비스듬히 (너무 반듯하지 않게)

            foreach (var r in bullet.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                r.receiveShadows = true;
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] 떨어진 탄환 모양을 BULIT 모델로 바꿨어요 (크기 {Scale}배).");
        }
    }
}
