namespace InfrastructureApp.ViewModels.Account;

public class ManageUserRolesViewModel
{
    public string UserId { get; set; }
    public string UserName { get; set; }
    public List<RoleSelection> Roles { get; set; } = new List<RoleSelection>();
}