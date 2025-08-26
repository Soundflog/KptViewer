using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Kpt_Viewer.Infrastructure;
using Kpt_Viewer.Services;

namespace Kpt_Viewer.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly XmlIndexBuilder _indexBuilder = new();
    private readonly ExportService _exportService = new();


    public ObservableCollection<RootNodeVm> Roots { get; } = new();
    private object? _selectedNode;
    public string SelectedHeader => (_selectedNode as INodeVm)?.Header ?? string.Empty;
    public string SelectedXmlPretty => Pretty((_selectedNode as INodeVm)?.Element);


    public ICommand OpenFileCommand { get; }
    public ICommand SaveSelectedCommand { get; }
    public ICommand ShowHelpCommand { get; }
    public ICommand CopyXmlCommand { get; }
    public ICommand ExitCommand { get; }


    private const string AuthorName = "Vershinin Pavel A., tg: @soundflogdev";


    public MainViewModel()
    {
        OpenFileCommand = new RelayCommand(_ => OpenFile());
        SaveSelectedCommand = new RelayCommand(_ => SaveSelected(),
            _ => Roots.SelectMany(r => r.Children).Any(c => c.IsSelected));
        ShowHelpCommand = new RelayCommand(_ => ShowHelp());
        CopyXmlCommand = new RelayCommand(_ => CopyXml(), _ => (_selectedNode as INodeVm)?.Element != null);
        ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
    }

    public void OnTreeSelectionChanged(object? selected)
    {
        _selectedNode = selected;
        RaisePropertyChanged(nameof(SelectedHeader));
        RaisePropertyChanged(nameof(SelectedXmlPretty));
    }


    private void OpenFile()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
            Title = "Open КПТ XML"
        };
        if (dlg.ShowDialog() == true)
        {
            XDocument doc;
            try
            {
                doc = XDocument.Load(dlg.FileName, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load XML: {ex.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }


            BuildTree(doc);
        }
    }

    private void BuildTree(XDocument doc)
    {
        Roots.Clear();
        var model = XmlIndexBuilder.Build(doc);


        foreach (var root in model.Roots)
        {
            var rootVm = new RootNodeVm(root.DisplayName);
            foreach (var item in root.Items)
                rootVm.Children.Add(new ItemNodeVm(item));


            rootVm.RefreshHeader();
            Roots.Add(rootVm);
        }


        // Auto-select first child if exists
        var firstItem = Roots.SelectMany(r => r.Children).FirstOrDefault();
        if (firstItem != null) OnTreeSelectionChanged(firstItem);
    }

    private void SaveSelected()
    {
        var selectedItems = Roots
            .SelectMany(r => r.Children)
            .Where(c => c.IsSelected)
            .Select(c => c.Model)
            .ToList();


        if (!selectedItems.Any())
        {
            MessageBox.Show("No nodes selected.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }


        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
            Title = "Save selected nodes as XML",
            FileName = "kpt_export.xml"
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                var doc = _exportService.Export(selectedItems);
                doc.Save(dlg.FileName);
                MessageBox.Show("Export completed.", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private static void ShowHelp()
    {
        var sb = new StringBuilder();
        sb.AppendLine("KPT Viewer — краткое руководство:");
        sb.AppendLine("1. Нажмите ‘Open XML’ и выберите файл КПТ.");
        sb.AppendLine(
            "2. В левой панели появятся корневые группы (Parcels, ObjectRealty, SpatialData, Bounds, Zones).");
        sb.AppendLine(
            "3. Под каждой группой — объекты с уникальным идентификатором. Выберите узел, чтобы увидеть полный XML справа.");
        sb.AppendLine(
            "4. Отметьте галочками нужные узлы и нажмите ‘Save selected’ для сохранения выбранных узлов в новый XML (с полной вложенностью).");
        sb.AppendLine();
        sb.AppendLine($"Автор: {AuthorName}");
        sb.AppendLine($"Дата выполнения: 26.08.2025");


        MessageBox.Show(sb.ToString(), "Помощь", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void CopyXml()
    {
        var xml = SelectedXmlPretty;
        if (!string.IsNullOrWhiteSpace(xml))
            Clipboard.SetText(xml);
    }


    private static string Pretty(XElement? element)
    {
        if (element == null) return string.Empty;
        try
        {
            return element.ToString(SaveOptions.None);
        }
        catch
        {
            return element.ToString();
        }
    }
}