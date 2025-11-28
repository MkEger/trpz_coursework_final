using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using TextEditorMK.Models;

namespace TextEditorMK.Data
{
    public class MySqlDatabaseHelper
    {
        private readonly string _connectionString;

        public MySqlDatabaseHelper()
        {
            _connectionString = "Server=localhost;Port=3306;Database=texteditor_db;Uid=root;Pwd=root;";
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            try
            {
                // Створюємо базу даних якщо не існує
                TestAndCreateDatabase();

                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();

                    // Створюємо всі необхідні таблиці
                    CreateTablesIfNotExists(connection);
                 
                    // Заповнюємо таблиці початковими даними
                    SeedDataIfEmpty(connection);

                    MessageBox.Show("Database connected successfully! Your data is preserved.", "MySQL Ready",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database error: {ex.Message}\n\nFalling back to in-memory storage.",
                    "MySQL Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                throw; 
            }
        }

        private void TestAndCreateDatabase()
        {
            // Підключаємся без вказівки бази даних для створення
            string connectionWithoutDb = "Server=localhost;Port=3306;Uid=root;Pwd=root;";

            using (var connection = new MySqlConnection(connectionWithoutDb))
            {
                connection.Open();

                string createDbQuery = "CREATE DATABASE IF NOT EXISTS texteditor_db CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";
                using (var command = new MySqlCommand(createDbQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        private void CreateTablesIfNotExists(MySqlConnection connection)
        {
            // Створюємо таблицю для кодувань тексту
            string createTextEncodingsTable = @"
                CREATE TABLE IF NOT EXISTS TextEncodings (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    Name VARCHAR(100) NOT NULL,
                    CodePage VARCHAR(50) NOT NULL,
                    IsDefault BOOLEAN DEFAULT FALSE,
                    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE KEY unique_name (Name)
                ) ENGINE=InnoDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";

            using (var command = new MySqlCommand(createTextEncodingsTable, connection))
            {
                command.ExecuteNonQuery();
            }

            // Створюємо таблицю для налаштувань редактора
            string createEditorSettingsTable = @"
                CREATE TABLE IF NOT EXISTS EditorSettings (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    FontFamily VARCHAR(100) DEFAULT 'Consolas',
                    FontSize INT DEFAULT 12,
                    Theme VARCHAR(50) DEFAULT 'Light',
                    WordWrap BOOLEAN DEFAULT FALSE,
                    ShowLineNumbers BOOLEAN DEFAULT TRUE,
                    AutoSave BOOLEAN DEFAULT TRUE,
                    AutoSaveInterval INT DEFAULT 30,
                    ShowStatusBar BOOLEAN DEFAULT TRUE,
                    WindowWidth INT DEFAULT 1000,
                    WindowHeight INT DEFAULT 700,
                    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                ) ENGINE=InnoDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";

            using (var command = new MySqlCommand(createEditorSettingsTable, connection))
            {
                command.ExecuteNonQuery();
            }

            // Створюємо таблицю для останніх файлів (це головне!)
            string createRecentFilesTable = @"
                CREATE TABLE IF NOT EXISTS RecentFiles (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    FilePath VARCHAR(500) NOT NULL,
                    FileName VARCHAR(255) NOT NULL,
                    LastOpenedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    OpenCount INT DEFAULT 1,
                    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    UNIQUE KEY unique_filepath (FilePath),
                    INDEX idx_last_opened (LastOpenedAt DESC),
                    INDEX idx_filename (FileName)
                ) ENGINE=InnoDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";

            using (var command = new MySqlCommand(createRecentFilesTable, connection))
            {
                command.ExecuteNonQuery();
            }

            // Створюємо таблицю для документів (якщо потрібно)
            string createDocumentsTable = @"
                CREATE TABLE IF NOT EXISTS Documents (
                    Id INT AUTO_INCREMENT PRIMARY KEY,
                    FileName VARCHAR(255) NOT NULL,
                    FilePath VARCHAR(500),
                    Content LONGTEXT,
                    EncodingId INT,
                    IsSaved BOOLEAN DEFAULT FALSE,
                    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                    FOREIGN KEY (EncodingId) REFERENCES TextEncodings(Id) ON DELETE SET NULL,
                    INDEX idx_filepath (FilePath),
                    INDEX idx_filename (FileName)
                ) ENGINE=InnoDB CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci";

            using (var command = new MySqlCommand(createDocumentsTable, connection))
            {
                command.ExecuteNonQuery();
            }

            System.Diagnostics.Debug.WriteLine("✅ All database tables created successfully");
        }

        private void SeedDataIfEmpty(MySqlConnection connection)
        {
            // Перевірити чи TextEncodings порожня
            string checkEncodings = "SELECT COUNT(*) FROM TextEncodings";
            using (var cmd = new MySqlCommand(checkEncodings, connection))
            {
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 0)
                {
                    string insertEncodings = @"
                        INSERT INTO TextEncodings (Name, CodePage, IsDefault) VALUES 
                        ('UTF-8', 'utf-8', TRUE),
                        ('UTF-16 LE', 'utf-16', FALSE),
                        ('Windows-1251', 'windows-1251', FALSE)";

                    using (var insertCmd = new MySqlCommand(insertEncodings, connection))
                    {
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }

            // Перевірити чи EditorSettings порожня
            string checkSettings = "SELECT COUNT(*) FROM EditorSettings";
            using (var cmd = new MySqlCommand(checkSettings, connection))
            {
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count == 0)
                {
                    string insertSettings = @"
                        INSERT INTO EditorSettings (FontFamily, FontSize, Theme, WordWrap, ShowLineNumbers) 
                        VALUES ('Consolas', 12, 'Light', FALSE, TRUE)";

                    using (var insertCmd = new MySqlCommand(insertSettings, connection))
                    {
                        insertCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }

        public bool TestConnection()
        {
            try
            {
                using (var connection = new MySqlConnection(_connectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
