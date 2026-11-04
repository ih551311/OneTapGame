using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 총 모양을 캐릭터(JellyFishGirl)에 원래 붙어 있던 총(modelGun)으로 되돌린다 (2026-11-04 사용자 요청: "처음 캐릭터한테 있던 걸로 그대로").
    //   3인칭: 꺼 두었던 modelGun을 다시 켜고, 오른손에 붙였던 pistol1(ThirdPersonPistol)을 지운다.
    //   1인칭: Main Camera/FirstPersonGun/Model의 pistol1(Pistol)을 지우고, modelGun을 총 쥔 자세로 떼어 낸 메시를 팔(Arm) 아래 Gun으로 붙인다.
    //          (팔과 같은 "손 뼈 기준 좌표"로 떼어 내서, 3인칭에서 손에 쥔 모양 그대로 손에 들려 있다)
    //          총의 긴 방향(총신)이 화면 앞을 보도록 팔과 총을 주먹을 중심으로 함께 돌린다.
    //   결과 메시: Assets/Models/FirstPerson/FirstPersonArm_Gun.asset, 머티리얼 Assets/Materials/FirstPersonGun_Original.mat(캐릭터 총 재질 복사)
    // 먼저 Phase 51(1인칭 손)이 있어야 한다. 다시 실행해도 같은 결과다. 메뉴: LastBullet > Phase 60 총을 캐릭터 원래 총으로 되돌리기
    public static class Phase60OriginalGunSetup
    {
        const string MeshPath = "Assets/Models/FirstPerson/FirstPersonArm_Gun.asset";
        const string MaterialPath = "Assets/Materials/FirstPersonGun_Original.mat";
        static readonly Vector3 FistInHand = new Vector3(-0.02f, -0.055f, 0.10f); // 손 뼈 기준 주먹 가운데

        // 1인칭 총·손 크기와 자리 (화면에서 세 가지를 비교해 고른 값: 십자선을 가리지 않고 오른쪽 아래에 보인다)
        const float ModelScale = 0.7f;
        static readonly Vector3 RestPosition = new Vector3(0.25f, -0.22f, 0.8f);
        static readonly Vector3 AimPosition = new Vector3(0.16f, -0.17f, 0.8f);

        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 60 총을 캐릭터 원래 총으로 되돌리기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Mesh gunMesh = null;
            Material gunMaterial = null;
            Vector3 barrelInHand = Vector3.forward;

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = GameObject.FindWithTag("Player");
                Transform hand = null;
                SkinnedMeshRenderer modelGun = null;
                if (player != null)
                {
                    foreach (var t in player.GetComponentsInChildren<Transform>(true))
                        if (t.name == "R.Hand")
                            hand = t;
                    foreach (var r in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        if (r.name == "modelGun")
                            modelGun = r;
                }
                var camera = Camera.main;
                var model = camera != null ? camera.transform.Find("FirstPersonGun/Model") : null;
                var arm = model != null ? model.Find("Arm") : null;
                if (hand == null || modelGun == null || arm == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: 손 뼈·modelGun·1인칭 팔 중 없는 것이 있어 건너뜀 (먼저 Phase 51)");
                    continue;
                }

                // 3인칭: 원래 총 켜기, pistol1 지우기
                modelGun.enabled = true;
                var thirdPistol = hand.Find("ThirdPersonPistol");
                if (thirdPistol != null)
                    Object.DestroyImmediate(thirdPistol.gameObject);

                // 총 쥔 자세 (Phase 51과 같은 Wait01 한 장면)
                var animator = player.GetComponentInChildren<Animator>();
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                    if (clip.name == "Wait01")
                        clip.SampleAnimation(animator.gameObject, clip.length * 0.5f);

                if (gunMesh == null)
                {
                    gunMesh = ExtractWhole(modelGun, hand);
                    AssetDatabase.DeleteAsset(MeshPath);
                    AssetDatabase.CreateAsset(gunMesh, MeshPath);
                    gunMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
                    if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) == null)
                        AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(modelGun.sharedMaterial), MaterialPath);
                    gunMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                    barrelInHand = BarrelAxis(gunMesh.vertices, FistInHand);
                    Debug.Log($"[LastBullet] 원래 총 메시: 정점 {gunMesh.vertexCount}, 손 기준 총신 방향 {barrelInHand}");
                }

                // 1인칭: pistol1 지우고 원래 총 붙이기
                var pistol = model.Find("Pistol");
                if (pistol != null)
                    Object.DestroyImmediate(pistol.gameObject);
                var oldGun = arm.Find("Gun");
                if (oldGun != null)
                    Object.DestroyImmediate(oldGun.gameObject);
                var gun = new GameObject("Gun", typeof(MeshFilter), typeof(MeshRenderer));
                gun.transform.SetParent(arm, false);
                gun.GetComponent<MeshFilter>().sharedMesh = gunMesh;
                var gunRenderer = gun.GetComponent<MeshRenderer>();
                gunRenderer.sharedMaterial = gunMaterial;
                gunRenderer.shadowCastingMode = ShadowCastingMode.Off;
                gunRenderer.receiveShadows = false;

                // 팔과 총을 주먹을 중심으로 돌려서 총신이 화면 앞(+Z)을 보게 한다
                float k = arm.localScale.x;
                Vector3 fistModel = arm.localPosition + arm.localRotation * (FistInHand * k);
                Vector3 barrelModel = arm.localRotation * barrelInHand;
                Quaternion turn = Quaternion.FromToRotation(barrelModel, Vector3.forward);
                arm.localRotation = turn * arm.localRotation;
                arm.localPosition = fistModel - arm.localRotation * (FistInHand * k);

                // 원래 총은 pistol1보다 훨씬 커서 화면을 가린다. 0.7배로 줄이고 화면 오른쪽 아래로 내린다.
                model.localScale = Vector3.one * ModelScale;
                var firstPerson = model.parent.GetComponent<LastBullet.Player.FirstPersonGun>();
                if (firstPerson != null)
                {
                    var fpSo = new SerializedObject(firstPerson);
                    fpSo.FindProperty("restPosition").vector3Value = RestPosition;
                    fpSo.FindProperty("aimPosition").vector3Value = AimPosition;
                    fpSo.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 총을 캐릭터 원래 총으로 되돌렸어요 (돌린 각도 {Quaternion.Angle(Quaternion.identity, turn):0}도).");
            }
            AssetDatabase.SaveAssets();
        }

        // 스킨 메시 전체를 현재 자세로 손 뼈 기준 좌표의 메시로 만든다
        static Mesh ExtractWhole(SkinnedMeshRenderer renderer, Transform hand)
        {
            var source = renderer.sharedMesh;
            var bones = renderer.bones;
            var bindposes = source.bindposes;
            var weights = source.boneWeights;
            var vertices = source.vertices;
            var normals = source.normals;
            Matrix4x4 worldToHand = hand.worldToLocalMatrix;
            var newVertices = new Vector3[vertices.Length];
            var newNormals = new Vector3[vertices.Length];
            for (int v = 0; v < vertices.Length; v++)
            {
                newVertices[v] = worldToHand.MultiplyPoint3x4(Phase51FirstPersonHandSetup.Skin(v, weights, bones, bindposes, vertices, true));
                newNormals[v] = worldToHand.MultiplyVector(Phase51FirstPersonHandSetup.Skin(v, weights, bones, bindposes, normals, false)).normalized;
            }
            var mesh = new Mesh { name = "FirstPersonArm_Gun", indexFormat = vertices.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.vertices = newVertices;
            mesh.normals = newNormals;
            mesh.uv = source.uv;
            mesh.subMeshCount = source.subMeshCount;
            for (int s = 0; s < source.subMeshCount; s++)
                mesh.SetTriangles(source.GetTriangles(s), s);
            mesh.RecalculateBounds();
            return mesh;
        }

        // 총의 긴 방향(주성분)을 구한다. 주먹에서 먼 쪽이 총구 쪽이다.
        static Vector3 BarrelAxis(Vector3[] points, Vector3 fist)
        {
            Vector3 mean = Vector3.zero;
            foreach (var p in points)
                mean += p;
            mean /= points.Length;
            float xx = 0, xy = 0, xz = 0, yy = 0, yz = 0, zz = 0;
            foreach (var p in points)
            {
                var d = p - mean;
                xx += d.x * d.x; xy += d.x * d.y; xz += d.x * d.z;
                yy += d.y * d.y; yz += d.y * d.z; zz += d.z * d.z;
            }
            Vector3 axis = new Vector3(1f, 1f, 1f).normalized;
            for (int i = 0; i < 50; i++)
            {
                axis = new Vector3(xx * axis.x + xy * axis.y + xz * axis.z,
                                   xy * axis.x + yy * axis.y + yz * axis.z,
                                   xz * axis.x + yz * axis.y + zz * axis.z).normalized;
            }
            // 주먹에서 더 멀리 뻗은 쪽을 앞으로
            float forwardMost = float.MinValue, backMost = float.MinValue;
            foreach (var p in points)
            {
                float s = Vector3.Dot(p - fist, axis);
                forwardMost = Mathf.Max(forwardMost, s);
                backMost = Mathf.Max(backMost, -s);
            }
            return forwardMost >= backMost ? axis : -axis;
        }
    }
}
