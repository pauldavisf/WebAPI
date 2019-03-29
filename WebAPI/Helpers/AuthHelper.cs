using System.Security.Claims;
using WebAPI.Models;

namespace WebAPI.Helpers
{
    public static class AuthHelper
    {
        public static bool IsUserAdmin(ClaimsPrincipal currentUser)
        {
            return currentUser.Identity.Name == "admin";
        }

        public static bool IsItemAcceessible(TodoItem item, ClaimsPrincipal currentUser)
        {
            if (IsUserAdmin(currentUser) || item.UserName == currentUser.Identity.Name)
            {
                return true;
            }

            return false;
        }
    }
}
