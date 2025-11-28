using System.Collections.Generic;
using TextEditorMK.Models;

namespace TextEditorMK.Repositories.Interfaces
{
    public interface IBookmarkRepository
    {
        void Add(Bookmark bookmark);
        void Update(Bookmark bookmark);
        void Delete(int id);
        Bookmark GetById(int id);
        List<Bookmark> GetAll();
        List<Bookmark> GetActiveBookmarks();
        Bookmark GetByLineNumber(int lineNumber);
        void DeleteByLineNumber(int lineNumber);
        void ClearAll();
    }
}