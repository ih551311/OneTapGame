using System.Collections.Generic;
using System.IO;
using LastBullet.Inventory;
using LastBullet.Stage;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastBullet.EditorTools
{
    // 미술관과 전투 맵에 "사람이 지나간 흔적" 같은 작은 디테일을 더한다 (2026-11-08 사용자 요청: "AI티 안 나게" — 맵·미술관 디테일).
    //   미술관: ① 액자마다 아래에 놋쇠 이름표 "제1작 … 제9작"  ② 그림 작업대·합성대 둘레와 입구 쪽 바닥에 튄 물감 자국 (9색, 제각각 크기·방향)
    //   전투 맵(숲·성 마을·사막): 시작 위치에서 보스가 있는 한가운데까지 이어지는 물감 발자국 길(그 맵의 3색)과, 보스 둘레 바닥에 크게 번진 물감 — 어디로 가야 하는지 은근히 알려 주고 보스 자리를 눈에 띄게 한다.
    //   물감 자국은 흰색 얼룩 그림(Assets/UI/PaintBlob.png)에 색을 입힌 투명 쿼드(Assets/Materials/FloorPaint_*.mat 9개)라서, 그림·색을 바꿔 끼울 수 있다.
    // 다시 누르면 지우고 새로 만든다. 메뉴: LastBullet > Phase 90 미술관·맵 소품 더하기
    public static class Phase90DetailsSetup
    {
        const string BlobPng = "Assets/UI/PaintBlob.png";
        const string FontPath = "Assets/UI/Fonts/font1 SDF.asset";

        static readonly string[] ColorNames = { "Red", "Orange", "Yellow", "Green", "Blue", "Purple", "Pink", "White", "Black" };

        [MenuItem("LastBullet/Phase 90 미술관·맵 소품 더하기")]
        static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var materials = new Material[9];
            for (int i = 0; i < 9; i++)
                materials[i] = GetOrCreateMaterial(i);

            Museum(materials);
            foreach (var (path, boss) in new[] { ("Assets/Scenes/2_Forest.unity", "Spawner_Boss_01"), ("Assets/Scenes/3_CastleVillage.unity", "Spawner_Boss_01"), ("Assets/Scenes/4_Desert.unity", "Spawner_Boss_01") })
                BattleMap(path, boss, materials);
            AssetDatabase.SaveAssets();
        }

        // ───────────── 미술관 ─────────────
        static void Museum(Material[] materials)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var hall = GameObject.Find("MuseumHall");
            if (hall == null)
                return;
            var old = hall.transform.Find("Details");
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            var root = new GameObject("Details").transform;
            root.SetParent(hall.transform, false);

            // ① 액자 이름표
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var brass = ArtSlots.GetOrCreateMaterial("Plaque_Brass", new Color(0.86f, 0.70f, 0.38f));
            brass.SetFloat("_Smoothness", 0.35f);
            brass.SetFloat("_Metallic", 0.2f);
            brass.EnableKeyword("_EMISSION"); // 어두운 미술관에서도 이름표가 보이게 은은하게 빛난다
            brass.SetColor("_EmissionColor", new Color(0.30f, 0.22f, 0.10f));
            brass.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(brass);
            var frames = GameObject.Find("PictureFrames");
            string[] numerals = { "제1작", "제2작", "제3작", "제4작", "제5작", "제6작", "제7작", "제8작", "제9작" };
            if (frames != null)
            {
                int index = 0;
                foreach (Transform frame in frames.transform)
                {
                    if (index >= 9)
                        break;
                    var plaque = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    plaque.name = $"Plaque_{index + 1}";
                    Object.DestroyImmediate(plaque.GetComponent<Collider>());
                    plaque.transform.SetParent(root, false);
                    // 액자 앞 그림 면이 -Z쪽을 보고, 벽은 +Z쪽: 액자 아래 가운데, 벽에서 조금 나오게
                    Vector3 p = frame.position;
                    plaque.transform.position = new Vector3(p.x, 0.33f, p.z - 0.06f);
                    plaque.transform.localScale = new Vector3(1.15f, 0.3f, 0.04f);
                    plaque.GetComponent<Renderer>().sharedMaterial = brass;
                    plaque.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

                    // 글자는 이름표의 자식이 아니라 같은 자리에 따로 둔다 (배율이 얽히지 않게). 이름표 앞면(-Z쪽)에서 조금 앞으로.
                    var textGo = new GameObject($"Label_{index + 1}", typeof(RectTransform));
                    textGo.transform.SetParent(root, false);
                    textGo.transform.position = new Vector3(p.x, 0.33f, p.z - 0.095f);
                    textGo.transform.rotation = Quaternion.identity; // 글자 앞면이 -Z(방 안쪽)를 본다
                    var rect = (RectTransform)textGo.transform;
                    rect.sizeDelta = new Vector2(1.15f, 0.3f);
                    var text = textGo.AddComponent<TextMeshPro>();
                    text.font = font != null ? font : TMP_Settings.defaultFontAsset;
                    text.text = numerals[index];
                    text.fontSize = 2.4f;
                    text.alignment = TextAlignmentOptions.Center;
                    text.verticalAlignment = VerticalAlignmentOptions.Middle;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.color = new Color32(0x2A, 0x1A, 0x0C, 0xFF); // 놋쇠 위에 새긴 어두운 갈색 글자
                    index++;
                }
            }

            // ② 바닥에 튄 물감 자국
            var rnd = new System.Random(2026);
            var spots = new List<Vector2>();
            // 그림 작업대(4, 3)와 합성대(-4, 3) 둘레 + 입구 근처 + 액자 앞쪽
            for (int i = 0; i < 9; i++) spots.Add(new Vector2(4f + Range(rnd, -2.8f, 2.8f), 3.5f + Range(rnd, -2.6f, 2.6f)));
            for (int i = 0; i < 4; i++) spots.Add(new Vector2(-4f + Range(rnd, -2.2f, 2.2f), 3f + Range(rnd, -2.2f, 2.2f)));
            for (int i = 0; i < 5; i++) spots.Add(new Vector2(Range(rnd, -5f, 5f), -6.5f + Range(rnd, -1.3f, 1.3f)));
            for (int i = 0; i < 4; i++) spots.Add(new Vector2(Range(rnd, -9f, 9f), 12.5f + Range(rnd, -1f, 1f)));
            foreach (var spot in spots)
            {
                if (Mathf.Abs(spot.x) < 1.7f)
                    continue; // 빨간 카펫 위는 비운다 (바닥 높이가 다르다)
                MakeSplat(root, materials[rnd.Next(9)], new Vector3(spot.x, 0.02f, spot.y), Vector3.up, (float)rnd.NextDouble() * 360f, Range(rnd, 0.5f, 1.5f), (float)rnd.NextDouble());
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LastBullet] 1_Museum: 이름표 {numerals.Length}개와 바닥 물감 자국 {spots.Count}개를 더했어요.");
        }

        // ───────────── 전투 맵 ─────────────
        static void BattleMap(string path, string bossName, Material[] materials)
        {
            var scene = EditorSceneManager.OpenScene(path);
            var player = GameObject.FindWithTag("Player");
            var boss = GameObject.Find(bossName);
            if (player == null || boss == null)
            {
                Debug.LogWarning($"[LastBullet] {scene.name}: Player나 {bossName}이 없어 건너뜀");
                return;
            }
            var old = GameObject.Find("PaintTrail");
            if (old != null)
                Object.DestroyImmediate(old);
            var root = new GameObject("PaintTrail").transform;

            var colors = StageColors.For(scene.name);
            var rnd = new System.Random(scene.name.GetHashCode());
            Vector3 start = player.transform.position, end = boss.transform.position;
            Vector3 along = end - start;
            along.y = 0f;
            float length = along.magnitude;
            Vector3 dir = along / length;
            Vector3 side = Vector3.Cross(Vector3.up, dir);

            int count = 0;
            // 시작 위치에서 보스까지: 4~6m마다 한 방울, 좌우로 조금씩 어긋나게 (보스 앞 16m 전에서 멈춘다 — 그 뒤는 보스 둘레 얼룩이 이어받는다)
            for (float d = 6f; d < length - 16f; d += 4f + (float)rnd.NextDouble() * 2.5f)
            {
                Vector3 p = start + dir * d + side * Range(rnd, -2.8f, 2.8f);
                var color = Pick(colors, rnd);
                if (Ground(p, out var hit))
                {
                    MakeSplat(root, materials[color], hit.point + hit.normal * 0.03f, hit.normal, (float)rnd.NextDouble() * 360f, Range(rnd, 1.0f, 2.2f), (float)rnd.NextDouble());
                    count++;
                }
            }
            // 보스 둘레: 크게 번진 얼룩 14개 (반지름 5~13m)
            for (int i = 0; i < 14; i++)
            {
                float angle = (float)rnd.NextDouble() * Mathf.PI * 2f;
                float radius = Range(rnd, 5f, 13f);
                Vector3 p = end + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                var color = Pick(colors, rnd);
                if (Ground(p, out var hit))
                {
                    MakeSplat(root, materials[color], hit.point + hit.normal * 0.03f, hit.normal, (float)rnd.NextDouble() * 360f, Range(rnd, 2.5f, 5.5f), (float)rnd.NextDouble());
                    count++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[LastBullet] {scene.name}: 물감 길과 보스 둘레 얼룩 {count}개를 더했어요.");
        }

        static int Pick(IReadOnlyList<PaintColor> colors, System.Random rnd)
        {
            if (colors == null || colors.Count == 0)
                return rnd.Next(9);
            return (int)colors[rnd.Next(colors.Count)];
        }

        static bool Ground(Vector3 around, out RaycastHit hit)
        {
            // 위에서 아래로 쏘아 땅(나무·돌 같은 큰 물체가 아닌 가장 위 표면 중 완만한 곳)에 붙인다
            if (Physics.Raycast(new Vector3(around.x, around.y + 60f, around.z), Vector3.down, out hit, 200f, ~0, QueryTriggerInteraction.Ignore))
                return hit.normal.y > 0.8f; // 가파른 곳(벽·바위)에는 붙이지 않는다
            return false;
        }

        static float Range(System.Random rnd, float min, float max) => min + (float)rnd.NextDouble() * (max - min);

        // 바닥에 눕힌 투명 쿼드 한 장 (물감 자국)
        static void MakeSplat(Transform parent, Material material, Vector3 position, Vector3 normal, float spin, float size, float stretch)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Splat";
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.back, normal) * Quaternion.Euler(0f, 0f, spin);
            // 쿼드는 -Z쪽을 보고 있으므로 위(normal)를 보게 눕힌다. 가로세로를 조금 다르게 해서 찌그러진 얼룩 모양
            go.transform.localScale = new Vector3(size * (1f + stretch * 0.35f), size * (1f - stretch * 0.2f), 1f);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        static Material GetOrCreateMaterial(int colorIndex)
        {
            string path = $"Assets/Materials/FloorPaint_{ColorNames[colorIndex]}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent + 5;
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(BlobPng));
            var c = (Color)PaintColors.Colors[colorIndex];
            c.a = 0.78f; // 바닥 결이 조금 비치게
            material.SetColor("_BaseColor", c);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
