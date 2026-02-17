using System.Linq;
using System.Reflection;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using Community.VisualStudio.Toolkit;

namespace MicaVisualStudio.Options;

public abstract class ObservableOptionModel<TSelf> : BaseOptionModel<TSelf>, INotifyPropertyChanged
    where TSelf : ObservableOptionModel<TSelf>, new()
{
    public event PropertyChangedEventHandler PropertyChanged;

    private readonly List<PropertyInfo> _properties = [];

    private object[] _originalValues;

    public ObservableOptionModel() =>
        _properties.AddRange([.. GetPropertyWrappers().OfType<OptionModelPropertyWrapper>().Select(i => i.PropertyInfo)]);

    public override async Task LoadAsync()
    {
        await base.LoadAsync();
        _originalValues = [.. _properties.Select(p => p.GetValue(this))];
    }

    public override Task SaveAsync()
    {
        if (_originalValues is not null)
        {
            for (int i = 0; i < _properties.Count; ++i)
            {
                var property = _properties[i];

                if (_originalValues[i] != (_originalValues[i] = property.GetValue(this)))
                {
                    OnPropertyChanged(property.Name);
                }
            }
        }

        return base.SaveAsync();
    }

    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new(propertyName));
}
