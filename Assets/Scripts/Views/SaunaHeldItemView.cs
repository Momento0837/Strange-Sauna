using UnityEngine;
namespace SuspiciousSauna
{
    public sealed class SaunaHeldItemView : MonoBehaviour
    {
        public SpriteRenderer icon;
        [Min(.1f)] public float iconSize = .68f;
        SaunaItemDefinition current;
        GameObject instance;
        public void Present(SaunaItemDefinition item)
        {
            if (current == item) return;
            current = item;
            if (instance != null) Destroy(instance);
            icon.gameObject.SetActive(item != null && item.heldPrefab == null);
            if (item == null) return;
            if (item.heldPrefab != null) instance = Instantiate(item.heldPrefab, transform);
            else
            {
                icon.sprite = item.icon;
                if (item.icon != null) icon.transform.localScale = Vector3.one * (iconSize / Mathf.Max(item.icon.bounds.size.x, item.icon.bounds.size.y));
            }
        }
    }
}
