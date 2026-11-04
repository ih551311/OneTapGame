using LastBullet.Combat;
using LastBullet.Player;
using LastBullet.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // 총을 한 발 탄환(OneBulletGun)에서 연사 총(AutoGun)으로 바꾼다 (2026-11-04 사용자 결정: 전투를 데스티니식으로 — A단계).
    //   Player: OneBulletGun을 지우고 AutoGun을 붙인다 (총구·탄도선·맞은 점·발사 소리 연결은 그대로 옮긴다).
    //           재장전 소리용 ReloadSound(2D AudioSource)를 만들고 탄창 빼기·넣기·장전 소리를 연결한다.
    //           PlayerAnimator·FirstPersonGun이 새 총을 보게 다시 연결한다.
    //   Canvas: 탄환 위치 안내(BulletIndicatorUI, ReturnGauge, BulletArrow)를 지우고, 오른쪽 아래에 탄약 표시(AmmoDisplay)를 만든다.
    // 이미 바꿨으면 건너뛰는 부분은 건너뛴다. 메뉴: LastBullet > Phase 59 연사 총으로 바꾸기
    public static class Phase59AutoGunSetup
    {
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";
        const string Reload = "Assets/FreeWeaponSFX_v1.1/Handgun/Reload/";

        static readonly string[] Scenes =
        {
            "Assets/Scenes/1_Museum.unity",
            "Assets/Scenes/2_Forest.unity",
            "Assets/Scenes/3_CastleVillage.unity",
            "Assets/Scenes/4_Desert.unity",
        };

        [MenuItem("LastBullet/Phase 59 연사 총으로 바꾸기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var clips = new[]
            {
                AssetDatabase.LoadAssetAtPath<AudioClip>(Reload + "sfx_wpn_hgn_reload_02_magOut.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(Reload + "sfx_wpn_hgn_reload_04_magIn.wav"),
                AssetDatabase.LoadAssetAtPath<AudioClip>(Reload + "sfx_wpn_hgn_reload_05_charge.wav"),
            };

            foreach (var path in Scenes)
            {
                var scene = EditorSceneManager.OpenScene(path);
                var player = GameObject.FindWithTag("Player");
                var canvas = ArtSlots.FindCanvas();
                if (player == null || canvas == null)
                {
                    Debug.LogWarning($"[LastBullet] {scene.name}: Player나 Canvas가 없어 건너뜀");
                    continue;
                }

                // 1. 총: OneBulletGun → AutoGun
                var old = player.GetComponent<OneBulletGun>();
                var gun = player.GetComponent<AutoGun>();
                if (gun == null)
                    gun = player.AddComponent<AutoGun>();
                var so = new SerializedObject(gun);
                if (old != null)
                {
                    var oldSo = new SerializedObject(old);
                    so.FindProperty("muzzle").objectReferenceValue = oldSo.FindProperty("muzzle").objectReferenceValue;
                    so.FindProperty("tracer").objectReferenceValue = oldSo.FindProperty("tracer").objectReferenceValue;
                    so.FindProperty("impactMarkerPrefab").objectReferenceValue = oldSo.FindProperty("impactMarkerPrefab").objectReferenceValue;
                    so.FindProperty("shotSound").objectReferenceValue = oldSo.FindProperty("shotSound").objectReferenceValue;
                }

                // 2. 재장전 소리 (2D)
                var holder = player.transform.Find("ReloadSound");
                if (holder == null)
                {
                    var go = new GameObject("ReloadSound", typeof(AudioSource));
                    go.transform.SetParent(player.transform, false);
                    holder = go.transform;
                }
                var audio = holder.GetComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.spatialBlend = 0f;
                so.FindProperty("reloadAudio").objectReferenceValue = audio;
                var clipArray = so.FindProperty("reloadClips");
                clipArray.arraySize = clips.Length;
                for (int i = 0; i < clips.Length; i++)
                    clipArray.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
                so.ApplyModifiedPropertiesWithoutUndo();

                if (old != null)
                    Object.DestroyImmediate(old);

                // 3. 새 총을 보는 다른 스크립트 다시 연결
                var animator = player.GetComponent<PlayerAnimator>();
                if (animator != null)
                {
                    var animSo = new SerializedObject(animator);
                    animSo.FindProperty("gun").objectReferenceValue = gun;
                    animSo.ApplyModifiedPropertiesWithoutUndo();
                }
                var camera = Camera.main;
                var firstPerson = camera != null ? camera.GetComponentInChildren<FirstPersonGun>(true) : null;
                if (firstPerson != null)
                {
                    var fpSo = new SerializedObject(firstPerson);
                    fpSo.FindProperty("gun").objectReferenceValue = gun;
                    fpSo.ApplyModifiedPropertiesWithoutUndo();
                }

                // 4. 탄환 위치 안내 지우기
                var indicator = canvas.GetComponent<BulletIndicatorUI>();
                if (indicator != null)
                    Object.DestroyImmediate(indicator);
                foreach (var name in new[] { "ReturnGauge", "BulletArrow" })
                {
                    var t = canvas.transform.Find(name);
                    if (t != null)
                        Object.DestroyImmediate(t.gameObject);
                }

                // 5. 탄약 표시 (오른쪽 아래)
                BuildAmmoDisplay(canvas.transform);

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[LastBullet] {scene.name}: 연사 총과 탄약 표시를 만들었어요.");
            }
            AssetDatabase.SaveAssets();
        }

        static void BuildAmmoDisplay(Transform canvas)
        {
            var old = canvas.Find("AmmoDisplay");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);

            var root = Phase6PaintBoardSetup.Sliced("AmmoDisplay", canvas, "Assets/UI/Hud_Name.png", 14f);
            root.GetComponent<Image>().raycastTarget = false;
            var rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(-40f, 40f);
            rect.sizeDelta = new Vector2(300f, 96f);

            var ammo = Text("AmmoText", root.transform, 54f, TextAlignmentOptions.MidlineRight, new Color(0.93f, 0.9f, 0.85f));
            Phase4StageSelectSetup.Stretch(ammo.rectTransform);
            ammo.rectTransform.offsetMin = new Vector2(16f, 6f);
            ammo.rectTransform.offsetMax = new Vector2(-18f, -6f);
            ammo.text = "30 / 30";

            var status = Text("StatusText", root.transform, 22f, TextAlignmentOptions.TopLeft, new Color(1f, 0.85f, 0.4f));
            Phase4StageSelectSetup.Stretch(status.rectTransform);
            status.rectTransform.offsetMin = new Vector2(16f, 6f);
            status.rectTransform.offsetMax = new Vector2(-16f, -8f);
            status.text = "";
            status.enabled = false;

            var display = root.AddComponent<AmmoDisplay>();
            var so = new SerializedObject(display);
            so.FindProperty("ammoText").objectReferenceValue = ammo;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static TextMeshProUGUI Text(string name, Transform parent, float size, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }
    }
}
