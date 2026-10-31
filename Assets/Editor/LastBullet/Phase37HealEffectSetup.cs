using LastBullet.Audio;
using LastBullet.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 힐 스킬의 효과음·연출을 만든다 (2026-10-31 사용자 요청). 먼저 Phase 36(힐 스킬)이 있어야 한다.
    //   Player/HealSound      종소리 (Assets/Audio/Heal/heal_01, 02 중 무작위, 2D)
    //   Player/HealParticles  초록 반짝이 (재료: Assets/Textures/Heal_Sparkle.png, 머티리얼 Assets/Materials/HealParticle.mat)
    //   Canvas/HitFeedback/HealFlash  초록 테두리 번쩍 (그림 Assets/UI/Vignette_Heal.png)
    //   Player의 HealEffect가 힐을 쓰는 순간 이 셋을 켠다.
    // 다시 실행하면 지우고 새로 만든다. 메뉴: LastBullet > Phase 37 힐 효과 넣기
    public static class Phase37HealEffectSetup
    {
        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 37 힐 효과 넣기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var material = GetParticleMaterial();
            var clips = new[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Heal/heal_01.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Heal/heal_02.wav"),
            };

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = GameObject.FindWithTag("Player");
                var canvas = ArtSlots.FindCanvas();
                var skill = player != null ? player.GetComponent<HealSkill>() : null;
                if (skill == null || canvas == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: 먼저 Phase 36 힐 스킬 만들기를 실행하세요");
                    continue;
                }

                Remove(player.transform, "HealSound");
                Remove(player.transform, "HealParticles");

                // 소리
                var soundObject = new GameObject("HealSound", typeof(AudioSource), typeof(SfxPlayer));
                soundObject.transform.SetParent(player.transform, false);
                var audio = soundObject.GetComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.spatialBlend = 0f; // 2D: 어디서나 같은 크기
                var sfx = soundObject.GetComponent<SfxPlayer>();
                var sfxSo = new SerializedObject(sfx);
                var clipArray = sfxSo.FindProperty("clips");
                clipArray.arraySize = clips.Length;
                for (int i = 0; i < clips.Length; i++)
                    clipArray.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
                sfxSo.FindProperty("volume").floatValue = 0.6f;
                sfxSo.FindProperty("pitchRange").vector2Value = new Vector2(0.98f, 1.03f);
                sfxSo.ApplyModifiedPropertiesWithoutUndo();

                // 반짝이
                var particleObject = new GameObject("HealParticles", typeof(ParticleSystem));
                particleObject.transform.SetParent(player.transform, false);
                particleObject.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                particleObject.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // 위로 뿜는다
                var particles = ConfigureParticles(particleObject.GetComponent<ParticleSystem>(), material);

                // 화면 번쩍
                var feedback = canvas.transform.Find("HitFeedback");
                var parent = feedback != null ? feedback : canvas.transform;
                Remove(parent, "HealFlash");
                var flashObject = Phase4StageSelectSetup.CreateImage("HealFlash", parent, "Assets/UI/Vignette_Heal.png");
                Phase4StageSelectSetup.Stretch(flashObject.GetComponent<RectTransform>());
                var flash = flashObject.GetComponent<Image>();
                flash.preserveAspect = false;
                var c = flash.color; c.a = 0f; flash.color = c;

                var effect = player.GetComponent<HealEffect>();
                if (effect == null)
                    effect = player.AddComponent<HealEffect>();
                var so = new SerializedObject(effect);
                so.FindProperty("skill").objectReferenceValue = skill;
                so.FindProperty("sound").objectReferenceValue = sfx;
                so.FindProperty("particles").objectReferenceValue = particles;
                so.FindProperty("flash").objectReferenceValue = flash;
                so.ApplyModifiedPropertiesWithoutUndo();

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 힐 효과(소리·반짝이·화면 번쩍)를 만들었어요.");
            }
            AssetDatabase.SaveAssets();
        }

        static void Remove(Transform parent, string name)
        {
            var old = parent.Find(name);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
        }

        static ParticleSystem ConfigureParticles(ParticleSystem ps, Material material)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.25f, 0.85f, 0.4f), new Color(0.55f, 0.95f, 0.5f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local; // 플레이어를 따라다닌다
            main.maxParticles = 60;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 4f;
            shape.radius = 0.7f;
            shape.radiusThickness = 0.5f;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        // 반짝이 전용 머티리얼. 이미 있으면 사용자가 고친 것이므로 그대로 둔다.
        static Material GetParticleMaterial()
        {
            const string path = "Assets/Materials/HealParticle.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Heal_Sparkle.png");
            var material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f); // Transparent
            material.SetFloat("_Blend", 0f);   // Alpha (밝은 모래 위에서도 초록색이 보이게. 더하기 방식은 하얗게 날아간다)
            material.SetFloat("_SrcBlend", 5f);
            material.SetFloat("_DstBlend", 10f);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = 3000;
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
