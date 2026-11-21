using UnityEngine;
using UnityEngine.UI;
namespace SuspiciousSauna
{
    public sealed class SaunaFoodButtonView : MonoBehaviour
    {
        public SaunaItemDefinition item;
        public Button button;
        public Image icon;
        public Text label;
        public void Bind(SaunaGame game)
        {
            label.text = item.displayName; icon.sprite = item.icon;
            button.onClick.AddListener(() => game.Cook(item));
        }
    }
}
