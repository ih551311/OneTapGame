using System.IO;
using LastBullet.Combat;
using LastBullet.Effects;
using LastBullet.Player;
using LastBullet.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 바주카포 무기를 만든다 (2026-11-08 사용자 요청: 에셋 Low Poly Weapons VOL.1의 RPG7을 바주카 무기로, 플레이어 모션도 바주카 전용으로).
    //   모델: Assets/Low Poly Weapons VOL.1/Prefabs/RPG7.prefab (몸통 RPG7 + 로켓 Rocket + 방아쇠 Trigger). 모델의 총구는 -Z쪽이라 Y 180도 돌려서 쓴다.
    //   3인칭: R.Hand 아래 ThirdPersonBazooka(ThirdPersonBazookaPose) — 어깨에 얹고 오른손 뒤 손잡이, 왼손 앞 손잡이. 소총(ThirdPersonGun)은 꺼 둔다.
    //   1인칭: Main Camera/FirstPersonGun/Model/BazookaHolder — RPG7 + 양팔(소총 팔 메시를 복사해 손잡이 자리에 맞춰 옮김) + 총구 불꽃. 소총(Ak47Holder)은 꺼 둔다.
    //   Player: BazookaWeapon(키 1·2·F로 무기 바꾸기, 로켓·폭발) + GunEffects·AmmoDisplay에 바주카 연결.
    //   새로 만드는 것: Assets/Materials/Bazooka.mat, Assets/UI/Weapon_Bazooka.png(HUD 그림), Assets/Audio/Combat/Bazooka_*.wav
    // 먼저 Phase 61·62·64·90·92가 있어야 한다. 다시 실행하면 바주카 오브젝트를 지우고 새로 만든다. 메뉴: LastBullet > Phase 93 바주카포 만들기
    public static class Phase93BazookaSetup
    {
        const string PrefabPath = "Assets/Low Poly Weapons VOL.1/Prefabs/RPG7.prefab";
        const string IconPng = "Assets/UI/Weapon_Bazooka.png";
        const int SampleRate = 44100;

        const float FirstScale = 0.7f;   // 1인칭 바주카 크기
        const float ThirdScale = 1.0f;   // 3인칭 바주카 크기
        static readonly Vector3 RearGrip = new Vector3(0f, -0.13f, -0.075f);   // 바주카 모델 안 뒤 손잡이 (오른손)
        static readonly Vector3 FrontGrip = new Vector3(0f, -0.12f, 0.125f);   // 앞 손잡이 (왼손)
        static readonly Vector3 RifleRightGrip = new Vector3(0f, -0.04f, -0.157f); // 1인칭 팔 메시가 만들어진 소총의 오른손 자리
        static readonly Vector3 RifleLeftGrip = new Vector3(0f, -0.045f, 0.12f);   // 소총의 왼손 자리
        static readonly Vector3 FirstOffset = new Vector3(0f, 0f, 0.12f);          // 1인칭에서 뒤 끝이 카메라에 파묻히지 않게 앞으로 미는 양
        static readonly string[] ColorNames = { "Red", "Orange", "Yellow", "Green", "Blue", "Purple", "Pink", "White", "Black" };

        static readonly string[] Scenes =
        {
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 93 바주카포 만들기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[LastBullet] {PrefabPath}를 찾지 못했어요 (Low Poly Weapons VOL.1 에셋을 먼저 가져오세요)");
                return;
            }
            var material = GetMaterial();
            MakeIcon();
            AssetDatabase.Refresh();
            var bazookaSprite = ArtSlots.LoadSprite(IconPng);
            var fireSound = EnsureSound("Bazooka_Fire", MakeFire);
            var boomSound = EnsureSound("Bazooka_Boom", MakeBoom);
            var reloadGrab = EnsureSound("Bazooka_ReloadGrab", MakeGrab);
            var reloadLock = EnsureSound("Bazooka_ReloadLock", MakeLock);
            var paintMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PaintBomb.mat");
            var puddles = new Material[9];
            for (int i = 0; i < 9; i++)
            {
                puddles[i] = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Materials/FloorPaint_{ColorNames[i]}.mat");
                if (puddles[i] == null)
                {
                    Debug.LogWarning("[LastBullet] FloorPaint 머티리얼이 없어요. 먼저 메뉴 Phase 90을 눌러 주세요.");
                    return;
                }
            }
            var burstPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PaintBurst.prefab")?.GetComponent<PaintBurstEffect>();
            var rifleIcon = ArtSlots.LoadSprite("Assets/UI/Weapon_Icon.png");

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = GameObject.FindWithTag("Player");
                var camera = Camera.main;
                var autoGun = player != null ? player.GetComponent<AutoGun>() : null;
                Transform hand = Find(player, "R.Hand");
                Transform thirdRifle = Find(player, "ThirdPersonGun");
                var gunRoot = camera != null ? camera.transform.Find("FirstPersonGun") : null;
                var model = gunRoot != null ? gunRoot.Find("Model") : null;
                var rifleFirst = model != null ? model.Find("Ak47Holder") : null;
                if (autoGun == null || hand == null || thirdRifle == null || rifleFirst == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: Player·손 뼈·소총(1인칭/3인칭) 중 없는 것이 있어 건너뜀 (먼저 Phase 61·62)");
                    continue;
                }

                // 옛 바주카를 지운다 (다시 실행할 때)
                Remove(hand, "ThirdPersonBazooka");
                Remove(model, "BazookaHolder");
                var oldWeapon = player.GetComponent<BazookaWeapon>();
                if (oldWeapon != null)
                    Object.DestroyImmediate(oldWeapon);
                // 소총이 꺼져 있으면(이전에 바주카를 켠 채 저장) 다시 켠다
                thirdRifle.gameObject.SetActive(true);
                rifleFirst.gameObject.SetActive(true);

                var thirdPose = thirdRifle.GetComponent<ThirdPersonGunPose>();
                var thirdRoot = BuildThird(prefab, material, hand, player, thirdPose, autoGun, out var thirdRocket, out var thirdFlash);
                var firstRoot = BuildFirst(prefab, material, model, rifleFirst, autoGun, out var firstRocket, out var firstFlash);

                // Player: BazookaWeapon
                var weapon = player.AddComponent<BazookaWeapon>();
                var so = new SerializedObject(weapon);
                so.FindProperty("firstHolder").objectReferenceValue = firstRoot.gameObject;
                so.FindProperty("thirdHolder").objectReferenceValue = thirdRoot.gameObject;
                so.FindProperty("rifleFirst").objectReferenceValue = rifleFirst.gameObject;
                so.FindProperty("rifleThird").objectReferenceValue = thirdRifle.gameObject;
                so.FindProperty("firstRocket").objectReferenceValue = firstRocket;
                so.FindProperty("thirdRocket").objectReferenceValue = thirdRocket;
                so.FindProperty("rocketMaterial").objectReferenceValue = material;
                so.FindProperty("paintMaterial").objectReferenceValue = paintMaterial;
                var array = so.FindProperty("puddleMaterials");
                array.arraySize = 9;
                for (int i = 0; i < 9; i++)
                    array.GetArrayElementAtIndex(i).objectReferenceValue = puddles[i];
                so.FindProperty("burstPrefab").objectReferenceValue = burstPrefab;
                so.FindProperty("fireSound").objectReferenceValue = fireSound;
                so.FindProperty("boomSound").objectReferenceValue = boomSound;
                var clips = so.FindProperty("reloadClips");
                clips.arraySize = 2;
                clips.GetArrayElementAtIndex(0).objectReferenceValue = reloadGrab;
                clips.GetArrayElementAtIndex(1).objectReferenceValue = reloadLock;
                so.ApplyModifiedPropertiesWithoutUndo();

                // GunEffects: 바주카 불꽃 연결
                var effects = player.GetComponent<GunEffects>();
                if (effects != null)
                {
                    var eso = new SerializedObject(effects);
                    eso.FindProperty("bazooka").objectReferenceValue = weapon;
                    eso.FindProperty("bazookaFirst.flash").objectReferenceValue = firstFlash;
                    eso.FindProperty("bazookaThird.flash").objectReferenceValue = thirdFlash;
                    eso.ApplyModifiedPropertiesWithoutUndo();
                }

                // 화면 탄약 표시: 무기 그림 바꾸기
                var canvas = ArtSlots.FindCanvas();
                var ammo = canvas != null ? canvas.GetComponentInChildren<AmmoDisplay>(true) : null;
                if (ammo != null)
                {
                    var icon = ammo.transform.Find("WeaponIcon");
                    var aso = new SerializedObject(ammo);
                    aso.FindProperty("weaponIcon").objectReferenceValue = icon != null ? icon.GetComponent<UnityEngine.UI.Image>() : null;
                    aso.FindProperty("rifleSprite").objectReferenceValue = rifleIcon;
                    aso.FindProperty("bazookaSprite").objectReferenceValue = bazookaSprite;
                    aso.ApplyModifiedPropertiesWithoutUndo();
                }

                // 시작은 소총 (Awake도 같은 상태로 만든다)
                thirdRoot.gameObject.SetActive(false);
                firstRoot.gameObject.SetActive(false);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 바주카포를 만들었어요 (키 1 소총 · 2 바주카 · F 바꾸기).");
            }
            AssetDatabase.SaveAssets();
        }

        // 3인칭: 오른손 뼈 아래 ThirdPersonBazooka
        static Transform BuildThird(GameObject prefab, Material material, Transform hand, GameObject player, ThirdPersonGunPose rifle,
            AutoGun gun, out Transform rocket, out ParticleSystem flash)
        {
            float parentScale = hand.lossyScale.x > 0f ? hand.lossyScale.x : 1f;
            var root = new GameObject("ThirdPersonBazooka").transform;
            root.SetParent(hand, false);
            root.localScale = Vector3.one * (ThirdScale / parentScale);

            var pose = root.gameObject.AddComponent<ThirdPersonBazookaPose>();
            var so = new SerializedObject(pose);
            var src = new SerializedObject(rifle); // 뼈 연결과 손 모양은 소총 자세와 같다
            foreach (var name in new[] { "body", "chest", "rightUpper", "rightMid", "rightHand", "leftUpper", "leftMid", "leftHand" })
                so.FindProperty(name).objectReferenceValue = src.FindProperty(name).objectReferenceValue;
            foreach (var name in new[] { "fistInHand", "leftHandEuler", "leftFistInHand" })
                so.FindProperty(name).vector3Value = src.FindProperty(name).vector3Value;
            so.FindProperty("gun").objectReferenceValue = gun;
            so.ApplyModifiedPropertiesWithoutUndo();

            rocket = PlaceRpg(prefab, material, root, ShadowCastingMode.On);
            flash = MakeFlash(root, rifle.transform.Find("MuzzleFlash"), 2.6f);
            return root;
        }

        // 1인칭: Model 아래 BazookaHolder (뒤 손잡이가 Model 원점 근처에 온다)
        static Transform BuildFirst(GameObject prefab, Material material, Transform model, Transform rifleHolder, AutoGun gun,
            out Transform rocket, out ParticleSystem flash)
        {
            var holder = new GameObject("BazookaHolder").transform;
            holder.SetParent(model, false);
            holder.localScale = Vector3.one * FirstScale;
            holder.localPosition = -RearGrip * FirstScale + FirstOffset;

            rocket = PlaceRpg(prefab, material, holder, ShadowCastingMode.Off);

            // 양팔: 소총용으로 뽑아 둔 팔 메시를 복사해서 바주카 손잡이 자리로 옮긴다 (팔 메시는 소총 손 자리 기준이다)
            foreach (var side in new[] { "RightArm", "LeftArm" })
            {
                var source = rifleHolder.Find(side);
                if (source == null)
                    continue;
                var arm = new GameObject(side).transform;
                arm.SetParent(holder, false);
                foreach (Transform part in source)
                {
                    var filter = part.GetComponent<MeshFilter>();
                    var renderer = part.GetComponent<MeshRenderer>();
                    if (filter == null || renderer == null)
                        continue;
                    var go = new GameObject(part.name, typeof(MeshFilter), typeof(MeshRenderer));
                    go.transform.SetParent(arm, false);
                    go.GetComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                    var r = go.GetComponent<MeshRenderer>();
                    r.sharedMaterial = renderer.sharedMaterial;
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }
                if (side == "RightArm")
                {
                    arm.localPosition = RearGrip - RifleRightGrip;
                }
                else
                {
                    arm.localPosition = FrontGrip - RifleLeftGrip;
                    var slide = arm.gameObject.AddComponent<BazookaArmSlide>();
                    var sso = new SerializedObject(slide);
                    sso.FindProperty("gun").objectReferenceValue = gun;
                    sso.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            flash = MakeFlash(holder, rifleHolder.Find("MuzzleFlash"), 2.2f);
            return holder;
        }

        // RPG7 모델 (총구가 -Z라서 Y 180도 돌려 바주카 앞이 +Z가 되게 한다). 로켓(Rocket) 자식을 돌려준다.
        static Transform PlaceRpg(GameObject prefab, Material material, Transform parent, ShadowCastingMode shadows)
        {
            var rpg = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            rpg.name = "Rpg";
            rpg.transform.localPosition = Vector3.zero;
            rpg.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            rpg.transform.localScale = Vector3.one;
            Transform rocket = null;
            foreach (var t in rpg.GetComponentsInChildren<Transform>(true))
                if (t.name == "Rocket")
                    rocket = t;
            foreach (var r in rpg.GetComponentsInChildren<Renderer>(true))
            {
                r.sharedMaterial = material;
                r.shadowCastingMode = shadows;
                r.receiveShadows = shadows == ShadowCastingMode.On;
            }
            return rocket;
        }

        // 소총 불꽃을 복사해 발사관 앞 끝에 붙인다
        static ParticleSystem MakeFlash(Transform parent, Transform rifleFlash, float scale)
        {
            if (rifleFlash == null)
                return null;
            var copy = Object.Instantiate(rifleFlash.gameObject, parent, false);
            copy.name = "MuzzleFlash";
            copy.transform.localPosition = new Vector3(0f, 0f, 0.42f);
            copy.transform.localRotation = Quaternion.identity;
            copy.transform.localScale = Vector3.one * scale;
            return copy.GetComponent<ParticleSystem>();
        }

        // 바주카 전용 머티리얼: 에셋의 그림(Textures_Guns.png)을 URP Lit에 입힌다. 이미 있으면 사용자가 고친 것이므로 그대로 둔다.
        static Material GetMaterial()
        {
            const string path = "Assets/Materials/Bazooka.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Low Poly Weapons VOL.1/Textures_Guns.png");
            if (texture != null)
                material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.15f);
            material.SetFloat("_Metallic", 0f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ── HUD 그림 (소총 그림과 같은 색으로, 크림색 몸 + 갈색 테두리) ──
        static void MakeIcon()
        {
            var reference = new Texture2D(2, 2);
            reference.LoadImage(File.ReadAllBytes("Assets/UI/Weapon_Icon.png"));
            int w = reference.width, h = reference.height;
            Color fill = new Color(0.93f, 0.87f, 0.74f, 1f), line = new Color(0.45f, 0.33f, 0.2f, 1f);
            var counts = new System.Collections.Generic.Dictionary<Color32, int>();
            float darkest = 9f;
            foreach (var c in reference.GetPixels32())
            {
                if (c.a < 250)
                    continue;
                counts[c] = counts.TryGetValue(c, out int n) ? n + 1 : 1;
                float lum = (c.r + c.g + c.b) / 765f;
                if (lum < darkest)
                {
                    darkest = lum;
                    line = c;
                }
            }
            int best = 0;
            foreach (var pair in counts)
                if (pair.Value > best && (pair.Key.r + pair.Key.g + pair.Key.b) / 765f > darkest + 0.25f)
                {
                    best = pair.Value;
                    fill = pair.Key;
                }

            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            const float Outline = 2.2f;
            float sx = w / 128f, sy = h / 48f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float coverFill = 0f, coverLine = 0f;
                    const int S = 3;
                    for (int j = 0; j < S; j++)
                        for (int i = 0; i < S; i++)
                        {
                            var p = new Vector2((x + (i + 0.5f) / S) / sx, (y + (j + 0.5f) / S) / sy);
                            float d = Shape(p);
                            if (d <= 0f) coverFill += 1f;
                            if (d <= Outline) coverLine += 1f;
                        }
                    coverFill /= S * S;
                    coverLine /= S * S;
                    Color result = Color.Lerp(line, fill, coverFill / Mathf.Max(coverLine, 0.0001f));
                    result.a = coverLine;
                    texture.SetPixel(x, y, result);
                }
            texture.Apply();
            File.WriteAllBytes(IconPng, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(reference);
        }

        // 바주카 모양 (128x48 칸, 좌표는 왼쪽 아래 기준). 가장 가까운 모양까지의 거리 (안쪽이면 음수)
        static float Shape(Vector2 p)
        {
            float d = Capsule(p, new Vector2(24f, 27f), 5.2f, new Vector2(88f, 27f), 5.2f);   // 발사관
            d = Mathf.Min(d, Capsule(p, new Vector2(6f, 27f), 10f, new Vector2(22f, 27f), 5.5f)); // 뒤쪽 나팔
            d = Mathf.Min(d, Box(p, new Vector2(82f, 27f), new Vector2(4f, 9f)));              // 앞 테두리
            d = Mathf.Min(d, Capsule(p, new Vector2(92f, 27f), 5.5f, new Vector2(122f, 27f), 1.2f)); // 로켓 머리
            d = Mathf.Min(d, Box(p, new Vector2(52f, 14f), new Vector2(3.5f, 9f)));            // 뒤 손잡이
            d = Mathf.Min(d, Box(p, new Vector2(70f, 15f), new Vector2(3.5f, 8f)));            // 앞 손잡이
            d = Mathf.Min(d, Box(p, new Vector2(40f, 36f), new Vector2(1.6f, 4f)));            // 뒤 가늠자
            d = Mathf.Min(d, Box(p, new Vector2(80f, 35.5f), new Vector2(1.6f, 3.5f)));        // 앞 가늠쇠
            return d;
        }

        static float Box(Vector2 p, Vector2 center, Vector2 half)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - center.x), Mathf.Abs(p.y - center.y)) - half;
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f);
        }

        // 양 끝 반지름이 다른 캡슐 (선분 위를 따라 반지름이 변한다)
        static float Capsule(Vector2 p, Vector2 a, float ra, Vector2 b, float rb)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).magnitude - Mathf.Lerp(ra, rb, t);
        }

        // ── 소리 (합성) ──
        static AudioClip EnsureSound(string name, System.Func<float[]> make)
        {
            string path = $"Assets/Audio/Combat/{name}.wav";
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(path) == null)
            {
                WriteWav(path, make());
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        // 발사: 낮은 "쿵" + 뒤로 뿜는 쉭 소리 (0.7초)
        static float[] MakeFire()
        {
            var rnd = new System.Random(41);
            int n = (int)(SampleRate * 0.7f);
            var data = new float[n];
            float lp = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float freq = 140f * Mathf.Exp(-t * 7f) + 38f;
                phase += freq / SampleRate;
                float thump = Mathf.Sin(6.283f * phase) * Mathf.Exp(-t * 7f);
                float cutoff = Mathf.Lerp(0.5f, 0.04f, Mathf.Clamp01(t / 0.5f));
                lp += (((float)rnd.NextDouble() * 2f - 1f) - lp) * cutoff;
                float whoosh = lp * Mathf.Exp(-t * 4.5f) * (t < 0.006f ? t / 0.006f : 1f);
                data[i] = thump * 0.9f + whoosh * 1.1f;
            }
            return Normalize(data, 0.9f);
        }

        // 폭발: 낮고 긴 울림 + 흩어지는 잡음 (1.4초)
        static float[] MakeBoom()
        {
            var rnd = new System.Random(42);
            int n = (int)(SampleRate * 1.4f);
            var data = new float[n];
            float lp = 0f, lp2 = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float freq = 90f * Mathf.Exp(-t * 3f) + 30f;
                phase += freq / SampleRate;
                float rumble = Mathf.Sin(6.283f * phase) * Mathf.Exp(-t * 2.6f);
                float raw = (float)rnd.NextDouble() * 2f - 1f;
                lp += (raw - lp) * Mathf.Lerp(0.5f, 0.05f, Mathf.Clamp01(t / 0.6f));
                lp2 += (lp - lp2) * 0.3f;
                float crack = lp * Mathf.Exp(-t * 8f);
                float body = lp2 * Mathf.Exp(-t * 3f);
                data[i] = rumble * 0.9f + crack * 0.7f + body * 1.2f;
            }
            return Normalize(data, 0.95f);
        }

        // 로켓을 잡는 소리: 짧은 쇳소리 (0.2초)
        static float[] MakeGrab()
        {
            var rnd = new System.Random(43);
            int n = (int)(SampleRate * 0.2f);
            var data = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                lp += (((float)rnd.NextDouble() * 2f - 1f) - lp) * 0.35f;
                data[i] = (Mathf.Sin(6.283f * 310f * t) * 0.5f + Mathf.Sin(6.283f * 790f * t) * 0.25f + lp * 0.5f) * Mathf.Exp(-t * 28f);
            }
            return Normalize(data, 0.6f);
        }

        // 로켓이 철컥 끼워지는 소리: 쇳소리 두 번 (0.35초)
        static float[] MakeLock()
        {
            var rnd = new System.Random(44);
            int n = (int)(SampleRate * 0.35f);
            var data = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                lp += (((float)rnd.NextDouble() * 2f - 1f) - lp) * 0.4f;
                float a = (Mathf.Sin(6.283f * 180f * t) * 0.7f + lp * 0.6f) * Mathf.Exp(-t * 35f);
                float c = t - 0.12f;
                float b = c > 0f ? (Mathf.Sin(6.283f * 420f * c) * 0.5f + Mathf.Sin(6.283f * 1100f * c) * 0.2f + lp * 0.4f) * Mathf.Exp(-c * 30f) : 0f;
                data[i] = a + b;
            }
            return Normalize(data, 0.7f);
        }

        static float[] Normalize(float[] data, float peak)
        {
            float max = 0.0001f;
            foreach (var v in data)
                max = Mathf.Max(max, Mathf.Abs(v));
            for (int i = 0; i < data.Length; i++)
                data[i] = data[i] / max * peak;
            return data;
        }

        static void WriteWav(string path, float[] data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = new FileStream(path, FileMode.Create))
            using (var w = new BinaryWriter(stream))
            {
                int bytes = data.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(SampleRate);
                w.Write(SampleRate * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(bytes);
                foreach (var v in data)
                    w.Write((short)(Mathf.Clamp(v, -1f, 1f) * 32767f));
            }
        }

        static Transform Find(GameObject root, string name)
        {
            if (root == null)
                return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name)
                    return t;
            return null;
        }

        static void Remove(Transform parent, string name)
        {
            var old = parent.Find(name);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
        }
    }
}
