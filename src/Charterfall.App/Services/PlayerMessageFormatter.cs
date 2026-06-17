namespace Charterfall.App.Services;

public sealed class PlayerMessageFormatter : IPlayerMessageFormatter
{
    public string MissingRunForInquiry() =>
        "No PolityKit run has been created yet. Draft a charter and resolve the crisis before opening the inquiry.";

    public string MissingAmendedRunForComparison() =>
        "No amended run is available yet. Amend the charter before comparing outcomes, or continue with the clearly marked placeholder comparison.";

    public string IntegrationPending() =>
        "Integration pending: this surface is wired for the prototype loop, but these values are not backed by deterministic PolityKit output yet.";
}
