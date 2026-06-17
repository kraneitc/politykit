namespace Charterfall.App.Services;

public interface IPlayerMessageFormatter
{
    string MissingRunForInquiry();

    string MissingAmendedRunForComparison();

    string IntegrationPending();
}
