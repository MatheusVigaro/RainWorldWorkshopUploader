using Steamworks;

public class WorkshopDataClass
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ID { get; set; }
    public string? Version { get; set; }
    public string? TargetGameVersion { get; set; }
    public string? Requirements { get; set; }
    public string? RequirementNames { get; set; }
    public string? Authors { get; set; }
    public string? Visibility { get; set; }
    public string[]? Tags { get; set; }
    public ulong WorkshopID { get; set; }

    public static ERemoteStoragePublishedFileVisibility VisibilityFromText(string text)
    {
        switch (text)
        {
            case "Public":
                return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic;
            case "Friends-only":
                return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityFriendsOnly;
            case "Hidden":
                return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate;
            default:
                return ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityUnlisted;
        }
    }

    public static string TextFromVisibility(ERemoteStoragePublishedFileVisibility visibility)
    {
        switch (visibility)
        {
            case ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic:
                return "Public";
            case ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityFriendsOnly:
                return "Friends-only";
            case ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate:
                return "Hidden";
            default:
                return "Unlisted";
        }
    }
}