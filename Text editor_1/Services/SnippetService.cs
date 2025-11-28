using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TextEditorMK.Models;
using TextEditorMK.Repositories.Interfaces;

namespace TextEditorMK.Services
{

    public class SnippetService
    {
        private readonly RichTextBox _textBox;
        private readonly ISnippetRepository _snippetRepository;
        private readonly List<CodeSnippet> _cachedSnippets;

        public SnippetService(RichTextBox textBox, ISnippetRepository snippetRepository = null)
        {
            _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
            _snippetRepository = snippetRepository;
            _cachedSnippets = new List<CodeSnippet>();
            
            InitializeSnippets();
        }

        public List<CodeSnippet> GetSnippetsForLanguage(string language)
        {
            try
            {
                if (_snippetRepository != null)
                {
                    if (string.IsNullOrEmpty(language))
                        return _snippetRepository.GetAll();
                    else
                        return _snippetRepository.GetByLanguage(language);
                }
                else
                {
                    // Fallback to cached snippets
                    if (string.IsNullOrEmpty(language))
                        return new List<CodeSnippet>(_cachedSnippets);

                    return _cachedSnippets.Where(s => s.Language.Equals(language, StringComparison.OrdinalIgnoreCase))
                                         .OrderBy(s => s.Name)
                                         .ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error getting snippets: {ex.Message}");
                return new List<CodeSnippet>();
            }
        }

        public void InsertSnippet(CodeSnippet snippet, int cursorPosition)
        {
            if (snippet == null)
                throw new ArgumentNullException(nameof(snippet));

            try
            {
                string expandedCode = ExpandSnippet(snippet);
                

                var currentFont = _textBox.Font;
                var currentColor = _textBox.ForeColor;
                
                _textBox.SelectionStart = cursorPosition;
                _textBox.SelectionLength = 0;

                _textBox.SelectionFont = currentFont;
                _textBox.SelectionColor = currentColor;
                _textBox.SelectionBackColor = _textBox.BackColor;
                
                _textBox.SelectedText = expandedCode;
                

                _textBox.SelectionStart = cursorPosition + expandedCode.Length;
                _textBox.SelectionLength = 0;

                // Update usage statistics
                snippet.UpdateUsage();
                
                // Save to database if repository available
                if (_snippetRepository != null && snippet.Id > 0)
                {
                    _snippetRepository.Update(snippet);
                    System.Diagnostics.Debug.WriteLine($"?? Updated snippet usage in DB: {snippet.Name}");
                }

                System.Diagnostics.Debug.WriteLine($"? Inserted snippet: {snippet.Name}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inserting snippet '{snippet.Name}': {ex.Message}", ex);
            }
        }

        public void AddSnippet(CodeSnippet snippet)
        {
            if (snippet == null)
                throw new ArgumentNullException(nameof(snippet));

            try
            {
                snippet.CreatedDate = DateTime.Now;

                if (_snippetRepository != null)
                {
                    // Check if snippet already exists
                    var existing = _snippetRepository.GetByTrigger(snippet.Trigger, snippet.Language);
                    if (existing != null)
                    {
                        // Update existing snippet
                        existing.Name = snippet.Name;
                        existing.Description = snippet.Description;
                        existing.Code = snippet.Code;
                        existing.Category = snippet.Category;
                        _snippetRepository.Update(existing);
                        System.Diagnostics.Debug.WriteLine($"?? Updated existing snippet: {snippet.Name}");
                    }
                    else
                    {
                        // Add new snippet
                        _snippetRepository.Add(snippet);
                        System.Diagnostics.Debug.WriteLine($"? Added new snippet to DB: {snippet.Name}");
                    }
                }
                else
                {
                    // Fallback to cache
                    _cachedSnippets.RemoveAll(s => s.Name == snippet.Name && s.Language == snippet.Language);
                    _cachedSnippets.Add(snippet);
                    System.Diagnostics.Debug.WriteLine($"? Added snippet to cache: {snippet.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error adding snippet: {ex.Message}");
                throw;
            }
        }

        public void DeleteSnippet(string snippetName)
        {
            try
            {
                bool found = false;

                if (_snippetRepository != null)
                {
                    var allSnippets = _snippetRepository.GetAll();
                    var snippetsToDelete = allSnippets.Where(s => s.Name == snippetName).ToList();
                    
                    foreach (var snippet in snippetsToDelete)
                    {
                        _snippetRepository.Delete(snippet.Id);
                        found = true;
                    }
                    
                    if (found)
                        System.Diagnostics.Debug.WriteLine($"??? Deleted snippet from DB: {snippetName}");
                }
                else
                {
                    // Fallback to cache
                    int removed = _cachedSnippets.RemoveAll(s => s.Name == snippetName);
                    found = removed > 0;
                    
                    if (found)
                        System.Diagnostics.Debug.WriteLine($"??? Deleted snippet from cache: {snippetName}");
                }

                if (!found)
                    throw new ArgumentException($"Snippet '{snippetName}' not found");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error deleting snippet: {ex.Message}");
                throw;
            }
        }

        public CodeSnippet FindSnippetByTrigger(string trigger, string language)
        {
            if (string.IsNullOrWhiteSpace(trigger))
                return null;

            try
            {
                if (_snippetRepository != null)
                {
                    return _snippetRepository.GetByTrigger(trigger, language);
                }
                else
                {
                    // Fallback to cache
                    return _cachedSnippets.FirstOrDefault(s => 
                        s.Trigger.Equals(trigger, StringComparison.OrdinalIgnoreCase) && 
                        (string.IsNullOrEmpty(language) || s.Language.Equals(language, StringComparison.OrdinalIgnoreCase)));
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error finding snippet by trigger: {ex.Message}");
                return null;
            }
        }

        public List<CodeSnippet> GetMostUsedSnippets(int limit = 10)
        {
            try
            {
                if (_snippetRepository != null)
                {
                    return _snippetRepository.GetMostUsed(limit);
                }
                else
                {
                    // Fallback to cache
                    return _cachedSnippets
                        .OrderByDescending(s => s.UsageCount)
                        .ThenByDescending(s => s.LastUsedDate)
                        .Take(limit)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error getting most used snippets: {ex.Message}");
                return new List<CodeSnippet>();
            }
        }

        private string ExpandSnippet(CodeSnippet snippet)
        {
            if (snippet == null)
                return string.Empty;

            string code = snippet.Code;
            
            // Basic variable expansion
            code = code.Replace("$DATE", DateTime.Now.ToString("yyyy-MM-dd"));
            code = code.Replace("$TIME", DateTime.Now.ToString("HH:mm:ss"));
            code = code.Replace("$USER", Environment.UserName);

            return code;
        }

        private void InitializeSnippets()
        {
            try
            {
                if (_snippetRepository != null)
                {
                    // Load snippets from database
                    var dbSnippets = _snippetRepository.GetAll();
                    System.Diagnostics.Debug.WriteLine($"?? Loaded {dbSnippets.Count} snippets from database");

                    // If no snippets in DB, add defaults
                    if (dbSnippets.Count == 0)
                    {
                        AddDefaultSnippets();
                    }
                }
                else
                {
                    // Fallback to cache with defaults
                    AddDefaultSnippetsToCache();
                    System.Diagnostics.Debug.WriteLine($"?? Initialized {_cachedSnippets.Count} default snippets in cache");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"? Error initializing snippets: {ex.Message}");
                // Fallback to cache
                AddDefaultSnippetsToCache();
            }
        }

        private void AddDefaultSnippets()
        {
            var defaultSnippets = GetDefaultSnippets();
            
            foreach (var snippet in defaultSnippets)
            {
                try
                {
                    _snippetRepository.Add(snippet);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"? Error adding default snippet {snippet.Name}: {ex.Message}");
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"? Added {defaultSnippets.Count} default snippets to database");
        }

        private void AddDefaultSnippetsToCache()
        {
            _cachedSnippets.AddRange(GetDefaultSnippets());
        }

        private List<CodeSnippet> GetDefaultSnippets()
        {
            return new List<CodeSnippet>
            {
                new CodeSnippet
                {
                    Name = "class",
                    Trigger = "class",
                    Language = "csharp",
                    Description = "Create a new class",
                    Code = "public class ClassName\n{\n    public ClassName()\n    {\n        \n    }\n}",
                    Category = "Class"
                },
                new CodeSnippet
                {
                    Name = "method", 
                    Trigger = "method",
                    Language = "csharp",
                    Description = "Create a new method",
                    Code = "public void MethodName()\n{\n    \n}",
                    Category = "Method"
                },
                new CodeSnippet
                {
                    Name = "property", 
                    Trigger = "prop",
                    Language = "csharp",
                    Description = "Create a new property",
                    Code = "public string PropertyName { get; set; }",
                    Category = "Property"
                },
                new CodeSnippet
                {
                    Name = "constructor", 
                    Trigger = "ctor",
                    Language = "csharp",
                    Description = "Create a constructor",
                    Code = "public ClassName()\n{\n    \n}",
                    Category = "Constructor"
                },
                new CodeSnippet
                {
                    Name = "for loop", 
                    Trigger = "for",
                    Language = "csharp",
                    Description = "Create a for loop",
                    Code = "for (int i = 0; i < length; i++)\n{\n    \n}",
                    Category = "Loop"
                }
            };
        }
    }
}