using System.Collections.Generic;
using LastBullet.Combat;
using LastBullet.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 1인칭을 다시 만든다 (2026-11-04 사용자 요청: 1인칭 손이 이상하다, 총을 조금 키워라, 재장전이 어색하다).
    //   3인칭 캐릭터가 소총을 두 손으로 든 자세(ThirdPersonGunPose)를 한 번 잡아서, 그 자세의 오른팔·왼팔(어깨~손)을 총 기준 좌표의 메시로 떼어 낸다.
    //   → 1인칭에서도 팔과 손이 총을 쥐는 각도가 3인칭과 똑같이 맞고, 양손이 다 보인다.
    //   Main Camera/FirstPersonGun/Model 아래:
    //     Ak47Holder            총 모델 단위 좌표 (크기 HolderScale, 쥐는 점이 Model 원점에 오게)
    //       Ak47                총 (탄창 재장전 모션 MagazineReloadMotion)
    //       RightArm/Skin·Outer 오른팔 (피부·소매)
    //       LeftArm/Skin·Outer  왼팔 — ArmReloadSlide가 재장전 때 시간표대로 움직인다
    //   결과 메시: Assets/Models/FirstPerson/FirstPersonArm_{Skin,Outer}_{R,L}.asset (캐릭터·자세가 바뀌면 이 메뉴를 다시 실행한다)
    // 먼저 Phase 51·59·61이 있어야 한다. 다시 실행하면 새로 만든다. 메뉴: LastBullet > Phase 62 1인칭 양손 만들기
    public static class Phase62FirstPersonArmsSetup
    {
        const float HolderScale = 1.15f;     // 1인칭 총·팔 크기 (이전: 총 약 0.9배 → 사용자 요청으로 조금 키움. 1.5는 화면을 너무 가렸다)
        static readonly Vector3 GripInGun = new Vector3(0f, -0.04f, -0.157f);
        static readonly Vector3 LeftHandEuler = new Vector3(0f, 0f, 90f);
        static readonly Vector3 LeftFistInHand = new Vector3(-0.02f, -0.055f, 0.10f);
        static readonly Vector3 RestPosition = new Vector3(0.28f, -0.25f, 0.6f);
        static readonly Vector3 AimPosition = new Vector3(0.1f, -0.19f, 0.58f);

        static readonly string[] ArmParts =
        {
            "Shoulder", "Arm", "Arm_Roll_1", "Arm_Roll_2", "ForeArm", "ForeArm_Roll_1", "ForeArm_Roll_2", "Hand", "Hand_End",
        };
        static readonly string[] ForearmParts = { "ForeArm", "ForeArm_Roll_1", "ForeArm_Roll_2", "Hand", "Hand_End" };
        static readonly string[] Fingers = { "Thumb", "Index", "Middle", "Ring", "Pinky" };

        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 62 1인칭 양손 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var akPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PolyOne/Free Gun/Prefabs/SM_Ak47.prefab");
            var meshes = new Dictionary<string, Mesh>();

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = GameObject.FindWithTag("Player");
                var camera = Camera.main;
                var autoGun = player != null ? player.GetComponent<AutoGun>() : null;
                Transform thirdGun = null;
                if (player != null)
                    foreach (var t in player.GetComponentsInChildren<Transform>(true))
                        if (t.name == "ThirdPersonGun")
                            thirdGun = t;
                var gunRoot = camera != null ? camera.transform.Find("FirstPersonGun") : null;
                var model = gunRoot != null ? gunRoot.Find("Model") : null;
                if (thirdGun == null || autoGun == null || model == null || akPrefab == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: ThirdPersonGun·AutoGun·1인칭 총·AK47 중 없는 것이 있어 건너뜀 (먼저 Phase 61)");
                    continue;
                }

                // 3인칭 소총 자세를 한 번 잡는다 (애니메이션 Wait01 → ThirdPersonGunPose가 총과 양팔을 맞춘다)
                var animator = player.GetComponentInChildren<Animator>();
                foreach (var clip in animator.runtimeAnimatorController.animationClips)
                    if (clip.name == "Wait01")
                        clip.SampleAnimation(animator.gameObject, clip.length * 0.5f);
                var pose = thirdGun.GetComponent<ThirdPersonGunPose>();
                // 왼손 방향: 손바닥이 총(오른쪽)을 향해야 한다 (2026-11-04 사용자: 손이 반대로 보인다)
                var handSo = new SerializedObject(pose);
                handSo.FindProperty("leftHandEuler").vector3Value = LeftHandEuler;
                handSo.FindProperty("leftFistInHand").vector3Value = LeftFistInHand;
                handSo.ApplyModifiedPropertiesWithoutUndo();
                typeof(ThirdPersonGunPose).GetMethod("LateUpdate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(pose, null);

                if (meshes.Count == 0)
                {
                    Matrix4x4 toGun = thirdGun.worldToLocalMatrix; // 총 모델 단위 좌표
                    foreach (var renderer in player.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        // 피부(modelBody)의 팔꿈치 아래~손만 쓴다. 옷(modelOuter)은 소매 조각이 지저분하게 보여서 쓰지 않는다.
                        // 피부는 소매에 가려지는 부분이 원래 없어서 팔이 중간에서 끊긴다 → 끊긴 자리에 같은 굵기의 팔 모양 통(Extension)을 이어 붙여 화면 밖까지 팔이 이어지게 한다.
                        if (renderer.name != "modelBody")
                            continue;
                        bool outer = false;
                        foreach (var side in new[] { "R", "L" })
                        {
                            Vector3 handPos = toGun.MultiplyPoint3x4(Find(player, side + ".Hand").position);
                            Vector3 elbowPos = toGun.MultiplyPoint3x4(Find(player, side + ".ForeArm").position);
                            var mesh = ExtractArm(renderer, toGun, side, ForearmParts, handPos, elbowPos);
                            if (mesh == null)
                                continue;
                            string key = $"{(outer ? "Outer" : "Skin")}_{side}";
                            mesh.name = "FirstPersonArm_" + key;
                            string assetPath = $"Assets/Models/FirstPerson/FirstPersonArm_{key}.asset";
                            AssetDatabase.DeleteAsset(assetPath);
                            AssetDatabase.CreateAsset(mesh, assetPath);
                            meshes[key] = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                            Debug.Log($"[LastBullet] 1인칭 팔 메시 {key}: 정점 {mesh.vertexCount}");
                        }
                    }
                }

                var skinMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FirstPersonArm_Skin.mat");
                var outerMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FirstPersonArm_Outer.mat");

                // 1인칭 구조 새로 만들기
                Remove(model, "Arm");
                Remove(model, "Ak47Holder");
                Remove(model, "Pistol");
                model.localScale = Vector3.one;
                model.localPosition = Vector3.zero;
                model.localRotation = Quaternion.identity;

                var holder = new GameObject("Ak47Holder").transform;
                holder.SetParent(model, false);
                holder.localScale = Vector3.one * HolderScale;
                holder.localPosition = -GripInGun * HolderScale; // 쥐는 점이 Model 원점에 온다

                Phase61Ak47Setup.PlaceAk(akPrefab, holder, autoGun, ShadowCastingMode.Off);
                // PlaceAk가 Y 90도로 놓는다. 여기서는 holder가 총 모델 단위 좌표라서 그대로 둔다.

                foreach (var side in new[] { "R", "L" })
                {
                    var arm = new GameObject(side == "R" ? "RightArm" : "LeftArm").transform;
                    arm.SetParent(holder, false);
                    AddPart(arm, "Skin", meshes.GetValueOrDefault("Skin_" + side), skinMat);
                    AddPart(arm, "Outer", meshes.GetValueOrDefault("Outer_" + side), outerMat);
                    if (side == "L")
                    {
                        var slide = arm.gameObject.AddComponent<ArmReloadSlide>();
                        var slideSo = new SerializedObject(slide);
                        slideSo.FindProperty("gun").objectReferenceValue = autoGun;
                        var poseSo = new SerializedObject(pose);
                        slideSo.FindProperty("rest").vector3Value = poseSo.FindProperty("leftGripInGun").vector3Value;
                        slideSo.FindProperty("magazine").vector3Value = poseSo.FindProperty("magazineInGun").vector3Value;
                        slideSo.FindProperty("charge").vector3Value = poseSo.FindProperty("chargeInGun").vector3Value;
                        slideSo.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                var firstPerson = gunRoot.GetComponent<FirstPersonGun>();
                if (firstPerson != null)
                {
                    var fpSo = new SerializedObject(firstPerson);
                    fpSo.FindProperty("restPosition").vector3Value = RestPosition;
                    fpSo.FindProperty("aimPosition").vector3Value = AimPosition;
                    fpSo.FindProperty("restPitch").floatValue = 1f;
                    fpSo.FindProperty("restYaw").floatValue = -1f;
                    fpSo.ApplyModifiedPropertiesWithoutUndo();
                }

                // 3인칭 총 자세는 다시 풀어 둔다 (씬에는 자세가 저장되지 않는다. 플레이할 때 매 프레임 맞춘다)
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 1인칭 양손과 AK47을 만들었어요 (크기 {HolderScale}).");
            }
            AssetDatabase.SaveAssets();
        }

        static void AddPart(Transform parent, string name, Mesh mesh, Material material)
        {
            if (mesh == null)
                return;
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static void Remove(Transform parent, string name)
        {
            var old = parent.Find(name);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
        }

        // 한쪽(R/L) 팔 뼈에 70% 이상 붙은 삼각형만, 현재 자세 그대로 toFrame 좌표의 새 메시로 만든다
        static Transform Find(GameObject root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name)
                    return t;
            return null;
        }

        static Mesh ExtractArm(SkinnedMeshRenderer renderer, Matrix4x4 toFrame, string side, string[] parts, Vector3 handPos, Vector3 elbowPos)
        {
            var source = renderer.sharedMesh;
            var bones = renderer.bones;
            var bindposes = source.bindposes;
            var weights = source.boneWeights;
            var vertices = source.vertices;
            var normals = source.normals;
            var uv = source.uv;

            var armBones = new HashSet<string>();
            foreach (var part in parts)
                armBones.Add($"{side}.{part}");
            foreach (var bone in bones)
                if (bone != null && bone.name.StartsWith(side + "."))
                    foreach (var finger in Fingers)
                        if (bone.name.Contains(finger))
                            armBones.Add(bone.name);

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

            var remap = new int[vertices.Length];
            for (int v = 0; v < remap.Length; v++)
                remap[v] = -1;
            var newVertices = new List<Vector3>();
            var newNormals = new List<Vector3>();
            var newUv = new List<Vector2>();
            var triangles = new List<int>();
            var sourceTriangles = source.triangles;
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
                        newVertices.Add(toFrame.MultiplyPoint3x4(Phase51FirstPersonHandSetup.Skin(v, weights, bones, bindposes, vertices, true)));
                        newNormals.Add(toFrame.MultiplyVector(Phase51FirstPersonHandSetup.Skin(v, weights, bones, bindposes, normals, false)).normalized);
                        newUv.Add(uv.Length > v ? uv[v] : Vector2.zero);
                    }
                    triangles.Add(remap[v]);
                }
            }
            if (triangles.Count == 0)
                return null;
            AddExtension(newVertices, newNormals, newUv, triangles, handPos, elbowPos);

            var mesh = new Mesh { indexFormat = newVertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(newVertices);
            mesh.SetNormals(newNormals);
            mesh.SetUVs(0, newUv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        // 잘린 팔 끝(손에서 팔꿈치 쪽으로 가장 먼 자리)에 같은 굵기로 시작해 조금씩 굵어지는 통을 이어 붙인다 (양면)
        static void AddExtension(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles, Vector3 handPos, Vector3 elbowPos)
        {
            Vector3 axis = (elbowPos - handPos).normalized;
            float maxS = float.MinValue;
            foreach (var v in vertices)
                maxS = Mathf.Max(maxS, Vector3.Dot(v - handPos, axis));
            var cut = new List<int>();
            for (int i = 0; i < vertices.Count; i++)
                if (Vector3.Dot(vertices[i] - handPos, axis) > maxS - 0.03f)
                    cut.Add(i);
            if (cut.Count < 3)
                return;
            Vector3 center = Vector3.zero;
            Vector2 uv = Vector2.zero;
            foreach (int i in cut)
            {
                center += vertices[i];
                uv += uvs[i];
            }
            center /= cut.Count;
            uv /= cut.Count;
            float radius = 0f;
            foreach (int i in cut)
            {
                Vector3 d = vertices[i] - center;
                radius += Vector3.ProjectOnPlane(d, axis).magnitude;
            }
            radius /= cut.Count;

            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 w = Vector3.Cross(axis, u);
            const int Segments = 14, Rings = 4;
            const float Length = 1.1f;
            int start = vertices.Count;
            for (int r = 0; r <= Rings; r++)
            {
                float t = r / (float)Rings;
                Vector3 c = center + axis * (Length * t);
                float rad = radius * (1f + 0.35f * t);
                for (int s = 0; s < Segments; s++)
                {
                    float a = s / (float)Segments * Mathf.PI * 2f;
                    Vector3 dir = u * Mathf.Cos(a) + w * Mathf.Sin(a);
                    vertices.Add(c + dir * rad);
                    normals.Add(dir);
                    uvs.Add(uv);
                }
            }
            for (int r = 0; r < Rings; r++)
                for (int s = 0; s < Segments; s++)
                {
                    int a = start + r * Segments + s, b = start + r * Segments + (s + 1) % Segments;
                    int c = a + Segments, d = b + Segments;
                    triangles.AddRange(new[] { a, b, c, b, d, c });
                    triangles.AddRange(new[] { a, c, b, b, c, d });
                }
        }
        static void Add(ref float sum, int index, float weight, Transform[] bones, HashSet<string> armBones)
        {
            if (weight > 0f && index >= 0 && index < bones.Length && bones[index] != null && armBones.Contains(bones[index].name))
                sum += weight;
        }
    }
}
