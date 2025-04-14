using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PluginLoader2.ViewModels;

class ViewModelBase : INotifyPropertyChanged
{
    private readonly Dictionary<string, PropertyChangedEventHandler> childEventHandlers = new Dictionary<string, PropertyChangedEventHandler>();

    protected void OnPropertyChanged([CallerMemberName]string propertyName = null)
    {
        if (propertyName == null)
            return;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        BubbledPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected void OnPropertyChanged(INotifyPropertyChanged from, INotifyPropertyChanged to, [CallerMemberName]string propertyName = null)
    {
        if (propertyName == null)
            return;

        if (!childEventHandlers.TryGetValue(propertyName, out PropertyChangedEventHandler childEventHandler))
        {
            childEventHandler = (o, e) =>
            {
                BubbledPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            };
            childEventHandlers[propertyName] = childEventHandler;
        }

        if (from != null)
            from.PropertyChanged -= childEventHandler;
        to.PropertyChanged += childEventHandler;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        BubbledPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }


    public event PropertyChangedEventHandler PropertyChanged;
    public event PropertyChangedEventHandler BubbledPropertyChanged;
}