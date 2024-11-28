using System.Collections.Generic;
using System.Collections;

namespace RoommateGroceryAccountant
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Ask the user for the directory containing CSV files
            Console.WriteLine("Enter the directory containing CSV files:");
            string directory = Console.ReadLine();

            // Check if the directory exists
            if (!Directory.Exists(directory))
            {
                Console.WriteLine("Invalid directory.");
                return;
            }


            // Get all CSV files in the directory
            string[] csvFiles = Directory.GetFiles(directory, "*.csv");
            if (csvFiles.Length == 0)
            {
                Console.WriteLine("No CSV files found in the directory.");
                return;
            }

            // Allow the user to select a CSV file using the arrow keys
            int selectedIndex = 0;
            ConsoleKey key;
            do
            {
                Console.Clear();
                for (int i = 0; i < csvFiles.Length; i++)
                {
                    if (i == selectedIndex)
                        Console.WriteLine($"> {Path.GetFileName(csvFiles[i])}");
                    else
                        Console.WriteLine($"  {Path.GetFileName(csvFiles[i])}");
                }
                Console.WriteLine("Enter to select");

                key = Console.ReadKey().Key;
                if (key == ConsoleKey.UpArrow)
                    selectedIndex = (selectedIndex == 0) ? csvFiles.Length - 1 : selectedIndex - 1;
                else if (key == ConsoleKey.DownArrow)
                    selectedIndex = (selectedIndex == csvFiles.Length - 1) ? 0 : selectedIndex + 1;

            } while (key != ConsoleKey.Enter);

            // Get the selected CSV file
            string selectedFile = csvFiles[selectedIndex];
            List<Item> allItems = new List<Item>();
            Dictionary<string, List<Item>> personItems = new Dictionary<string, List<Item>>();

            bool isFirstLine = true;
            // Read each line in the CSV file and parse the data
            foreach (var line in File.ReadLines(selectedFile))
            {
                var columns = line.Split(',').Select(col => col.Trim()).ToArray();
                if (columns.Length < 3) continue;
                if(isFirstLine)
                {
                    isFirstLine = false;
                    continue;
                }

                string itemName = columns[0];
                float itemPrice = float.Parse(columns[1]);
                string people = columns[2];
                bool hasExtraTax = columns.Length > 3 && !string.IsNullOrEmpty(columns[3]);

                // Apply extra tax if needed
                if (hasExtraTax)
                {
                    itemPrice *= 1.13f;
                }

                // Create a new Item object
                var item = new Item
                {
                    Name = itemName,
                    Price = itemPrice,
                    Share = people.Length
                };

                // Add the item to the list of all items
                allItems.Add(item);

                // Add the item to each person's list who shares it
                foreach (char person in people)
                {
                    if (!personItems.ContainsKey(person.ToString()))
                    {
                        personItems[person.ToString()] = new List<Item>();
                    }
                    personItems[person.ToString()].Add(item);
                }
            }

            Console.Clear();
            // Generate a report for each person
            foreach (var entry in personItems)
            {
                string personName = entry.Key;
                List<Item> items = entry.Value;

                Console.WriteLine("============================================");
                Console.WriteLine($"{personName} pays for the following items:");
                float totalExpense = 0;

                // Print each item that the person is responsible for
                foreach (var item in items)
                {
                    string itemName = item.GetNameShared();
                    float itemPrice = item.GetPriceShared();
                    Console.WriteLine($"{itemName}, ${itemPrice:F2}");
                    totalExpense += itemPrice;
                }

                // Print the total expense for the person
                Console.WriteLine($"Total: ${totalExpense:F2}\n");
                Console.WriteLine("============================================");
            }

            // Calculate and print the grand total and total number of items
            float grandTotal = allItems.Sum(item => item.Price);
            int totalItems = allItems.Count;

            Console.WriteLine($"Total number of items: {totalItems}");
            Console.WriteLine($"Grand Total: ${grandTotal:F2}");
        }
    }
}