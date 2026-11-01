using SearchBuddy.Domain.Exceptions;

namespace SearchBuddy.Domain.Entities
{
    public class Document
    {
        public string Path { get; private set; }
        public string ContentHash { get; private set; }
        public IReadOnlyList<Chunk> Chunks { get; private set; }

        public Document(string path, string contentHash, IEnumerable<Chunk> chunks)
        {
            if (string.IsNullOrEmpty(path)) { throw new DomainException($"Can't assign document with empty path. Check {nameof(Document)} mapping."); }
            if (string.IsNullOrEmpty(contentHash)) { throw new DomainException($"Can't assign document with empty ContentHash. Check {nameof(Document)} mapping."); }
            if (!chunks.Any()) { throw new DomainException($"Can't assign document with empty chunks. Should have at lest one. Check {nameof(Document)} mapping."); }

            Path = path;
            ContentHash = contentHash;
            Chunks = chunks.ToList();
        }

        public void ChangePath(string newPath)
        {
            if (String.IsNullOrWhiteSpace(newPath)) { throw new DomainException("Can't assign empty path to document."); }
            this.Path = newPath;
        }
    }
}
