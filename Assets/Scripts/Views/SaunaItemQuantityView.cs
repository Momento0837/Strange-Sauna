using UnityEngine;
using UnityEngine.UI;
namespace SuspiciousSauna
{
    public sealed class SaunaItemQuantityView : MonoBehaviour
    {
        public Image icon;
        public Text quantity;
        public void Present(SaunaItemDefinition item, int count)
        {
            gameObject.SetActive(item != null);
            if (item == null) return;
            icon.sprite = item.icon; quantity.text = "×" + count;
        }
    }
}
