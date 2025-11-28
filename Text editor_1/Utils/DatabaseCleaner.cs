using System;
using MySql.Data.MySqlClient;
using TextEditorMK.Data;

namespace TextEditorMK.Utils
{
    public class DatabaseCleaner
    {
        private readonly MySqlDatabaseHelper _dbHelper;

        public DatabaseCleaner()
        {
            _dbHelper = new MySqlDatabaseHelper();
        }

        public void DropSnippetsTable()
        {
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string dropTableQuery = "DROP TABLE IF EXISTS Snippets";

                    using (var command = new MySqlCommand(dropTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                        System.Diagnostics.Debug.WriteLine("? Snippets table dropped");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error dropping Snippets table: {ex.Message}");
                throw;
            }
        }

        public void RecreateSnippetsTable()
        {
            try
            {
                // Спочатку видаляємо таблицю
                DropSnippetsTable();

                // Потім створюємо нову з правильним синтаксисом
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string createTableQuery = @"
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

                    using (var command = new MySqlCommand(createTableQuery, connection))
                    {
                        command.ExecuteNonQuery();
                        System.Diagnostics.Debug.WriteLine("? Snippets table recreated successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error recreating Snippets table: {ex.Message}");
                throw;
            }
        }
    }
}