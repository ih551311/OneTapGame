using System;
using System.Collections.Generic;
using System.IO;
using LastBullet.Core;
using UnityEngine;

namespace LastBullet.Drawing
{
    // 작품 목록 한 줄 (paintings.json, 기획서 3.5)
    [Serializable]
    public class PaintingInfo
    {
        public string id;        // 고유 ID
        public string fileName;  // painting_<ID>.png
        public string createdAt; // 만든 날짜·시간 (yyyy-MM-dd HH:mm:ss)
    }

    [Serializable]
    class PaintingList
    {
        public List<PaintingInfo> paintings = new List<PaintingInfo>();
    }

    // 완성한 그림(작품) 목록과 PNG 파일을 다룬다 (기획서 3.5).
    //   SavedData/paintings.json        작품 ID, 파일명, 만든 날짜
    //   SavedData/Paintings/painting_<ID>.png
    // 읽기, 등록(Add: 그림판의 "완성"이 씀), 삭제(Delete: 그림 보기 팝업의 [삭제])가 있다.
    public static class PaintingLibrary
    {
        // 그림 종이(그림판 캔버스)는 정사각형으로 고정한다 (사용자 결정, 2026-10-30). 크기는 기획서 2.8 기본값 256×256.
        public const int CanvasSize = 256;

        public static string PaintingsFolder => Path.Combine(SaveManager.FolderPath, "Paintings");
        static string ListPath => Path.Combine(SaveManager.FolderPath, "paintings.json");

        // 작품이 새로 생기거나 지워질 때 (화면 왼쪽 위 상태창의 "그림 N장"이 듣는다)
        public static event Action Changed;

        static readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();

        // 만든 순서대로의 작품 목록
        public static List<PaintingInfo> LoadList()
        {
            try
            {
                if (File.Exists(ListPath))
                {
                    var list = JsonUtility.FromJson<PaintingList>(File.ReadAllText(ListPath));
                    if (list != null && list.paintings != null)
                        return list.paintings;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PaintingLibrary] 작품 목록 읽기 실패: {e.Message}");
            }
            return new List<PaintingInfo>();
        }

        public static PaintingInfo Find(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            return LoadList().Find(p => p.id == id);
        }

        // 작품 그림을 불러온다 (한 번 불러오면 기억해 둔다). 파일이 없으면 null.
        public static Texture2D LoadTexture(PaintingInfo info)
        {
            if (info == null)
                return null;
            if (textureCache.TryGetValue(info.id, out var cached) && cached != null)
                return cached;

            string path = Path.Combine(PaintingsFolder, info.fileName);
            if (!File.Exists(path))
                return null;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            texture.filterMode = FilterMode.Point; // 픽셀 그림이 흐려지지 않게
            textureCache[info.id] = texture;
            return texture;
        }

        // 새 작품 등록: PNG로 저장하고 목록에 추가한다. 저장에 실패하면 null (기획서 2.8: 저장 성공 후에만 종이 차감).
        public static PaintingInfo Add(Texture2D texture)
        {
            if (texture.width != texture.height)
            {
                Debug.LogError($"[PaintingLibrary] 그림은 정사각형이어야 해요 ({texture.width}×{texture.height})");
                return null;
            }
            try
            {
                Directory.CreateDirectory(PaintingsFolder);
                string id = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                var info = new PaintingInfo
                {
                    id = id,
                    fileName = $"painting_{id}.png",
                    createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                };
                File.WriteAllBytes(Path.Combine(PaintingsFolder, info.fileName), texture.EncodeToPNG());

                var list = new PaintingList { paintings = LoadList() };
                list.paintings.Add(info);
                File.WriteAllText(ListPath, JsonUtility.ToJson(list, true));
                Debug.Log($"[PaintingLibrary] 작품 등록: {info.fileName}");
                Changed?.Invoke();
                return info;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PaintingLibrary] 작품 저장 실패: {e.Message}");
                return null;
            }
        }

        // 작품 하나를 지운다 (2026-10-31 사용자 요청): 목록에서 빼고 PNG 파일을 지우고,
        // 타이틀 액자·미술관 액자에 걸려 있던 것도 비운다. 되돌릴 수 없다. 못 찾으면 false.
        public static bool Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            try
            {
                var list = new PaintingList { paintings = LoadList() };
                var info = list.paintings.Find(p => p.id == id);
                if (info == null)
                    return false;

                // 목록을 먼저 고친다 (파일 지우기가 실패해도 목록에서는 사라지게)
                list.paintings.Remove(info);
                File.WriteAllText(ListPath, JsonUtility.ToJson(list, true));

                string path = Path.Combine(PaintingsFolder, info.fileName);
                if (File.Exists(path))
                    File.Delete(path);

                if (textureCache.TryGetValue(id, out var texture))
                {
                    if (texture != null)
                        UnityEngine.Object.Destroy(texture);
                    textureCache.Remove(id);
                }

                SaveManager.ClearTitleFramesWith(id);
                GalleryStore.ClearPainting(id);
                ProfileStore.ClearIfPainting(id);
                Debug.Log($"[PaintingLibrary] 작품 삭제: {info.fileName}");
                Changed?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PaintingLibrary] 작품 삭제 실패: {e.Message}");
                return false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            Changed = null;
            textureCache.Clear();
        }
    }
}
