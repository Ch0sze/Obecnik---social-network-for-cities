using Application.Api.Extensions;
using Application.Api.Models;
using Application.Infastructure.Database;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.Controllers;

[Route("/")]
public class HomeController(DatabaseContext databaseContext, ILogger<HomeController> logger) : Controller
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Index(Guid? communityId, Guid? channelId, Guid? openPostId, bool? onlyPetitions)
    {
        var userId = User.GetId();
        var user = await databaseContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return RedirectToAction("Login", "Account");

        if (user.Role == "Banned")
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account", new { error = "Banned", message = "Váš účet byl zabanován." });
        }

        var availableCommunityIds = await databaseContext.UserCommunities
            .Where(uc => uc.UserId == userId)
            .Select(uc => uc.CommunityId)
            .ToListAsync();

        var selectedCommunityId = communityId.HasValue && availableCommunityIds.Contains(communityId.Value)
            ? communityId.Value
            : availableCommunityIds.FirstOrDefault();

        var communityName = await databaseContext.Communities
            .Where(c => c.Id == selectedCommunityId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync() ?? "No Community";

        var availableChannelIds = await databaseContext.Channels
            .Where(c => c.CommunityId == selectedCommunityId)
            .Select(c => c.Id)
            .ToListAsync();

        var selectedChannelId = channelId.HasValue && availableChannelIds.Contains(channelId.Value)
            ? channelId.Value
            : availableChannelIds.FirstOrDefault();

        var isCommunityAdmin = await databaseContext.CommunityAdmins
            .AnyAsync(ca => ca.UserId == userId && ca.CommunityId == selectedCommunityId);

        var adminRole = user.Role == "Admin";

        logger.LogInformation("User {UserId} is Community Admin: {IsCommunityAdmin}, Admin Role: {AdminRole}", userId,
            isCommunityAdmin,
            adminRole);

        var postsQuery = databaseContext.Posts
            .Include(post => post.User)
            .Where(post => post.ChannelId == selectedChannelId)
            .OrderByDescending(post => post.CreatedAt)
            .Take(10);

        // Apply filter if 'onlyPetitions' is true
        if (onlyPetitions.HasValue && onlyPetitions.Value)
        {
            postsQuery = postsQuery.Where(post => post.Type == "Petition");
        }

        var posts = await postsQuery
            .Select(post => new HomeViewModel.Post
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                CreatedAt = post.CreatedAt,
                CreatedBy = post.User!.Firstname + " " + post.User!.LastName,
                Photo = post.Photo != null,
                IsAdmin = isCommunityAdmin && adminRole,
                Type = post.Type,
                IsPinned = post.IsPinned,
                CreatedById = post.User!.Id,
                UserHasPhoto = post.User!.Picture != null,
                HasUserSigned = databaseContext.PetitionSignatures
                    .Any(sig => sig.PostId == post.Id && sig.UserId == userId)
            })
            .ToListAsync();

        var channels = await databaseContext.Channels
            .Where(c => c.CommunityId == selectedCommunityId)
            .Select(c => new ChannelViewModel
            {
                Id = c.Id,
                Name = c.Name
            })
            .ToListAsync();

        HomeViewModel.Post? openedPost = null;
        if (openPostId.HasValue)
        {
            var postEntity = await databaseContext.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == openPostId.Value);

            if (postEntity != null)
            {
                var postCommunityId = await databaseContext.Channels
                    .Where(c => c.Id == postEntity.ChannelId)
                    .Select(c => c.CommunityId)
                    .FirstOrDefaultAsync();

                var isPostCommunityAdmin = await databaseContext.CommunityAdmins
                    .AnyAsync(ca => ca.UserId == userId && ca.CommunityId == postCommunityId);

                openedPost = new HomeViewModel.Post
                {
                    Id = postEntity.Id,
                    Title = postEntity.Title,
                    Description = postEntity.Description,
                    CreatedAt = postEntity.CreatedAt,
                    CreatedBy = postEntity.User!.Firstname + " " + postEntity.User!.LastName,
                    Photo = postEntity.Photo != null,
                    Type = postEntity.Type,
                    IsAdmin = isPostCommunityAdmin && adminRole,
                    CreatedById = postEntity.User.Id,
                    UserHasPhoto = postEntity.User.Picture != null,
                    HasUserSigned = databaseContext.PetitionSignatures
                        .Any(sig => sig.PostId == postEntity.Id && sig.UserId == userId)
                };
            }
        }

        var homeViewModel = new HomeViewModel
        {
            Posts = posts,
            CommunityName = communityName,
            CommunityId = selectedCommunityId,
            OpenedPost = openedPost,
            Channels = channels,
            SelectedChannelId = selectedChannelId
        };

        var accountViewModel = new AccountViewModel
        {
            Email = user?.Email ?? string.Empty,
            Name = $"{user?.Firstname} {user?.LastName}",
            Hometown = $"{user?.Residence}, {user?.PostalCode}"
        };

        var combinedViewModel = new CombinedViewModel
        {
            AccountViewModel = accountViewModel,
            HomeViewModel = homeViewModel
        };

        return View(combinedViewModel);
    }

    [Authorize]
    [HttpGet("load-posts")]
    public async Task<IActionResult> LoadPosts(int pageNumber, int pageSize, Guid? communityId, Guid? channelId,
        bool? onlyPetitions)
    {
        var userId = User.GetId();
        var user = await databaseContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return Unauthorized();

        var validCommunityIds = await databaseContext.UserCommunities
            .Where(uc => uc.UserId == user.Id)
            .Select(uc => uc.CommunityId)
            .ToListAsync();

        var selectedCommunityId = communityId.HasValue && validCommunityIds.Contains(communityId.Value)
            ? communityId.Value
            : validCommunityIds.FirstOrDefault();

        var availableChannelIds = await databaseContext.Channels
            .Where(c => c.CommunityId == selectedCommunityId)
            .Select(c => c.Id)
            .ToListAsync();

        var selectedChannelId = channelId.HasValue && availableChannelIds.Contains(channelId.Value)
            ? channelId.Value
            : Guid.Empty;

        if (selectedChannelId == Guid.Empty)
            return PartialView("_PostsPartial", new List<HomeViewModel.Post>());

        var selectedChannel = await databaseContext.Channels.FirstOrDefaultAsync(c => c.Id == selectedChannelId);

        bool filterPetitionsOnly =
            onlyPetitions.HasValue && onlyPetitions.Value; // Apply filter from the query parameter

        var isCommunityAdmin = await databaseContext.CommunityAdmins
            .AnyAsync(ca => ca.UserId == userId && ca.CommunityId == selectedCommunityId);

        var adminRole = user?.Role == "Admin";

        var postsQuery = databaseContext.Posts
            .Include(post => post.User)
            .Where(post => post.ChannelId == selectedChannelId);

        // Apply filter if 'onlyPetitions' is true
        if (filterPetitionsOnly)
        {
            postsQuery = postsQuery.Where(post => post.Type == "Petition");
        }

        var posts = await postsQuery
            .OrderByDescending(post => post.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(post => new HomeViewModel.Post
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                CreatedAt = post.CreatedAt,
                CreatedBy = post.User!.Firstname + " " + post.User!.LastName,
                Photo = post.Photo != null,
                IsAdmin = isCommunityAdmin && adminRole,
                Type = post.Type,
                IsPinned = post.IsPinned,
                CreatedById = post.User!.Id,
                UserHasPhoto = post.User!.Picture != null,
                HasUserSigned = databaseContext.PetitionSignatures
                    .Any(sig => sig.PostId == post.Id && sig.UserId == userId)
            })
            .ToListAsync();

        return PartialView("_PostsPartial", posts);
    }


    [Authorize]
    [HttpPost("toggle-pin/{postId}")]
    public async Task<IActionResult> TogglePin(Guid postId)
    {
        var userId = User.GetId();
        var user = await databaseContext.Users.FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            return Unauthorized();

        var post = await databaseContext.Posts.FirstOrDefaultAsync(p => p.Id == postId);

        if (post == null)
            return NotFound();

        // Check if user is admin
        var communityId = await databaseContext.UserCommunities
            .Where(uc => uc.UserId == userId)
            .Select(uc => uc.CommunityId)
            .FirstOrDefaultAsync();

        var isAdmin = await databaseContext.CommunityAdmins
            .AnyAsync(ca => ca.UserId == userId && ca.CommunityId == communityId);

        if (!isAdmin && user.Role != "Admin")
            return Forbid();

        // Toggle the pin status
        post.IsPinned = !post.IsPinned;
        await databaseContext.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            isPinned = post.IsPinned,
            message = post.IsPinned ? "Post pinned successfully" : "Post unpinned successfully"
        });
    }

    [Authorize]
    [HttpGet("get-pinned-posts")]
    public async Task<IActionResult> GetPinnedPosts(Guid? communityId)
    {
        var userId = User.GetId();
        var user = databaseContext.Users.FirstOrDefault(u => u.Id == userId);

        if (user == null)
            return Unauthorized();

        // Validate communityId
        var validCommunityIds = databaseContext.UserCommunities
            .Where(uc => uc.UserId == user.Id)
            .Select(uc => uc.CommunityId)
            .ToList();

        var selectedCommunityId = communityId.HasValue && validCommunityIds.Contains(communityId.Value)
            ? communityId.Value
            : validCommunityIds.FirstOrDefault();

        var channelId = databaseContext.Channels
            .Where(c => c.CommunityId == selectedCommunityId)
            .Select(c => c.Id)
            .FirstOrDefault();

        var isCommunityAdmin = databaseContext.CommunityAdmins
            .Any(ca => ca.UserId == userId && ca.CommunityId == selectedCommunityId);
        var adminRole = user.Role == "Admin";

        var pinnedPosts = databaseContext.Posts
            .Include(post => post.User)
            .Where(post => post.ChannelId == channelId && post.IsPinned)
            .OrderByDescending(post => post.CreatedAt)
            .Select(post => new PinnedPostViewModel
            {
                Id = post.Id,
                Title = post.Title,
                Description = post.Description,
                CreatedAt = post.CreatedAt,
                CreatedBy = post.User!.Firstname + " " + post.User!.LastName,
                CreatedById = post.User!.Id,
                UserHasPhoto = post.User!.Picture != null,
                isAdmin = adminRole && isCommunityAdmin
            })
            .ToList();

        return Json(pinnedPosts);
    }

    [Authorize]
    [HttpGet("user-photo/{userId}")]
    public IActionResult GetUserPhoto(Guid userId)
    {
        var user = databaseContext.Users.FirstOrDefault(u => u.Id == userId);
        if (user?.Picture == null)
        {
            // Return generic avatar if no photo exists
            return File(System.IO.File.ReadAllBytes("wwwroot/Images/GenericAvatar.png"), "image/png");
        }

        return File(user.Picture, "image/jpeg");
    }

    [Authorize]
    [HttpGet("image/{postId}")]
    public IActionResult GetImage(Guid postId)
    {
        var post = databaseContext.Posts.FirstOrDefault(p => p.Id == postId);
        if (post?.Photo == null) return NotFound();
        //Response.Headers.CacheControl = "public,max-age=31536000";
        return File(post.Photo, "image/jpeg");
    }

    [Authorize]
    [HttpGet("community/image/{communityId}")]
    public IActionResult GetCommunityImage(Guid communityId)
    {
        var community = databaseContext.Communities.FirstOrDefault(c => c.Id == communityId);
        if (community?.Picture == null) return NotFound();
        Response.Headers.CacheControl = "public,max-age=31536000";
        return File(community.Picture, "image/jpeg");
    }

    [Authorize]
    [HttpGet("OpenedPost/{id}")]
    public IActionResult OpenedPost(Guid id)
    {
        var userId = User.GetId();
        var user = databaseContext.Users.FirstOrDefault(u => u.Id == userId);

        var postDo = databaseContext.Posts
            .Include(p => p.User)
            .FirstOrDefault(p => p.Id == id);

        if (postDo == null)
        {
            return NotFound("Příspěvek nebyl nalezen.");
        }

        var communityId = databaseContext.Channels
            .Where(c => c.Id == postDo.ChannelId)
            .Select(c => c.CommunityId)
            .FirstOrDefault();

        var isCommunityAdmin = databaseContext.CommunityAdmins
            .Any(ca => ca.UserId == userId && ca.CommunityId == communityId);

        var isAdmin = isCommunityAdmin && user?.Role == "Admin";

        // Přemapování na ViewModel
        var postViewModel = new HomeViewModel.Post
        {
            Id = postDo.Id,
            Title = postDo.Title,
            Description = postDo.Description,
            Type = postDo.Type,
            CreatedBy = $"{postDo.User.Firstname} {postDo.User.LastName}" ?? "Neznámý",
            CreatedAt = postDo.CreatedAt,
            CreatedById = postDo.User.Id,
            IsAdmin = isAdmin,
            Photo = postDo.Photo != null
        };

        // Nastavení Open Graph metadat
        ViewData["OgTitle"] = postDo.Title;
        ViewData["OgDescription"] = postDo.Description;
        var ogImage = postDo.Photo != null ? "wwwroot/image.png" : "wwwroot/image.png";
        ViewData["OgImage"] = ogImage;
        ViewData["OgUrl"] = Url.Action("OpenedPost", "Home", new { id = id }, Request.Scheme);

        return PartialView("_OpenedPost", postViewModel);
    }

    [HttpGet("viewpost/{communityId}/{postId}")]
    public IActionResult ViewPost(Guid communityId, Guid postId)
    {
        return Redirect($"/?communityId={communityId}&openPostId={postId}");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View("Error");
    }

    [HttpGet("get-top-petition")]
    public async Task<IActionResult> GetTopPetition(Guid communityId)
    {
        var userId = User.GetId();

        var channelId = await databaseContext.Channels
            .Where(c => c.CommunityId == communityId)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();

        var topPetition = await databaseContext.Posts
            .Where(p => p.ChannelId == channelId && p.Type == "Petition")
            .OrderByDescending(p => databaseContext.PetitionSignatures.Count(sig => sig.PostId == p.Id))
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Description,
                Signatures = databaseContext.PetitionSignatures.Count(sig => sig.PostId == p.Id)
            })
            .FirstOrDefaultAsync();

        if (topPetition == null)
            return NoContent();

        return Json(topPetition);
    }
}