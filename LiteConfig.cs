using System.Text.Json;

namespace JesusHack.LiteConfig
{
    public struct JesusHackConfig
    {
        public JesusHackConfig()
        {
            OnlineBlacklist = true;
            LinkToOnlineBlacklist = "https://raw.githubusercontent.com/spersoks142/BlackList/refs/heads/main/Blacklist.txt";
        }

        public bool OnlineBlacklist { get; set; }
        public string LinkToOnlineBlacklist { get; set; }
    }

    public static class ConfigManager
    {
        private const string ConfigPath = "UserData/ChillBlacklist/Config.json";
        public const string BlacklistPath = "UserData/ChillBlacklist/Blacklist.txt";

        public static JesusHackConfig Load()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));

            if (File.Exists(ConfigPath))
            {
                try
                {
                    var json = File.ReadAllText(ConfigPath);

                    return JsonSerializer.Deserialize<JesusHackConfig?>(json) ?? new JesusHackConfig();
                }
                catch { }
            }

            return Save(new JesusHackConfig());
        }

        public static JesusHackConfig Save(JesusHackConfig config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);

            return config;
        }
    }
}
