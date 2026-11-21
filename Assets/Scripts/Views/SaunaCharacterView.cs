using UnityEngine;
using UnityEngine.Rendering;
namespace SuspiciousSauna
{
    public sealed class SaunaCharacterView : MonoBehaviour
    {
        [Tooltip("Sprite 또는 Visual 자식을 교체하세요. 실행 중 덮어쓰지 않습니다.")] public SpriteRenderer body;
        public Transform visual;
        public SortingGroup sorting;
        public Animator animator;
        public string walkingParameter = "Walking";
        public GameObject sweatEffect;
        public SaunaHeldItemView rightHand, leftHand;
        public SaunaGuestStatusView guestStatus;
        float rejectUntil;
        public void Reject(float time) { rejectUntil = time + .65f; }
        public void ClearReaction() { rejectUntil = 0; }
        Vector3 baseLocal; bool initialized;
        public void Present(Vector2 position, bool walking, bool faceLeft, float time, float bounce = .04f, bool grumbling = false)
        {
            if (!initialized) { baseLocal = visual != null ? visual.localPosition : Vector3.zero; initialized = true; }
            transform.position = position;
            float reaction = Mathf.Clamp01((rejectUntil - time) / .65f);
            float hop = bounce > .05f ? Mathf.Abs(Mathf.Sin(time * 11)) : Mathf.Sin(time * 14);
            if (visual != null) visual.localPosition = baseLocal + Vector3.up * (walking ? hop * bounce : 0) + Vector3.right * (reaction > 0 ? Mathf.Sin((1 - reaction) * 32) * .13f * reaction : 0);
            if (visual != null && grumbling) visual.localPosition += Vector3.right * (Mathf.Sin(time * 11) * .035f);
            if (sweatEffect != null) sweatEffect.SetActive(reaction > 0);
            if (body != null) body.flipX = faceLeft;
            if (sorting != null) sorting.sortingOrder = 500 - Mathf.RoundToInt(position.y * 10);
            if (animator != null && !string.IsNullOrEmpty(walkingParameter))
                foreach (var p in animator.parameters) if (p.name == walkingParameter && p.type == AnimatorControllerParameterType.Bool) { animator.SetBool(walkingParameter, walking); break; }
        }
    }
}
