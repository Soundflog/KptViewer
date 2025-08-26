using System.Xml.Linq;

namespace Kpt_Viewer.ViewModels;

public interface INodeVm
{
    string Header { get; }
    XElement? Element { get; }
}