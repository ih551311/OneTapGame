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
    // 지금은 읽기와 등록(Add)만 있다. 그림판의 "완성"(Phase 7)이 Add를 쓴다.
    public static class PaintingLibrary
    {
        public static string PaintingsFolder => Path.Combine(SaveManager.FolderPath, "Paintings");
        static string ListPath => Path.Combine(SaveManager.FolderPath, "paintings.json");

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
                return info;
            }
            catch (Exception e)
            {
                Debug.LogError($"[PaintingLibrary] 작품 저장 실패: {e.Message}");
                return null;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            textureCache.Clear();
        }
    }
}
