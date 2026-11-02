using System;
using System.Collections.Generic;
using System.Text;

namespace SearchBuddy.Application.Files
{
    public interface IFileEnumerator
    {
        IEnumerable<string> GetDrives();
        IEnumerable<string> Enumerate(CancellationToken ct);
    }
}
