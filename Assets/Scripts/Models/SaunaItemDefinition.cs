using UnityEngine;
namespace SuspiciousSauna
{
    public enum ItemCategory { Food, Reading, Hygiene, Miscellaneous }
    [CreateAssetMenu(menuName = "수상한 찜질방/소지품", fileName = "Item")]
    public sealed class SaunaItemDefinition : ScriptableObject
    {
        public string id, displayName;
        public ItemCategory category;
        public Sprite icon;
        [Tooltip("선택 사항. 지정하면 손에 든 모습으로 이 프리팹을 사용합니다.")]
        public GameObject heldPrefab;
    }
}
