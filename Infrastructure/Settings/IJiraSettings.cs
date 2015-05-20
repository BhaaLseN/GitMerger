namespace GitMerger.Infrastructure.Settings
{
    public interface IJiraSettings
    {
        string[] ValidTransitions { get; }
        string[] ValidResolutions { get; }
        string[] ClosedStatus { get; }

        string DisableAutomergeFieldName { get; }
        string DisableAutomergeFieldValue { get; }

        string BaseUrl { get; }
        string UserName { get; }
        string Password { get; }
    }
}
