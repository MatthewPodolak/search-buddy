using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Infrastructure.Persistance
{
    public class DatabaseSettings
    {
        public const string SectionName = "Database";

        public string Path { get; set; } = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "SearchBuddy", "index.db");
    }
}
