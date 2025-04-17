

namespace RoommateGroceryAccountant
{
    internal class Person
    {
        public string PersonName { get; set; } = string.Empty;
        public List<Item> Items {get; set;} = new List<Item>();

        public Person(string personName)
        {
            PersonName = personName;
        }

        public bool Equals(Person other)
        {
            return PersonName == other.PersonName;
        }
    }
}
