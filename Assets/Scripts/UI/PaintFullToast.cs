using System.Collections;
using LastBullet.Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastBullet.UI
{
    // 물감이 500을 넘어 버려졌을 때 "[물감 아이콘] 가득 참! -버린 양" 알림을 잠깐 보여 준다 (기획서 2.7).
    // 여러 색이 한꺼번에 넘치면 줄이 여러 개 생긴다.
    // 그림: Assets/UI/Toast_PaintFull.png ("가득 참!"), Icon_Paint_*.png
    // 붙일 곳: Canvas/PaintFullToast (자식 Row는 복사해서 쓰는 틀이라 꺼 둔다)
    public class PaintFullToast : MonoBehaviour
    {
        [Tooltip("Template for one row (kept disabled)")]
        [SerializeField] GameObject rowTemplate;

        [Tooltip("Icons of the 9 paint colors, from Red to Black")]
        [SerializeField] Sprite[] paintIcons = new Sprite[PaintColors.Count];

        [SerializeField] float showTime = 2.5f;

        void Start()
        {
            rowTemplate.SetActive(false);
            InventoryManager.Instance.PaintOverflow += Show;
        }

        void OnDestroy()
        {
            if (InventoryManager.Exists)
                InventoryManager.Instance.PaintOverflow -= Show;
        }

        void Show(PaintColor color, int discarded)
        {
            var row = Instantiate(rowTemplate, rowTemplate.transform.parent);
            row.transform.Find("Icon").GetComponent<Image>().sprite = paintIcons[(int)color];
            row.transform.Find("Amount").GetComponent<TMP_Text>().text = $"-{discarded}";
            row.SetActive(true);
            StartCoroutine(HideLater(row));
        }

        IEnumerator HideLater(GameObject row)
        {
            yield return new WaitForSeconds(showTime);
            Destroy(row);
        }
    }
}
