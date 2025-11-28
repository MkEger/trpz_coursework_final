using System;
using System.Linq;
using TextEditorMK.Repositories.Implementations;
using TextEditorMK.Models;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("?? Testing MySQL Snippets Repository - Persistence Test...\n");
        
        try
        {
            // Створюємо репозиторій
            Console.WriteLine("?? Creating MySqlSnippetRepository...");
            var repository = new MySqlSnippetRepository();
            Console.WriteLine("? Repository created and table checked");

            // Спочатку перевіряємо, що вже є в БД
            Console.WriteLine("\n?? Checking existing snippets in database...");
            var existingSnippets = repository.GetAll();
            Console.WriteLine($"?? Found {existingSnippets.Count} existing snippets in database:");
            
            foreach (var existing in existingSnippets)
            {
                Console.WriteLine($"   - {existing.Name} (trigger: {existing.Trigger}, created: {existing.CreatedDate:yyyy-MM-dd HH:mm})");
            }

            // Створюємо новий тестовий сніппет з унікальним часом
            var timestamp = DateTime.Now.ToString("HHmmss");
            var snippet = new CodeSnippet
            {
                Name = $"persistent-test-{timestamp}",
                Trigger = $"test{timestamp}",
                Language = "csharp",
                Description = $"Persistence test snippet created at {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                Code = $"public void TestMethod_{timestamp}()\n{{\n    // Created at {DateTime.Now}\n    Console.WriteLine(\"Persistent snippet works!\");\n}}",
                Category = "PersistenceTest",
                CreatedDate = DateTime.Now,
                UsageCount = 0
            };

            // Додаємо новий сніппет
            Console.WriteLine($"\n?? Adding new snippet: {snippet.Name}...");
            repository.Add(snippet);
            Console.WriteLine($"? Added snippet: {snippet.Name} with ID: {snippet.Id}");

            // Отримуємо всі сніппети після додавання
            Console.WriteLine("\n?? Getting all snippets after adding new one...");
            var allSnippets = repository.GetAll();
            Console.WriteLine($"?? Total snippets in database: {allSnippets.Count}");

            Console.WriteLine("\n?? All snippets in database:");
            foreach (var s in allSnippets.OrderBy(x => x.CreatedDate))
            {
                Console.WriteLine($"   - {s.Name} (trigger: {s.Trigger}, category: {s.Category}, created: {s.CreatedDate:yyyy-MM-dd HH:mm})");
            }

            // Тестуємо пошук нового сніппету
            Console.WriteLine($"\n?? Searching for new snippet with trigger '{snippet.Trigger}'...");
            var foundSnippet = repository.GetByTrigger(snippet.Trigger, "csharp");
            if (foundSnippet != null)
            {
                Console.WriteLine($"?? Found snippet: {foundSnippet.Name}");
                Console.WriteLine($"?? Description: {foundSnippet.Description}");
                Console.WriteLine($"?? Usage count: {foundSnippet.UsageCount}");
            }
            else
            {
                Console.WriteLine("? New snippet not found by trigger");
            }

            Console.WriteLine("\n?? Test completed successfully!");
            Console.WriteLine("?? Restart the application to verify that snippets persist between sessions.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error: {ex.Message}");
            Console.WriteLine($"?? Stack trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
}