using Application.Api.Extensions;
using Application.Api.Models;
using Application.Infastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/sidebar")]
    public class SideBarController : ControllerBase
    {
        private readonly DatabaseContext _databaseContext;

        public SideBarController(DatabaseContext databaseContext)
        {
            _databaseContext = databaseContext;
        }

        [HttpGet("communities")]
        public IActionResult GetUserCommunities()
        {
            var userId = User.GetId(); // Custom extension method for user ID

            var communities = _databaseContext.UserCommunities
                .Include(uc => uc.Community)
                .Where(uc => uc.UserId == userId && uc.Community != null)  // Ensure that Community is not null
                .Select(uc => new CommunityViewModel
                {
                    Name = uc.Community.Name,
                    // Safely generating ImageUrl if Community is not null
                    ImageUrl = uc.Community.Picture != null ? 
                               Url.Action("GetCommunityImage", "SideBar", new { communityId = uc.Community.Id }) 
                               : "/Images/GenericCommunity.png", // Default image URL for null images
                    Dot = true,  // Change this condition based on your logic (e.g., new notifications)
                    Link = $"{uc.Community.Id}"  // Link to the community page
                })
                .ToList();

            return Ok(communities);
        }

        // Community image retrieval within the same controller
        [HttpGet("community/image/{communityId}")]
        public IActionResult GetCommunityImage(Guid communityId)
        {
            var community = _databaseContext.Communities.FirstOrDefault(c => c.Id == communityId);
            if (community?.Picture == null) return NotFound();
            Response.Headers.CacheControl = "public,max-age=31536000";
            return File(community.Picture, "image/jpeg");
        }
    }
}

