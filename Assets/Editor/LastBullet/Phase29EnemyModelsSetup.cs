using LastBullet.Enemy;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 적 3종(돌진형·방어형·원거리형)을 에셋 Free Pack - Stick Man(PolyOne Studio) 모델로 바꾼다 (2026-10-31 사용자 요청).
    //  - 모양: 스틱맨(몸 색은 이 적을 잡으면 주는 물감 색: EnemyDrop이 정해 칠한다). 크기는 돌진형 1.25, 방어형 1.35, 원거리형 0.95배
    //  - 맞는 부위(Hitbox)는 그대로: 예전 상자·공은 보이지 않게 하고(충돌만 남김) 스틱맨의 몸·머리 위치에 맞춘다
    //  - 방어형의 방패는 3D 도형으로 새로 만든다: 둥근 방패(테두리 + 판 + 가운데 장식). 막는 방식(정면 ±60°)은 그대로
    //  - 동작: 대기·걷기·달리기·빨리 달리기를 이동 속도로 정한다 (EnemyAnimator + Assets/Animations/EnemyStickman.controller)
    // 에셋의 동작 clip은 되풀이(loop)가 꺼져 있어서 대기·걷기·달리기·빨리 달리기만 켠다.
    // 모든 적 부품은 Enemy 태그 (규칙). 메뉴: LastBullet > Phase 29 적을 스틱맨으로 바꾸기
    public static class Phase29EnemyModelsSetup
    {
        const string Root = "Assets/PolyOne/Free Stickman/";
        const string ControllerPath = "Assets/Animations/EnemyStickman.controller";

        struct EnemyDef
        {
            public string prefab, material;
            public float scale;
        }

        static readonly EnemyDef[] Enemies =
        {
            new EnemyDef { prefab = "Enemy_Charger", material = "Enemy_Charger", scale = 1.25f },
            new EnemyDef { prefab = "Enemy_Shield", material = "Enemy_Shield", scale = 1.35f },
            new EnemyDef { prefab = "Enemy_Thrower", material = "Enemy_Thrower", scale = 0.95f },
        };

        [MenuItem("LastBullet/Phase 29 적을 스틱맨으로 바꾸기")]
        static void Apply()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefabs/Free Pack - Stick Man.prefab");
            if (model == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "에셋 Free Pack - Stick Man(Assets/PolyOne)을 먼저 가져오세요.", "확인");
                return;
            }
            LoopClips();
            BuildController();
            foreach (var def in Enemies)
                BuildEnemy(def, model);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] 적 3종을 스틱맨으로 바꿨어요.");
        }

        // ───────────── 동작 ─────────────

        static AnimationClip Clip(string name)
        {
            return AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Root}Animation/{name}.anim");
        }

        static void LoopClips()
        {
            foreach (var name in new[] { "Idle", "Walk", "Run", "Run Fast" })
            {
                var clip = Clip(name);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                if (settings.loopTime)
                    continue;
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                EditorUtility.SetDirty(clip);
            }
        }

        static void BuildController()
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var sm = controller.layers[0].stateMachine;
            var idle = sm.AddState("Idle"); idle.motion = Clip("Idle");
            var walk = sm.AddState("Walk"); walk.motion = Clip("Walk");
            var run = sm.AddState("Run"); run.motion = Clip("Run");
            var fast = sm.AddState("RunFast"); fast.motion = Clip("Run Fast");
            sm.defaultState = idle;

            // 속도(m/s): 0.3 이하 대기 / 3.5 이하 걷기(방어형 3) / 6 이하 달리기(추적 4) / 그 위는 빨리 달리기(돌진 7~9)
            Link(idle, walk, 0.3f, true); Link(walk, idle, 0.3f, false);
            Link(walk, run, 3.5f, true); Link(run, walk, 3.5f, false);
            Link(run, fast, 6f, true); Link(fast, run, 6f, false);
            EditorUtility.SetDirty(controller);
        }

        static void Link(AnimatorState from, AnimatorState to, float speed, bool greater)
        {
            var t = from.AddTransition(to);
            t.AddCondition(greater ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, speed, "Speed");
            t.hasExitTime = false;
            t.duration = 0.15f;
        }

        // ───────────── 적 프리팹 ─────────────

        static void BuildEnemy(EnemyDef def, GameObject modelPrefab)
        {
            string path = $"Assets/Prefabs/{def.prefab}.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            float s = def.scale;

            // 이미 바꿨으면 다시 만든다 (모델·방패를 지우고 새로)
            foreach (var old in new[] { "Model", "Shield" })
            {
                var t = root.transform.Find(old);
                if (t != null)
                    Object.DestroyImmediate(t.gameObject);
            }

            // 1. 스틱맨 모델
            var model = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, root.transform);
            model.name = "Model";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * s;
            var animator = model.GetComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            animator.applyRootMotion = false; // 움직임은 NavMeshAgent가 한다
            var skinned = model.GetComponentInChildren<SkinnedMeshRenderer>();
            skinned.updateWhenOffscreen = true;
            var material = ArtSlots.GetOrCreateMaterial(def.material, Color.white);
            material.shader = Shader.Find("Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", Color.white); // 실제 색은 EnemyDrop이 적마다 칠한다
            material.SetFloat("_Smoothness", 0.25f);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);
            skinned.sharedMaterial = material;

            // 2. 맞는 부위: 예전 상자·공은 보이지 않게 하고 충돌(Hitbox)만 스틱맨에 맞춘다
            var body = root.transform.Find("Body");
            var head = root.transform.Find("Head");
            foreach (var part in new[] { body, head })
            {
                Object.DestroyImmediate(part.GetComponent<MeshRenderer>());
                Object.DestroyImmediate(part.GetComponent<MeshFilter>());
            }
            body.localPosition = new Vector3(0f, 0.72f * s, 0f);
            body.localScale = new Vector3(0.7f * s, 1.45f * s, 0.45f * s);
            var headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            Vector3 headCenter = root.transform.InverseTransformPoint(headBone.position) + Vector3.up * 0.1f * s;
            head.localPosition = headCenter;
            head.localScale = Vector3.one * 0.6f * s; // 구 충돌 반지름 0.3 × 크기

            // 3. 방어형: 새 방패
            var enemyShield = root.GetComponent<EnemyShield>();
            if (enemyShield != null)
                BuildShield(root.transform, s, enemyShield);

            // 4. 원거리형: 돌이 나가는 자리를 몸 크기에 맞춘다
            var throwPoint = root.transform.Find("ThrowPoint");
            if (throwPoint != null)
                throwPoint.localPosition = new Vector3(0f, 1.2f * s, 0.45f * s);

            // 5. 동작 스크립트
            var enemyAnimator = root.GetComponent<EnemyAnimator>();
            if (enemyAnimator == null)
                enemyAnimator = root.AddComponent<EnemyAnimator>();
            var so = new SerializedObject(enemyAnimator);
            so.FindProperty("animator").objectReferenceValue = animator;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 6. 모든 부품에 Enemy 태그 (규칙)
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.tag = EnemyTagSetup.Tag;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            Debug.Log($"[LastBullet] {def.prefab}: 스틱맨(크기 {s})으로 바꿨어요.");
        }

        // 둥근 방패: 어두운 테두리 + 밝은 판 + 가운데 금빛 장식. 앞(+Z)을 보고 가슴 높이에 든다. 충돌 없음 (막는 건 EnemyShield가 각도로 정한다)
        static void BuildShield(Transform root, float s, EnemyShield enemyShield)
        {
            var group = new GameObject("Shield").transform;
            group.SetParent(root, false);
            group.localPosition = new Vector3(0f, 1.05f * s, 0.5f * s);

            var rim = Disc("Rim", group, 1.0f * s, 0.10f * s, 0f, "EnemyShieldRim", new Color32(0x4A, 0x4F, 0x5A, 0xFF));
            var plate = Disc("Plate", group, 0.86f * s, 0.14f * s, 0.05f * s, "EnemyShieldPlate", new Color32(0xB4, 0xC0, 0xD4, 0xFF));
            Boss(group, 0.28f * s, 0.14f * s);

            var so = new SerializedObject(enemyShield);
            so.FindProperty("plate").objectReferenceValue = plate.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 납작한 원통(원판). 원통은 Y축이 길어서 X로 90도 눕혀 앞(+Z)을 보게 한다. 높이 값 = 두께 (원통 기본 높이가 2라서 절반)
        static GameObject Disc(string name, Transform parent, float diameter, float thickness, float forward, string materialName, Color32 color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, forward);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            go.transform.localScale = new Vector3(diameter, thickness * 0.5f, diameter);
            go.GetComponent<Renderer>().sharedMaterial = ArtSlots.GetOrCreateMaterial(materialName, color);
            return go;
        }

        static void Boss(Transform parent, float diameter, float forward)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Boss";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 0f, forward);
            go.transform.localScale = new Vector3(diameter, diameter, diameter * 0.55f);
            go.GetComponent<Renderer>().sharedMaterial = ArtSlots.GetOrCreateMaterial("EnemyShieldBoss", new Color32(0xD8, 0xB8, 0x60, 0xFF));
        }
    }
}
