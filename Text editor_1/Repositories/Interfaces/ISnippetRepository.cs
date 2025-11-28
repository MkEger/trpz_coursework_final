using System.Collections.Generic;
using TextEditorMK.Models;

namespace TextEditorMK.Repositories.Interfaces
{
    public interface ISnippetRepository
    {
        void Add(CodeSnippet snippet);
        void Update(CodeSnippet snippet);
        void Delete(int id);
        CodeSnippet GetById(int id);
        List<CodeSnippet> GetAll();
        List<CodeSnippet> GetByLanguage(string language);
        CodeSnippet GetByTrigger(string trigger, string language);
        List<CodeSnippet> GetByCategory(string category);
        List<CodeSnippet> GetMostUsed(int limit = 10);
    }
}