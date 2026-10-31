using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 1인칭 화면에 오른손과 팔을 보이게 한다 (2026-11-01 사용자 요청: 총만 있으니 어색하다).
    // 방법: 캐릭터(JellyFishGirl)의 몸·옷 메시에서 "오른쪽 어깨~팔~손" 뼈에 붙은 부분만 따로 떼어 새 메시로 저장한다.
    //   - 떼어 낼 때의 자세는 총을 쥔 오른손(주먹)이다. 손 뼈 기준 좌표로 저장해서, 3인칭에서 총을 쥐는 자리와 똑같이 맞는다.
    //   - 1인칭 총(FirstPersonGun/Model) 아래에 Arm을 만들고, 3인칭에서 총과 손의 위치 관계를 그대로 옮긴다 (손잡이가 주먹 안에 있다).
    //   - 총과 손은 같은 비율로 줄인다(Scale 아래). 팔은 어깨까지 있어서 화면 아래·뒤로 길게 빠지고, 잘린 단면은 화면 밖에 있다.
    //   - 피부·옷 머티리얼은 캐릭터 것을 복사한 전용 머티리얼(Assets/Materials/FirstPersonArm_Skin.mat, _Outer.mat)이다.
    // 결과 메시: Assets/Models/FirstPerson/FirstPersonArm_Skin.asset, FirstPersonArm_Outer.asset (캐릭터 모델이 바뀌면 이 메뉴를 다시 실행한다)
    // 먼저 Phase 48~50이 있어야 한다. 다시 실행하면 Arm을 지우고 새로 만든다.
    // 메뉴: LastBullet > Phase 51 1인칭 손 만들기
    public static class Phase51FirstPersonHandSetup
    {
        // 3인칭 총(0.5배)에 대한 1인칭 총의 비율. 총 크기 = 0.5 × 이 값, 손 크기 = 이 값
        const float Scale = 0.65f;
        const float ThirdPersonPistolScale = 0.5f;

        // 1인칭 총(손 포함)이 놓이는 자리 (카메라 기준, m). 손 가운데가 화면 오른쪽 아래에 오도록 정했다.
        static readonly Vector3 RestPosition = new Vector3(0.17f, -0.08f, 0.72f);
        static readonly Vector3 AimPosition = new Vector3(0.13f, -0.06f, 0.72f); // 조준 때도 손이 십자선을 가리지 않게 평소와 비슷한 자리
        const float RestPitch = 8f;
        static readonly Vector3 MeshCenter = new Vector3(0f, 0.184f, 0.05f);

        const string MeshFolder = "Assets/Models/FirstPerson";

        // 떼어 낼 뼈 (오른쪽 어깨, 팔, 팔꿈치 아래, 손, 손가락 전부)
        static readonly string[] ArmBoneNames =
        {
            "R.Shoulder", "R.Arm", "R.Arm_Roll_1", "R.Arm_Roll_2", "R.ForeArm", "R.ForeArm_Roll_1", "R.ForeArm_Roll_2",
            "R.Hand", "R.Hand_End",
        };

        static readonly string[] ForearmBoneNames =
        {
            "R.ForeArm", "R.ForeArm_Roll_1", "R.ForeArm_Roll_2", "R.Hand", "R.Hand_End",
        };

        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 51 1인칭 손 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Directory_Ensure(MeshFolder);
            var meshes = new Dictionary<string, Mesh>();
            var materials = new Dictionary<string, Material>();

            for (int i = 0; i < Scenes.Length; i++)
            {
                var scene = EditorSceneManager.OpenScene(Scenes[i]);
                var player = GameObject.FindWithTag("Player");
                var camera = Camera.main;
                var gun = camera != null ? camera.transform.Find("FirstPersonGun") : null;
                var model = gun != null ? gun.Find("Model") : null;
                var fpPistol = model != null ? model.Find("Pistol") : null;
                Transform hand = null, thirdPistol = null;
                if (player != null)
                    foreach (var t in player.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "R.Hand") hand = t;
                        if (t.name == "ThirdPersonPistol") thirdPistol = t;
                    }
                if (hand == null || thirdPistol == null || fpPistol == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: 먼저 Phase 49·50이 필요해서 건너뜀");
                    continue;
                }

                // 캐릭터 뼈를 총 쥔 자세로 맞춘다 (애니메이션 Wait01 한 장면. 손가락은 늘 주먹이다)
                var animator = player.GetComponentInChildren<Animator>();
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                    if (clip.name == "Wait01")
                        clip.SampleAnimation(animator.gameObject, clip.length * 0.5f);

                // 메시는 한 번만 떼어 낸다 (캐릭터가 모든 씬에서 같다)
                if (meshes.Count == 0)
                {
                    foreach (var renderer in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        // 피부 메시는 소매에 가려지는 부분에 면이 없어서, 옷(modelOuter)의 팔꿈치 아래 소매를 같이 써야 팔 끝이 열려 보이지 않는다
                        if (renderer.name != "modelBody" && renderer.name != "modelOuter")
                            continue;
                        string key = renderer.name == "modelBody" ? "Skin" : "Outer";
                        var mesh = ExtractArm(renderer, hand);
                        if (mesh == null)
                            continue;
                        mesh.name = "FirstPersonArm_" + key;
                        string path = $"{MeshFolder}/FirstPersonArm_{key}.asset";
                        AssetDatabase.DeleteAsset(path);
                        AssetDatabase.CreateAsset(mesh, path);
                        meshes[key] = AssetDatabase.LoadAssetAtPath<Mesh>(path);

                        string matPath = $"Assets/Materials/FirstPersonArm_{key}.mat";
                        if (AssetDatabase.LoadAssetAtPath<Material>(matPath) == null)
                        {
                            var source = AssetDatabase.GetAssetPath(renderer.sharedMaterial);
                            AssetDatabase.CopyAsset(source, matPath);
                        }
                        materials[key] = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        Debug.Log($"[LastBullet] 팔 메시 {key}: 정점 {mesh.vertexCount}, 삼각형 {mesh.triangles.Length / 3}");
                    }
                }

                // 1인칭 총 크기를 맞추고, 손 좌표계 → 1인칭 총 좌표계 변환을 구한다
                float gunScale = ThirdPersonPistolScale * Scale;
                fpPistol.localScale = Vector3.one * gunScale;
                fpPistol.localRotation = Quaternion.identity;
                fpPistol.localPosition = -MeshCenter * gunScale;

                // 3인칭: 손 좌표의 점 x → 모델 점 m = R^-1 (x - p) / s.   1인칭: 모델 점 → 1인칭 좌표 = p_fp + rot_fp (m × s_fp)
                Quaternion handToModelRot = Quaternion.Inverse(thirdPistol.localRotation);
                float k = gunScale / thirdPistol.localScale.x;
                Vector3 armPosition = fpPistol.localPosition - k * (handToModelRot * thirdPistol.localPosition);

                var old = model.Find("Arm");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
                var arm = new GameObject("Arm").transform;
                arm.SetParent(model, false);
                arm.localRotation = handToModelRot;
                arm.localScale = Vector3.one * k;
                arm.localPosition = armPosition;

                foreach (var pair in meshes)
                {
                    var part = new GameObject(pair.Key, typeof(MeshFilter), typeof(MeshRenderer));
                    part.transform.SetParent(arm, false);
                    part.GetComponent<MeshFilter>().sharedMesh = pair.Value;
                    var meshRenderer = part.GetComponent<MeshRenderer>();
                    meshRenderer.sharedMaterial = materials[pair.Key];
                    meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                    meshRenderer.receiveShadows = false;
                }

                // 손이 화면 안에 들어오도록 총 자리를 맞춘다 (손이 화면 오른쪽 아래에 보이고, 팔은 아래로 빠진다)
                var gunScript = gun.GetComponent<LastBullet.Player.FirstPersonGun>();
                if (gunScript != null)
                {
                    var gunSo = new SerializedObject(gunScript);
                    gunSo.FindProperty("restPosition").vector3Value = RestPosition;
                    gunSo.FindProperty("aimPosition").vector3Value = AimPosition;
                    gunSo.FindProperty("restPitch").floatValue = RestPitch;
                    gunSo.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 1인칭 손과 팔을 붙였어요 (크기 {k:0.00}).");
            }
            AssetDatabase.SaveAssets();
        }

        // 스킨 메시에서 팔 뼈에 붙은 부분만 손 뼈 기준 좌표의 새 메시로 만든다 (현재 자세로 CPU 스키닝)
        static Mesh ExtractArm(SkinnedMeshRenderer renderer, Transform hand)
        {
            var source = renderer.sharedMesh;
            var bones = renderer.bones;
            var bindposes = source.bindposes;
            var weights = source.boneWeights;
            var vertices = source.vertices;
            var normals = source.normals;
            var uv = source.uv;

            // 옷(Outer)은 어깨·윗팔 쪽에 셔츠 몸통 조각이 있어서 1인칭에서 손을 가린다. 옷은 팔꿈치 아래(소매 끝·손목 밴드)만 쓴다.
            var armBones = new HashSet<string>(renderer.name == "modelOuter" ? ForearmBoneNames : ArmBoneNames);
            foreach (var bone in bones)
                if (bone != null && bone.name.StartsWith("R.") && (bone.name.Contains("Thumb") || bone.name.Contains("Index") || bone.name.Contains("Middle") || bone.name.Contains("Ring") || bone.name.Contains("Pinky")))
                    armBones.Add(bone.name);

            // 그 정점이 팔 뼈의 영향을 70% 이상 받으면 팔 (경계에 걸친 정점을 빼서 뾰족한 조각이 안 생기게)
            var isArm = new bool[vertices.Length];
            for (int v = 0; v < vertices.Length; v++)
            {
                var w = weights[v];
                float sum = 0f;
                Add(ref sum, w.boneIndex0, w.weight0, bones, armBones);
                Add(ref sum, w.boneIndex1, w.weight1, bones, armBones);
                Add(ref sum, w.boneIndex2, w.weight2, bones, armBones);
                Add(ref sum, w.boneIndex3, w.weight3, bones, armBones);
                isArm[v] = sum >= 0.7f;
            }

            // 삼각형: 세 정점이 모두 팔인 것만
            var remap = new int[vertices.Length];
            for (int v = 0; v < remap.Length; v++)
                remap[v] = -1;
            var newVertices = new List<Vector3>();
            var newNormals = new List<Vector3>();
            var newUv = new List<Vector2>();
            var triangles = new List<int>();
            var sourceTriangles = source.triangles;
            Matrix4x4 worldToHand = hand.worldToLocalMatrix;
            for (int t = 0; t < sourceTriangles.Length; t += 3)
            {
                int a = sourceTriangles[t], b = sourceTriangles[t + 1], c = sourceTriangles[t + 2];
                if (!isArm[a] || !isArm[b] || !isArm[c])
                    continue;
                foreach (int v in new[] { a, b, c })
                {
                    if (remap[v] < 0)
                    {
                        remap[v] = newVertices.Count;
                        Vector3 world = Skin(v, weights, bones, bindposes, vertices, true);
                        Vector3 normal = Skin(v, weights, bones, bindposes, normals, false);
                        newVertices.Add(worldToHand.MultiplyPoint3x4(world));
                        newNormals.Add(worldToHand.MultiplyVector(normal).normalized);
                        newUv.Add(uv.Length > v ? uv[v] : Vector2.zero);
                    }
                    triangles.Add(remap[v]);
                }
            }
            if (triangles.Count == 0)
                return null;

            var mesh = new Mesh { indexFormat = newVertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(newVertices);
            mesh.SetNormals(newNormals);
            mesh.SetUVs(0, newUv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Add(ref float sum, int index, float weight, Transform[] bones, HashSet<string> armBones)
        {
            if (weight > 0f && index >= 0 && index < bones.Length && bones[index] != null && armBones.Contains(bones[index].name))
                sum += weight;
        }

        // 한 정점(또는 법선)을 현재 뼈 자세로 월드 좌표에 옮긴다
        static Vector3 Skin(int v, BoneWeight[] weights, Transform[] bones, Matrix4x4[] bindposes, Vector3[] data, bool point)
        {
            var w = weights[v];
            Vector3 result = Vector3.zero;
            AddSkin(ref result, w.boneIndex0, w.weight0, bones, bindposes, data[v], point);
            AddSkin(ref result, w.boneIndex1, w.weight1, bones, bindposes, data[v], point);
            AddSkin(ref result, w.boneIndex2, w.weight2, bones, bindposes, data[v], point);
            AddSkin(ref result, w.boneIndex3, w.weight3, bones, bindposes, data[v], point);
            return result;
        }

        static void AddSkin(ref Vector3 result, int index, float weight, Transform[] bones, Matrix4x4[] bindposes, Vector3 value, bool point)
        {
            if (weight <= 0f || bones[index] == null)
                return;
            Matrix4x4 m = bones[index].localToWorldMatrix * bindposes[index];
            result += (point ? m.MultiplyPoint3x4(value) : m.MultiplyVector(value)) * weight;
        }

        static void Directory_Ensure(string folder)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Models"))
                AssetDatabase.CreateFolder("Assets", "Models");
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Models", "FirstPerson");
        }
    }
}
