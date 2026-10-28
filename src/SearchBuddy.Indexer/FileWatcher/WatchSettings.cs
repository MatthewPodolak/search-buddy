using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Indexer.FileWatcher
{
    internal class WatchSettings
    {
        public const string SectionName = "Watch";
        public string[] Extensions { get; set; } = [];
    }
}
