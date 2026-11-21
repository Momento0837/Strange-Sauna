using UnityEngine;
using UnityEngine.UI;
namespace SuspiciousSauna
{
    public sealed class SaunaCookingView : MonoBehaviour
    {
        public RectTransform card, content;
        public ScrollRect scroll;
        public SaunaFoodButtonView[] foods;
        public Button closeButton;
        public Text hands;
        SaunaGame game;
        public void Initialize(SaunaGame controller)
        {
            game = controller;
            foreach (var food in foods) food.Bind(game);
            closeButton.onClick.AddListener(game.CloseCooking);
        }
        void OnEnable() { if (scroll != null) scroll.verticalNormalizedPosition = 1; }
        void Update()
        {
            if (game == null) return;
            hands.text = "오른손  " + Name(game.Model.inventory.Right) + "     왼손  " + Name(game.Model.inventory.Left);
        }
        static string Name(SaunaItemDefinition item) => item == null ? "비어 있음" : item.displayName;
    }
}
