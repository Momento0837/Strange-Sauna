namespace SuspiciousSauna
{
    // Transient animation state belongs to the view, not to the order model.
    public sealed class Receipt
    {
        public SaunaTable table;
        public RetailOrder retailOrder;
        public float slide, y;
        public bool closing;
        public SaunaReceiptView view;
    }
}
