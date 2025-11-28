using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TextEditorMK.Models;
using TextEditorMK.Repositories.Interfaces;
using TextEditorMK.Repositories.Implementations;

namespace TextEditorMK.Services
{
    public class EditorExtensionsService
    {
        private readonly RichTextBox _textBox;
        private readonly ISnippetRepository _snippetRepository;
        private readonly IBookmarkRepository _bookmarkRepository;
        
        // Інформація про поточний документ
        public string CurrentDocumentName { get; private set; } = "Untitled";
        public string CurrentDocumentPath { get; private set; } = string.Empty;

        public MacroService Macros { get; private set; }
        public SnippetService Snippets { get; private set; }
        public BookmarkService Bookmarks { get; private set; }
        
        // Поточна мова програмування
        public string CurrentLanguage { get; private set; } = "text";
        public bool IsExtensionsEnabled { get; set; } = true;

        // Події для інтеграції з MainForm
        public event EventHandler<ExtensionEventArgs> MacroStarted;
        public event EventHandler<ExtensionEventArgs> MacroStopped;
        public event EventHandler<ExtensionEventArgs> SnippetInserted;
        public event EventHandler<ExtensionEventArgs> BookmarkToggled;

        public EditorExtensionsService(RichTextBox textBox, ISnippetRepository snippetRepository = null, IBookmarkRepository bookmarkRepository = null)
        {
            _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
            _snippetRepository = snippetRepository;
            _bookmarkRepository = bookmarkRepository;
            InitializeServices();
        }

        private void InitializeServices()
        {
            try
            {
                Macros = new MacroService(_textBox);
                
                // Ініціалізуємо Snippets з репозиторієм або без
                Snippets = new SnippetService(_textBox, _snippetRepository);
                
                // Ініціалізуємо Bookmarks з репозиторієм або без
                Bookmarks = new BookmarkService(_textBox, _bookmarkRepository);

                System.Diagnostics.Debug.WriteLine($"? Editor extensions initialized with DB support: {_snippetRepository != null && _bookmarkRepository != null}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Failed to initialize editor extensions: {ex.Message}");
                throw new Exception("Failed to initialize editor extensions", ex);
            }
        }


        public void DetectLanguageFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                SetLanguage("text");
                return;
            }

            try
            {
                string extension = Path.GetExtension(filePath).ToLower();
                
                string language;
                switch (extension)
                {
                    case ".cs":
                        language = "csharp";
                        break;
                    case ".md":
                        language = "markdown";
                        break;
                    case ".html":
                    case ".htm":
                        language = "html";
                        break;
                    case ".js":
                        language = "javascript";
                        break;
                    case ".json":
                        language = "json";
                        break;
                    case ".xml":
                        language = "xml";
                        break;
                    case ".css":
                        language = "css";
                        break;
                    case ".sql":
                        language = "sql";
                        break;
                    default:
                        language = "text";
                        break;
                }

                SetLanguage(language);
                System.Diagnostics.Debug.WriteLine($"?? Language detected: {language} from {extension}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error detecting language: {ex.Message}");
                SetLanguage("text");
            }
        }

        public void SetLanguage(string language)
        {
            CurrentLanguage = language?.ToLower() ?? "text";
            System.Diagnostics.Debug.WriteLine($"?? Language set to: {CurrentLanguage}");
        }

        /// <summary>
        /// Оновлює інформацію про поточний документ
        /// </summary>
        public void SetCurrentDocument(string documentName, string documentPath = null)
        {
            CurrentDocumentName = string.IsNullOrEmpty(documentName) ? "Untitled" : documentName;
            CurrentDocumentPath = documentPath ?? string.Empty;
            System.Diagnostics.Debug.WriteLine($"?? Document set to: {CurrentDocumentName}");
        }


        public void TriggerSmartSnippet(string trigger)
        {
            if (!IsExtensionsEnabled || string.IsNullOrEmpty(trigger))
                return;

            try
            {
                var snippet = Snippets.FindSnippetByTrigger(trigger, CurrentLanguage);
                if (snippet != null)
                {
                    int cursorPosition = _textBox.SelectionStart;
                    Snippets.InsertSnippet(snippet, cursorPosition);
                    
                    OnSnippetInserted(snippet.Name, trigger);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error inserting snippet: {ex.Message}");
            }
        }


        public void StartMacroRecording(string macroName)
        {
            if (!IsExtensionsEnabled)
                return;

            try
            {
                Macros.StartRecording(macroName);
                OnMacroStarted(macroName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error starting macro recording: {ex.Message}");
            }
        }


        public void StopMacroRecording()
        {
            if (!IsExtensionsEnabled || !Macros.IsRecording)
                return;

            try
            {
                string macroName = Macros.CurrentMacroName;
                Macros.StopRecording();
                OnMacroStopped(macroName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error stopping macro recording: {ex.Message}");
            }
        }


        public void ToggleBookmarkAtCursor()
        {
            if (!IsExtensionsEnabled)
                return;

            try
            {
                int currentLine = GetCurrentLineNumber();
                
                // Передаємо інформацію про документ в BookmarkService
                if (Bookmarks is BookmarkService bookmarkService)
                {
                    bookmarkService.SetCurrentDocument(CurrentDocumentName, CurrentDocumentPath);
                }
                
                Bookmarks.ToggleBookmark(currentLine);
                OnBookmarkToggled(currentLine);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error toggling bookmark: {ex.Message}");
            }
        }

        public bool GoToNextBookmark()
        {
            if (!IsExtensionsEnabled)
                return false;

            try
            {
                int currentLine = GetCurrentLineNumber();
                int? nextLine = Bookmarks.GoToNextBookmark(currentLine);
                return nextLine.HasValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error navigating to next bookmark: {ex.Message}");
                return false;
            }
        }


        public bool GoToPreviousBookmark()
        {
            if (!IsExtensionsEnabled)
                return false;

            try
            {
                int currentLine = GetCurrentLineNumber();
                int? prevLine = Bookmarks.GoToPreviousBookmark(currentLine);
                return prevLine.HasValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error navigating to previous bookmark: {ex.Message}");
                return false;
            }
        }


        public EditorExtensionsStatistics GetStatistics()
        {
            return new EditorExtensionsStatistics
            {
                CurrentLanguage = CurrentLanguage,
                IsExtensionsEnabled = IsExtensionsEnabled,
                TotalMacros = Macros.GetAllMacros().Count,
                TotalSnippets = Snippets.GetSnippetsForLanguage(CurrentLanguage).Count,
                TotalBookmarks = Bookmarks.GetAllBookmarks().Count,
                IsRecordingMacro = Macros.IsRecording,
                CurrentMacroName = Macros.CurrentMacroName
            };
        }

        private int GetCurrentLineNumber()
        {
            try
            {
                return _textBox.GetLineFromCharIndex(_textBox.SelectionStart) + 1;
            }
            catch
            {
                return 1;
            }
        }


        protected virtual void OnMacroStarted(string macroName)
        {
            MacroStarted?.Invoke(this, new ExtensionEventArgs("MacroStarted", macroName));
        }

        protected virtual void OnMacroStopped(string macroName)
        {
            MacroStopped?.Invoke(this, new ExtensionEventArgs("MacroStopped", macroName));
        }

        protected virtual void OnSnippetInserted(string snippetName, string trigger)
        {
            SnippetInserted?.Invoke(this, new ExtensionEventArgs("SnippetInserted", $"{snippetName} ({trigger})"));
        }

        protected virtual void OnBookmarkToggled(int lineNumber)
        {
            BookmarkToggled?.Invoke(this, new ExtensionEventArgs("BookmarkToggled", $"Line {lineNumber}"));
        }
    }

    public class ExtensionEventArgs : EventArgs
    {
        public string ActionType { get; }
        public string Details { get; }
        public DateTime Timestamp { get; }

        public ExtensionEventArgs(string actionType, string details)
        {
            ActionType = actionType;
            Details = details;
            Timestamp = DateTime.Now;
        }
    }

    public class EditorExtensionsStatistics
    {
        public string CurrentLanguage { get; set; }
        public bool IsExtensionsEnabled { get; set; }
        public int TotalMacros { get; set; }
        public int TotalSnippets { get; set; }
        public int TotalBookmarks { get; set; }
        public bool IsRecordingMacro { get; set; }
        public string CurrentMacroName { get; set; }

        public override string ToString()
        {
            return $"Language: {CurrentLanguage}\n" +
                   $"Extensions Enabled: {IsExtensionsEnabled}\n" +
                   $"Macros: {TotalMacros}\n" +
                   $"Snippets: {TotalSnippets}\n" +
                   $"Bookmarks: {TotalBookmarks}\n" +
                   $"Recording: {IsRecordingMacro} {(IsRecordingMacro ? $"({CurrentMacroName})" : "")}";
        }
    }
}