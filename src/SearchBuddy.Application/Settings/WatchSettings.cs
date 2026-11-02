using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.Settings
{
    public class WatchSettings
    {
        public const string SectionName = "Watch";
        public string[] Extensions { get; set; } = [];
        public string[] ExcludedDirectoryNames { get; set; } = [];
    }
}
