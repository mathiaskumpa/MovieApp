﻿using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using MovieApp.Models.Domain;
using MovieApp.Models.DTO;
using MovieApp.Repositories.Abstract;

namespace MovieApp.Repositories.Implemementation
{
    public class UserAuthenticationService : IUserAuthenticationService
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public UserAuthenticationService(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }

        public async Task<Status> LoginAsync(LoginModel model)
        {
            var status = new Status();
            var user = await userManager.FindByNameAsync(model.UserName);   
            if(user == null) 
            {
                status.StatusCode = 0;
                status.Message = "Invalid Username"; 
                return status;
            }
            //matching the password
            if (!await userManager.CheckPasswordAsync(user, model.Password))
            {
                status.StatusCode = 0;
                status.Message = "Invalid Password";
                return status;
            }
            var signInResult = await signInManager.PasswordSignInAsync(user, model.Password, false, true);
            if (signInResult.Succeeded) 
            { 
                var userRoles = await userManager.GetRolesAsync(user);
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName)
                };
                 foreach(var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }
                status.StatusCode = 1;
                status.Message = "Logged in Successifully";
                return status;

            }
            else if(signInResult.IsLockedOut)
            {
                status.StatusCode = 0;
                status.Message = "User Locked out";
                return status;
            }
            else
            {
                status.StatusCode = 0;
                status.Message = "Error on Loggin in";
                return status;
            }
            
        }

        public async Task LogoutAsync()
        {
            await signInManager.SignOutAsync();
        }

        public async Task<Status> RegistrationAsync(RegistrationModel model)
        {
           var status = new Status();
            var userExists = await userManager.FindByNameAsync(model.Username);
            if (userExists != null) 
            {
                status.StatusCode = 0;
                status.Message = "User already Exists";
                return status;
            }

            ApplicationUser user = new ApplicationUser
            {
                SecurityStamp = Guid.NewGuid().ToString(),
                Name = model.Name,
                Email = model.Email,
                UserName = model.Username,
                EmailConfirmed = true,

            };

            var result = await userManager.CreateAsync(user, model.Password); 
            if (!result.Succeeded)
            {
                status.StatusCode = 0;
                status.Message = "User Creation Failed";
                return status;
            }
            //role management
            if (!await roleManager.RoleExistsAsync(model.Role))
                await roleManager.CreateAsync(new IdentityRole(model.Role));
            if(await roleManager.RoleExistsAsync(model.Role))
            {
                await userManager.AddToRoleAsync(user, model.Role);
            }
            status.StatusCode = 1;
            status.Message = "User has Registered Successifully";
            return status;
        }
    }
}
