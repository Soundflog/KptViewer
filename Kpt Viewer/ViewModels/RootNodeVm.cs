using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace Kpt_Viewer.ViewModels;

public class RootNodeVm(string displayName) : ViewModelBase, INodeVm
{
    public string DisplayName { get; } = displayName;
    public ObservableCollection<ItemNodeVm> Children { get; } = new();
    public XElement? Element => null; // not used for roots


    private string _header = displayName;
    public string Header
    {
        get => _header;
        private set { _header = value; RaisePropertyChanged(); }
    }


    public void RefreshHeader()
    {
        Header = $"{DisplayName} ({Children.Count})";
    }
}
