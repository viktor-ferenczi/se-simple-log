using ClientPlugin.Settings;
using ClientPlugin.Settings.Elements;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace ClientPlugin;

public class Config : INotifyPropertyChanged
{
    #region Options

    private bool jsonl;

    #endregion

    #region User interface

    public readonly string Title = "Config - Simple Log";

    [Separator("Some settings")]
    [Checkbox(description: "Log in JSONL format (one JSON object on each line)")]
    public bool Toggle
    {
        get => jsonl;
        set => SetField(ref jsonl, value);
    }

    #endregion

    #region Property change notification boilerplate

    public static readonly Config Default = new();
    public static readonly Config Current = ConfigStorage.Load();

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}