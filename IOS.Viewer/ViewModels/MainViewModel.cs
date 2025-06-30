using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;

namespace IOS.Viewer.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private int _stepIndex = 1;
    [ObservableProperty] private double _progressValue = 52;
    [ObservableProperty] private bool _isTextVisible = true;
    [ObservableProperty] private bool _isIndeterminate;
}
