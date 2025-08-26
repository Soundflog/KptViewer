using System.Xml.Linq;
using Kpt_Viewer.Domain;

namespace Kpt_Viewer.ViewModels;

public class ItemNodeVm(NodeModel model) : ViewModelBase, INodeVm
{
    public NodeModel Model { get; } = model;
    public string Header => Model.DisplayId;
    public XElement? Element => Model.Element;


    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; RaisePropertyChanged(); }
    }
}