namespace LittleBigBot.Entities
{
    public class LittleBigBotConfig
    {
        public LittleBigBotConfigRootSection LittleBigBot { get; set; }
        public LittleBigBotConfigDiscordSection Discord { get; set; }
        public LittleBigBotConfigSpotifySection Spotify { get; set; }
        public LittleBigBotConfigGitHubSection GitHub { get; set; }
        public LittleBigBotConfigDatabaseSection Database { get; set; }
    }

    public class LittleBigBotConfigRootSection
    {
        public string Prefix { get; set; }
        public string PlayingStatus { get; set; }
    }

    public class LittleBigBotConfigDiscordSection
    {
        public string Token { get; set; }
    }

    public class LittleBigBotConfigSpotifySection
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
    }

    public class LittleBigBotConfigGitHubSection
    {
        public string Token { get; set; }
        public string Username { get; set; }
    }

    public class LittleBigBotConfigDatabaseSection
    {
        public string Path { get; set; }
    }
}