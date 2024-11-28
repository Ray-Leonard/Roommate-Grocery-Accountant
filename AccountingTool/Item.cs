

namespace RoommateGroceryAccountant
{
    internal class Item
    {
        public string Name { get; set; }
        public float Price { get; set; }
        public int Share { get; set; }

        public string GetNameShared()
        {
            return $"1/{Share} {Name}";
        }

        public float GetPriceShared()
        {
            return Price / Share;
        }
    }
}
