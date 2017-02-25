using System;

namespace GitMerger.Infrastructure.Settings
{
    public class RepositoryInfo
    {
        public string OriginalString { get; private set; }
        public string RelativePath { get; private set; }
        public RepositoryInfo(string original, string relativePath)
        {
            if (string.IsNullOrEmpty(original))
                throw new ArgumentNullException(nameof(original), $"{nameof(original)} is null or empty.");
            if (string.IsNullOrEmpty(relativePath))
                throw new ArgumentNullException(nameof(relativePath), $"{nameof(relativePath)} is null or empty.");
            RelativePath = relativePath;
            OriginalString = original;
        }
    }
}
