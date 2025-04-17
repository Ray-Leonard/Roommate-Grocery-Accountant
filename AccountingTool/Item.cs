

namespace RoommateGroceryAccountant
{
    internal class Item
    {
        public string ItemName { get; set; } = string.Empty;
        public float Price { get; set; }
        public int ShareCount { get; set; }
        public Person? Payer { get; set; }

        public string GetNameShared()
        {
            return $"1/{ShareCount} * {ItemName}";
        }

        public float GetPriceShared()
        {
            return Price / ShareCount;
        }
    }
}
