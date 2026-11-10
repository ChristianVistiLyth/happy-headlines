namespace NewsletterService;

/// <summary>
/// Names of the feature toggles, as written in toggles.json under "FeatureManagement".
/// </summary>
public static class Toggles
{
    /// <summary>
    /// Release toggle (Fowler): SubscriberService is deployed, but only released while this toggle is on.
    /// It is flipped in toggles.json while everything is running - no rebuild, no restart, no redeploy.
    /// </summary>
    public const string SubscriberService = "SubscriberService";
}
