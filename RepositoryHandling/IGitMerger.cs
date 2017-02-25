namespace GitMerger.RepositoryHandling
{
    public interface IGitMerger
    {
        void QueueRequest(MergeRequest mergeRequest);
    }
}
