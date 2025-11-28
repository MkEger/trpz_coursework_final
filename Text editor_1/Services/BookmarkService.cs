using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TextEditorMK.Models;
using TextEditorMK.Repositories.Interfaces;

namespace TextEditorMK.Services
{
    public class BookmarkService
    {
        private readonly RichTextBox _textBox;
        private readonly IBookmarkRepository _bookmarkRepository;
        private readonly List<Bookmark> _cachedBookmarks;
        
        public string CurrentDocumentName { get; private set; } = "Untitled";
        public string CurrentDocumentPath { get; private set; } = string.Empty;

        public BookmarkService(RichTextBox textBox, IBookmarkRepository bookmarkRepository = null)
        {
            _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
            _bookmarkRepository = bookmarkRepository;
            _cachedBookmarks = new List<Bookmark>();
            
            LoadBookmarks();
        }

        public void SetCurrentDocument(string documentName, string documentPath = null)
        {
            CurrentDocumentName = string.IsNullOrEmpty(documentName) ? "Untitled" : documentName;
            CurrentDocumentPath = documentPath ?? string.Empty;
        }

        public void AddBookmark(int lineNumber, string description = null)
        {
            if (lineNumber < 1)
                throw new ArgumentException("Line number must be greater than 0");

            if (HasBookmark(lineNumber))
                throw new InvalidOperationException($"Bookmark already exists on line {lineNumber}");

            string linePreview = GetLinePreview(lineNumber);

            var bookmark = new Bookmark
            {
                LineNumber = lineNumber,
                Description = description ?? $"Bookmark at line {lineNumber}",
                LinePreview = linePreview,
                DocumentName = CurrentDocumentName,
                DocumentPath = CurrentDocumentPath,
                CreatedDate = DateTime.Now,
                IsActive = true
            };

            try
            {
                if (_bookmarkRepository != null)
                {
                    _bookmarkRepository.Add(bookmark);
                }
                else
                {
                    _cachedBookmarks.Add(bookmark);
                    _cachedBookmarks.Sort((b1, b2) => b1.LineNumber.CompareTo(b2.LineNumber));
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void RemoveBookmark(int lineNumber)
        {
            try
            {
                bool found = false;

                if (_bookmarkRepository != null)
                {
                    var bookmark = _bookmarkRepository.GetByLineNumber(lineNumber);
                    if (bookmark != null)
                    {
                        _bookmarkRepository.Delete(bookmark.Id);
                        found = true;
                    }
                }
                else
                {
                    var bookmark = _cachedBookmarks.FirstOrDefault(b => b.LineNumber == lineNumber);
                    if (bookmark != null)
                    {
                        _cachedBookmarks.Remove(bookmark);
                        found = true;
                    }
                }

                if (!found)
                    throw new ArgumentException($"No bookmark found at line {lineNumber}");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void ToggleBookmark(int lineNumber, string description = null)
        {
            if (HasBookmark(lineNumber))
            {
                RemoveBookmark(lineNumber);
            }
            else
            {
                AddBookmark(lineNumber, description);
            }
        }

        public int? GoToNextBookmark(int currentLine)
        {
            try
            {
                var allBookmarks = GetAllActiveBookmarks();
                var nextBookmark = allBookmarks
                    .Where(b => b.LineNumber > currentLine)
                    .OrderBy(b => b.LineNumber)
                    .FirstOrDefault();

                if (nextBookmark != null)
                {
                    GoToLine(nextBookmark.LineNumber);
                    
                    nextBookmark.UpdateAccess();
                    UpdateBookmarkInRepository(nextBookmark);
                    
                    return nextBookmark.LineNumber;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public int? GoToPreviousBookmark(int currentLine)
        {
            try
            {
                var allBookmarks = GetAllActiveBookmarks();
                var prevBookmark = allBookmarks
                    .Where(b => b.LineNumber < currentLine)
                    .OrderByDescending(b => b.LineNumber)
                    .FirstOrDefault();

                if (prevBookmark != null)
                {
                    GoToLine(prevBookmark.LineNumber);
                    
                    prevBookmark.UpdateAccess();
                    UpdateBookmarkInRepository(prevBookmark);
                    
                    return prevBookmark.LineNumber;
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public List<Bookmark> GetAllBookmarks()
        {
            return GetAllActiveBookmarks().OrderBy(b => b.LineNumber).ToList();
        }

        public void ClearAllBookmarks()
        {
            try
            {
                if (_bookmarkRepository != null)
                {
                    _bookmarkRepository.ClearAll();
                }
                else
                {
                    _cachedBookmarks.Clear();
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool HasBookmark(int lineNumber)
        {
            try
            {
                if (_bookmarkRepository != null)
                {
                    var bookmark = _bookmarkRepository.GetByLineNumber(lineNumber);
                    return bookmark != null && bookmark.IsActive;
                }
                else
                {
                    return _cachedBookmarks.Any(b => b.IsActive && b.LineNumber == lineNumber);
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        private List<Bookmark> GetAllActiveBookmarks()
        {
            try
            {
                if (_bookmarkRepository != null)
                {
                    return _bookmarkRepository.GetActiveBookmarks();
                }
                else
                {
                    return _cachedBookmarks.Where(b => b.IsActive).ToList();
                }
            }
            catch (Exception)
            {
                return new List<Bookmark>();
            }
        }

        private void UpdateBookmarkInRepository(Bookmark bookmark)
        {
            try
            {
                if (_bookmarkRepository != null && bookmark.Id > 0)
                {
                    _bookmarkRepository.Update(bookmark);
                }
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        private void LoadBookmarks()
        {
            try
            {
                if (_bookmarkRepository != null)
                {
                    var dbBookmarks = _bookmarkRepository.GetAll();
                }
            }
            catch (Exception)
            {
                // Silent fail
            }
        }

        private string GetLinePreview(int lineNumber)
        {
            try
            {
                if (_textBox != null && lineNumber > 0 && lineNumber <= _textBox.Lines.Length)
                {
                    string line = _textBox.Lines[lineNumber - 1];
                    return line.Length > 100 ? line.Substring(0, 100) + "..." : line;
                }
            }
            catch (Exception)
            {
                // Silent fail
            }
            return string.Empty;
        }

        private void GoToLine(int lineNumber)
        {
            try
            {
                if (lineNumber > _textBox.Lines.Length)
                    return;

                int charIndex = _textBox.GetFirstCharIndexFromLine(lineNumber - 1);
                if (charIndex >= 0)
                {
                    _textBox.SelectionStart = charIndex;
                    _textBox.SelectionLength = 0;
                    _textBox.ScrollToCaret();
                    _textBox.Focus();
                }
            }
            catch (Exception)
            {
                // Silent fail
            }
        }
    }
}