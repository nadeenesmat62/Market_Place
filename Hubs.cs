using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using JWTRefreshTokenInDotNet6.Models;
using Microsoft.AspNetCore.Identity;


namespace JWTRefreshTokenInDotNet6.Hubs
{
public class NotificationHub : Hub
{
    // public override async Task OnConnectedAsync()
    // {
    //     var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    //     if (!string.IsNullOrEmpty(userId))
    //     {
    //         await Groups.AddToGroupAsync(Context.ConnectionId, "User-" + userId);
    //         Console.WriteLine($"📡 User connected: {userId}");
    //         var userManager = Context.GetHttpContext().RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
    //         var user = await userManager.FindByIdAsync(userId);

    //         if (await userManager.IsInRoleAsync(user, "Admin"))
    //         {
    //             await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
    //             Console.WriteLine($"🛡️ Admin user added to Admins group: {user.UserName}");
    //         }
    //     }

    //     await base.OnConnectedAsync();
    // }
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;


        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine("❌ No user ID found in JWT claims. Connection rejected.");
            await base.OnConnectedAsync();
            return;
        }

        Console.WriteLine($"🔑 Connecting user ID: {userId}");

        var httpContext = Context.GetHttpContext();
        if (httpContext == null)
        {
            Console.WriteLine("❌ HttpContext is null. Cannot continue.");
            await base.OnConnectedAsync();
            return;
        }

        var userManager = httpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);

        if (user == null)
        {
            Console.WriteLine($"❌ No user found in database for ID: {userId}");
            await base.OnConnectedAsync();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, "User-" + userId);
        Console.WriteLine($"📡 User connected and added to group: User-{userId}");

        var isAdmin = await userManager.IsInRoleAsync(user, "Admin");
        if (isAdmin)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            Console.WriteLine($"🛡️ Admin user added to Admins group: {user.UserName}");
        }

        await base.OnConnectedAsync();
    }
}

}


