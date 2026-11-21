using UnityEngine;
namespace SuspiciousSauna
{
    // Authored anchors define the customer side and the staff-only interaction side.
    public sealed class SaunaRetailLayout : MonoBehaviour
    {
        public Transform staffPoint, shelfInteraction, overflowLookPoint;
        public Transform[] queueSpots;
        public Transform exitSidePoint;
        public float staffReach = 1.15f, shelfReach = 1.5f;
        public bool AtStaffSide(Vector2 player) => staffPoint != null && Vector2.Distance(player, staffPoint.position) <= staffReach;
        public bool AtShelf(Vector2 player) => shelfInteraction != null && Vector2.Distance(player, shelfInteraction.position) <= shelfReach;
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            if (staffPoint != null) Gizmos.DrawWireSphere(staffPoint.position, staffReach);
            if (shelfInteraction != null) Gizmos.DrawWireSphere(shelfInteraction.position, shelfReach);
            if (queueSpots != null) foreach (var spot in queueSpots) if (spot != null) Gizmos.DrawWireSphere(spot.position, .45f);
        }
    }
}
