using System;
using UnityEngine;

namespace SuspiciousSauna
{
    public enum QuestKind { BySection, FoodOrder, Placeholder, TableRetailOrder }
    [Serializable]
    public class QuestEntry
    {
        [Tooltip("고정 식별자: 숫자 1~20 또는 알파벳 a~p")]
        public string id;
        public string title;
        [TextArea(3, 8)] public string description;
        [Min(5)] public float deliverySeconds = 100;
        [Tooltip("섹션 기준: 숫자는 음식 주문, 알파벳은 임시 특수 주문")]
        public QuestKind kind;
        public bool IsFoodOrder => kind == QuestKind.FoodOrder || (kind == QuestKind.BySection && int.TryParse(id, out _));
    }

    [CreateAssetMenu(menuName = "수상한 찜질방/퀘스트 목록", fileName = "QuestCatalog")]
    public class QuestCatalog : ScriptableObject
    {
        [Header("숫자 섹션 · 1~20")]
        public QuestEntry[] numbers = new QuestEntry[20];
        [Header("알파벳 섹션 · a~p")]
        public QuestEntry[] letters = new QuestEntry[16];

        public void FillDefaults()
        {
            numbers = new QuestEntry[20];
            letters = new QuestEntry[16];
            for (int i = 0; i < 20; i++) numbers[i] = Make((i + 1).ToString(), false);
            for (int i = 0; i < 16; i++) letters[i] = Make(((char)('a' + i)).ToString(), true);
        }

        static QuestEntry Make(string id, bool unusual) => new QuestEntry
        {
            id = id, title = id == "a" ? "테이블 잡화 주문" : unusual ? "수상한 부탁" : "손님의 부탁",
            kind = id == "a" ? QuestKind.TableRetailOrder : QuestKind.BySection,
            description = id == "a" ? "진열대에서 주문한 잡화를 가져와 이 테이블에 전달하세요." : "여기에 퀘스트 내용을 입력하세요.\n지금은 F를 다시 누르면 완료됩니다.",
            deliverySeconds = 100
        };

        public static float NumberChance(float rating) => rating > 3f ? .7f : rating > 2f ? .5f : .2f;

        public QuestEntry Draw(float rating)
        {
            bool number = UnityEngine.Random.value < NumberChance(rating);
            var section = number ? numbers : letters;
            if (section == null || section.Length == 0) return Make(number ? "1" : "a", !number);
            int index = UnityEngine.Random.Range(0, section.Length);
            return section[index] ?? Make(number ? (index + 1).ToString() : ((char)('a' + index)).ToString(), !number);
        }
    }
}
