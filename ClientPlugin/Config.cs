using ClientPlugin.Settings;
using ClientPlugin.Settings.Elements;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace ClientPlugin;

public class Config : INotifyPropertyChanged
{
    #region Options

    private bool jsonlFormat;
    private bool utcTimestamps;

    #endregion

    #region User interface

    public readonly string Title = "Config - Simple Log";

    [Separator("Log format")]
    [Checkbox(description: "Log in JSONL format (one JSON object on each line)")]
    public bool JsonlFormat
    {
        get => jsonlFormat;
        set => SetField(ref jsonlFormat, value);
    }
    
    [Checkbox(description: "Log UTC timestamps (applied only to the JSONL log)")]
    public bool UtcTimestamps
    {
        get => utcTimestamps;
        set => SetField(ref utcTimestamps, value);
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