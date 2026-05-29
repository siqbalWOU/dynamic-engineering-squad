namespace InfrastructureApp.Services
{
    public interface IIssueNameService
    {
        Task<bool> AssignNameAsync(int reportId, string name);
    }
}
