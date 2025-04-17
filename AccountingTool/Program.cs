using System.Collections.Generic;
using System.Collections;

namespace RoommateGroceryAccountant
{
    internal class Program
    {
        private static string directory = string.Empty;

        private static string GetSelectedFile()
        {
            // Ask the user for the directory containing CSV files
            Console.WriteLine("Enter the directory containing CSV files:");
            directory = Console.ReadLine();

            // Check if the directory exists
            if (!Directory.Exists(directory))
            {
                Console.WriteLine("Invalid directory.");
                throw new Exception("Invalid directory.");
            }


            // Get all CSV files in the directory
            string[] csvFiles = Directory.GetFiles(directory, "*.csv");
            if (csvFiles.Length == 0)
            {
                Console.WriteLine("No CSV files found in the directory.");
                throw new Exception("No CSV files found in the directory.");
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

            return csvFiles[selectedIndex];
        }


        static void Main(string[] args)
        {
            // Get the selected CSV file
            string selectedFile = string.Empty;
            try
            {
                selectedFile = GetSelectedFile();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            // safety check.
            if(string.IsNullOrEmpty(selectedFile))
            {
                Console.WriteLine("No file selected.");
                return;
            }


            List<Item> allItems = new List<Item>();
            List<Person> allPersons = new List<Person>();
            // Dictionary<string, List<Item>> personItems = new Dictionary<string, List<Item>>();

            bool isFirstLine = true;
            // Read each line in the CSV file and parse the data
            foreach (var line in File.ReadLines(selectedFile))
            {
                // skip the first line as it contains the column names
                if(isFirstLine)
                {
                    isFirstLine = false;
                    continue;
                }


                // ========== parse the data ==========
                var columns = line.Split(',').Select(col => col.Trim()).ToArray();
                if (columns.Length < 5) continue;
                // item name, item price, people, extra tax, payer
                string itemName = columns[0].Trim();
                float itemPrice;
                try
                {
                    itemPrice = float.Parse(columns[1].Trim());
                }
                catch (Exception)
                {
                    Console.WriteLine($"Failed to parse price for item: {itemName}, going to the next item");
                    continue;
                }
                string sharees = columns[2].Trim();
                bool hasExtraTax = !string.IsNullOrEmpty(columns[3].Trim());
                // Apply extra tax to data if hasExtraTax is true
                if (hasExtraTax)
                {
                    itemPrice *= 1.13f;
                }
                string payerRaw = columns[4].Trim(); // the payer should only be one person
                if(payerRaw.Length > 1)
                {
                    Console.WriteLine($"Payer should only be one person: {payerRaw}, going to the next item");
                    continue;
                }

                // ========== person creation ============
                // create sharees list for this item.
                List<Person> shareesForThisItem = new List<Person>();
                // go through the sharees string.
                foreach(char personChar in sharees)
                {
                    // check if the person object already exists, if not, add it to the list.   
                    if(!allPersons.Any(p => p.PersonName == personChar.ToString()))
                    {
                        Person person = new Person(personChar.ToString());
                        allPersons.Add(person);
                        shareesForThisItem.Add(person);
                    }
                    else
                    {
                        // if the person object already exists, find the existing person and to this Item's sharees list.
                        shareesForThisItem.Add(allPersons.First(p => p.PersonName == personChar.ToString()));
                    }
                }

                // check for payer string, and create the payer person object.
                Person? payer = null;
                if(!string.IsNullOrEmpty(payerRaw))
                {
                    // string is not null, the item has a payer, so create a payer person object.
                    // check if the payer person object already exists, if not, add it to the list.
                    if(!allPersons.Any(p => p.PersonName == payerRaw))
                    {
                        payer = new Person(payerRaw);
                        allPersons.Add(payer);
                    }
                    else
                    {
                        // if the person object already exists, find the existing person and to this Item's sharees list.
                        payer = allPersons.First(p => p.PersonName == payerRaw);
                    }
                }

                // ========== item creation ============
                var item = new Item
                {
                    ItemName = itemName,
                    Price = itemPrice,
                    ShareCount = sharees.Length,
                    Payer = payer
                };
                // Add the item to the list of all items
                allItems.Add(item);

                // Add the item to each person's list who shares it
                foreach(Person person in shareesForThisItem)
                {
                    person.Items.Add(item);
                }
            }


            //Console.Clear();
            // Generate a report for each person
            string resultFileName = Path.Combine(directory, Path.GetFileNameWithoutExtension(selectedFile) + "_results.txt");
            Console.WriteLine(resultFileName);
            using (FileStream stream = new FileStream(resultFileName, FileMode.Create))
            using (StreamWriter writer = new StreamWriter(stream))
            {
                // calculate debts for each person
                float[,] debtMatrix = new float[allPersons.Count, allPersons.Count];
                // Create index reference dictionary for persons, so we can use it for matrix
                Dictionary<Person, int> personIndexRef = new Dictionary<Person, int>();
                for(int i = 0; i < allPersons.Count; i++)
                {
                    personIndexRef.Add(allPersons[i], i);
                }
                // create debt matrix, person x person, row is sharee who owes money to payer, column is payer

                // fill the debt matrix
                foreach(Person person in allPersons)
                {
                    foreach(Item item in person.Items)
                    {
                        if(item.Payer == null) continue;

                        int personIndex = personIndexRef[person];
                        int payerIndex = personIndexRef[item.Payer];
                        float itemPrice = item.GetPriceShared();
                        debtMatrix[personIndex, payerIndex] += itemPrice;
                    }
                }

                // simplify the debt matrix
                for(int i = 0; i < allPersons.Count; i++)
                {
                    for(int j = 0; j < allPersons.Count; j++)
                    {
                        // eliminate diagonal elements
                        if(i == j)
                        {
                            debtMatrix[i, j] = 0;
                            continue;
                        }

                        float person1OwesPerson2 = debtMatrix[i, j];
                        float person2OwesPerson1 = debtMatrix[j, i];
                        float netAmount = person1OwesPerson2 - person2OwesPerson1;

                        if(netAmount > 0)
                        {
                            debtMatrix[i, j] = netAmount;
                            debtMatrix[j, i] = 0;
                        }
                        else if(netAmount < 0)
                        {
                            debtMatrix[i, j] = 0;
                            debtMatrix[j, i] = -netAmount;
                        }
                        else
                        {
                            debtMatrix[i, j] = 0;
                            debtMatrix[j, i] = 0;
                        }

                    }
                }


                // calculate the total expense for each person and print result including debt payout info.
                foreach(Person person in allPersons)
                {
                    writer.WriteLine("============================================");
                    writer.WriteLine($"{person.PersonName} pays for the following items:");
                    float totalExpense = 0;

                    // Print each item that the person is responsible for
                    foreach (var item in person.Items)
                    {
                        string itemName = item.GetNameShared();
                        float itemPrice = item.GetPriceShared();
                        writer.WriteLine($"{itemName}, ${itemPrice:F2}");
                        totalExpense += itemPrice;
                    }

                    // print debt info
                    int personIndex = personIndexRef[person];
                    for(int i = 0; i < allPersons.Count; i++)
                    {
                        if(debtMatrix[personIndex, i] > 0)
                        {
                            writer.WriteLine($"{person.PersonName} need to pay {allPersons[i].PersonName} ${debtMatrix[personIndex, i]:F2}");
                        }
                    }

                    // Print the total expense for the person
                    writer.WriteLine($"Total Expense: ${totalExpense:F2}\n");
                    writer.WriteLine("============================================");
                }

                // Calculate and print the grand total and total number of items
                float grandTotal = allItems.Sum(item => item.Price);
                int totalItems = allItems.Count;

                writer.WriteLine($"Total number of items: {totalItems}");
                writer.WriteLine($"Grand Total: ${grandTotal:F2}");
            }
            Console.WriteLine($"Results written to {resultFileName}");
        }
    }
}