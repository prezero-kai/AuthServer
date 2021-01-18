using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace Ids
{
    public class ResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IPasswordHasher<IdentityUser> _passwordHasher;

        public ResourceOwnerPasswordValidator(UserManager<IdentityUser> userManager, IPasswordHasher<IdentityUser> passwordHasher)
        {
            _userManager = userManager;
            _passwordHasher = passwordHasher;
        }
        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            var user = await _userManager.FindByNameAsync(context.UserName);
            //根据context.UserName和context.Password与数据库的数据做校验，判断是否合法
            if (user != null && await _userManager.CheckPasswordAsync(user, context.Password))
            {
                context.Result = new GrantValidationResult(
                    subject: context.UserName,
                    authenticationMethod: OidcConstants.AuthenticationMethods.Password);
            }
            else
            {
                //验证失败
                context.Result = new GrantValidationResult(
                    TokenRequestErrors.InvalidGrant,
                    "invalid custom credential"
                );
            }
        }
    }
}
