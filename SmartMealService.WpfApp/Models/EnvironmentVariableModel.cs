using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartMealService.WpfApp.Models;

public partial class EnvironmentVariableModel : ObservableObject
{
    [ObservableProperty]
    private string _field = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private string _comment = string.Empty;

    public string OriginalValue { get; set; } = string.Empty;
}
