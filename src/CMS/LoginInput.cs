using System.ComponentModel.DataAnnotations;

namespace CMS
{
    public class LoginInput
    {
        [Required(ErrorMessage = "Username is requried!")]
        public string username { get; set; }

        [Required(ErrorMessage = "Password is requried!")]
        public string password { get; set; }
    }
}