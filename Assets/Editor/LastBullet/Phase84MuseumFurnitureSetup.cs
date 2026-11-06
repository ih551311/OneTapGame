using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 미술관을 에셋 "Furniture Mega Pack - Free"(dlgames)로 새로 꾸민다 (2026-11-06 사용자 요청).
    //   - 옛날 유리 탁자·의자 8개와 옷장 4개를 지운다 (옷장은 문이 열리는 6개 중 북쪽 모서리 2개만 남긴다).
    //   - 지우지 않는 것: 일기장 책상(GrandpaDesk), 이야기 안내판(StorySign), 거울 포털(StageMap), 종이 합성대(PaperCraftTable), 그림판(PaintWorkbench), 액자·시계·양탄자·조명.
    //   - 새 가구는 MuseumHall/Decor/Furniture 아래에 이름(Furn_...)을 붙여 놓는다. 가구마다 충돌 상자(BoxCollider)가 있어 통과하지 못한다.
    //   - 가구 앞면은 모델의 -Z쪽이다 (모델 안에서 확인). 모델의 발이 바닥(y=0)에 닿게, 벽 가구는 뒷면이 벽에 닿게 잰다.
    //   - 쓰는 텍스처는 크기를 1024로 줄여(메모리) 가져온다. 에셋 폴더는 .gitignore에 있고, 쓰는 파일만 git add -f 로 올린다 (목록: Temp/furniture_files.txt).
    // 다시 누르면 Furniture를 지우고 다시 만든다. 메뉴: LastBullet > Phase 84 미술관 가구 놓기
    public static class Phase84MuseumFurnitureSetup
    {
        const string Root = "Assets/Furniture Mega Pack/Prefabs/";
        // 미술관 안쪽 벽면 (벽 가운데 ±18.2 / -8.2 / 15.2, 두께 0.4)
        const float WestWall = -18.0f, EastWall = 18.0f, SouthWall = -8.0f;
        const float Gap = 0.03f;
        const float FrontYawOffset = 180f; // 모델 앞면이 -Z라서, 보게 할 방향에 180도를 더해 돌린다

        enum Wall { None, West, East, South }

        class Piece
        {
            public string prefab, name;
            public float x, z;      // 가운데 (벽에 붙이면 벽에서 먼 쪽 좌표는 무시)
            public float facing;    // 앞면이 향할 방향: 0 = +Z(북), 90 = +X(동), 180 = -Z(남), 270 = -X(서)
            public Wall wall;
        }

        static Piece P(string prefab, string name, float x, float z, float facing, Wall wall = Wall.None)
        {
            return new Piece { prefab = prefab, name = name, x = x, z = z, facing = facing, wall = wall };
        }

        static List<Piece> Layout()
        {
            var list = new List<Piece>
            {
                // 2026-11-06 사용자 요청 "미술관처럼 꾸며": 식탁·서랍장·의자 세트는 집 같아서 빼고,
                // 벽에는 그림 액자(Phase 85), 가운데에는 작품을 보며 쉬는 낮은 의자와 벽 소파만 둔다.
                // ── 서쪽 벽 (앞면이 동쪽을 본다) ──
                P("Sofas/Sofa23", "Furn_SofaWest", 0, 5.3f, 90, Wall.West),
                P("Chairs/Chair01", "Furn_DeskChair", -15.3f, 0.5f, 270),
                // ── 동쪽 벽 (앞면이 서쪽을 본다) ──
                P("Sofas/Sofa13", "Furn_SofaEast", 0, 0.5f, 270, Wall.East),
                // ── 남쪽(입구) 벽 쉼터 ──
                P("Sofas/Sofa26", "Furn_SofaSouthWest", -9.5f, 0, 0, Wall.South),
                P("Sofas/Sofa27", "Furn_SofaSouthEast", 9.5f, 0, 0, Wall.South),
                // ── 액자를 보며 쉬는 낮은 의자(등받이 없는 푸프) 4개: 북쪽 액자를 바라보는 자리 ──
                P("Sofas/Sofa04", "Furn_PoufA", -12.8f, 9.6f, 0),
                P("Sofas/Sofa04", "Furn_PoufB", -6.4f, 9.6f, 0),
                P("Sofas/Sofa04", "Furn_PoufC", 6.4f, 9.6f, 0),
                P("Sofas/Sofa04", "Furn_PoufD", 12.8f, 9.6f, 0),
            };
            return list;
        }

        [MenuItem("LastBullet/Phase 84 미술관 가구 놓기")]
        static void Create()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/1_Museum.unity");
            var decor = GameObject.Find("MuseumHall/Decor");
            if (decor == null)
            {
                Debug.LogWarning("[LastBullet] 1_Museum: MuseumHall/Decor 가 없어요.");
                return;
            }

            // 1) 옛 가구 지우기: 유리 탁자·의자, 옷장은 북쪽 모서리(z=11) 2개만 남긴다
            var toDelete = new List<GameObject>();
            foreach (Transform child in decor.transform)
            {
                string n = child.name;
                if (n == "GlassTable" || n == "OldChair" || n == "StrawChair")
                    toDelete.Add(child.gameObject);
                else if (n.StartsWith("Wardrobe_") && child.position.z < 10f)
                    toDelete.Add(child.gameObject);
            }
            foreach (var go in toDelete)
                Object.DestroyImmediate(go);

            var oldFurniture = decor.transform.Find("Furniture");
            if (oldFurniture != null)
                Object.DestroyImmediate(oldFurniture.gameObject);
            var container = new GameObject("Furniture").transform;
            container.SetParent(decor.transform, false);

            // 2) 새 가구 놓기
            var used = new HashSet<string>();
            int count = 0;
            foreach (var piece in Layout())
            {
                string path = Root + piece.prefab + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"[LastBullet] 프리팹이 없어요: {path}");
                    continue;
                }
                used.Add(path);
                PlacePiece(prefab, piece, container, scene);
                count++;
            }

            // 3) 쓰는 파일 정리: 텍스처 1024로, 목록 저장
            var files = new HashSet<string>();
            foreach (var path in used)
                foreach (var dep in AssetDatabase.GetDependencies(path, true))
                {
                    if (!dep.StartsWith("Assets/Furniture Mega Pack/"))
                        continue;
                    files.Add(dep);
                    files.Add(dep + ".meta");
                }
            int shrunk = 0;
            foreach (var file in files)
            {
                if (!file.EndsWith(".png") && !file.EndsWith(".jpg") && !file.EndsWith(".tga"))
                    continue;
                var importer = AssetImporter.GetAtPath(file) as TextureImporter;
                if (importer != null && importer.maxTextureSize > 1024)
                {
                    importer.maxTextureSize = 1024;
                    importer.SaveAndReimport();
                    shrunk++;
                }
            }
            Directory.CreateDirectory("Temp");
            File.WriteAllLines("Temp/furniture_files.txt", new List<string>(files));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LastBullet] 1_Museum: 옛 가구 {toDelete.Count}개를 지우고 새 가구 {count}개를 놓았어요 (쓰는 파일 {files.Count}개, 텍스처 {shrunk}장 1024로 줄임).");
        }

        static void PlacePiece(GameObject prefab, Piece piece, Transform parent, UnityEngine.SceneManagement.Scene scene)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.name = piece.name;
            go.transform.SetParent(parent, false);
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;

            // 돌리기 전의 모델 크기로 충돌 상자를 만든다 (모델 기준 좌표)
            var local = WorldBounds(go);
            var box = go.AddComponent<BoxCollider>();
            box.center = local.center;
            box.size = local.size;

            go.transform.rotation = Quaternion.Euler(0f, piece.facing + FrontYawOffset, 0f);
            var b = WorldBounds(go);
            Vector3 shift = new Vector3(piece.x, 0f, piece.z) - new Vector3(b.center.x, 0f, b.center.z);
            shift.y = -b.min.y; // 발이 바닥에 닿게
            switch (piece.wall)
            {
                case Wall.West: shift.x = (WestWall + Gap) - b.min.x; break;
                case Wall.East: shift.x = (EastWall - Gap) - b.max.x; break;
                case Wall.South: shift.z = (SouthWall + Gap) - b.min.z; break;
            }
            go.transform.position += shift;
        }

        static Bounds WorldBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            var b = renderers[0].bounds;
            foreach (var r in renderers)
                b.Encapsulate(r.bounds);
            return b;
        }
    }
}
