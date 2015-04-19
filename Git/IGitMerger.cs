namespace GitMerger.Git
{
    public interface IGitMerger
    {
        void QueueRequest(MergeRequest mergeRequest);
    }
}
