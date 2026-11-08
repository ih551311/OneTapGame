using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastBullet.EditorTools
{
    // 화면 그림(UI)을 손으로 그린 듯한 종이·잉크 느낌으로 다시 그린다 (2026-11-08 사용자 요청: "AI티 안 나게 잘 만들어봐").
    //   지금까지 코드로 만든 임시 그림은 반듯한 흰 막대·납작한 색 칸이라 "만들다 만 느낌"이 났다.
    //   그래서 종이 창과 같은 따뜻한 갈색 잉크 테두리 + 종이 결 + 살짝 흔들리는 선으로 다시 그린다.
    //   크기는 그대로라서 스프라이트 설정(9-slice 등)은 그대로 먹는다. 사용자가 같은 이름으로 덮어써도 된다.
    //   (이 메뉴를 다시 누르면 아래 목록의 그림만 다시 그린다.)
    // 메뉴: LastBullet > Phase 89 화면 그림 손맛 내기
    public static class Phase89UiArtPolish
    {
        static readonly Color Ink = new Color32(0x3E, 0x28, 0x18, 0xFF);        // 짙은 갈색 잉크
        static readonly Color InkSoft = new Color32(0x6E, 0x50, 0x34, 0xFF);
        static readonly Color Cream = new Color32(0xF4, 0xEA, 0xD2, 0xFF);
        static readonly Color Tan = new Color32(0xD6, 0xBC, 0x94, 0xFF);
        static readonly Color TanDark = new Color32(0xB8, 0x98, 0x6C, 0xFF);
        static readonly Color Leather = new Color32(0x4A, 0x32, 0x22, 0xFF);
        static readonly Color PaintRed = new Color32(0xD0, 0x3A, 0x2E, 0xFF);
        static readonly Color Amber = new Color32(0xE9, 0xA0, 0x28, 0xFF);

        const string Folder = "Assets/UI/";

        // ───────────── 그리기 도구 ─────────────
        class Img
        {
            public readonly int w, h;
            public readonly Color[] px;
            public Img(int w, int h) { this.w = w; this.h = h; px = new Color[w * h]; }
        }

        delegate float Sdf(float x, float y);       // 모양까지의 거리 (안쪽이 음수)
        delegate Color Paintf(float x, float y);    // 칠할 색

        static void Paint(Img img, Sdf sdf, Paintf color)
        {
            for (int y = 0; y < img.h; y++)
                for (int x = 0; x < img.w; x++)
                {
                    float d = sdf(x + 0.5f, y + 0.5f);
                    float a = Mathf.Clamp01(0.5f - d);
                    if (a <= 0f)
                        continue;
                    Color src = color(x + 0.5f, y + 0.5f);
                    a *= src.a;
                    Color dst = img.px[y * img.w + x];
                    float oa = a + dst.a * (1f - a);
                    if (oa <= 0f)
                        continue;
                    Color o = (src * a + dst * dst.a * (1f - a)) / oa;
                    o.a = oa;
                    img.px[y * img.w + x] = o;
                }
        }

        static Paintf Solid(Color c) => (x, y) => c;

        // 위(y 큼)에서 아래로 색이 변한다
        static Paintf Vertical(int h, Color top, Color bottom, float grain = 0.03f, float seed = 0f) =>
            (x, y) =>
            {
                var c = Color.Lerp(bottom, top, y / h);
                float n = (Noise(x * 0.5f + seed, y * 0.5f) - 0.5f) * grain + (Noise(x * 0.08f, y * 0.08f + seed) - 0.5f) * grain;
                return new Color(c.r + n, c.g + n, c.b + n * 0.8f, c.a);
            };

        static Sdf Wobble(Sdf inner, float amount, float freq = 0.12f, float seed = 0f) =>
            (x, y) => inner(x + (Noise(x * freq + seed, y * freq) - 0.5f) * amount * 2f, y + (Noise(x * freq, y * freq + seed + 31f) - 0.5f) * amount * 2f);

        static Sdf Grow(Sdf inner, float amount) => (x, y) => inner(x, y) - amount;

        static Sdf Union(params Sdf[] shapes) => (x, y) =>
        {
            float d = float.MaxValue;
            foreach (var s in shapes)
                d = Mathf.Min(d, s(x, y));
            return d;
        };

        static Sdf RoundRect(float cx, float cy, float hx, float hy, float r) => (x, y) =>
        {
            float qx = Mathf.Abs(x - cx) - (hx - r), qy = Mathf.Abs(y - cy) - (hy - r);
            return Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) + Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f)) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        };

        static Sdf Circle(float cx, float cy, float r) => (x, y) => Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) - r;

        static Sdf Capsule(float ax, float ay, float bx, float by, float r) => (x, y) =>
        {
            float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
            float t = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay + 1e-6f));
            float dx = pax - bax * t, dy = pay - bay * t;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        };

        // 다각형 (y는 그림 위쪽이 작은 값: 화면 좌표로 적고 아래에서 뒤집는다)
        static Sdf Poly(int h, params float[] xy) => (x, yy) =>
        {
            float y = h - yy;
            int n = xy.Length / 2;
            float d = (x - xy[0]) * (x - xy[0]) + (y - xy[1]) * (y - xy[1]);
            float s = 1f;
            for (int i = 0, j = n - 1; i < n; j = i, i++)
            {
                float ex = xy[j * 2] - xy[i * 2], ey = xy[j * 2 + 1] - xy[i * 2 + 1];
                float wx = x - xy[i * 2], wy = y - xy[i * 2 + 1];
                float t = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey + 1e-6f));
                float bx = wx - ex * t, by = wy - ey * t;
                d = Mathf.Min(d, bx * bx + by * by);
                bool c1 = y >= xy[i * 2 + 1], c2 = y < xy[j * 2 + 1], c3 = ex * wy > ey * wx;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3))
                    s = -s;
            }
            return s * Mathf.Sqrt(d);
        };

        static float Noise(float x, float y)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(xi, yi), b = Hash(xi + 1, yi), c = Hash(xi, yi + 1), e = Hash(xi + 1, yi + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, e, fx), fy);
        }

        static float Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
            }
        }

        static void Save(Img img, string name)
        {
            var texture = new Texture2D(img.w, img.h, TextureFormat.RGBA32, false);
            texture.SetPixels(img.px);
            texture.Apply();
            string path = Folder + name + ".png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed; // 작은 그림이 깨져 보이지 않게
                importer.SaveAndReimport();
            }
        }

        // 잉크 테두리 + 안쪽 칠: 모양을 한 번 부풀려 잉크색으로 깔고, 그 위에 칠한다
        static void Inked(Img img, Sdf shape, Paintf fill, float outline, Color ink)
        {
            Paint(img, Grow(shape, outline), Solid(ink));
            Paint(img, shape, fill);
        }

        // ───────────── 그림들 ─────────────
        [MenuItem("LastBullet/Phase 89 화면 그림 손맛 내기")]
        static void Run()
        {
            Crosshair();
            HitMarker();
            EnemyBar();
            HealthBar();
            WeaponIcon();
            Slider();
            Scrollbar();
            PaintTools();
            IconPaper();
            ArrowRight();
            Profile();
            InputBackground();
            TitleBackground();
            AssetDatabase.SaveAssets();
            Debug.Log("[LastBullet] 화면 그림을 종이·잉크 느낌으로 다시 그렸어요.");
        }

        // 조준점: 크림색 짧은 네 획 + 갈색 잉크 테두리 (밝은 곳·어두운 곳 어디서나 보이게)
        static void Crosshair()
        {
            var img = new Img(64, 64);
            Sdf bars = Union(Capsule(32, 7, 32, 21, 2.0f), Capsule(32, 43, 32, 57, 2.0f), Capsule(7, 32, 21, 32, 2.0f), Capsule(43, 32, 57, 32, 2.0f));
            bars = Wobble(bars, 0.35f, 0.3f, 3f);
            Inked(img, bars, Solid(Cream), 1.5f, Ink);
            Save(img, "Crosshair");
        }

        // 힛마커: 대각선 네 획
        static void HitMarker()
        {
            var img = new Img(64, 64);
            Sdf bars = Union(Capsule(10, 10, 22, 22, 2.2f), Capsule(54, 10, 42, 22, 2.2f), Capsule(10, 54, 22, 42, 2.2f), Capsule(54, 54, 42, 42, 2.2f));
            bars = Wobble(bars, 0.35f, 0.3f, 7f);
            Inked(img, bars, Solid(Cream), 1.5f, Ink);
            Save(img, "HitMarker");
        }

        // 적 체력바: 어두운 잉크 바탕 + 붉은 물감 채움 (64×8)
        static void EnemyBar()
        {
            var back = new Img(64, 8);
            Paint(back, RoundRect(32, 4, 32, 4, 3.5f), Solid(Ink));
            Paint(back, RoundRect(32, 4, 30.6f, 2.6f, 2.4f), Vertical(8, new Color32(0x5A, 0x40, 0x2C, 0xFF), new Color32(0x32, 0x22, 0x16, 0xFF)));
            Save(back, "EnemyBar_Back");

            var fill = new Img(64, 8);
            Paint(fill, RoundRect(32, 4, 32, 4, 3.5f), Vertical(8, new Color32(0xE8, 0x5A, 0x48, 0xFF), new Color32(0xB0, 0x28, 0x1E, 0xFF), 0.06f, 3f));
            Paint(fill, RoundRect(32, 6.3f, 29f, 0.8f, 0.8f), Solid(new Color(1f, 0.85f, 0.8f, 0.55f))); // 윗부분 반짝
            Save(fill, "EnemyBar_Fill");
        }

        // 플레이어 체력바 (256×32): 종이 위에 놓이는 잉크 테두리 트랙 + 붉은 물감 / 회복 때 번지는 연두
        static void HealthBar()
        {
            var back = new Img(256, 32);
            Sdf outer = Wobble(RoundRect(128, 16, 127, 15.4f, 14f), 0.5f, 0.1f, 2f);
            Paint(back, outer, Solid(Ink));
            Sdf inner = Wobble(RoundRect(128, 16, 124.4f, 12.6f, 11.6f), 0.4f, 0.1f, 5f);
            Paint(back, inner, (x, y) =>
            {
                var c = Color.Lerp(new Color32(0x9A, 0x7C, 0x58, 0xFF), new Color32(0xCF, 0xB5, 0x8E, 0xFF), y / 32f);
                float n = (Noise(x * 0.4f, y * 0.4f) - 0.5f) * 0.06f;
                return new Color(c.r + n, c.g + n, c.b + n, 1f);
            });
            Save(back, "HealthBar_Back");

            var fill = new Img(256, 32);
            Sdf bar = Wobble(RoundRect(128, 16, 126.5f, 14.3f, 12.5f), 0.45f, 0.1f, 9f);
            Paint(fill, bar, (x, y) =>
            {
                var c = Color.Lerp(new Color32(0xB4, 0x2A, 0x22, 0xFF), new Color32(0xE4, 0x58, 0x44, 0xFF), y / 32f);
                float streak = (Noise(x * 0.05f, y * 0.9f) - 0.5f) * 0.13f; // 붓 자국
                return new Color(c.r + streak, c.g + streak * 0.6f, c.b + streak * 0.6f, 1f);
            });
            Paint(fill, RoundRect(128, 24.5f, 118f, 1.5f, 1.5f), Solid(new Color(1f, 0.9f, 0.85f, 0.45f)));
            Save(fill, "HealthBar_Fill");

            var gain = new Img(256, 32);
            Paint(gain, Wobble(RoundRect(128, 16, 126.5f, 14.3f, 12.5f), 0.45f, 0.1f, 4f), (x, y) =>
            {
                var c = Color.Lerp(new Color32(0x6E, 0xA8, 0x4C, 0xFF), new Color32(0xA8, 0xD2, 0x78, 0xFF), y / 32f);
                float streak = (Noise(x * 0.05f, y * 0.9f + 8f) - 0.5f) * 0.1f;
                return new Color(c.r + streak, c.g + streak, c.b + streak * 0.6f, 1f);
            });
            Save(gain, "HealthBar_Gain");
        }

        // 탄약 옆 총 아이콘: AK47 옆모습 (크림색 + 잉크 테두리)
        static void WeaponIcon()
        {
            const int H = 48;
            var img = new Img(128, H);
            Sdf gun = Union(
                Poly(H, 8, 21, 32, 19, 36, 30, 10, 34),                // 개머리판
                Poly(H, 30, 17, 82, 17, 82, 29, 30, 29),               // 몸통(리시버)
                Poly(H, 82, 19, 106, 19, 106, 28, 82, 28),             // 손잡이 덮개
                Capsule(104, H - 23, 124, H - 23, 1.8f),               // 총열
                Capsule(86, H - 16, 106, H - 16, 1.3f),                // 가스관
                Poly(H, 116, 14, 119.5f, 14, 119.5f, 22, 116, 22),     // 가늠쇠
                Poly(H, 49, 28, 61, 28, 67, 40, 63, 46, 54, 44),       // 휜 탄창
                Poly(H, 37, 28, 45, 28, 43, 42, 35, 40),               // 권총 손잡이
                Poly(H, 30, 15, 44, 15, 44, 17, 30, 17));              // 위 덮개
            gun = Wobble(gun, 0.25f, 0.25f, 11f);
            Inked(img, gun, Vertical(H, new Color32(0xFA, 0xF2, 0xDE, 0xFF), new Color32(0xE2, 0xD2, 0xB0, 0xFF), 0.02f), 1.6f, Ink);
            // 몸통에 잉크 선 몇 개 (부품 느낌)
            Paint(img, Union(Capsule(31, H - 23, 81, H - 23, 0.5f), Capsule(60, H - 17, 60, H - 29, 0.5f)), Solid(new Color(Ink.r, Ink.g, Ink.b, 0.55f)));
            Save(img, "Weapon_Icon");
        }

        // 옵션 슬라이더 (128×32 / 128×32 / 64×64)
        static void Slider()
        {
            var back = new Img(128, 32);
            Paint(back, Wobble(RoundRect(64, 16, 63, 12.5f, 12.5f), 0.35f, 0.12f, 1f), Solid(Ink));
            Paint(back, Wobble(RoundRect(64, 16, 60.6f, 9.4f, 9.4f), 0.3f, 0.12f, 6f), Vertical(32, new Color32(0x8E, 0x70, 0x4E, 0xFF), new Color32(0xC9, 0xAE, 0x86, 0xFF)));
            Save(back, "Slider_Background");

            var fill = new Img(128, 32);
            Paint(fill, Wobble(RoundRect(64, 16, 63, 11f, 11f), 0.35f, 0.12f, 12f), (x, y) =>
            {
                var c = Color.Lerp(new Color32(0xD4, 0x84, 0x1A, 0xFF), new Color32(0xF2, 0xB8, 0x48, 0xFF), y / 32f);
                float streak = (Noise(x * 0.06f, y * 0.9f) - 0.5f) * 0.1f;
                return new Color(c.r + streak, c.g + streak, c.b + streak * 0.5f, 1f);
            });
            Save(fill, "Slider_Fill");

            var handle = new Img(64, 64);
            Inked(handle, Wobble(Circle(32, 32, 22), 0.4f, 0.15f, 3f), Vertical(64, new Color32(0xFF, 0xF8, 0xE6, 0xFF), new Color32(0xE6, 0xD2, 0xA8, 0xFF), 0.03f), 3.2f, Ink);
            Paint(handle, Circle(32, 32, 6.5f), Solid(PaintRed));              // 가운데 물감 한 방울
            Paint(handle, Circle(30.5f, 34.5f, 2f), Solid(new Color(1f, 0.85f, 0.8f, 0.7f)));
            Save(handle, "Slider_Handle");
        }

        // 스크롤바 (48×48 9-slice)
        static void Scrollbar()
        {
            var track = new Img(48, 48);
            Paint(track, Wobble(RoundRect(24, 24, 23, 23, 13f), 0.3f, 0.12f, 2f), Solid(Ink));
            Paint(track, Wobble(RoundRect(24, 24, 20.4f, 20.4f, 10.5f), 0.3f, 0.12f, 8f), Vertical(48, new Color32(0x7E, 0x62, 0x44, 0xFF), new Color32(0xA9, 0x8C, 0x64, 0xFF)));
            Save(track, "Scrollbar_Track");

            var handle = new Img(48, 48);
            Paint(handle, Wobble(RoundRect(24, 24, 23, 23, 13f), 0.3f, 0.12f, 4f), Solid(Ink));
            Paint(handle, Wobble(RoundRect(24, 24, 20.4f, 20.4f, 10.5f), 0.3f, 0.12f, 10f), Vertical(48, new Color32(0xF6, 0xDA, 0x9A, 0xFF), new Color32(0xDB, 0xAE, 0x5C, 0xFF)));
            Save(handle, "Scrollbar_Handle");
        }

        // 그림판 도구 칸 (96×96): 바탕 + 펜 점 4개 + 지우개 + 선택 테두리
        static void PaintTools()
        {
            var bar = new Img(96, 96);
            Inked(bar, Wobble(RoundRect(48, 48, 44, 44, 20f), 0.35f, 0.1f, 5f), (x, y) =>
            {
                var c = Color.Lerp(new Color32(0xC9, 0xAC, 0x82, 0xFF), new Color32(0xE4, 0xCE, 0xA6, 0xFF), y / 96f);
                float n = (Noise(x * 0.4f, y * 0.4f) - 0.5f) * 0.05f;
                return new Color(c.r + n, c.g + n, c.b + n, 1f);
            }, 2.6f, Ink);
            Save(bar, "Paint_Toolbar");

            // 펜 굵기 점: 예전 그림의 점 크기(불투명 면적)에 맞춰 같은 크기로 그린다
            foreach (var pen in new[] { "Paint_Tool_Pen1", "Paint_Tool_Pen2", "Paint_Tool_Pen3", "Paint_Tool_Pen4" })
            {
                float radius = MeasureDotRadius(Folder + pen + ".png");
                var img = new Img(96, 96);
                Paint(img, Wobble(Circle(48, 48, radius), 0.25f, 0.2f, 15f), Solid(Ink));
                Paint(img, Circle(48 - radius * 0.3f, 48 + radius * 0.3f, Mathf.Max(0.6f, radius * 0.22f)), Solid(new Color(1f, 1f, 1f, 0.18f)));
                Save(img, pen);
            }

            var eraser = new Img(96, 96);
            Sdf body = Wobble(RoundRect(48, 48, 22, 11, 6f), 0.3f, 0.15f, 2f);
            // 지우개: 비스듬히 놓인 직사각형 (45도 회전)
            Sdf rotated = (x, y) => body(48 + (x - 48) * 0.7071f - (y - 48) * 0.7071f, 48 + (x - 48) * 0.7071f + (y - 48) * 0.7071f);
            Inked(eraser, rotated, (x, y) =>
            {
                float side = ((x - 48) * 0.7071f - (y - 48) * 0.7071f);
                return side > 5f ? new Color32(0xE8, 0x7A, 0x84, 0xFF) : new Color32(0xF1, 0xE4, 0xC6, 0xFF); // 분홍 지우개 끝 + 종이 띠
            }, 2.4f, Ink);
            Save(eraser, "Paint_Tool_Eraser");

            // 선택 테두리: 호박색 띠(안쪽은 비워 아래 그림이 보이게) + 바깥·안쪽 얇은 잉크선
            var ring = new Img(96, 96);
            Sdf outerRing = Wobble(RoundRect(48, 48, 44, 44, 20f), 0.4f, 0.1f, 9f);
            Sdf innerRing = RoundRect(48, 48, 36.5f, 36.5f, 14f);
            Paint(ring, (x, y) => Mathf.Max(outerRing(x, y), -innerRing(x, y)), Solid(Amber));
            var ink = new Img(96, 96);
            Paint(ink, (x, y) => Mathf.Abs(outerRing(x, y) + 0.2f) - 1.1f, Solid(Ink));
            Paint(ink, (x, y) => Mathf.Abs(innerRing(x, y)) - 1.1f, Solid(Ink));
            for (int i = 0; i < ring.px.Length; i++)
            {
                Color r = ring.px[i], k = ink.px[i];
                float oa = k.a + r.a * (1f - k.a);
                ring.px[i] = oa <= 0f ? Color.clear : new Color((k.r * k.a + r.r * r.a * (1f - k.a)) / oa, (k.g * k.a + r.g * r.a * (1f - k.a)) / oa, (k.b * k.a + r.b * r.a * (1f - k.a)) / oa, oa);
            }
            Save(ring, "Paint_Tool_Selected");
        }

        // 아직 이 그림의 점이 얼마나 큰지(불투명 면적 → 반지름)
        static float MeasureDotRadius(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null)
                return 6f;
            // 가져오기 설정과 상관없이 읽으려고 파일에서 직접 읽는다
            var temp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            temp.LoadImage(File.ReadAllBytes(path));
            float area = 0f;
            foreach (var c in temp.GetPixels())
                area += c.a;
            UnityEngine.Object.DestroyImmediate(temp);
            return Mathf.Max(2f, Mathf.Sqrt(area / Mathf.PI));
        }

        // 종이 아이콘 (64×64): 접힌 귀퉁이가 있는 종이 + 글줄
        static void IconPaper()
        {
            const int H = 64;
            var img = new Img(64, H);
            Sdf page = Wobble(Poly(H, 12, 6, 40, 6, 54, 20, 54, 58, 12, 58), 0.3f, 0.2f, 6f);
            Inked(img, page, Vertical(H, new Color32(0xFF, 0xFA, 0xEA, 0xFF), new Color32(0xEE, 0xE2, 0xC2, 0xFF), 0.03f), 2.4f, Ink);
            Inked(img, Poly(H, 40, 6, 40, 20, 54, 20), Solid(new Color32(0xD8, 0xC4, 0x9A, 0xFF)), 1.6f, Ink); // 접힌 귀퉁이
            for (int i = 0; i < 4; i++)
                Paint(img, Capsule(19, H - (28 + i * 7), 47 - (i == 3 ? 12 : 0), H - (28 + i * 7), 0.9f), Solid(new Color(Ink.r, Ink.g, Ink.b, 0.6f)));
            Save(img, "Icon_Paper");
        }

        // 화살표 (96×64)
        static void ArrowRight()
        {
            const int H = 64;
            var img = new Img(96, H);
            Sdf arrow = Wobble(Poly(H, 8, 24, 52, 24, 52, 8, 88, 32, 52, 56, 52, 40, 8, 40), 0.3f, 0.15f, 8f);
            Inked(img, arrow, Vertical(H, new Color32(0xFF, 0xF4, 0xD8, 0xFF), new Color32(0xE6, 0xCF, 0xA2, 0xFF), 0.03f), 2.6f, Ink);
            Save(img, "Arrow_Right");
        }

        // 프로필: 빈 프로필(종이색 바탕에 갈색 사람 모양)과 이름 칸(가죽 + 바느질)
        static void Profile()
        {
            var empty = new Img(256, 256);
            Paint(empty, RoundRect(128, 128, 128, 128, 0f), (x, y) =>
            {
                var c = Color.Lerp(new Color32(0xC9, 0xAE, 0x86, 0xFF), new Color32(0xE2, 0xCB, 0xA2, 0xFF), y / 256f);
                float n = (Noise(x * 0.3f, y * 0.3f) - 0.5f) * 0.06f + (Noise(x * 0.04f, y * 0.04f) - 0.5f) * 0.06f;
                return new Color(c.r + n, c.g + n, c.b + n, 1f);
            });
            var person = Union(Circle(128, 156, 40), RoundRect(128, 52, 76, 52, 34f));
            Paint(empty, Wobble(person, 1.2f, 0.05f, 4f), (x, y) => new Color(0x8A / 255f + (Noise(x * 0.3f, y * 0.3f) - 0.5f) * 0.05f, 0x6A / 255f, 0x48 / 255f, 1f));
            Save(empty, "Profile_Empty");

            var plate = new Img(96, 96);
            Paint(plate, Wobble(RoundRect(48, 48, 46, 46, 14f), 0.35f, 0.1f, 2f), Solid(Ink));
            Paint(plate, Wobble(RoundRect(48, 48, 43f, 43f, 11.5f), 0.3f, 0.1f, 5f), (x, y) =>
            {
                var c = Color.Lerp(Leather, new Color32(0x5E, 0x42, 0x2E, 0xFF), y / 96f);
                float n = (Noise(x * 0.7f, y * 0.7f) - 0.5f) * 0.07f;
                return new Color(c.r + n, c.g + n, c.b + n, 1f);
            });
            // 바느질 (점선)
            for (int i = 0; i < 40; i++)
            {
                float t = i / 40f * 4f;
                float px, py;
                float s = 36f; // 중심에서 36px 안쪽의 네모
                if (t < 1f) { px = 48 - s + 2 * s * t; py = 48 + s; }
                else if (t < 2f) { px = 48 + s; py = 48 + s - 2 * s * (t - 1f); }
                else if (t < 3f) { px = 48 + s - 2 * s * (t - 2f); py = 48 - s; }
                else { px = 48 - s; py = 48 - s + 2 * s * (t - 3f); }
                if (i % 2 == 0)
                    Paint(plate, Circle(px, py, 1.1f), Solid(new Color32(0xD8, 0xB8, 0x78, 0xFF)));
            }
            Save(plate, "Profile_NamePlate");
        }

        // 입력 칸 (96×96): 가죽 안쪽을 파낸 듯한 어두운 칸
        static void InputBackground()
        {
            var img = new Img(96, 96);
            Paint(img, Wobble(RoundRect(48, 48, 46, 46, 14f), 0.35f, 0.1f, 6f), Solid(Ink));
            Paint(img, Wobble(RoundRect(48, 48, 43f, 43f, 11.5f), 0.3f, 0.1f, 9f), (x, y) =>
            {
                var c = Color.Lerp(new Color32(0x2C, 0x1C, 0x12, 0xFF), new Color32(0x44, 0x2E, 0x1E, 0xFF), y / 96f);
                float n = (Noise(x * 0.6f, y * 0.6f) - 0.5f) * 0.05f;
                return new Color(c.r + n, c.g + n, c.b + n, 1f);
            });
            Paint(img, RoundRect(48, 48, 43f, 43f, 11.5f), (x, y) => y > 90f ? new Color(0f, 0f, 0f, 0.25f) : Color.clear); // 위쪽 안쪽 그림자
            Save(img, "Input_Background");
        }

        // 타이틀 배경 (1920×1080): 따뜻한 나무 판벽 + 나무 바닥 + 가운데 위 램프 빛 + 가장자리에 번진 물감 얼룩
        static void TitleBackground()
        {
            const int W = 1920, H = 1080;
            var img = new Img(W, H);
            var splash = new[]
            {
                (cx: 190f, cy: 880f, r: 260f, c: (Color)new Color32(0xEF, 0x44, 0x44, 0xFF)),
                (cx: 520f, cy: 1010f, r: 210f, c: (Color)new Color32(0xF9, 0x73, 0x16, 0xFF)),
                (cx: 1500f, cy: 1030f, r: 240f, c: (Color)new Color32(0xFA, 0xCC, 0x15, 0xFF)),
                (cx: 1760f, cy: 840f, r: 270f, c: (Color)new Color32(0x22, 0xC5, 0x5E, 0xFF)),
                (cx: 120f, cy: 330f, r: 240f, c: (Color)new Color32(0x3B, 0x82, 0xF6, 0xFF)),
                (cx: 1820f, cy: 300f, r: 250f, c: (Color)new Color32(0xA8, 0x55, 0xF7, 0xFF)),
                (cx: 330f, cy: 120f, r: 200f, c: (Color)new Color32(0xF7, 0x55, 0xA6, 0xFF)),
                (cx: 1560f, cy: 90f, r: 190f, c: (Color)new Color32(0xF4, 0xEA, 0xD2, 0xFF)),
            };
            float floorTop = H * 0.27f; // 아래 27%가 바닥 (그림 좌표: 아래가 0)
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float fx = x / (float)W, fy = y / (float)H;
                    Color c;
                    if (y < floorTop)
                    {
                        // 나무 바닥: 가로 널빤지
                        float plank = Mathf.Floor(y / 46f);
                        float local = (y % 46f) / 46f;
                        float shade = 0.85f + Hash((int)plank, 7) * 0.3f;
                        float grain = Noise(x * 0.012f + plank * 9f, y * 0.35f) * 0.18f + Noise(x * 0.2f, y * 0.2f) * 0.05f;
                        c = new Color(0.30f * shade + grain * 0.5f, 0.20f * shade + grain * 0.35f, 0.13f * shade + grain * 0.2f);
                        if (local < 0.04f) c *= 0.55f;                                   // 널빤지 틈
                        c *= Mathf.Lerp(0.55f, 1f, fy / 0.27f);                         // 아래로 갈수록 어둡게
                    }
                    else
                    {
                        // 나무 판벽: 세로 널빤지
                        float panel = Mathf.Floor(x / 128f);
                        float local = (x % 128f) / 128f;
                        float shade = 0.88f + Hash((int)panel, 3) * 0.24f;
                        float grain = Noise(x * 0.35f, y * 0.012f + panel * 5f) * 0.14f + Noise(x * 0.15f, y * 0.15f) * 0.04f;
                        c = new Color(0.27f * shade + grain * 0.5f, 0.18f * shade + grain * 0.35f, 0.11f * shade + grain * 0.2f);
                        if (local < 0.012f || local > 0.992f) c *= 0.5f;                // 판 사이 틈
                        if (y < floorTop + 22f) c = Color.Lerp(c, new Color(0.42f, 0.29f, 0.18f), 0.6f); // 걸레받이
                        // 가운데 위에서 내려오는 램프 빛
                        float dx = (fx - 0.5f) * 1.6f, dy = (fy - 0.9f) * 1.2f;
                        float lamp = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) * 1.15f);
                        c += new Color(0.26f, 0.17f, 0.07f) * lamp * lamp;
                    }
                    // 물감 얼룩 (아주 옅게)
                    foreach (var s in splash)
                    {
                        float dx = x - s.cx, dy = y - s.cy;
                        float angle = Mathf.Atan2(dy, dx);
                        // 끊김 없이 이어지는 각도별 잡음으로 들쭉날쭉한 얼룩 모양 (꽃처럼 규칙적으로 보이지 않게)
                        float ca = Mathf.Cos(angle), sa = Mathf.Sin(angle);
                        float rr = s.r * (0.55f + 0.7f * Noise(ca * 1.5f + s.cx * 0.013f, sa * 1.5f + s.cy * 0.017f) + 0.12f * Noise(ca * 6f + s.cy * 0.01f, sa * 6f + s.cx * 0.01f));
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        float a = Mathf.Clamp01((rr - dist) / 18f) * 0.22f;
                        if (a > 0f)
                            c = Color.Lerp(c, s.c, a);
                    }
                    // 비네팅
                    float vx = (fx - 0.5f) * 2f, vy = (fy - 0.5f) * 2f;
                    float vig = 1f - Mathf.Clamp01((Mathf.Sqrt(vx * vx * 0.8f + vy * vy) - 0.55f) * 0.55f);
                    c *= vig;
                    c.a = 1f;
                    img.px[y * W + x] = c;
                }
            Save(img, "Title_Background");
        }
    }
}
