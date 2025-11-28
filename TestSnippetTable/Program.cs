using System;
using MySql.Data.MySqlClient;

namespace TestSnippetTable
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("?? Testing Snippets Table Creation...\n");

            string connectionString = "Server=localhost;Port=3306;Database=texteditor_db;Uid=root;Pwd=root;";
            
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    Console.WriteLine("? Connected to MySQL");

                    // Drop existing table
                    string dropQuery = "DROP TABLE IF EXISTS Snippets";
                    using (var dropCmd = new MySqlCommand(dropQuery, connection))
                    {
                        dropCmd.ExecuteNonQuery();
                        Console.WriteLine("??? Dropped existing Snippets table");
                    }

                    // Create new table with correct syntax
                    string createQuery = @"
                        CREATE TABLE Snippets (
                            Id INT AUTO_INCREMENT PRIMARY KEY,
                            Name VARCHAR(100) NOT NULL,
                            `Trigger` VARCHAR(50) NOT NULL,
                            Language VARCHAR(50) NOT NULL,
                            Description TEXT,
                            Code TEXT NOT NULL,
                            Category VARCHAR(50),
                            CreatedDate DATETIME NOT NULL,
                            LastUsedDate DATETIME,
                            UsageCount INT DEFAULT 0,
                            INDEX idx_trigger_language (`Trigger`, Language),
                            INDEX idx_language (Language),
                            INDEX idx_category (Category)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";

                    using (var createCmd = new MySqlCommand(createQuery, connection))
                    {
                        createCmd.ExecuteNonQuery();
                        Console.WriteLine("? Created Snippets table with correct structure");
                    }

                    // Test insert
                    string insertQuery = @"
                        INSERT INTO Snippets (Name, `Trigger`, Language, Description, Code, Category, CreatedDate, UsageCount)
                        VALUES (@Name, @Trigger, @Language, @Description, @Code, @Category, @CreatedDate, @UsageCount)";

                    using (var insertCmd = new MySqlCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.AddWithValue("@Name", "test-snippet");
                        insertCmd.Parameters.AddWithValue("@Trigger", "test");
                        insertCmd.Parameters.AddWithValue("@Language", "csharp");
                        insertCmd.Parameters.AddWithValue("@Description", "Test snippet");
                        insertCmd.Parameters.AddWithValue("@Code", "// Test code");
                        insertCmd.Parameters.AddWithValue("@Category", "Test");
                        insertCmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                        insertCmd.Parameters.AddWithValue("@UsageCount", 0);

                        int rowsAffected = insertCmd.ExecuteNonQuery();
                        Console.WriteLine($"? Inserted test snippet. Rows affected: {rowsAffected}");
                    }

                    // Test select
                    string selectQuery = "SELECT * FROM Snippets";
                    using (var selectCmd = new MySqlCommand(selectQuery, connection))
                    using (var reader = selectCmd.ExecuteReader())
                    {
                        Console.WriteLine("\n?? Snippets in table:");
                        while (reader.Read())
                        {
                            Console.WriteLine($"   - ID: {reader["Id"]}, Name: {reader["Name"]}, Trigger: {reader["Trigger"]}");
                        }
                    }
                }

                Console.WriteLine("\n?? All tests passed!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}