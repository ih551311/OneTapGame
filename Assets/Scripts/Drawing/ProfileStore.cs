using System;
using System.IO;
using LastBullet.Core;
using UnityEngine;

namespace LastBullet.Drawing
{
    // 내 프로필: 닉네임과 프로필 사진 (2026-10-31 사용자 요청). save.json에 저장된다.
    //  - 닉네임: 옵션에서 바꾼다. 비어 있으면 기본 이름("견습 관장"), 최대 10글자.
    //  - 프로필 사진: ① 타이틀에서 직접 그린 그림(SavedData/profile.png — 물감·종이를 쓰지 않고 작품 목록에도 들어가지 않는다)
    //                 ② 미술관에서 그린 작품 중 하나 (작품 ID만 기억한다. 그 작품을 지우면 프로필 사진도 비워진다)
    // 바뀌면 Changed가 불려서 타이틀의 프로필 카드가 새로 그려진다.
    public static class ProfileStore
    {
        public const string DefaultNickname = "견습 관장";
        public const int MaxNicknameLength = 10;
        public const string DrawnId = "drawn"; // 직접 그린 프로필 그림

        public static event Action Changed;

        static Texture2D drawnTexture;

        static string DrawnPath => Path.Combine(SaveManager.FolderPath, "profile.png");

        public static string Nickname
        {
            get
            {
                string name = SaveManager.Data.nickname;
                return string.IsNullOrWhiteSpace(name) ? DefaultNickname : name;
            }
            set
            {
                string clean = Clean(value);
                if (clean == SaveManager.Data.nickname)
                    return;
                SaveManager.Data.nickname = clean;
                SaveManager.Save();
                Changed?.Invoke();
            }
        }

        // "" = 사진 없음, "drawn" = 직접 그린 그림, 그 외 = 작품 ID
        public static string PictureId => SaveManager.Data.profilePicture ?? "";

        // 입력 글자를 다듬는다: 줄바꿈·제어 문자 제거, 앞뒤 공백 제거, 최대 길이로 자름. 이름이 아니면 빈 문자열(= 기본 이름).
        public static string Clean(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "";
            var chars = new System.Text.StringBuilder();
            foreach (char c in raw)
            {
                if (!char.IsControl(c))
                    chars.Append(c);
            }
            string name = chars.ToString().Trim();
            if (name.Length > MaxNicknameLength)
                name = name.Substring(0, MaxNicknameLength).TrimEnd();
            return name;
        }

        // 미술관에서 그린 작품을 프로필 사진으로
        public static void SetPainting(string paintingId)
        {
            SaveManager.Data.profilePicture = paintingId ?? "";
            SaveManager.Save();
            Changed?.Invoke();
        }

        // 타이틀에서 그린 그림을 프로필 사진으로 (PNG로 저장). 실패하면 false.
        public static bool SetDrawn(Texture2D texture)
        {
            try
            {
                Directory.CreateDirectory(SaveManager.FolderPath);
                File.WriteAllBytes(DrawnPath, texture.EncodeToPNG());
            }
            catch (Exception e)
            {
                Debug.LogError($"[ProfileStore] 프로필 그림 저장 실패: {e.Message}");
                return false;
            }
            if (drawnTexture != null)
                UnityEngine.Object.Destroy(drawnTexture);
            drawnTexture = null;
            SaveManager.Data.profilePicture = DrawnId;
            SaveManager.Save();
            Changed?.Invoke();
            Debug.Log("[ProfileStore] 프로필 그림 저장");
            return true;
        }

        // 지금 프로필 사진. 없거나 파일이 사라졌으면 null.
        public static Texture2D LoadTexture()
        {
            string id = PictureId;
            if (string.IsNullOrEmpty(id))
                return null;
            if (id != DrawnId)
                return PaintingLibrary.LoadTexture(PaintingLibrary.Find(id));

            if (drawnTexture != null)
                return drawnTexture;
            if (!File.Exists(DrawnPath))
                return null;
            drawnTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            drawnTexture.LoadImage(File.ReadAllBytes(DrawnPath));
            return drawnTexture;
        }

        // 지워진 작품이 프로필 사진이었다면 비운다 (PaintingLibrary.Delete가 부른다)
        public static void ClearIfPainting(string paintingId)
        {
            if (string.IsNullOrEmpty(paintingId) || PictureId != paintingId)
                return;
            SaveManager.Data.profilePicture = "";
            SaveManager.Save();
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            drawnTexture = null;
            Changed = null;
        }
    }
}
