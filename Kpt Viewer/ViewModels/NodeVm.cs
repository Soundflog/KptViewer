using System.Collections.ObjectModel;
using System.Xml.Linq;
using Kpt_Viewer.Domain;

namespace Kpt_Viewer.ViewModels;

public interface INodeVm
{
    string Header { get; }
    XElement? Element { get; }
}

public class RootNodeVm : ViewModelBase, INodeVm
{
    public string DisplayName { get; }
    public ObservableCollection<ItemNodeVm> Children { get; } = new();
    public XElement? Element => null; // not used for roots


    private string _header;
    public string Header
    {
        get => _header;
        private set { _header = value; RaisePropertyChanged(); }
    }


    public RootNodeVm(string displayName)
    {
        DisplayName = displayName;
        _header = displayName;
    }


    public void RefreshHeader()
    {
        Header = $"{DisplayName} ({Children.Count})";
    }
}


public class ItemNodeVm : ViewModelBase, INodeVm
{
    public NodeModel Model { get; }
    public string Header => Model.DisplayId;
    public XElement? Element => Model.Element;


    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; RaisePropertyChanged(); }
    }


    public ItemNodeVm(NodeModel model)
    {
        Model = model;
    }
}