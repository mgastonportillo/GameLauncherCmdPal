using System;

namespace GameLauncherCmdPal.Models
{
    public class Game
    {
        public string Name { get; set; }
        public string Uri { get; set; }
        public string? IconPath { get; set; }
        public string Platform { get; set; }
        // public string UninstallAction { get; set; }
        public int RecentScore { get; set; }
        public bool Hidden { get; set; }

        public Game(string name, string uri, string platform, string? iconPath = null)
        {
            Name = name;
            Uri = uri;
            Platform = platform;
            IconPath = iconPath;
            RecentScore = 0;
            Hidden = false;
        }

        public void UpdateRecentScore()
        {
            RecentScore = Environment.TickCount;
        }
    }
}

