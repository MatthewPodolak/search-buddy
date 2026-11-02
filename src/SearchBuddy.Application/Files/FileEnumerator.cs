using Microsoft.Extensions.Options;
using SearchBuddy.Application.Settings;
using System;
using System.Collections.Generic;
using System.IO.Enumeration;
using System.Text;
using static System.Net.WebRequestMethods;

namespace SearchBuddy.Application.Files
{
    public class FileEnumerator : IFileEnumerator
    {
        private readonly PathFilter _filter;
        private static readonly EnumerationOptions Options = new()
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Offline,
            ReturnSpecialDirectories = false,
            BufferSize = 64 * 1024
        };

        public FileEnumerator(PathFilter filter)
        {
            _filter = filter;
        }
        public IEnumerable<string> GetDrives()
        {
            return DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady).Select(d => d.RootDirectory.FullName);
        }

        public IEnumerable<string> Enumerate(CancellationToken ct)
        {
            foreach (var drive in GetDrives())
            {
                var files = new FileSystemEnumerable<string>(drive, (ref FileSystemEntry e) => e.ToFullPath(), Options)
                {
                    ShouldIncludePredicate = (ref FileSystemEntry e) => !e.IsDirectory && _filter.HasIndexedExtension(e.FileName),
                    ShouldRecursePredicate = (ref FileSystemEntry e) => !_filter.IsExcludedDirectory(e.FileName)
                };

                foreach (var path in files)
                {
                    ct.ThrowIfCancellationRequested();
                    yield return path;
                }
            }
        }
    }
}