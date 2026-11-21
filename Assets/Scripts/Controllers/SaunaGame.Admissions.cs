using UnityEngine;
namespace SuspiciousSauna
{
    public sealed partial class SaunaGame
    {
        void TurnAwayArrivingParty(int forcedSize)
        {
            int count = forcedSize > 0 ? Mathf.Clamp(forcedSize, 1, 4) : Random.Range(1, 5);
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Vector2.right * ((i - (count - 1) * .5f) * .42f);
                Vector2 start = Map.entrance + offset;
                var guest = new Guest { id = ++nextId, table = -1, seat = i, position = start, state = GuestState.NoSeatApproach };
                guest.path = Navigation.Path(start, Map.entrance + Vector2.up * 2.4f + offset, guest.id);
                Guests.Add(guest);
            }
            // One party would have occupied one table. Charge once at admission,
            // never per actor or again when the departure animation finishes.
            Rating = Mathf.Clamp(Rating - 5 * tuning.failurePercent, 1, 5); Failed++;
            Sound.Play(2); Say("앉을 수 있는 깨끗한 테이블이 없어 손님들이 돌아가요.");
            if (Rating <= 1.0001f) SetState(ScreenState.GameOver);
        }
    }
}
