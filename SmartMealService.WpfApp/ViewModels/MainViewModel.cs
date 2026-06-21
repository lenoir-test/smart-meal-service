using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Configuration;
using Serilog;
using SmartMealService.WpfApp.Models;

namespace SmartMealService.WpfApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public ObservableCollection<EnvironmentVariableModel> Variables { get; } = new();

    public MainViewModel(IConfiguration configuration)
    {
        var envVars = configuration.GetSection("EnvironmentVariables").Get<string[]>() ?? Array.Empty<string>();

        foreach (var varName in envVars)
        {
            var existingValue = Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.User);
            var value = existingValue ?? string.Empty;

            var model = new EnvironmentVariableModel
            {
                Field = varName,
                Value = value,
                OriginalValue = value,
                Comment = existingValue == null ? "Значение по умолчанию (создано)" : "Существующая переменная"
            };

            if (existingValue == null)
            {
                // Init default
                SetEnvVar(model.Field, model.Value);
            }

            model.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(EnvironmentVariableModel.Value))
                {
                    var sender = (EnvironmentVariableModel)s!;
                    SetEnvVar(sender.Field, sender.Value);
                    Log.Information("Изменена переменная {Field}: старое значение '{OldValue}', новое значение '{NewValue}'", 
                        sender.Field, sender.OriginalValue, sender.Value);
                    sender.OriginalValue = sender.Value;
                }
            };

            Variables.Add(model);
        }
    }

    public MainViewModel()
    {
        // For Designer
    }

    private void SetEnvVar(string name, string value)
    {
        try
        {
            Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Ошибка при установке переменной среды {Field}", name);
        }
    }
}
