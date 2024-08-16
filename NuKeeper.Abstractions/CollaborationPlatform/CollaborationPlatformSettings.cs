using NuKeeper.Abstractions.Configuration;

using System;

namespace NuKeeper.Abstractions.CollaborationPlatform
{
    public class CollaborationPlatformSettings
    {
        public Uri BaseApiUrl { get; set; }
        public string Token { get; set; }
        public string Username { get; set; }
        public ForkMode? ForkMode { get; set; }
    }
}
