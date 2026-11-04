using System.IO;
using LastBullet.Enemy;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 적 처치 연출을 만든다 (2026-11-04 사용자 요청: 적이 죽을 때 터지는 효과, 물감이 튀는 효과, 쓰러지는 동작).
    //   그림·머티리얼 (이미 있으면 덮어쓰지 않는다): Assets/Effects/DeathPop.png (부드러운 동그라미), Assets/Materials/DeathPop.mat
    //   프리팹 Assets/Prefabs/DeathPop.prefab: 방울 28개가 사방으로 튀는 파티클(Blobs) + 순간 번지는 둥근 빛(Flash). 색은 코드가 그 적의 물감 색으로 바꾼다.
    //   적 프리팹 4개(돌진·투척·분열·작은 분열)에 EnemyDeathEffect를 붙이고 DeathPop을 연결한다.
    // 메뉴: LastBullet > Phase 67 처치 연출 만들기
    public static class Phase67EnemyDeathSetup
    {
        const string PopPng = "Assets/Effects/DeathPop.png";
        const string PopPrefab = "Assets/Prefabs/DeathPop.prefab";

        static readonly string[] Enemies =
        {
            "Assets/Prefabs/Enemy_Charger.prefab",
            "Assets/Prefabs/Enemy_Thrower.prefab",
            "Assets/Prefabs/Enemy_Splitter.prefab",
            "Assets/Prefabs/Enemy_SplitterMini.prefab",
        };

        [MenuItem("LastBullet/Phase 67 처치 연출 만들기")]
        static void Create()
        {
            Directory.CreateDirectory("Assets/Effects");
            CreatePng();
            AssetDatabase.Refresh();
            var material = GetMaterial(AssetDatabase.LoadAssetAtPath<Texture2D>(PopPng));
            var pop = BuildPrefab(material);

            foreach (var path in Enemies)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                var effect = root.GetComponent<EnemyDeathEffect>();
                if (effect == null)
                    effect = root.AddComponent<EnemyDeathEffect>();
                var so = new SerializedObject(effect);
                so.FindProperty("popPrefab").objectReferenceValue = pop;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                Debug.Log($"[LastBullet] {path}: 처치 연출을 붙였어요.");
            }
            AssetDatabase.SaveAssets();
        }

        static GameObject BuildPrefab(Material material)
        {
            var root = new GameObject("DeathPop");

            // 방울: 사방으로 튀었다가 중력으로 떨어지며 작아진다
            var blobs = NewSystem("Blobs", root.transform, material);
            var main = blobs.main;
            main.loop = false;
            main.playOnAwake = true;
            main.duration = 0.1f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.36f);
            main.gravityModifier = 1.3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;
            var emission = blobs.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 28) });
            var shape = blobs.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;
            var size = blobs.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

            // 번쩍: 둥근 빛이 순식간에 커졌다 사라진다
            var flash = NewSystem("Flash", root.transform, material);
            var fMain = flash.main;
            fMain.loop = false;
            fMain.playOnAwake = true;
            fMain.duration = 0.1f;
            fMain.startLifetime = 0.2f;
            fMain.startSpeed = 0f;
            fMain.startSize = 1.8f;
            fMain.simulationSpace = ParticleSystemSimulationSpace.World;
            fMain.maxParticles = 4;
            var fEmission = flash.emission;
            fEmission.rateOverTime = 0f;
            fEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
            var fShape = flash.shape;
            fShape.enabled = false;
            var fSize = flash.sizeOverLifetime;
            fSize.enabled = true;
            fSize.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(1f, 1f)));
            var fFade = flash.colorOverLifetime;
            fFade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
            fFade.color = gradient;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PopPrefab);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static ParticleSystem NewSystem(string name, Transform parent, Material material)
        {
            var go = new GameObject(name, typeof(ParticleSystem));
            go.transform.SetParent(parent, false);
            var system = go.GetComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return system;
        }

        static void CreatePng()
        {
            if (File.Exists(PopPng))
                return;
            const int N = 64;
            var texture = new Texture2D(N, N, TextureFormat.RGBA32, false);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x - (N - 1) / 2f) / (N / 2f), dy = (y - (N - 1) / 2f) / (N / 2f);
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - Mathf.SmoothStep(0.55f, 1f, r)); // 가운데는 꽉, 가장자리는 부드럽게
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            texture.Apply();
            File.WriteAllBytes(PopPng, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(PopPng);
        }

        static Material GetMaterial(Texture2D texture)
        {
            const string path = "Assets/Materials/DeathPop.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetFloat("_Surface", 1f); // 투명
            material.SetFloat("_Blend", 0f);   // 보통 섞기 (물감 색이 그대로 보이게)
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
