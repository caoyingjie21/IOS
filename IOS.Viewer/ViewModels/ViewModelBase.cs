using CommunityToolkit.Mvvm.ComponentModel;
using System;
using Material.Icons;

namespace IOS.Viewer.ViewModels;

public class ViewModelBase : ObservableObject
{
    protected IServiceProvider? Services { get; set; } = null;
    public ViewModelBase(IServiceProvider serviceProvider)
    {
        this.Services = Services;
    }
}
