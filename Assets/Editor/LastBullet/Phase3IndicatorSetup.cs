using System.IO;
using LastBullet.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace LastBullet.EditorTools
{
    // Phase 3-4: 떨어진 탄환 위에 빛 기둥을 세우고, 화면 밖 탄환을 가리키는 화살표를 만든다.
    // 메뉴: LastBullet > Phase 3-4 빛 기둥과 방향 화살표 만들기
    public static class Phase3IndicatorSetup
    {
        const string PrefabPath = "Assets/Prefabs/DroppedBullet.prefab";
        const string PillarMaterialPath = "Assets/Materials/LightPillar.mat";
        const string ArrowSpritePath = "Assets/UI/Arrow.png";
        static readonly Color32 PaintYellow = new Color32(0xFA, 0xCC, 0x15, 0xFF);

        [MenuItem("LastBullet/Phase 3-4 빛 기둥과 방향 화살표 만들기")]
        static void Create()
        {
            // Hideout 씬이 열려 있지 않으면 자동으로 연다.
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Hideout")
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    return;
                scene = EditorSceneManager.OpenScene("Assets/Scenes/Hideout.unity");
            }

            var hud = ArtSlots.FindCanvas();
            var indicator = hud != null ? hud.GetComponent<BulletIndicatorUI>() : null;
            if (indicator == null)
            {
                EditorUtility.DisplayDialog("LastBullet", "Phase 3-3 메뉴를 먼저 실행하세요.", "확인");
                return;
            }
            if (hud.transform.Find("BulletArrow") != null)
            {
                EditorUtility.DisplayDialog("LastBullet", "HUD에 이미 BulletArrow가 있어요.", "확인");
                return;
            }

            // 1. 탄환 프리팹에 빛 기둥 추가: 높이 6m의 반투명 노란 원기둥 (충돌, 그림자 없음)
            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot.transform.Find("LightPillar") == null)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = "LightPillar";
                Object.DestroyImmediate(pillar.GetComponent<Collider>());
                pillar.transform.SetParent(prefabRoot.transform, false);
                pillar.transform.localPosition = new Vector3(0f, 3f, 0f);   // 원기둥은 높이 2라서 3배 → 6m, 바닥부터 위로
                pillar.transform.localScale = new Vector3(0.25f, 3f, 0.25f);
                var renderer = pillar.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = GetOrCreatePillarMaterial();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            // 2. 화면 밖 방향 화살표 (노란 삼각형, 평소에는 숨김)
            var arrowObject = new GameObject("BulletArrow", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(arrowObject, "Create BulletArrow");
            arrowObject.transform.SetParent(hud.transform, false);
            var rect = arrowObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(40f, 40f);
            var arrow = arrowObject.GetComponent<Image>();
            arrow.sprite = GetOrCreateArrowSprite();
            arrow.color = PaintYellow;
            arrow.raycastTarget = false;
            arrow.enabled = false;
            arrowObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);

            // 3. BulletIndicatorUI에 화살표 연결
            var so = new SerializedObject(indicator);
            so.FindProperty("arrow").objectReferenceValue = arrow;
            so.ApplyModifiedProperties();

            Selection.activeGameObject = hud;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[LastBullet] Phase 3-4: 빛 기둥과 방향 화살표를 만들고 Hideout 씬을 저장했어요.");
        }

        // URP Unlit 반투명 머티리얼 (노랑, 투명도 35%)
        static Material GetOrCreatePillarMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(PillarMaterialPath);
            if (material != null)
                return material;

            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", new Color(PaintYellow.r / 255f, PaintYellow.g / 255f, PaintYellow.b / 255f, 0.35f));
            // Inspector에서 Surface Type을 Transparent로 바꾸는 것과 같은 설정
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, PillarMaterialPath);
            return material;
        }

        // 64px 흰 삼각형(위쪽을 가리킴) 이미지를 만들어 스프라이트로 가져온다.
        static Sprite GetOrCreateArrowSprite()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowSpritePath);
            if (sprite != null)
                return sprite;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                // 아래쪽이 넓고 위쪽이 뾰족한 삼각형
                float halfWidth = (size - 1 - y) * 0.5f;
                for (int x = 0; x < size; x++)
                {
                    float fromCenter = Mathf.Abs(x + 0.5f - size / 2f);
                    float alpha = Mathf.Clamp01(halfWidth - fromCenter + 0.5f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            File.WriteAllBytes(Path.Combine(Application.dataPath, "UI/Arrow.png"), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(ArrowSpritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ArrowSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(ArrowSpritePath);
        }
    }
}
