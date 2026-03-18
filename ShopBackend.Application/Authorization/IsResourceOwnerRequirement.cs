using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace ShopBackend.Application.Authorization
{
    public class IsResourceOwnerRequirement : IAuthorizationRequirement
    {
        // Bleibt leer, da es nur ein leeres Marker-Interface von Microsoft ist --> Dient nur als Markierung für den Handler
    }
}
