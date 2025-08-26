using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using Kpt_Viewer.Infrastructure;
using Kpt_Viewer.Services;
using Microsoft.Win32;
using System.Collections.Generic;
using Kpt_Viewer.Domain;

namespace Kpt_Viewer.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly XmlIndexBuilder _indexBuilder = new();
    private readonly ExportService _exportService = new();


    public ObservableCollection<RootNodeVm> Roots { get; } = new();
    private object? _selectedNode;
    private List<RootItems> _allData = new();
    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText == value) return;
            _searchText = value;
            RaisePropertyChanged();
            ApplyFilter();
        }
    }
    
    public string SelectedHeader => (_selectedNode as INodeVm)?.Header ?? string.Empty;
    public string SelectedXmlPretty => Pretty((_selectedNode as INodeVm)?.Element);


    public ICommand OpenFileCommand { get; }
    public ICommand SaveSelectedCommand { get; }
    public ICommand ShowHelpCommand { get; }
    public ICommand CopyXmlCommand { get; }
    public ICommand ExitCommand { get; }
    public ICommand ClearSearchCommand { get; }
    

    private const string AuthorName = "Vershinin Pavel A., tg: @soundflogdev";


    public MainViewModel()
    {
        OpenFileCommand = new RelayCommand(_ => OpenFile());
        SaveSelectedCommand = new RelayCommand(_ => SaveSelected(),
            _ => Roots.SelectMany(r => r.Children).Any(c => c.IsSelected));
        ShowHelpCommand = new RelayCommand(_ => ShowHelp());
        CopyXmlCommand = new RelayCommand(_ => CopyXml(), _ => (_selectedNode as INodeVm)?.Element != null);
        ExitCommand = new RelayCommand(_ => Application.Current.Shutdown());
        ClearSearchCommand = new RelayCommand(_ => { SearchText = string.Empty; });
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
        var model = _indexBuilder.Build(doc);

        // Сохраняем исходные данные для фильтрации
        _allData = model.Roots.ToList();

        // Применяем текущий фильтр
        ApplyFilter();
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
        
        var dlg = new SaveFileDialog
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

    // По мере ввода текст сразу фильтрует список по DisplayId (т.е. по cad_number/sk_id/reg_numb_border и т.п.).
    private void ApplyFilter()
    {
        Roots.Clear();
        var query = (SearchText ?? string.Empty).Trim();
        bool hasQuery = query.Length > 0;
        StringComparison cmp = StringComparison.OrdinalIgnoreCase;

        foreach (var root in _allData)
        {
            var rootVm = new RootNodeVm(root.DisplayName);
            IEnumerable<NodeModel> items = root.Items;
            if (hasQuery)
                items = items.Where(i => i.DisplayId?.IndexOf(query, cmp) >= 0);

            foreach (var item in items)
                rootVm.Children.Add(new ItemNodeVm(item));

            if (rootVm.Children.Count > 0 || !hasQuery)
            {
                rootVm.RefreshHeader();
                Roots.Add(rootVm);
            }
        }

        // Обновить доступность команд (например, Save selected)
        CommandManager.InvalidateRequerySuggested();

        // Автовыбор первого найденного
        var firstItem = Roots.SelectMany(r => r.Children).FirstOrDefault();
        OnTreeSelectionChanged(firstItem);
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