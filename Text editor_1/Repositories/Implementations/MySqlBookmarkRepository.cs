using System;
using System.Collections.Generic;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using TextEditorMK.Models;
using TextEditorMK.Repositories.Interfaces;
using TextEditorMK.Data;

namespace TextEditorMK.Repositories.Implementations
{
    public class MySqlBookmarkRepository : IBookmarkRepository
    {
        private readonly MySqlDatabaseHelper _dbHelper;

        public MySqlBookmarkRepository()
        {
            _dbHelper = new MySqlDatabaseHelper();
            CreateTableIfNotExists();
        }

        private void CreateTableIfNotExists()
        {
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    
                    // —початку перев≥р€Їмо, чи ≥снують нов≥ колонки
                    bool hasDocumentColumns = false;
                    try
                    {
                        string checkColumnsQuery = @"
                            SELECT COUNT(*) 
                            FROM INFORMATION_SCHEMA.COLUMNS 
                            WHERE TABLE_SCHEMA = 'texteditor_db' 
                            AND TABLE_NAME = 'Bookmarks' 
                            AND COLUMN_NAME IN ('DocumentName', 'DocumentPath')";
                        
                        using (var checkCommand = new MySqlCommand(checkColumnsQuery, connection))
                        {
                            var result = checkCommand.ExecuteScalar();
                            hasDocumentColumns = Convert.ToInt32(result) >= 2;
                        }
                    }
                    catch { }
                    
                    if (!hasDocumentColumns)
                    {
                        // —творюЇмо або оновлюЇмо таблицю з новими колонками
                        string createTableQuery = @"
                            CREATE TABLE IF NOT EXISTS Bookmarks (
                                Id INT AUTO_INCREMENT PRIMARY KEY,
                                LineNumber INT NOT NULL,
                                Description TEXT,
                                LinePreview TEXT,
                                DocumentName VARCHAR(255),
                                DocumentPath TEXT,
                                CreatedDate DATETIME NOT NULL,
                                LastAccessedDate DATETIME,
                                AccessCount INT DEFAULT 0,
                                IsActive BOOLEAN DEFAULT TRUE,
                                INDEX idx_line_number (LineNumber),
                                INDEX idx_is_active (IsActive),
                                INDEX idx_document_name (DocumentName)
                            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";

                        using (var command = new MySqlCommand(createTableQuery, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                        
                        // якщо таблиц€ ≥снувала, додаЇмо нов≥ колонки
                        try
                        {
                            string alterQuery1 = "ALTER TABLE Bookmarks ADD COLUMN DocumentName VARCHAR(255)";
                            string alterQuery2 = "ALTER TABLE Bookmarks ADD COLUMN DocumentPath TEXT";
                            
                            using (var alterCommand1 = new MySqlCommand(alterQuery1, connection))
                            {
                                alterCommand1.ExecuteNonQuery();
                            }
                            using (var alterCommand2 = new MySqlCommand(alterQuery2, connection))
                            {
                                alterCommand2.ExecuteNonQuery();
                            }
                            
                            System.Diagnostics.Debug.WriteLine("? Added DocumentName and DocumentPath columns to Bookmarks table");
                        }
                        catch
                        {
                            //  олонки вже ≥снують або ≥нша помилка
                        }
                    }
                    
                    System.Diagnostics.Debug.WriteLine("? Bookmarks table created/verified with document fields");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error creating Bookmarks table: {ex.Message}");
            }
        }

        public void Add(Bookmark bookmark)
        {
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        INSERT INTO Bookmarks (LineNumber, Description, LinePreview, DocumentName, DocumentPath, CreatedDate, LastAccessedDate, AccessCount, IsActive) 
                        VALUES (@LineNumber, @Description, @LinePreview, @DocumentName, @DocumentPath, @CreatedDate, @LastAccessedDate, @AccessCount, @IsActive);
                        SELECT LAST_INSERT_ID();";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@LineNumber", bookmark.LineNumber);
                        command.Parameters.AddWithValue("@Description", bookmark.Description ?? string.Empty);
                        command.Parameters.AddWithValue("@LinePreview", bookmark.LinePreview ?? string.Empty);
                        command.Parameters.AddWithValue("@DocumentName", bookmark.DocumentName ?? string.Empty);
                        command.Parameters.AddWithValue("@DocumentPath", bookmark.DocumentPath ?? string.Empty);
                        command.Parameters.AddWithValue("@CreatedDate", bookmark.CreatedDate);
                        command.Parameters.AddWithValue("@LastAccessedDate", bookmark.LastAccessedDate);
                        command.Parameters.AddWithValue("@AccessCount", bookmark.AccessCount);
                        command.Parameters.AddWithValue("@IsActive", bookmark.IsActive);

                        var result = command.ExecuteScalar();
                        bookmark.Id = Convert.ToInt32(result);
                        
                        System.Diagnostics.Debug.WriteLine($"? Bookmark added to DB: Line {bookmark.LineNumber} in {bookmark.DocumentName}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Bookmark Add Error: {ex.Message}");
            }
        }

        public void Update(Bookmark bookmark)
        {
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = @"
                        UPDATE Bookmarks 
                        SET LineNumber = @LineNumber, Description = @Description, LinePreview = @LinePreview,
                            DocumentName = @DocumentName, DocumentPath = @DocumentPath,
                            LastAccessedDate = @LastAccessedDate, AccessCount = @AccessCount, IsActive = @IsActive 
                        WHERE Id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", bookmark.Id);
                        command.Parameters.AddWithValue("@LineNumber", bookmark.LineNumber);
                        command.Parameters.AddWithValue("@Description", bookmark.Description ?? string.Empty);
                        command.Parameters.AddWithValue("@LinePreview", bookmark.LinePreview ?? string.Empty);
                        command.Parameters.AddWithValue("@DocumentName", bookmark.DocumentName ?? string.Empty);
                        command.Parameters.AddWithValue("@DocumentPath", bookmark.DocumentPath ?? string.Empty);
                        command.Parameters.AddWithValue("@LastAccessedDate", bookmark.LastAccessedDate);
                        command.Parameters.AddWithValue("@AccessCount", bookmark.AccessCount);
                        command.Parameters.AddWithValue("@IsActive", bookmark.IsActive);

                        command.ExecuteNonQuery();
                        System.Diagnostics.Debug.WriteLine($"? Bookmark updated in DB: Line {bookmark.LineNumber} in {bookmark.DocumentName}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Bookmark Update Error: {ex.Message}");
            }
        }

        public void Delete(int id)
        {
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = "DELETE FROM Bookmarks WHERE Id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        int rowsAffected = command.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                            System.Diagnostics.Debug.WriteLine($"? Bookmark deleted from DB: ID {id}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Bookmark Delete Error: {ex.Message}");
            }
        }

        public Bookmark GetById(int id)
        {
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM Bookmarks WHERE Id = @Id";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return CreateBookmarkFromReader(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Bookmark GetById Error: {ex.Message}");
            }
            return null;
        }

        public List<Bookmark> GetAll()
        {
            var bookmarks = new List<Bookmark>();
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM Bookmarks ORDER BY LineNumber";

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bookmarks.Add(CreateBookmarkFromReader(reader));
                        }
                    }
                }
                System.Diagnostics.Debug.WriteLine($"?? Loaded {bookmarks.Count} bookmarks from DB");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Bookmark GetAll Error: {ex.Message}");
            }
            return bookmarks;
        }

        public List<Bookmark> GetActiveBookmarks()
        {
            var bookmarks = new List<Bookmark>();
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM Bookmarks WHERE IsActive = TRUE ORDER BY LineNumber";

                    using (var command = new MySqlCommand(query, connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            bookmarks.Add(CreateBookmarkFromReader(reader));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Bookmark GetActiveBookmarks Error: {ex.Message}");
            }
            return bookmarks;
        }

        public Bookmark GetByLineNumber(int lineNumber)
        {
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT * FROM Bookmarks WHERE LineNumber = @LineNumber AND IsActive = TRUE LIMIT 1";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@LineNumber", lineNumber);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                return CreateBookmarkFromReader(reader);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Bookmark GetByLineNumber Error: {ex.Message}");
            }
            return null;
        }

        public void DeleteByLineNumber(int lineNumber)
        {
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = "DELETE FROM Bookmarks WHERE LineNumber = @LineNumber";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@LineNumber", lineNumber);
                        int rowsAffected = command.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                            System.Diagnostics.Debug.WriteLine($"? Bookmark deleted from DB: Line {lineNumber}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Bookmark DeleteByLineNumber Error: {ex.Message}");
            }
        }

        public void ClearAll()
        {
            try
            {
                using (var connection = _dbHelper.GetConnection())
                {
                    connection.Open();
                    string query = "DELETE FROM Bookmarks";

                    using (var command = new MySqlCommand(query, connection))
                    {
                        int rowsAffected = command.ExecuteNonQuery();
                        System.Diagnostics.Debug.WriteLine($"? Cleared {rowsAffected} bookmarks from DB");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? MySQL Bookmark ClearAll Error: {ex.Message}");
            }
        }

        private Bookmark CreateBookmarkFromReader(MySqlDataReader reader)
        {
            return new Bookmark
            {
                Id = reader.GetInt32("Id"),
                LineNumber = reader.GetInt32("LineNumber"),
                Description = reader["Description"] as string ?? string.Empty,
                LinePreview = reader["LinePreview"] as string ?? string.Empty,
                DocumentName = reader["DocumentName"] as string ?? string.Empty,
                DocumentPath = reader["DocumentPath"] as string ?? string.Empty,
                CreatedDate = reader.GetDateTime("CreatedDate"),
                LastAccessedDate = reader["LastAccessedDate"] as DateTime?,
                AccessCount = reader.GetInt32("AccessCount"),
                IsActive = reader.GetBoolean("IsActive")
            };
        }
    }
}