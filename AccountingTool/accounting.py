import csv
import os


class Item:
    def __init__(self, item_name, price, share_count, payer):
        self.item_name = item_name
        self.price = price
        self.share_count = share_count
        self.payer = payer

    def get_name_shared(self):
        return f"1/{self.share_count} * {self.item_name}"

    def get_price_shared(self):
        return self.price / self.share_count


class Person:
    def __init__(self, person_name):
        self.person_name = person_name
        self.items = []

    def __eq__(self, other):
        return self.person_name == other.person_name


def process_csv(input_file):
    all_items = []
    all_persons = []

    with open(input_file, "r", encoding="utf-8-sig") as f:
        reader = csv.reader(f)
        header = next(reader)

        for row in reader:
            if len(row) < 5:
                continue

            item_name = row[0].strip()
            try:
                item_price = float(row[1].strip())
            except ValueError:
                print(
                    f"Failed to parse price for item: {item_name}, going to the next item"
                )
                continue

            sharees = row[2].strip()
            has_extra_tax = row[3].strip() != ""
            if has_extra_tax:
                item_price *= 1.13

            payer_raw = row[4].strip()
            if len(payer_raw) > 1:
                print(
                    f"Payer should only be one person: {payer_raw}, going to the next item"
                )
                continue

            sharees_for_this_item = []
            for person_char in sharees:
                person_name = person_char
                existing_person = next(
                    (p for p in all_persons if p.person_name == person_name), None
                )
                if existing_person is None:
                    person = Person(person_name)
                    all_persons.append(person)
                    sharees_for_this_item.append(person)
                else:
                    sharees_for_this_item.append(existing_person)

            payer = None
            if payer_raw:
                existing_payer = next(
                    (p for p in all_persons if p.person_name == payer_raw), None
                )
                if existing_payer is None:
                    payer = Person(payer_raw)
                    all_persons.append(payer)
                else:
                    payer = existing_payer

            item = Item(item_name, item_price, len(sharees), payer)
            all_items.append(item)

            for person in sharees_for_this_item:
                person.items.append(item)

    return all_items, all_persons


def generate_results(input_file, all_items, all_persons):
    directory = os.path.dirname(input_file)
    base_name = os.path.splitext(os.path.basename(input_file))[0]
    result_file_name = os.path.join(directory, f"{base_name}_results.txt")

    num_persons = len(all_persons)
    person_index_ref = {person.person_name: i for i, person in enumerate(all_persons)}

    debt_matrix = [[0.0] * num_persons for _ in range(num_persons)]

    for person in all_persons:
        for item in person.items:
            if item.payer is None:
                continue
            person_index = person_index_ref[person.person_name]
            payer_index = person_index_ref[item.payer.person_name]
            item_price = item.get_price_shared()
            debt_matrix[person_index][payer_index] += item_price

    for i in range(num_persons):
        for j in range(num_persons):
            if i == j:
                debt_matrix[i][j] = 0
                continue

            person1_owes_person2 = debt_matrix[i][j]
            person2_owes_person1 = debt_matrix[j][i]
            net_amount = person1_owes_person2 - person2_owes_person1

            if net_amount > 0:
                debt_matrix[i][j] = net_amount
                debt_matrix[j][i] = 0
            elif net_amount < 0:
                debt_matrix[i][j] = 0
                debt_matrix[j][i] = -net_amount
            else:
                debt_matrix[i][j] = 0
                debt_matrix[j][i] = 0

    with open(result_file_name, "w", encoding="utf-8") as f:
        for person in all_persons:
            f.write("============================================\n")
            f.write(f"{person.person_name} pays for the following items:\n")
            total_expense = 0

            for item in person.items:
                item_name = item.get_name_shared()
                item_price = item.get_price_shared()
                f.write(f"{item_name}, ${item_price:.2f}\n")
                total_expense += item_price

            person_index = person_index_ref[person.person_name]
            for i in range(num_persons):
                if debt_matrix[person_index][i] > 0:
                    f.write(
                        f"{person.person_name} need to pay {all_persons[i].person_name} ${debt_matrix[person_index][i]:.2f}\n"
                    )

            f.write(f"Total Expense: ${total_expense:.2f}\n\n")
            f.write("============================================\n")

        grand_total = sum(item.price for item in all_items)
        total_items = len(all_items)

        f.write(f"Total number of items: {total_items}\n")
        f.write(f"Grand Total: ${grand_total:.2f}\n")

    print(f"Results written to {result_file_name}")
    return result_file_name


if __name__ == "__main__":
    import sys

    if len(sys.argv) > 1:
        input_file = sys.argv[1]
    else:
        input_file = input("Enter the path to the CSV file: ")

    if not os.path.exists(input_file):
        print("File does not exist.")
    else:
        all_items, all_persons = process_csv(input_file)
        result_file = generate_results(input_file, all_items, all_persons)
