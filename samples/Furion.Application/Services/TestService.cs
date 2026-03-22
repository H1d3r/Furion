namespace Furion.Application.Services;

internal class TestService : ITestService, ITransient
{
    public string GetName()
    {
        return "Furion";
    }
}