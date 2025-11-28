# Text Editor MK - Professional Course Project

## ?? Getting Started

### System Requirements:
- **Windows OS** (Windows 7 SP1 or higher)
- **.NET Framework 4.7.2** or newer version
- **Visual Studio 2017/2019/2022** 
- **MySQL 8.0+** (optional)

### Quick Start:

1. **Clone the repository:**
```bash
git clone https://github.com/MkEger/Text-editor_1
cd Text-editor_1
```

2. **Open the project:**
   - Open the `Text editor_1.sln` file in Visual Studio

3. **Build and run:**
   - Press **F5** to build and run with debugging
   - Or **Ctrl+F5** to run without debugging

## MySQL Database Configuration:

**Note:** MySQL database is optional for the "Recent Files" feature. The application works without MySQL configuration.

### To enable database functionality:

1. **Remove the `MySqlDatabaseHelper`** from the project
2. **Or configure the connection string in the constructor:**

In files `MySqlRecentFileRepository.cs` and `MySqlDocumentRepository.cs`, modify the connection string:

```csharp
// CHANGE THIS CONNECTION STRING:
private readonly string _connectionString = 
    "Server=localhost;Database=texteditor_db;Uid=your_username;Pwd=your_password;";

// Or in the GetConnection() method:
private MySqlConnection GetConnection()
{
    return new MySqlConnection("Server=localhost;Database=texteditor_db;Uid=root;Pwd=your_password;");
}
```

### Connection parameters:
- **Server**: MySQL server address (usually `localhost`)
- **Database**: Database name (create `texteditor_db`)
- **Uid**: MySQL username (usually `root`)
- **Pwd**: Password for MySQL user

## Features

### ?? Core Editor Features
- **Multi-format support**: Text, Markdown, HTML, CSS, JavaScript, C#
- **Syntax highlighting** for different programming languages
- **File management**: New, Open, Save, Save As operations
- **Find and Replace** with advanced options
- **Undo/Redo** functionality
- **Customizable themes** (Light, Dark, Custom)

### ?? Code Snippets
- **Smart code snippets** for different programming languages
- **Custom snippet creation** and management
- **Trigger-based insertion** (e.g., type `class` + Tab)
- **Variable expansion** (`$DATE`, `$TIME`, `$USER`)
- **Usage statistics** and most-used snippets tracking
- **Database persistence** for snippets

### ?? Bookmarks System
- **Line-based bookmarks** with descriptions
- **Quick navigation** between bookmarks
- **Bookmark persistence** across sessions
- **Visual bookmark indicators**
- **Document-specific bookmarks**

### ??? Recent Files Management
- **Recently opened files** tracking
- **Quick access** to recent documents
- **File metadata** storage (size, modified date)
- **MySQL database integration** for persistence

### ?? Advanced Features
- **Macro recording and playback**
- **Multiple text encodings** support
- **Customizable editor settings**
- **Extension system** for additional functionality
- **Database integration** for data persistence

## Project Structure

```
Text editor_1/
??? Models/              # Data models (CodeSnippet, Bookmark, Document, etc.)
??? Services/            # Business logic services
??? Repositories/        # Data access layer
?   ??? Interfaces/      # Repository interfaces
?   ??? Implementations/ # MySQL implementations
??? Utils/               # Utility classes
??? Forms/               # Windows Forms UI
??? Resources/           # Application resources
```

## Technologies Used

- **Framework**: .NET Framework 4.7.2
- **UI**: Windows Forms
- **Database**: MySQL 8.0+
- **ORM**: Custom repository pattern
- **Language**: C# 7.3
- **IDE**: Visual Studio 2017/2019/2022

## Contributing

This is a coursework project for educational purposes. Feel free to fork and modify for learning.

## License

This project is for educational purposes only.