using Microsoft.AspNetCore.Mvc;
using Student_Inquiry_Assistance_Portal.Models;

namespace Student_Inquiry_Assistance_Portal.Services
{
    public interface IAuth
    {
        Task<IActionResult> Register([FromBody] RegisterUser registerUser, string role);
        Task<IActionResult> Login([FromBody] LoginModel loginModel);
    }

   
}
