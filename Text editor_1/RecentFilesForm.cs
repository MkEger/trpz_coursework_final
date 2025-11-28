using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TextEditorMK.Models;
using TextEditorMK.Repositories.Interfaces;
using TextEditorMK.Helpers; 

namespace Text_editor_1
{
    public partial class RecentFilesForm : Form
    {
        private readonly IRecentFileRepository _recentFileRepository;
        private readonly IDocumentRepository _documentRepository;
        private readonly IEncodingRepository _encodingRepository;
        private readonly EditorSettings _currentSettings;
        
        public Document SelectedDocument { get; private set; }

        public RecentFilesForm(IRecentFileRepository recentFileRepository, 
                              IDocumentRepository documentRepository,
                              IEncodingRepository encodingRepository,
                              EditorSettings currentSettings = null)
        {
            InitializeComponent();
            _recentFileRepository = recentFileRepository;
            _documentRepository = documentRepository;
            _encodingRepository = encodingRepository;
            _currentSettings = currentSettings;
            
            System.Diagnostics.Debug.WriteLine("🔧 RecentFilesForm constructor called");
            
            // НЕ додаємо колонки тут, вони вже є в Designer
            SetupListViewEventHandlers();
            
            if (_currentSettings != null)
            {
                ApplyTheme();
            }
            
            LoadRecentFiles();
        }

        private void SetupListViewEventHandlers()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("📋 Setting up ListView event handlers...");
                
                // Налаштовуємо ListView
                listViewRecentFiles.View = View.Details;
                listViewRecentFiles.FullRowSelect = true;
                listViewRecentFiles.GridLines = true;
                listViewRecentFiles.MultiSelect = false;
                listViewRecentFiles.HideSelection = false;
                
                // Додаємо обробники подій
                listViewRecentFiles.DoubleClick += ListView_DoubleClick;
                listViewRecentFiles.SelectedIndexChanged += ListView_SelectedIndexChanged;
                listViewRecentFiles.ItemActivate += ListView_ItemActivate;
                
                System.Diagnostics.Debug.WriteLine($"✅ ListView setup complete. Columns count: {listViewRecentFiles.Columns.Count}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error setting up ListView: {ex.Message}");
            }
        }

        private void ListView_DoubleClick(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("👆 ListView double-clicked");
            btnOpen_Click(sender, e);
        }

        private void ListView_ItemActivate(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("👆 ListView item activated");
            btnOpen_Click(sender, e);
        }

        private void ListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool hasSelection = listViewRecentFiles.SelectedItems.Count > 0;
            btnOpen.Enabled = hasSelection;
            
            if (hasSelection)
            {
                var selectedFile = listViewRecentFiles.SelectedItems[0].Tag as RecentFile;
                System.Diagnostics.Debug.WriteLine($"📄 Selected file: {selectedFile?.FileName ?? "NULL"}");
            }
        }

        private void ApplyTheme()
        {
            try
            {
                if (_currentSettings == null) return;

                var theme = EditorTheme.GetByName(_currentSettings.Theme);
                if (theme == null) return;

                ThemeHelper.ApplyThemeToForm(this, theme);

                System.Diagnostics.Debug.WriteLine($"🎨 Applied {theme.Name} theme to RecentFilesForm");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error applying theme to RecentFilesForm: {ex.Message}");
            }
        }

        private void LoadRecentFiles()
        {
            if (_recentFileRepository == null)
            {
                System.Diagnostics.Debug.WriteLine("❌ RecentFileRepository is null");
                MessageBox.Show("Recent files are not available.\nMySQL database is not connected.", 
                    "Database Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("📂 LoadRecentFiles starting...");
                System.Diagnostics.Debug.WriteLine($"📊 ListView columns count: {listViewRecentFiles.Columns.Count}");
                
                var recentFiles = _recentFileRepository.GetRecent(10);
                
                System.Diagnostics.Debug.WriteLine($"📋 Retrieved {recentFiles.Count} recent files from repository");
                
                listViewRecentFiles.Items.Clear();
                listViewRecentFiles.BeginUpdate();
                
                if (recentFiles.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("📭 No recent files found");
                    
                    // Додаємо повідомлення про відсутність файлів
                    var emptyItem = new ListViewItem(new[]
                    {
                        "No recent files found",
                        "Open some files to see them here",
                        "",
                        ""
                    });
                    emptyItem.ForeColor = Color.Gray;
                    emptyItem.Tag = null;
                    listViewRecentFiles.Items.Add(emptyItem);
                    
                    btnOpen.Enabled = false;
                    listViewRecentFiles.EndUpdate();
                    return;
                }
                
                int addedCount = 0;
                foreach (var file in recentFiles)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"📄 Processing file: {file.FileName} at {file.FilePath}");
                        
                        var item = new ListViewItem(new[]
                        {
                            file.FileName ?? "Unknown",
                            file.FilePath ?? "Unknown",
                            file.LastOpenedAt.ToString("dd.MM.yyyy HH:mm"),
                            file.OpenCount.ToString()
                        });
                        
                        item.Tag = file;
                        
                        // Перевіряємо чи файл існує
                        if (!System.IO.File.Exists(file.FilePath))
                        {
                            item.ForeColor = Color.Red;
                            item.SubItems[1].Text += " (File not found)";
                            System.Diagnostics.Debug.WriteLine($"⚠️ File not found: {file.FilePath}");
                        }
                        
                        listViewRecentFiles.Items.Add(item);
                        addedCount++;
                        System.Diagnostics.Debug.WriteLine($"✅ Added item #{addedCount}: {file.FileName}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ Error adding file {file?.FileName}: {ex.Message}");
                    }
                }

                listViewRecentFiles.EndUpdate();
                System.Diagnostics.Debug.WriteLine($"✅ Successfully loaded {addedCount} items into ListView");
                System.Diagnostics.Debug.WriteLine($"📊 Final ListView items count: {listViewRecentFiles.Items.Count}");
                
                // Вмикаємо кнопку Open, якщо є валідні файли
                var hasValidFiles = listViewRecentFiles.Items.Cast<ListViewItem>().Any(item => item.Tag != null);
                btnOpen.Enabled = hasValidFiles;
                
                // Автоматично виділяємо перший елемент
                if (listViewRecentFiles.Items.Count > 0 && hasValidFiles)
                {
                    listViewRecentFiles.Items[0].Selected = true;
                    listViewRecentFiles.Focus();
                }
                
                System.Diagnostics.Debug.WriteLine($"🔘 Open button enabled: {btnOpen.Enabled}");
            }
            catch (Exception ex)
            {
                listViewRecentFiles.EndUpdate();
                System.Diagnostics.Debug.WriteLine($"❌ Error in LoadRecentFiles: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"📍 Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Error loading recent files: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📂 Open button clicked");
            
            if (listViewRecentFiles.SelectedItems.Count > 0)
            {
                var selectedItem = listViewRecentFiles.SelectedItems[0];
                var selectedFile = selectedItem.Tag as RecentFile;
                
                if (selectedFile == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ Selected item has no RecentFile tag");
                    MessageBox.Show("Please select a valid file to open.", "Invalid Selection", 
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"📂 Opening file: {selectedFile.FileName} at {selectedFile.FilePath}");
                
                try
                {
                    // Перевіряємо чи файл існує
                    if (!System.IO.File.Exists(selectedFile.FilePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ File not found: {selectedFile.FilePath}");
                        var result = MessageBox.Show(
                            $"File not found: {selectedFile.FilePath}\n\nDo you want to remove it from recent files?", 
                            "File Not Found", 
                            MessageBoxButtons.YesNo, 
                            MessageBoxIcon.Question);
                        
                        if (result == DialogResult.Yes)
                        {
                            _recentFileRepository.Delete(selectedFile.Id);
                            LoadRecentFiles(); // Перезавантажуємо список
                        }
                        return;
                    }
                    
                    var encoding = System.Text.Encoding.UTF8; 
                    string content = System.IO.File.ReadAllText(selectedFile.FilePath, encoding);
                    
                    SelectedDocument = new Document
                    {
                        Id = GenerateNewId(),
                        FileName = selectedFile.FileName,
                        FilePath = selectedFile.FilePath,
                        Content = content,
                        TextEncoding = _encodingRepository.GetDefault(),
                        IsSaved = true
                    };
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Document created successfully: {SelectedDocument.FileName}");

                    // Оновлюємо статистику відкриттів
                    selectedFile.UpdateLastOpened();
                    _recentFileRepository?.AddOrUpdate(selectedFile);
                    
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error opening file: {ex.Message}");
                    MessageBox.Show($"Error opening file: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("⚠️ No file selected");
                MessageBox.Show("Please select a file to open.", "No Selection", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("❌ Cancel button clicked");
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnClearHistory_Click(object sender, EventArgs e)
        {
            if (_recentFileRepository == null)
            {
                MessageBox.Show("Cannot clear history - database not available.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var result = MessageBox.Show("Clear all recent files history?", 
                "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
            if (result == DialogResult.Yes)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("🗑️ Clearing recent files history...");
                    
                    var allFiles = _recentFileRepository.GetAll();
                    foreach (var file in allFiles)
                    {
                        _recentFileRepository.Delete(file.Id);
                    }
                    
                    LoadRecentFiles();
                    
                    System.Diagnostics.Debug.WriteLine($"✅ Cleared {allFiles.Count} files from history");
                    MessageBox.Show("Recent files history cleared.", "Success",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Error clearing history: {ex.Message}");
                    MessageBox.Show($"Error clearing history: {ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private int GenerateNewId()
        {
            return new Random().Next(1, 10000);
        }

        private void RecentFilesForm_Load(object sender, EventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("📋 RecentFilesForm_Load event fired");
            System.Diagnostics.Debug.WriteLine($"📊 Form loaded - ListView items: {listViewRecentFiles.Items.Count}, Columns: {listViewRecentFiles.Columns.Count}");
            
            // Встановлюємо фокус на ListView
            if (listViewRecentFiles.Items.Count > 0)
            {
                var firstValidItem = listViewRecentFiles.Items.Cast<ListViewItem>().FirstOrDefault(item => item.Tag != null);
                if (firstValidItem != null)
                {
                    firstValidItem.Selected = true;
                    firstValidItem.Focused = true;
                    listViewRecentFiles.Focus();
                }
            }
        }
    }
}
