using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ShopBackend.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using ShopBackend.Domain.Entities;
using ShopBackend.Application.DTOs;

//Handler funktioniert ggf. nur für GET, PUT und DELETE, nicht aber für POST - WICHTIG, falls es mal wieder im Frontend knallt !!!
namespace ShopBackend.Application.Authorization
{
    public class IsResourceOwnerHandler : AuthorizationHandler<IsResourceOwnerRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUserService _userService; 
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService; 
        private readonly IInvoiceService _invoiceService;   

        public IsResourceOwnerHandler(
            IHttpContextAccessor httpContextAccessor,
            IUserService userService,
            IOrderService orderService,
            ICustomerService customerService,
            IInvoiceService invoiceService)
        {
            _httpContextAccessor = httpContextAccessor;
            _userService = userService;
            _orderService = orderService;
            _customerService = customerService;
            _invoiceService = invoiceService;
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
               return; // Wenn der User nicht existiert(z.b. Gelöscht wurde == null) oder die Rolle eben auf Inaktiv steht --> Abbruch.
            


            // Alle potenziellen IDs aus der Route fischen (z.B. /api/users/{id}
            var routeValues = _httpContextAccessor.HttpContext?.Request.RouteValues;

            // Nur die Main-Ids sind relevant, alle Combi-Controller bekommen im Bedarfsfall interne Security.
            // Der Code hier schützt vor Controllername + Id Kombinationsfehlern, dass z.B. orderId nicht als id erkannt wird. 
            var routeIdString = routeValues?["id"]?.ToString()
                               ?? routeValues?["orderId"]?.ToString()    
                               ?? routeValues?["customerId"]?.ToString() 
                               ?? routeValues?["invoiceId"]?.ToString();

            if (!int.TryParse(routeIdString, out int urlId)) 
                return;


            // Neu mit Switch, ist zwar für die Datenbank nicht so schön und ein Architektur-Schnitzer aber alles andere würde nen komplettes Refactoring benötigen!
            var controllerName = routeValues?["controller"]?.ToString();
            int comparisonUserId = 0;

            switch (controllerName)
            {
                case "User":
                    // Beim UserController ist die URL-ID schon die User-ID.
                    comparisonUserId = urlId;
                    break;

                case "Customer":
                    { // Klammer notwendig um die customer var doppelt nutzen zu können.
                        // Beim CustomerController ist die URL-ID allerdings die CustomerId. Daher:
                        var customer = await _customerService.GetByIdAsync(urlId);
                        if (customer != null)
                            comparisonUserId = customer.UserId;
                    } 
                    break;

                case "Order":
                    {
                        if (routeValues.ContainsKey("customerId"))
                        {
                            var customer = await _customerService.GetByIdAsync(urlId);
                            if (customer != null) comparisonUserId = customer.UserId;
                        }

                        else
                        {
                            var order = await _orderService.GetByIdAsync(urlId);
                            if (order != null && order.Customer != null) comparisonUserId = order.Customer.UserId;
                        }
                    }
                    break;

                case "Invoice":
                    {
                        // Wenn die Route hier /api/invoice/byCustomerId/{customerId} ist:
                        if (routeValues.ContainsKey("customerId"))
                        {
                            // im gegensatz zur orderId muss man hier nicht in die Invoices reinschauen, sondern direkt die Zuordnung abfragen
                            // Gehört die CustomerId dem User?
                            var customer = await _customerService.GetByIdAsync(urlId);
                            if (customer != null)
                                comparisonUserId = customer.UserId;
                        }

                        else if (routeValues.ContainsKey("orderId"))
                        {
                            var invoice = await _invoiceService.GetByOrderIdAsync(urlId);
                            if (invoice != null && invoice.Order?.Customer != null) 
                                comparisonUserId = invoice.Order.Customer.UserId;
                        }

                        else // Wenn Route /api/invoice/{id} ist:
                        {
                            var invoiceById = await _invoiceService.GetByIdAsync(urlId);
                            if (invoiceById != null && invoiceById.Order?.Customer != null)
                                comparisonUserId = invoiceById.Order.Customer.UserId;
                        }
                    }
                    break;

                default:
                    // Wenn ein Controller aufgerufen wird, den der Handler nicht kennt, blockt es hier ab. Kann natürlich erweitert werden
                    return;
            }
 


            // Hierachie-Logik der UserRollen:
            // Man kann die Logik später beliebig erweitern, z.B. SuperAdmins oder CustomerService Staff... Lagermitarbeiter usw.

            // 1. Regel: Admins dürfen immer alles - knackt auch die GetAll() Methoden ohne ID Abfrage!:
            if (userFromDb.Role == UserRole.Admin)
            {
                context.Succeed(requirement);
            }

            // 2. Regel: Der Staff darf Kunden helfen und auf sein eigenes Profil zugreifen, jedoch nicht auf andere Staff Mitglieder oder gar Admins:
            else if (userFromDb.Role == UserRole.Staff)
            {
                // Zielkunden, dem geholfen werden soll mittels UserService die Id aus der URL holen.
                // ACHTUNG: Hier comparisonUserId statt urlId, damit er nach der echten User-ID sucht!
                var targetUser = await _userService.GetByIdAsync(comparisonUserId); // der targetUser hier ist das Zugriffs-Ziel (Der Andere), während der userFromDb weiterhin das "Ich/ServiceMA" darstellt.

                if ((targetUser != null && targetUser.Role == UserRole.Customer) || loggedInUserId == comparisonUserId)
                {
                    context.Succeed(requirement); // Wenn das Ziel tatsächlich die Rolle eines Kunden hat und dieser existiert
                                                  // oder der StaffMember auf sein eigenes Profil (UserId aus dem JWT == Ziel Url Id) zugreift --> Vorraussetzung erfüllt, weitermachen.
                }

            }
            // 3. Regel: Ein Kunde darf nur an seine eigenen Daten:
            else if (userFromDb.Role == UserRole.Customer && loggedInUserId == comparisonUserId)
            {
                context.Succeed(requirement); // Wenn der User die Rolle Kunde hat und seine userId mit der Id des JWT übereinstimmt
            }

        }
    }
}