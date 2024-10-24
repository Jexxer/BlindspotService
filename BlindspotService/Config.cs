﻿namespace App.WindowsService
{
    public class Config
    {
        public string base_url { get; set; } = "https://api.blindspotdev.com";
        public string? api_key { get; set; }
        public string? agent_install_uuid { get; set; }
        public string? email { get; set; }
        public string? path { get; set; }
    }
}
