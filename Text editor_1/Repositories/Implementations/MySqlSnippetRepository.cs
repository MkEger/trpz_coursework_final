using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using TextEditorMK.Models;
using TextEditorMK.Repositories.Interfaces;
using TextEditorMK.Data;
using TextEditorMK.Utils;

namespace TextEditorMK.Repositories.Implementations
{
    public class MySqlSnippetRepository : ISnippetRepository
    {
        private readonly MySqlDatabaseHelper _dbHelper;
        private readonly DatabaseCleaner _cleaner;

        public MySqlSnippetRepository()
        {
            System.Diagnostics.Debug.WriteLine("??? MySqlSnippetRepository constructor called");
            _dbHelper = new MySqlDatabaseHelper();
            _cleaner = new DatabaseCleaner();
            System.Diagnostics.Debug.WriteLine("??? About to call CreateTableIfNotExists");
            CreateTableIfNotExists();
            System.Diagnostics.Debug.WriteLine("??? MySqlSnippetRepository initialization complete");
        }

        private void CreateTableIfNotExists()
        {
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    System.Diagnostics.Debug.WriteLine("?? Checking if Snippets table exists...");
                    
                    // Перевіряємо, чи існує таблиця з правильною структурою
                    string checkTableQuery = @"
                        SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_SCHEMA = 'texteditor_db' 
                        AND TABLE_NAME = 'Snippets'";
                    
                    bool tableExists = false;
                    using (var checkCommand = new MySqlCommand(checkTableQuery, connection))
                    {
                        var result = checkCommand.ExecuteScalar();
                        tableExists = Convert.ToInt32(result) > 0;
                    }

                    if (tableExists)
                    {
                        // Перевіряємо, чи є правильна структура (поле TriggerWord)
                        try
                        {
                            string checkStructureQuery = @"
                                SELECT COUNT(*) 
                                FROM INFORMATION_SCHEMA.COLUMNS 
                                WHERE TABLE_SCHEMA = 'texteditor_db' 
                                AND TABLE_NAME = 'Snippets' 
                                AND COLUMN_NAME = 'TriggerWord'";
                            
                            using (var structureCommand = new MySqlCommand(checkStructureQuery, connection))
                            {
                                var structureResult = structureCommand.ExecuteScalar();
                                bool hasCorrectStructure = Convert.ToInt32(structureResult) > 0;
                                
                                if (hasCorrectStructure)
                                {
                                    System.Diagnostics.Debug.WriteLine("? Snippets table already exists with correct structure");
                                    return; // Таблиця існує і має правильну структуру
                                }
                                else
                                {
                                    // Таблиця існує, але має стару структуру - потрібно оновити
                                    System.Diagnostics.Debug.WriteLine("?? Snippets table has old structure, updating...");
                                    
                                    // Переіменовуємо стару таблицю для backup
                                    string backupQuery = "RENAME TABLE Snippets TO Snippets_backup";
                                    using (var backupCommand = new MySqlCommand(backupQuery, connection))
                                    {
                                        backupCommand.ExecuteNonQuery();
                                    }
                                    
                                    // Створюємо нову таблицю
                                    CreateSnippetsTable(connection);
                                    
                                    // Мігруємо дані зі старої таблиці (якщо потрібно)
                                    try
                                    {
                                        string migrateQuery = @"
                                            INSERT INTO Snippets (Name, TriggerWord, Language, Description, Code, Category, CreatedDate, LastUsedDate, UsageCount)
                                            SELECT Name, `Trigger`, Language, Description, Code, Category, CreatedDate, LastUsedDate, UsageCount
                                            FROM Snippets_backup";
                                        
                                        using (var migrateCommand = new MySqlCommand(migrateQuery, connection))
                                        {
                                            int migratedRows = migrateCommand.ExecuteNonQuery();
                                            System.Diagnostics.Debug.WriteLine($"?? Migrated {migratedRows} snippets from old table");
                                        }
                                        
                                        // Видаляємо backup таблицю
                                        string dropBackupQuery = "DROP TABLE Snippets_backup";
                                        using (var dropBackupCommand = new MySqlCommand(dropBackupQuery, connection))
                                        {
                                            dropBackupCommand.ExecuteNonQuery();
                                        }
                                    }
                                    catch (Exception migrateEx)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"?? Migration failed: {migrateEx.Message}");
                                        // Залишаємо backup таблицю на всякий випадок
                                    }
                                }
                            }
                        }
                        catch (Exception structureEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"?? Error checking table structure: {structureEx.Message}");
                            // Якщо не можемо перевірити структуру, просто залишаємо існуючу таблицю
                            return;
                        }
                    }
                    else
                    {
                        // Таблиця не існує - створюємо нову
                        System.Diagnostics.Debug.WriteLine("?? Creating new Snippets table...");
                        CreateSnippetsTable(connection);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error in CreateTableIfNotExists: {ex.Message}");
                // Не кидаємо виключення, щоб не порушувати роботу програми
            }
        }

        private void CreateSnippetsTable(MySqlConnection connection)
        {
            string createTableQuery = @"
                CREATE TABLE Snippets (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Name VARCHAR(100) NOT NULL,
                    TriggerWord VARCHAR(50) NOT NULL,
                    Language VARCHAR(50) NOT NULL,
                    Description TEXT,
                    Code TEXT NOT NULL,
                    Category VARCHAR(50),
                    CreatedDate DATETIME NOT NULL,
                    LastUsedDate DATETIME,
                    UsageCount INT DEFAULT 0,
                    INDEX idx_trigger_language (TriggerWord, Language),
                    INDEX idx_language (Language),
                    INDEX idx_category (Category)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";

            using (var command = new MySqlCommand(createTableQuery, connection))
            {
                command.ExecuteNonQuery();
                System.Diagnostics.Debug.WriteLine("? Snippets table created successfully with TriggerWord field");
            }
        }

        public void Add(CodeSnippet snippet)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"?? Adding snippet: {snippet.Name} with trigger: {snippet.Trigger}");
                
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO Snippets (Name, TriggerWord, Language, Description, Code, Category, CreatedDate, LastUsedDate, UsageCount) 
                        VALUES (@Name, @TriggerWord, @Language, @Description, @Code, @Category, @CreatedDate, @LastUsedDate, @UsageCount);
                        SELECT LAST_INSERT_ID();";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Name", snippet.Name ?? string.Empty);
                        command.Parameters.AddWithValue("@TriggerWord", snippet.Trigger ?? string.Empty);
                        command.Parameters.AddWithValue("@Language", snippet.Language ?? string.Empty);
                        command.Parameters.AddWithValue("@Description", snippet.Description ?? string.Empty);
                        command.Parameters.AddWithValue("@Code", snippet.Code ?? string.Empty);
                        command.Parameters.AddWithValue("@Category", snippet.Category ?? string.Empty);
                        command.Parameters.AddWithValue("@CreatedDate", snippet.CreatedDate);
                        command.Parameters.AddWithValue("@LastUsedDate", snippet.LastUsedDate);
                        command.Parameters.AddWithValue("@UsageCount", snippet.UsageCount);

                        System.Diagnostics.Debug.WriteLine($"?? Executing SQL query for snippet: {snippet.Name}");
                        var result = command.ExecuteScalar();
                        snippet.Id = Convert.ToInt32(result);
                        
                        System.Diagnostics.Debug.WriteLine($"? Snippet added to DB: {snippet.Name} with ID: {snippet.Id}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Snippet Add Error for '{snippet.Name}': {ex.Message}");
                throw; // Прокидаємо помилку для обробки у верхньому рівні
            }
        }

        public void Update(CodeSnippet snippet)
        {
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        UPDATE Snippets 
                        SET Name = @Name, TriggerWord = @TriggerWord, Language = @Language, 
                            Description = @Description, Code = @Code, Category = @Category,
                            LastUsedDate = @LastUsedDate, UsageCount = @UsageCount 
                        WHERE Id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", snippet.Id);
                        command.Parameters.AddWithValue("@Name", snippet.Name ?? string.Empty);
                        command.Parameters.AddWithValue("@TriggerWord", snippet.Trigger ?? string.Empty);
                        command.Parameters.AddWithValue("@Language", snippet.Language ?? string.Empty);
                        command.Parameters.AddWithValue("@Description", snippet.Description ?? string.Empty);
                        command.Parameters.AddWithValue("@Code", snippet.Code ?? string.Empty);
                        command.Parameters.AddWithValue("@Category", snippet.Category ?? string.Empty);
                        command.Parameters.AddWithValue("@LastUsedDate", snippet.LastUsedDate);
                        command.Parameters.AddWithValue("@UsageCount", snippet.UsageCount);

                        command.ExecuteNonQuery();
                        System.Diagnostics.Debug.WriteLine($"? Snippet updated in DB: {snippet.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Snippet Update Error: {ex.Message}");
                throw;
            }
        }

        public void Delete(int id)
        {
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = "DELETE FROM Snippets WHERE Id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        int rowsAffected = command.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                            System.Diagnostics.Debug.WriteLine($"? Snippet deleted from DB: ID {id}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Snippet Delete Error: {ex.Message}");
            }
        }

        public CodeSnippet GetById(int id)
        {
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM Snippets WHERE Id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return CreateSnippetFromReader(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Snippet GetById Error: {ex.Message}");
            }
            return null;
        }

        public List<CodeSnippet> GetAll()
        {
            var snippets = new List<CodeSnippet>();
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM Snippets ORDER BY Name";

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            snippets.Add(CreateSnippetFromReader(reader));
                        }
                    }
                }
                System.Diagnostics.Debug.WriteLine($"?? Loaded {snippets.Count} snippets from DB");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Snippet GetAll Error: {ex.Message}");
            }
            return snippets;
        }

        public List<CodeSnippet> GetByLanguage(string language)
        {
            var snippets = new List<CodeSnippet>();
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM Snippets WHERE Language = @Language ORDER BY Name";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Language", language ?? string.Empty);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                snippets.Add(CreateSnippetFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Snippet GetByLanguage Error: {ex.Message}");
            }
            return snippets;
        }

        public CodeSnippet GetByTrigger(string trigger, string language)
        {
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM Snippets WHERE TriggerWord = @TriggerWord AND Language = @Language LIMIT 1";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@TriggerWord", trigger ?? string.Empty);
                        command.Parameters.AddWithValue("@Language", language ?? string.Empty);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return CreateSnippetFromReader(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Snippet GetByTrigger Error: {ex.Message}");
            }
            return null;
        }

        public List<CodeSnippet> GetByCategory(string category)
        {
            var snippets = new List<CodeSnippet>();
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM Snippets WHERE Category = @Category ORDER BY Name";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Category", category ?? string.Empty);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                snippets.Add(CreateSnippetFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Snippet GetByCategory Error: {ex.Message}");
            }
            return snippets;
        }

        public List<CodeSnippet> GetMostUsed(int limit = 10)
        {
            var snippets = new List<CodeSnippet>();
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM Snippets ORDER BY UsageCount DESC, LastUsedDate DESC LIMIT @Limit";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Limit", limit);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                snippets.Add(CreateSnippetFromReader(reader));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Snippet GetMostUsed Error: {ex.Message}");
            }
            return snippets;
        }

        private CodeSnippet CreateSnippetFromReader(MySqlDataReader reader)
        {
            return new CodeSnippet
            {
                Id = reader.GetInt32("Id"),
                Name = reader["Name"] as string ?? string.Empty,
                Trigger = reader["TriggerWord"] as string ?? string.Empty, // Читаємо з TriggerWord
                Language = reader["Language"] as string ?? string.Empty,
                Description = reader["Description"] as string ?? string.Empty,
                Code = reader["Code"] as string ?? string.Empty,
                Category = reader["Category"] as string ?? string.Empty,
                CreatedDate = reader.GetDateTime("CreatedDate"),
                LastUsedDate = reader["LastUsedDate"] as DateTime?,
                UsageCount = reader.GetInt32("UsageCount")
            };
        }
    }
}