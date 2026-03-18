using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ShopBackend.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using ShopBackend.Domain.Entities;

namespace ShopBackend.Application.Authorization
{
    public class IsResourceOwnerHandler : AuthorizationHandler<IsResourceOwnerRequirement>  
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserService _userService; // Die Datenbankverbindung

        public IsResourceOwnerHandler(IHttpContextAccessor httpContextAccessor, IUserService userService)
        {
            _httpContextAccessor = httpContextAccessor;
            _userService = userService;
        }

        // überschreibt die StandardMethode von Microsoft im ResourceRequirement
        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IsResourceOwnerRequirement requirement)
        {
            // Holt sich die Daten aus dem JWT Token, die zur Zugangsprüfung notwendig sind: Id und Role
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int loggedInUserId))
            {
                return; // Wenn kein gültiger User-Claim existiert -> Abbruch. 
            }

            
            // Check gegen die DB, um Inaktive Mitarbeiter auszuschließen, da deren Token noch gültig sein könnte:
            var userFromDb = await _userService.GetByIdAsync(loggedInUserId); // der userFromDb ist sozusagen das "ICH", der Handelnde.

            if (userFromDb == null || userFromDb.Role == UserRole.Inactive) 
            { 
                return; // Wenn der User nicht existiert(z.b. Gelöscht wurde == null) oder die Rolle eben auf Inaktiv steht --> Abbruch.
            }

            // Ziel-Id aus der URL holen (z.B. /api/users/{id}
            var routeIdString = _httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
            int.TryParse(routeIdString, out int targetResourceId);


            //               Hierachie-Logik der UserRollen anwenden:
            // Man kann die Logik beliebig erweitern, z.B. SuperAdmins oder CustomerService Staff... Lagermitarbeiter usw.


            // 1. Regel: Admins dürfen immer alles:
            if (userFromDb.Role == UserRole.Admin)
            {
                context.Succeed(requirement);
            }


            // 2. Regel: Der Staff darf Kunden helfen und auf sein eigenes Profil zugreifen, jedoch nicht auf andere Staff Mitglieder oder gar Admins:
            else if (userFromDb.Role == UserRole.Staff)
            {
                // Zielkunden, dem geholfen werden soll mittels UserService die Id aus der URL holen (targetResourceId)
                var targetUser = await _userService.GetByIdAsync(targetResourceId); // der targetUser hier ist das Zugriffs-Ziel (Der Andere), während der userFromDb weiterhin das "Ich" darstellt.

                if ((targetUser != null && targetUser.Role == UserRole.Customer) || loggedInUserId == targetResourceId)
                {
                    context.Succeed(requirement); // Wenn das Ziel tatsächlich die Rolle eines Kunden hat und dieser existiert
                                                  // oder der StaffMember auf sein eigenes Profil (UserId aus dem JWT == Ziel Url Id) zugreift --> Vorraussetzung erfüllt, weitermachen.
                }


            // 3. Regel: Ein Kunde darf nur an seine eigenen Daten:
             else if (userFromDb.Role == UserRole.Customer && loggedInUserId == targetResourceId)
                {
                    context.Succeed(requirement); // Wenn der User die Rolle Kunde hat und seine userId mit der Id des JWT übereinstimmt
                }
            }

        }
    }
}
