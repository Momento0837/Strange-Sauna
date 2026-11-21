using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace SuspiciousSauna
{
    public sealed class SaunaRetailCustomerView : MonoBehaviour
    {
        public SaunaCharacterView character;
        public RectTransform bubble;
        public GameObject orderContents;
        public SaunaItemQuantityView[] items;
        public Image reaction;
        public Sprite music, dust;
        public Text waiting;
        Vector2 lastPosition;
        bool faceLeft;
        public void Present(RetailCustomer customer, float time, bool front)
        {
            var movement = customer.position - lastPosition;
            if (Mathf.Abs(movement.x) > .001f) faceLeft = movement.x < 0;
            bool walking = customer.state == RetailCustomerState.WalkingToQueue || customer.state == RetailCustomerState.OverflowApproach || customer.state == RetailCustomerState.Leaving;
            character.Present(customer.position, walking, faceLeft, time + customer.id, customer.happy ? .22f : .025f);
            lastPosition = customer.position;
            bool ordered = customer.state == RetailCustomerState.Ordered;
            bool emoting = customer.reactionSeconds > 0 && (customer.state == RetailCustomerState.Leaving || customer.state == RetailCustomerState.Grumbling);
            bool ready = front && customer.state == RetailCustomerState.Waiting;
            bubble.gameObject.SetActive(ordered || emoting || ready);
            orderContents.SetActive(ordered); reaction.gameObject.SetActive(emoting); waiting.gameObject.SetActive(ready);
            if (ordered)
            {
                var lines = RetailOrder.Counts(customer.order.requested).ToArray();
                bubble.sizeDelta = new Vector2(Mathf.Max(68, lines.Length * 52 + 16), 68);
                for (int i = 0; i < items.Length; i++)
                {
                    items[i].Present(i < lines.Length ? lines[i].item : null, i < lines.Length ? lines[i].quantity : 0);
                    if (i < lines.Length) SaunaUI.Box((RectTransform)items[i].transform, 8 + 52 * i, 6, 48, 54);
                }
            }
            else { bubble.sizeDelta = new Vector2(68, 68); reaction.sprite = customer.happy ? music : dust; }
        }
    }
}
