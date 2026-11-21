using UnityEngine;
namespace SuspiciousSauna
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class SaunaObstacle : MonoBehaviour
    {
        public Rect WorldRect()
        {
            var b = GetComponent<BoxCollider2D>(); var center = transform.TransformPoint(b.offset);
            var size = Vector2.Scale(b.size, transform.lossyScale);
            return new Rect((Vector2)center - size * .5f, size);
        }
    }
}
