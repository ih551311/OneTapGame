using System.Collections.Generic;
using LastBullet.Inventory;
using UnityEngine;

namespace LastBullet.Stage
{
    // 물감 지역 (2026-10-30 사용자 결정: 지역마다 얻을 수 있는 물감 색을 나눈다).
    // 이 오브젝트의 위치·크기(Size, 위에서 본 직사각형)가 지역이고, 그 안에서 만들어진 적은 Colors 중에서만 물감을 떨어뜨린다.
    // 어느 지역에도 없는 곳의 적은 예전처럼 9색 중 랜덤이다 (숲).
    // 판정은 적이 만들어진 자리(스포너 자리)로 한다. 적이 지역 밖으로 쫓아가도 색은 바뀌지 않는다 (EnemyDrop).
    // Scene 창에서 색깔 상자로 보이고, 위치·Size를 고치면 지역이 바뀐다.
    // 붙일 곳: 성 마을 씬의 PaintZones 아래 Zone_... 오브젝트
    public class PaintZone : MonoBehaviour
    {
        [Tooltip("지역 이름 (Scene 창과 로그에 표시)")]
        [SerializeField] string zoneName = "지역";

        [Tooltip("지역의 가로(X)·세로(Z) 크기 (m). 높이는 쓰지 않는다")]
        [SerializeField] Vector2 size = new Vector2(100f, 40f);

        [Tooltip("이 지역에서 얻을 수 있는 물감 색 (보통 3개)")]
        [SerializeField] PaintColor[] colors = { PaintColor.Red, PaintColor.Orange, PaintColor.Yellow };

        static readonly List<PaintZone> zones = new List<PaintZone>();

        public string ZoneName => zoneName;
        public IReadOnlyList<PaintColor> Colors => colors;

        void OnEnable()
        {
            zones.Add(this);
        }

        void OnDisable()
        {
            zones.Remove(this);
        }

        bool Contains(Vector3 position)
        {
            Vector3 local = position - transform.position;
            return Mathf.Abs(local.x) <= size.x * 0.5f && Mathf.Abs(local.z) <= size.y * 0.5f;
        }

        // 이 자리의 지역. 없으면 null
        public static PaintZone At(Vector3 position)
        {
            foreach (var zone in zones)
            {
                if (zone.Contains(position))
                    return zone;
            }
            return null;
        }

        // 이 자리에서 떨어뜨릴 물감 색 하나. 지역이 있으면 그 지역 색 중에서, 없으면 9색 중 랜덤.
        public static PaintColor PickColor(Vector3 position)
        {
            var zone = At(position);
            if (zone == null || zone.colors == null || zone.colors.Length == 0)
                return PaintColors.Random();
            return zone.colors[Random.Range(0, zone.colors.Length)];
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            zones.Clear();
        }

        // Scene 창에서 지역을 색깔 상자로 보여 준다 (게임 화면에는 안 나온다)
        void OnDrawGizmos()
        {
            Color tint = colors != null && colors.Length > 0 ? PaintColors.Colors[(int)colors[0]] : Color.white;
            Gizmos.color = new Color(tint.r, tint.g, tint.b, 0.12f);
            Gizmos.DrawCube(transform.position + Vector3.up * 0.2f, new Vector3(size.x, 0.4f, size.y));
            Gizmos.color = new Color(tint.r, tint.g, tint.b, 0.9f);
            Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, new Vector3(size.x, 4f, size.y));
#if UNITY_EDITOR
            string names = "";
            if (colors != null)
            {
                foreach (var color in colors)
                    names += PaintColors.KoreanNames[(int)color] + " ";
            }
            UnityEditor.Handles.Label(transform.position + Vector3.up * 4.5f, $"{zoneName}: {names}");
#endif
        }
    }
}
