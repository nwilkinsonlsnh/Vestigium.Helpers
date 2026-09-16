using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers;
using Vestigium.Helpers.Gallery;
using Vestigium.Helpers.Xml;

namespace Vestigium.Helpers.Xml.Demo;

public sealed partial class MainViewModel : GalleryViewModelBase
{
    private XmlSession? _document;
    private XmlSession? _search;
    private XmlDocumentStream? _multi;

    public MainViewModel()
    {
        SearchHits = [];
        MultiHits = [];
        StatusText = $"Logger initialized · APPID {HelperLog.AppIds.Xml}";
        SeedDemo();
        LoadDocument();
    }

    public string Identity => XmlHelper.Identity;

    public string ContentType => XmlHelper.ContentType();

    public string StartupSnippet =>
        "var doc = XmlHelper.Open(path);\n" +
        "doc.Snapshot();\n" +
        "doc.SetText(\"//u:action/u:name\", \"MagicOff\");\n" +
        "var changes = doc.Diff();     // path + op\n" +
        "doc.Commit();\n" +
        "doc.Save();                   // atomic replace";

    public string ExportFolder => XmlHelper.DefaultExportDirectory();

    public string CorpusFolder { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Xml");

    public ObservableCollection<SearchHitRow> SearchHits { get; }

    public ObservableCollection<SearchHitRow> MultiHits { get; }

    [ObservableProperty] private string documentPath = "";
    [ObservableProperty] private string searchPath = "";
    [ObservableProperty] private string multiPath = "";
    [ObservableProperty] private string editPath = "//u:action/u:name";
    [ObservableProperty] private string editValue = "MagicOff";
    [ObservableProperty] private string diffText = "Load a document, then Snapshot + Set.";
    [ObservableProperty] private string workingXml = "Load a document to see the working tree.";
    [ObservableProperty] private string documentCaption = "Working vs committed. Save writes committed only.";
    [ObservableProperty] private string queryKind = "xpath";
    [ObservableProperty] private string queryValue = "//u:action/u:name";
    [ObservableProperty] private string searchCaption = "Default xmlns binds to prefix u:";
    [ObservableProperty] private string multiQuery = "Fun";
    [ObservableProperty] private string multiCaption = "OpenMulti of diagwrn.xml. Open of that file is Failed.";
    [ObservableProperty] private string safetyText = "Open DevicePreparationDDF.xml. The PUBLIC DTD must not be fetched.";

    [RelayCommand]
    private void SeedDemo()
    {
        RunSafe("Seed", () =>
        {
            Directory.CreateDirectory(ExportFolder);
            Directory.CreateDirectory(CorpusFolder);
            var gold = Path.Combine(AppContext.BaseDirectory, "Xml");
            if (Directory.Exists(gold))
            {
                foreach (var file in Directory.GetFiles(gold, "*.xml"))
                {
                    var dest = Path.Combine(CorpusFolder, Path.GetFileName(file));
                    File.Copy(file, dest, overwrite: true);
                }
            }

            var source = Path.Combine(CorpusFolder, "osinfo.xml");
            DocumentPath = Path.Combine(ExportFolder, "osinfo.xml");
            if (File.Exists(source))
                File.Copy(source, DocumentPath, overwrite: true);

            SearchPath = Path.Combine(CorpusFolder, "osinfo.xml");
            MultiPath = Path.Combine(CorpusFolder, "diagwrn.xml");
            StatusText = $"Demo files · {CorpusFolder}";
            RefreshLines();
        });
    }

    [RelayCommand]
    private void LoadDocument()
    {
        RunSafe("Load", () =>
        {
            SeedDemo();
            if (!File.Exists(DocumentPath))
            {
                StatusText = "osinfo.xml is missing. Seed demo files first.";
                return;
            }

            _document?.Dispose();
            _document = XmlHelper.Open(DocumentPath);
            _document.Snapshot();
            DiffText = "No pending ops.";
            DocumentCaption = $"Loaded {DocumentPath} · root={_document.RootName} · {_document.EncodingName}/{_document.EncodingSource}";
            StatusText = DocumentCaption;
            BindWorkingXml();
            RefreshLines();
        });
    }

    [RelayCommand]
    private void ApplyEdit()
    {
        RunSafe("SetText", () =>
        {
            var doc = DocumentSession();
            doc.Snapshot();
            doc.SetText(EditPath, EditValue);
            var changes = doc.Diff();
            DiffText = FormatDiff(changes);
            DocumentCaption = $"{changes.Count} operation(s) · working only · disk unchanged";
            StatusText = DocumentCaption;
            BindWorkingXml();
            RefreshLines();
        });
    }

    [RelayCommand]
    private void CommitDocument()
    {
        RunSafe("Commit", () =>
        {
            var doc = DocumentSession();
            var changes = doc.Diff();
            doc.Commit();
            DiffText = FormatDiff(changes) + "\n· committed, not saved";
            DocumentCaption = "Committed. Save to write the file.";
            StatusText = DocumentCaption;
            BindWorkingXml();
            RefreshLines();
        });
    }

    [RelayCommand]
    private void SaveDocument()
    {
        RunSafe("Save", () =>
        {
            var doc = DocumentSession();
            doc.Commit();
            doc.Save();
            DocumentCaption = $"Saved {DocumentPath}";
            StatusText = DocumentCaption;
            BindWorkingXml();
            RefreshLines();
        });
    }

    [RelayCommand]
    private void CancelDocument()
    {
        RunSafe("Cancel", () =>
        {
            var doc = DocumentSession();
            doc.Cancel();
            DiffText = "Cancel dropped working. Disk unchanged.";
            DocumentCaption = "Cancel · disk unchanged";
            StatusText = DocumentCaption;
            BindWorkingXml();
            RefreshLines();
        });
    }

    [RelayCommand]
    private void RunSearch()
    {
        RunSafe("Search", () =>
        {
            EnsureSearchSession(SearchPath);
            SearchHits.Clear();
            var query = BuildQuery(QueryKind, QueryValue);
            foreach (var hit in _search!.Search(query))
                SearchHits.Add(ToRow(hit));
            SearchCaption = $"{SearchHits.Count} hit(s) · {SearchPath}";
            StatusText = SearchCaption;
            RefreshLines();
        });
    }

    [RelayCommand]
    private void SearchMagicOn()
    {
        SearchPath = Path.Combine(CorpusFolder, "osinfo.xml");
        QueryKind = "text";
        QueryValue = "MagicOn";
        RunSearch();
    }

    [RelayCommand]
    private void SearchActions()
    {
        SearchPath = Path.Combine(CorpusFolder, "ipcfg.xml");
        QueryKind = "xpath";
        QueryValue = "//u:action/u:name";
        RunSearch();
    }

    [RelayCommand]
    private void OpenMulti()
    {
        RunSafe("OpenMulti", () =>
        {
            SeedDemo();
            MultiHits.Clear();
            if (!File.Exists(MultiPath))
            {
                MultiCaption = "diagwrn.xml is missing. Seed demo files first.";
                StatusText = MultiCaption;
                return;
            }

            try
            {
                XmlHelper.Open(MultiPath);
                MultiCaption = "Open unexpectedly succeeded.";
            }
            catch (XmlException ex)
            {
                MultiCaption = $"Open failed as specified. {ex.Message}";
            }

            _multi?.Dispose();
            _multi = XmlHelper.OpenMulti(MultiPath);
            MultiCaption += $"{Environment.NewLine}OpenMulti documents={_multi.Count}";
            StatusText = MultiCaption;
            RefreshLines();
        });
    }

    [RelayCommand]
    private void ScanMulti()
    {
        RunSafe("Scan", () =>
        {
            if (_multi is null)
                OpenMulti();
            if (_multi is null)
                return;

            MultiHits.Clear();
            var query = XmlSearch.ByAttribute(string.IsNullOrWhiteSpace(MultiQuery) ? "Fun" : MultiQuery.Trim());
            var n = 0;
            for (var i = 0; i < _multi.Count; i++)
            {
                foreach (var hit in _multi[i].Search(query))
                {
                    MultiHits.Add(new SearchHitRow { Part = i + 1, NodePath = hit.Path, LocalName = hit.LocalName, Text = hit.Text });
                    n++;
                    if (n >= 80)
                        break;
                }
                if (n >= 80)
                    break;
            }

            MultiCaption = $"{_multi.Count} document(s) · {n} hit(s) (capped) · {MultiPath}";
            StatusText = MultiCaption;
            RefreshLines();
        });
    }

    [RelayCommand]
    private void OpenDdf()
    {
        RunSafe("Open DDF", () =>
        {
            SeedDemo();
            var path = Path.Combine(CorpusFolder, "DevicePreparationDDF.xml");
            if (!File.Exists(path))
            {
                SafetyText = "DevicePreparationDDF.xml is missing. Seed demo files first.";
                StatusText = SafetyText;
                return;
            }

            using var doc = XmlHelper.Open(path);
            SafetyText =
                $"path={path}{Environment.NewLine}" +
                $"root={doc.RootName}{Environment.NewLine}" +
                $"encoding={doc.EncodingName}/{doc.EncodingSource}{Environment.NewLine}" +
                $"media={doc.MediaType}{Environment.NewLine}" +
                $"DOCTYPE={(string.IsNullOrWhiteSpace(doc.Doctype) ? "none" : "captured, not fetched")}{Environment.NewLine}" +
                $"{doc.Doctype}";
            StatusText = "DDF opened · resolver was not asked";
            RefreshLines();
        });
    }

    [RelayCommand]
    private void TryXxe()
    {
        RunSafe("XXE", () =>
        {
            Directory.CreateDirectory(ExportFolder);
            var sentinel = Path.Combine(ExportFolder, "secret.txt");
            File.WriteAllText(sentinel, "SHOULD-NOT-BE-READ");
            var payload = $"""
                <?xml version="1.0"?>
                <!DOCTYPE root [
                  <!ENTITY xxe SYSTEM "{sentinel}">
                ]>
                <root>&xxe;should-stay-empty</root>
                """;
            var path = Path.Combine(ExportFolder, "xxe.xml");
            File.WriteAllText(path, payload);
            using var doc = XmlHelper.Open(path);
            var text = doc.First(XmlSearch.ByName("root"))?.Text ?? "";
            var leaked = text.Contains("SHOULD-NOT-BE-READ", StringComparison.Ordinal);
            SafetyText =
                $"XXE {(leaked ? "LEAKED" : "blocked")}{Environment.NewLine}" +
                $"text=\"{text}\"{Environment.NewLine}" +
                "DTD stripped. Undeclared entity refs dropped. Resolver asks=0.";
            StatusText = leaked ? "XXE leaked — implementation is wrong" : "XXE blocked";
            RefreshLines();
        });
    }

    [RelayCommand]
    private void OpenExportFolder()
    {
        var dir = ExportFolder;
        Directory.CreateDirectory(dir);
        Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        StatusText = dir;
    }

    [RelayCommand]
    private void RunProbe()
    {
        RunSafe("Probe", () =>
        {
            var id = XmlHelper.Probe();
            StatusText = $"Probe complete · Identity={id} · {ContentType}";
            RefreshLines();
        });
    }

    public override void Dispose()
    {
        _document?.Dispose();
        _search?.Dispose();
        _multi?.Dispose();
        base.Dispose();
    }

    private XmlSession DocumentSession()
    {
        if (_document is null)
            LoadDocument();
        return _document ?? throw new InvalidOperationException("Load a document first.");
    }

    private void EnsureSearchSession(string path)
    {
        SeedDemo();
        if (!File.Exists(path))
            throw new FileNotFoundException("Seed demo files first.", path);
        if (_search is not null && string.Equals(_search.Path, path, StringComparison.OrdinalIgnoreCase))
            return;
        _search?.Dispose();
        _search = XmlHelper.Open(path);
    }

    private static XmlSearch BuildQuery(string kind, string value)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "name" : value.Trim();
        return kind switch
        {
            "name" => XmlSearch.ByName(text),
            "attribute" => XmlSearch.ByAttribute(text),
            "text" => XmlSearch.ByText(text, XmlMatch.Contains),
            _ => XmlSearch.XPath(text)
        };
    }

    private void BindWorkingXml()
    {
        WorkingXml = _document is null
            ? "Load a document to see the working tree."
            : _document.WorkingXml(indent: true);
    }

    private void RunSafe(string name, Action action)
    {
        try
        {
            action();
        }
        catch (Exception ex) when (
            ex is XmlException
            or XPathException
            or KeyNotFoundException
            or InvalidOperationException
            or IOException
            or ArgumentException
            or FileNotFoundException
            or UnauthorizedAccessException)
        {
            StatusText = $"{name} failed · {ex.GetType().Name}: {ex.Message}";
            RefreshLines();
        }
    }

    private static SearchHitRow ToRow(XmlWorkNode hit)
        => new() { Part = 1, NodePath = hit.Path, LocalName = hit.LocalName, Text = hit.Text };

    private static string FormatDiff(IReadOnlyList<XmlChange> changes)
        => changes.Count == 0
            ? "0 operation(s)"
            : string.Join(Environment.NewLine, changes.Select(c => $"{c.Op} {c.Path}"));
}

public sealed class SearchHitRow
{
    public int Part { get; init; }
    public string NodePath { get; init; } = "";
    public string LocalName { get; init; } = "";
    public string Text { get; init; } = "";
}
