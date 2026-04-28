using System.ComponentModel.DataAnnotations;

namespace InfrastructureApp.ViewModels.Account
{
    public class DeleteAccountViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;
    }
}
