using Microsoft.Extensions.Options;
using SearchBuddy.Application.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.Files
{
    public class PathFilter
    {
        private readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _extensions;
        private readonly HashSet<string>.AlternateLookup<ReadOnlySpan<char>> _excludedNames;

        public PathFilter(IOptions<WatchSettings> settings)
        {
            _extensions = new HashSet<string>(settings.Value.Extensions, StringComparer.OrdinalIgnoreCase).GetAlternateLookup<ReadOnlySpan<char>>();
            _excludedNames = new HashSet<string>(settings.Value.ExcludedDirectoryNames, StringComparer.OrdinalIgnoreCase).GetAlternateLookup<ReadOnlySpan<char>>();
        }

        public bool HasIndexedExtension(ReadOnlySpan<char> fileName) 
        {
           return _extensions.Contains(Path.GetExtension(fileName));
        }

        public bool IsExcludedDirectory(ReadOnlySpan<char> name)
        {
           return _excludedNames.Contains(name);
        }
            

        public bool ShouldIndex(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) { return false; }

            var span = path.AsSpan();
            if (!HasIndexedExtension(span))
            {
                return false;
            }

            foreach (var range in span.Split('\\'))
            {
                if (_excludedNames.Contains(span[range]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
