using JobFlow.Domain;
using JobFlow.Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobFlow.API.Controllers
{
    [ApiController]
    [Route("i")]
    public class InviteRedirectController : ControllerBase
    {
        private readonly IRepository<EmployeeInvite> _invites;

        public InviteRedirectController(IRepository<EmployeeInvite> invites)
        {
            _invites = invites;
        }

        [HttpGet("{code}")]
        public async Task<IActionResult> RedirectInvite(string code)
        {
            var invite = await _invites.Query()
                .FirstOrDefaultAsync(i => i.ShortCode == code && !i.IsRevoked && !i.IsAccepted);

            if (invite is null || invite.ExpiresAt < DateTime.UtcNow)
                return NotFound("Invite not found or expired.");

            invite.AccessCount++;
            invite.AccessedAt = DateTime.UtcNow;
            invite.AccessIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            _invites.Update(invite);

            var redirectUrl = $"https://jobflow.katharix.com/invite/{invite.InviteToken}";
            return Redirect(redirectUrl);
        }
    }

}
