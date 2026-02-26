using System.ComponentModel;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Community.VisualStudio.Toolkit;

namespace MicaVisualStudio.Options;

public abstract class ObservableOptionModel<TSelf> : BaseOptionModel<TSelf>, INotifyPropertyChanged
    where TSelf : ObservableOptionModel<TSelf>, new()
{
    public event PropertyChangedEventHandler PropertyChanged;

    private bool IsLoading => _loadDepth > 0;
    private int _loadDepth;

    protected void SetValue<T>(ref T field, T value, [CallerMemberName] string name = "")
    {
        field = value;

        if (!IsLoading)
        {
            Save();
        }

        OnPropertyChanged(name);
    }

    public override async Task LoadAsync()
    {
        ++_loadDepth;
        try
        {
            await base.LoadAsync();
        }
        finally
        {
            --_loadDepth;
        }
    }

    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new(propertyName));
}
