using UnityEngine;
using UnityEngine.UI;
namespace SuspiciousSauna
{
    [ExecuteAlways]
    public sealed class SaunaTableView : MonoBehaviour
    {
        [Min(0)] public int index;
        [Tooltip("테이블 루트를 옮기면 좌석과 의자도 함께 이동합니다.")]
        public Transform[] seats = new Transform[4];
        public GameObject dirt;
        public SpriteRenderer tableSprite;
        [Header("우측 상단 번호판")]
        public SpriteRenderer numberBadge;
        public Text numberLabel;
        public void RefreshNumber()
        {
            if (numberLabel == null) return;
            string value = (index + 1).ToString();
            if (numberLabel.text != value) numberLabel.text = value;
        }
        void OnEnable() => RefreshNumber();
        void LateUpdate() => RefreshNumber();
        void OnDrawGizmosSelected()
        { Gizmos.color = Color.yellow; foreach (var s in seats) if (s != null) Gizmos.DrawWireSphere(s.position, .3f); }
    }
}
