using LastBullet.Effects;
using LastBullet.Enemy;
using UnityEditor;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 처치 연출 "물감 터짐"을 만든다 (기획서 2.11): 프리팹 Prefabs/PaintBurst를 만들고 적 3종의 EnemyDrop에 연결한다.
    // 프리팹: 방울(ParticleSystem, 공 모양 입자) + 자식 Puddle(바닥 물감 자국, 납작한 원)
    // 전용 머티리얼: PaintBurst(방울), PaintSplat(바닥 자국)
    // 메뉴: LastBullet > 처치 연출(물감 터짐) 만들기
    public static class Phase11EffectsSetup
    {
        const string PrefabPath = "Assets/Prefabs/PaintBurst.prefab";
        static readonly string[] EnemyPrefabs =
        {
            "Assets/Prefabs/Enemy_Charger.prefab", "Assets/Prefabs/Enemy_Shield.prefab", "Assets/Prefabs/Enemy_Thrower.prefab",
        };

        [MenuItem("LastBullet/처치 연출(물감 터짐) 만들기")]
        static void Create()
        {
            var burst = CreateBurstPrefab();
            foreach (var path in EnemyPrefabs)
                Connect(path, burst);
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] 처치 연출(PaintBurst)을 만들고 적 3종에 연결했어요.");
        }

        static PaintBurstEffect CreateBurstPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (existing != null)
                return existing.GetComponent<PaintBurstEffect>();

            var root = new GameObject("PaintBurst");
            var effect = root.AddComponent<PaintBurstEffect>();

            // 방울: 작은 공이 사방으로 튀고 중력으로 떨어진다. 땅에 닿으면 튕기고 줄어든다.
            var system = root.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = system.main;
            main.duration = 0.3f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.24f);
            main.gravityModifier = 2.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;

            var emission = system.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 46) });

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0f)));

            var collision = system.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.dampen = 0.6f;
            collision.bounce = 0.25f;
            collision.lifetimeLoss = 0.2f;
            collision.quality = ParticleSystemCollisionQuality.Low;

            var renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            var dropMaterial = ArtSlots.GetOrCreateMaterial("PaintBurst", Color.white);
            dropMaterial.shader = Shader.Find("Universal Render Pipeline/Particles/Unlit"); // 입자별 색(startColor)을 따른다
            EditorUtility.SetDirty(dropMaterial);
            renderer.sharedMaterial = dropMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // 바닥 자국: 납작한 공. 젖은 느낌이 나게 반짝임을 조금 준다. 색은 처치할 때 입힌다.
            var puddle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puddle.name = "Puddle";
            Object.DestroyImmediate(puddle.GetComponent<Collider>());
            puddle.transform.SetParent(root.transform, false);
            puddle.transform.localScale = new Vector3(1.5f, 0.02f, 1.5f);
            var splat = ArtSlots.GetOrCreateMaterial("PaintSplat", Color.white);
            splat.SetFloat("_Smoothness", 0.55f);
            EditorUtility.SetDirty(splat);
            var puddleRenderer = puddle.GetComponent<Renderer>();
            puddleRenderer.sharedMaterial = splat;
            puddleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var so = new SerializedObject(effect);
            so.FindProperty("droplets").objectReferenceValue = system;
            so.FindProperty("puddle").objectReferenceValue = puddle.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<PaintBurstEffect>();
        }

        // 적 프리팹의 EnemyDrop에 연출을 연결한다 (터지는 자리는 Head)
        static void Connect(string path, PaintBurstEffect burst)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            var drop = root.GetComponent<EnemyDrop>();
            var so = new SerializedObject(drop);
            so.FindProperty("burstPrefab").objectReferenceValue = burst;
            so.FindProperty("burstPoint").objectReferenceValue = root.transform.Find("Head");
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
