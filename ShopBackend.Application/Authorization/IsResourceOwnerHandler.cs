using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace ShopBackend.Application.Authorization
{
    public class IsResourceOwnerHandler : AuthorizationHandler<IsResourceOwnerRequirement>  
    {
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IsResourceOwnerRequirement requirement)
        {
            var role = context.User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;


        }
    }
}
