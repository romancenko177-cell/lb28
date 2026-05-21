using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;

namespace Lab28_FileManager;

public class MainForm : Form
{
    private readonly TextBox pathBox = new();
    private readonly ListBox listBox = new();
    private readonly TextBox editorBox = new();
    private readonly Label infoLabel = new();

    public MainForm()
    {
        Text = "ЛР28 - Робота з файловою системою";
        Width = 1100;
        Height = 720;
        StartPosition = FormStartPosition.CenterScreen;

        var main = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        Controls.Add(main);

        var left = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 4, Padding = new Padding(10) };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
        main.Controls.Add(left, 0, 0);

        var top = new FlowLayoutPanel { Dock = DockStyle.Fill };
        pathBox.Width = 360;
        pathBox.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var btnOpen = new Button { Text = "Відкрити", Width = 90 };
        btnOpen.Click += (_, _) => LoadDirectory(pathBox.Text);
        top.Controls.Add(pathBox);
        top.Controls.Add(btnOpen);
        left.Controls.Add(top, 0, 0);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        AddButton(buttons, "Створити каталог", CreateDirectoryClick);
        AddButton(buttons, "Видалити каталог", DeleteDirectoryClick);
        AddButton(buttons, "Перенести каталог", MoveDirectoryClick);
        AddButton(buttons, "Копіювати каталог", CopyDirectoryClick);
        AddButton(buttons, "Створити файл", CreateFileClick);
        AddButton(buttons, "Видалити файл", DeleteFileClick);
        AddButton(buttons, "Перенести файл", MoveFileClick);
        AddButton(buttons, "Копіювати файл", CopyFileClick);
        AddButton(buttons, "Атрибути", AttributesClick);
        AddButton(buttons, "ZIP", ZipClick);
        AddButton(buttons, "Розпакувати", UnzipClick);
        left.Controls.Add(buttons, 0, 1);

        listBox.Dock = DockStyle.Fill;
        listBox.DoubleClick += (_, _) => OpenSelected();
        left.Controls.Add(listBox, 0, 2);

        infoLabel.Dock = DockStyle.Fill;
        left.Controls.Add(infoLabel, 0, 3);

        var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, Padding = new Padding(10) };
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        main.Controls.Add(right, 1, 0);

        editorBox.Multiline = true;
        editorBox.ScrollBars = ScrollBars.Both;
        editorBox.Font = new Font("Consolas", 11);
        editorBox.Dock = DockStyle.Fill;
        right.Controls.Add(editorBox, 0, 0);

        var editorButtons = new FlowLayoutPanel { Dock = DockStyle.Fill };
        AddButton(editorButtons, "Відкрити текстовий файл", OpenTextClick);
        AddButton(editorButtons, "Зберегти текст", SaveTextClick);
        AddButton(editorButtons, "Очистити", (_, _) => editorBox.Clear());
        right.Controls.Add(editorButtons, 0, 1);

        LoadDirectory(pathBox.Text);
    }

    private void AddButton(FlowLayoutPanel panel, string text, EventHandler action)
    {
        var b = new Button { Text = text, Width = 145, Height = 30 };
        b.Click += action;
        panel.Controls.Add(b);
    }

    private void LoadDirectory(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                MessageBox.Show("Каталог не існує");
                return;
            }

            pathBox.Text = path;
            listBox.Items.Clear();
            foreach (var dir in Directory.GetDirectories(path)) listBox.Items.Add("[DIR] " + dir);
            foreach (var file in Directory.GetFiles(path)) listBox.Items.Add("[FILE] " + file);
            infoLabel.Text = "Поточний каталог: " + path;
        }
        catch (Exception ex) { MessageBox.Show(ex.Message); }
    }

    private string? SelectedPath()
    {
        if (listBox.SelectedItem == null) return null;
        string s = listBox.SelectedItem.ToString()!;
        return s.Replace("[DIR] ", "").Replace("[FILE] ", "");
    }

    private void OpenSelected()
    {
        string? selected = SelectedPath();
        if (selected == null) return;
        if (Directory.Exists(selected)) LoadDirectory(selected);
        else if (File.Exists(selected) && Path.GetExtension(selected).ToLower() == ".txt")
            editorBox.Text = File.ReadAllText(selected);
    }

    private string Ask(string title, string defaultValue = "")
    {
        using var f = new Form { Text = title, Width = 430, Height = 150, StartPosition = FormStartPosition.CenterParent };
        var tb = new TextBox { Left = 15, Top = 15, Width = 380, Text = defaultValue };
        var ok = new Button { Text = "OK", Left = 220, Top = 55, Width = 80, DialogResult = DialogResult.OK };
        var cancel = new Button { Text = "Скасувати", Left = 310, Top = 55, Width = 85, DialogResult = DialogResult.Cancel };
        f.Controls.Add(tb); f.Controls.Add(ok); f.Controls.Add(cancel);
        f.AcceptButton = ok; f.CancelButton = cancel;
        return f.ShowDialog(this) == DialogResult.OK ? tb.Text : "";
    }

    private void CreateDirectoryClick(object? sender, EventArgs e)
    {
        string name = Ask("Назва нового каталогу");
        if (name == "") return;
        Directory.CreateDirectory(Path.Combine(pathBox.Text, name));
        LoadDirectory(pathBox.Text);
    }

    private void DeleteDirectoryClick(object? sender, EventArgs e)
    {
        string? p = SelectedPath();
        if (p == null || !Directory.Exists(p)) return;
        if (MessageBox.Show("Видалити каталог з усім вмістом?", "Підтвердження", MessageBoxButtons.YesNo) == DialogResult.Yes)
        {
            Directory.Delete(p, true);
            LoadDirectory(pathBox.Text);
        }
    }

    private void MoveDirectoryClick(object? sender, EventArgs e)
    {
        string? p = SelectedPath();
        if (p == null || !Directory.Exists(p)) return;
        string newPath = Ask("Новий шлях для каталогу", p);
        if (newPath == "") return;
        Directory.Move(p, newPath);
        LoadDirectory(pathBox.Text);
    }

    private void CopyDirectoryClick(object? sender, EventArgs e)
    {
        string? p = SelectedPath();
        if (p == null || !Directory.Exists(p)) return;
        string newPath = Ask("Куди скопіювати каталог", p + "_copy");
        if (newPath == "") return;
        CopyDirectory(p, newPath);
        LoadDirectory(pathBox.Text);
    }

    private void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        foreach (string dir in Directory.GetDirectories(source))
            CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
    }

    private void CreateFileClick(object? sender, EventArgs e)
    {
        string name = Ask("Назва нового файлу", "newfile.txt");
        if (name == "") return;
        File.WriteAllText(Path.Combine(pathBox.Text, name), "");
        LoadDirectory(pathBox.Text);
    }

    private void DeleteFileClick(object? sender, EventArgs e)
    {
        string? p = SelectedPath();
        if (p == null || !File.Exists(p)) return;
        File.Delete(p);
        LoadDirectory(pathBox.Text);
    }

    private void MoveFileClick(object? sender, EventArgs e)
    {
        string? p = SelectedPath();
        if (p == null || !File.Exists(p)) return;
        string newPath = Ask("Новий шлях для файлу", p);
        if (newPath == "") return;
        File.Move(p, newPath, true);
        LoadDirectory(pathBox.Text);
    }

    private void CopyFileClick(object? sender, EventArgs e)
    {
        string? p = SelectedPath();
        if (p == null || !File.Exists(p)) return;
        string newPath = Ask("Куди скопіювати файл", Path.Combine(Path.GetDirectoryName(p)!, Path.GetFileNameWithoutExtension(p) + "_copy" + Path.GetExtension(p)));
        if (newPath == "") return;
        File.Copy(p, newPath, true);
        LoadDirectory(pathBox.Text);
    }

    private void AttributesClick(object? sender, EventArgs e)
    {
        string? p = SelectedPath();
        if (p == null || (!File.Exists(p) && !Directory.Exists(p))) return;
        FileAttributes attr = File.GetAttributes(p);
        string msg = "Поточні атрибути: " + attr + "\n\nТак - встановити ReadOnly, Ні - прибрати ReadOnly, Скасувати - нічого не змінювати.";
        var res = MessageBox.Show(msg, "Атрибути", MessageBoxButtons.YesNoCancel);
        if (res == DialogResult.Yes) File.SetAttributes(p, attr | FileAttributes.ReadOnly);
        if (res == DialogResult.No) File.SetAttributes(p, attr & ~FileAttributes.ReadOnly);
        LoadDirectory(pathBox.Text);
    }

    private void OpenTextClick(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*" };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            pathBox.Tag = ofd.FileName;
            editorBox.Text = File.ReadAllText(ofd.FileName);
        }
    }

    private void SaveTextClick(object? sender, EventArgs e)
    {
        string? current = pathBox.Tag as string;
        using var sfd = new SaveFileDialog { Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*", FileName = current ?? "text.txt" };
        if (sfd.ShowDialog() == DialogResult.OK)
        {
            File.WriteAllText(sfd.FileName, editorBox.Text);
            pathBox.Tag = sfd.FileName;
            LoadDirectory(pathBox.Text);
        }
    }

    private void ZipClick(object? sender, EventArgs e)
    {
        string? p = SelectedPath();
        if (p == null) return;
        using var sfd = new SaveFileDialog { Filter = "ZIP archive (*.zip)|*.zip", FileName = Path.GetFileName(p) + ".zip" };
        if (sfd.ShowDialog() != DialogResult.OK) return;
        if (Directory.Exists(p)) ZipFile.CreateFromDirectory(p, sfd.FileName, CompressionLevel.Optimal, true);
        else
        {
            using var zip = ZipFile.Open(sfd.FileName, ZipArchiveMode.Create);
            zip.CreateEntryFromFile(p, Path.GetFileName(p));
        }
        MessageBox.Show("Архів створено");
    }

    private void UnzipClick(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog { Filter = "ZIP archive (*.zip)|*.zip" };
        if (ofd.ShowDialog() != DialogResult.OK) return;
        using var fbd = new FolderBrowserDialog();
        if (fbd.ShowDialog() != DialogResult.OK) return;
        ZipFile.ExtractToDirectory(ofd.FileName, fbd.SelectedPath, true);
        LoadDirectory(fbd.SelectedPath);
        MessageBox.Show("Архів розпаковано");
    }
}
