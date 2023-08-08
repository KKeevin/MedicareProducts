using System;
using System.Collections.Generic;
using System.IO;
using System.Data.OleDb;

namespace ConsoleDB_MedicareProducts
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter path to Data.txt (ex:D:\\Data.txt):");
            string txtFilePath = Console.ReadLine();

            Console.WriteLine("Enter path to Access database file db.mdb (D:\\db.mdb):");
            string mdbFilePath = Console.ReadLine();

            List<DataRecord> txtRecords = ReadDataFromTxt(txtFilePath);
            List<DataRecord> mdbRecords = ReadDataFromMdb(mdbFilePath);

            List<DataRecord> matchedRecords = new List<DataRecord>();

            // Compare and process the records
            foreach (var txtRecord in txtRecords)
            {
                foreach (var mdbRecord in mdbRecords)
                {
                    if (txtRecord.Id == mdbRecord.Id)
                    {
                        // Process the matching records here
                        string currentTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                        string newItemData = txtRecord.Item.Replace("\\", "\\\\");

                        string newRecord = $"^T01{txtRecord.Id}^T02{currentTime}^T03{newItemData}_SS";
                        Console.WriteLine(newRecord); // Display the new record

                        matchedRecords.Add(new DataRecord(txtRecord.Id, newItemData));
                        break;
                    }
                }
            }

            UpdateDbMdb(mdbFilePath, matchedRecords);

            WriteMatchedRecordsToFile(txtFilePath, matchedRecords);

            Console.WriteLine("Data processing completed.");
        }

        static void WriteMatchedRecordsToFile(string filePath, List<DataRecord> matchedRecords)
        {
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                foreach (var record in matchedRecords)
                {
                    string newRecord = $"^T01{record.Id}^T02{DateTime.Now:yyyyMMddHHmmss}^T03{record.Item}_SS";
                    writer.WriteLine(newRecord);
                }
            }
        }

        static void UpdateDbMdb(string filePath, List<DataRecord> matchedRecords)
        {
            string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath + ";";
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                connection.Open();

                foreach (var record in matchedRecords)
                {
                    string updateQuery = $"UPDATE [order] SET writeTime = '{DateTime.Now:yyyyMMddHHmmss}' WHERE id = {record.Id}";
                    using (OleDbCommand updateCommand = new OleDbCommand(updateQuery, connection))
                    {
                        updateCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        static List<DataRecord> ReadDataFromTxt(string filePath)
        {
            List<DataRecord> records = new List<DataRecord>();

            // Read data from the text file and parse records
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                string[] parts = line.Split('^');
                if (parts.Length >= 4 && parts[1] == "T01")
                {
                    string idString = parts[2].Substring(1); // Remove leading 'S'
                    string item = parts[3].Substring(1); // Remove leading 'T'

                    if (!string.IsNullOrEmpty(idString) && int.TryParse(idString, out int id))
                    {
                        records.Add(new DataRecord(id, item));
                    }
                    else
                    {
                        Console.WriteLine($"Invalid data format in line: {line}");
                    }
                }
            }

            return records;
        }

        static List<DataRecord> ReadDataFromMdb(string filePath)
        {
            List<DataRecord> records = new List<DataRecord>();

            string connectionString = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" + filePath + ";";
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT * FROM [order]"; 
                using (OleDbCommand command = new OleDbCommand(query, connection))
                using (OleDbDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(reader.GetOrdinal("id")) && !reader.IsDBNull(reader.GetOrdinal("item")))
                        {
                            string idString = reader["id"].ToString();
                            int id = int.Parse(idString);
                            string item = reader["item"].ToString();
                            records.Add(new DataRecord(id, item));
                        }
                    }
                }
            }

            return records;
        }
    }

    class DataRecord
    {
        public int Id { get; }
        public string Item { get; }

        public DataRecord(int id, string item)
        {
            Id = id;
            Item = item;
        }
    }
}
