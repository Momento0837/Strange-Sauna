using UnityEngine;

namespace SuspiciousSauna
{
    [CreateAssetMenu(menuName = "수상한 찜질방/게임 밸런스", fileName = "SaunaTuning")]
    public class SaunaTuning : ScriptableObject
    {
        [Header("이동과 손님")]
        [Min(1)] public float playerSpeed = 8.4f;
        [Min(1)] public float guestSpeed = 3.1f;
        [Min(3)] public float arrivalInterval = 18;
        [Min(10)] public float orderPatienceSeconds = 100;
        [Range(1, SaunaMap.TableCount)] public int maxOccupiedTables = 7;
        [Header("별점 · 5점 만점의 퍼센트")]
        [Range(1, 5)] public float initialRating = 5;
        [Range(0, 1)] public float failurePercent = .05f;
        [Range(0, 1)] public float successPercent = .03f;
        [Header("청소")]
        [Min(.1f)] public float cleaningHoldSeconds = 3;
        [Min(1)] public int cleaningPresses = 10;
        [Header("잡화 구매")]
        [Min(3)] public float retailArrivalInterval = 28;
        [Range(1, 3)] public int retailQueueCapacity = 3;
        [Range(.1f, 1)] public float retailTimeMultiplier = .5f;
        [Min(0)] public float retailOverflowPenaltyMultiplier = .5f;
        [Min(0)] public float retailItemPenaltyMultiplier = .3f;
    }
}
