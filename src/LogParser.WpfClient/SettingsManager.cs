using LogParser.App;
using LogParser.ElmishApp.Interfaces;
using LogParser.WpfClient.Properties;

namespace LogParser.WpfClient;

public class SettingsManager : ISettingsManager
{
    static private SettingsManager? _settingsManager;
    static public ISettingsManager Instance => _settingsManager ??= new SettingsManager(); 

    public SettingsManager()
    {
    }

    public object Load(string key)
    {
        return Settings.Default[key];
    }

    public void Save(string key, object value)
    {
        Settings.Default[key] = value;
        Settings.Default.Save();
        Settings.Default.Reload();
    }

    public AppConfig AppConfig
    {
        get
        {
            var parserSubscriptionBatchSize = Settings.Default.ParserSubscriptionBatchSize;
            var parserBatchFlushTimeSpan = Settings.Default.ParserBatchFlushTimeSpan;
            return new AppConfig(parserSubscriptionBatchSize, parserBatchFlushTimeSpan);
        }
    }
}
