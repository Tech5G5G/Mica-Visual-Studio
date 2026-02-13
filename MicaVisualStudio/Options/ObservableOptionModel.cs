using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using Community.VisualStudio.Toolkit;

namespace MicaVisualStudio.Options;

public abstract class ObservableOptionModel<TSelf> : BaseOptionModel<TSelf>, INotifyPropertyChanged
    where TSelf : ObservableOptionModel<TSelf>, new()
{
    public event PropertyChangedEventHandler PropertyChanged;

    public override async Task LoadAsync()
    {
        var properties = GetPropertyWrappers().OfType<OptionModelPropertyWrapper>()
                                              .Select(i => i.PropertyInfo)
                                              .ToArray();

        var originalValues = properties.Select(p => p.GetValue(this)).ToArray();

        await base.LoadAsync();

        for (int i = 0; i < properties.Length; ++i)
        {
            var property = properties[i];

            if (property.GetValue(this) != originalValues[i])
            {
                OnPropertyChanged(property.Name);
            }
        }
    }

    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new(propertyName));
}
