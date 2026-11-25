using System;
using UnityEngine;
using UnityEngine.UI;
namespace SuspiciousSauna
{
    [Serializable] public sealed class SaunaRetailChoice
    {
        public SaunaItemDefinition item;
        public Button button;
    }
    public sealed class SaunaRetailPickerView : MonoBehaviour
    {
        public RectTransform card, content, bagRow;
        public ScrollRect scroll;
        public SaunaRetailChoice[] choices;
        public Button[] bagButtons;
        public Image[] bagIcons;
        public Text[] bagNumbers;
        public Button closeButton;
        public Text capacity;
        SaunaGame game;
        public void Initialize(SaunaGame controller)
        {
            game = controller;
            foreach (var choice in choices) { var item = choice.item; choice.button.onClick.AddListener(() => game.PickRetailItem(item)); }
            for (int i = 0; i < bagButtons.Length; i++) { int slot = i; bagButtons[i].onClick.AddListener(() => game.RemoveRetailItem(slot)); }
            closeButton.onClick.AddListener(game.CloseRetailPicking);
            RefreshBag();
        }
        void OnEnable() { if (scroll != null) scroll.verticalNormalizedPosition = 1; RefreshBag(); }
        void Update() => RefreshBag();
        void RefreshBag()
        {
            if (game == null) return;
            var inventory = game.Model.retailBag;
            bool canPick = game.HasShelfOrder;
            string destination = game.ActiveRetailOrder != null ? "포스기 잡화" : "테이블 잡화";
            capacity.text = destination + " " + inventory.Count + " / " + inventory.Capacity + "  ·  아래 잡화를 누르면 반납해요";
            foreach (var choice in choices) choice.button.interactable = canPick;
            for (int i = 0; i < bagButtons.Length; i++)
            {
                var item = inventory[i]; bagIcons[i].gameObject.SetActive(item != null);
                bagNumbers[i].text = item != null ? "−" : (i + 1).ToString();
                if (item != null) bagIcons[i].sprite = item.icon;
                bagButtons[i].interactable = item != null;
            }
        }
    }
}
