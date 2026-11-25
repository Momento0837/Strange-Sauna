using System.Linq;
using UnityEngine;
namespace SuspiciousSauna
{
    [CreateAssetMenu(menuName = "수상한 찜질방/조리 메뉴", fileName = "SaunaItemCatalog")]
    public sealed class SaunaItemCatalog : ScriptableObject
    {
        public SaunaItemDefinition[] cookingMenu;
        public SaunaItemDefinition[] retailMenu;
        public SaunaItemDefinition RandomRetail() => retailMenu[Random.Range(0, retailMenu.Length)];
        public SaunaItemDefinition RandomFood()
        {
            var choices = cookingMenu.Where(i => i != null && i.category == ItemCategory.Food).ToArray();
            return choices.Length == 0 ? null : choices[Random.Range(0, choices.Length)];
        }
    }
}
