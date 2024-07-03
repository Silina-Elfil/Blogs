using blogs.Data;
using blogs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
//using Microsoft.Build.Experimental.ProjectCache;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
//using System.Drawing.Text;
//using System.Reflection.Metadata;

namespace blogs.Controllers
{
    [Authorize(Roles = "admin, author")]
    public class AdminController: Controller
    {
        private readonly ApplicationDbContext dbContext;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IHttpContextAccessor httpContextAccessor;

        private readonly ILogger<HomeController> logger;

        //cache
        private readonly IMemoryCache cache;
        private readonly string cacheKey = "productsCacheKey"; 
        

        public AdminController(ILogger<HomeController> logger, IMemoryCache cache, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
            this.httpContextAccessor = httpContextAccessor;

            this.logger = logger;

            //cache
            this.cache = cache;
        }

        //cache
        public IActionResult Index()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            if (cache.TryGetValue(cacheKey, out IEnumerable<Blog> blogs))
            {
                logger.Log(LogLevel.Information, "Blog found in cache.");
            } else
            {
                logger.Log(LogLevel.Information, "Blog not found in cache. Loading from memory");
                blogs = dbContext.Blogs.ToList();  //always list

                var cacheEntryOptions = new MemoryCacheEntryOptions() 
                    .SetSlidingExpiration(TimeSpan.FromSeconds(45))  //clear after 45 sec if no request
                    .SetAbsoluteExpiration(TimeSpan.FromSeconds(3600))  //clear after 3600 sec anyway
                    .SetPriority(CacheItemPriority.Normal);  

                cache.Set(cacheKey, blogs, cacheEntryOptions);
            }

            stopwatch.Stop();

            logger.Log(LogLevel.Information, "Passed Time" + stopwatch.ElapsedMilliseconds);
            
            return View(blogs);
        }

        public IActionResult ClearCache() 
        {
            cache.Remove(cacheKey);
            logger.Log(LogLevel.Information, "Cleared cache");

            return RedirectToAction("Index"); 
        }


        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var authors = await userManager.GetUsersInRoleAsync("author");
            var currentUser = httpContextAccessor?.HttpContext?.User;


            var viewModel = new PostViewModel
            {
                Authors = authors.ToList(),
                IsAdmin = currentUser?.IsInRole("admin") ?? false,
                CurrentUsername = currentUser?.Identity?.Name
        };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Add(PostViewModel blog)
        {
           if (ModelState.IsValid)
            {
                string? uniqueFileName = null;

                if (blog.Image != null && blog.Image.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images");
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + blog.Image.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await blog.Image.CopyToAsync(stream);
                    }
                }

            var currentUser = httpContextAccessor?.HttpContext?.User;
            var userId = userManager.GetUserId(currentUser);
           
            if(userId == null)
            {
                return View(blog);  
            }
            var authorId = currentUser.IsInRole("admin") ? blog.AuthorId : userId;

            var dbBlog = new Blog
                {
                    AuthorId = authorId,
                    Title = blog.Title,
                    Content = blog.Content,
                    ImgSrc = uniqueFileName
                };

                dbContext.Blogs.Add(dbBlog);
                await dbContext.SaveChangesAsync();

                return RedirectToAction("List");
            }
            else
            {
                var errros = (ModelState.Select(x => x.ToString()));

                Console.Write(ModelState.Select(x => x.ToString()));
            }

           /* blog.Authors = await dbContext.Authors.ToListAsync();*/ // Refill the authors list if model state is invalid
            return View(blog);
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var blogs = new List<Blog>();
            //current user
            var currentUser = httpContextAccessor?.HttpContext?.User;
     
            if (currentUser != null && currentUser.IsInRole("author"))
            {
                var userId = userManager.GetUserId(currentUser);
                blogs = await dbContext.Blogs
                    .Include(x => x.Author)
                    .Where(x => x.AuthorId == userId)
                    .ToListAsync();
            }
            else
            {
                blogs = await dbContext.Blogs
                    .Include(x => x.Author)
                    .ToListAsync();
            }
           

     
            return View(blogs);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {
            var authors = await userManager.GetUsersInRoleAsync("author");
            var currentUser = httpContextAccessor?.HttpContext?.User;


            var blog = await dbContext.Blogs.FindAsync(id);

            if (blog == null)
            {
                return NotFound();
            }

            var viewModel = new PostViewModel
            {
                Id = blog.Id,
                Title = blog.Title,
                Content = blog.Content,
                AuthorId = blog.AuthorId,
                Authors = authors.ToList(),
                Image = null,
                IsAdmin = currentUser.IsInRole("admin"),
                CurrentUsername = currentUser?.Identity?.Name
            };

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> Edit(PostViewModel blog)
        {

            if (ModelState.IsValid)
            {
                string? uniqueFileName = null;

                if (blog.Image != null && blog.Image.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images");
                    uniqueFileName = Guid.NewGuid().ToString() + "_" + blog.Image.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await blog.Image.CopyToAsync(stream);
                    }
                }

                var blogFromDb = await dbContext.Blogs.FindAsync(blog.Id);

                var currentUser = httpContextAccessor?.HttpContext?.User;
                var userId = userManager.GetUserId(currentUser);

                if (userId == null)
                {
                    return View(blog);
                }
                var authorId = currentUser.IsInRole("admin") ? blog.AuthorId : userId;

                blogFromDb.Title = blog.Title;
                blogFromDb.AuthorId = authorId;
                blogFromDb.Content = blog.Content;
                blogFromDb.ImgSrc = uniqueFileName ?? blogFromDb.ImgSrc;

                dbContext.Blogs.Update(blogFromDb);
                await dbContext.SaveChangesAsync();

                return RedirectToAction("List");
            }
            else
            {
                var errros = (ModelState.Select(x => x.ToString()));

                Console.Write(ModelState.Select(x => x.ToString()));
            }

            return View(blog);
        }
       

        [HttpPost]
        public async Task<IActionResult> Delete(Blog viewModel)
        {
            var blog = await dbContext.Blogs
                .AsNoTracking().FirstOrDefaultAsync(x => x.Id == viewModel.Id);

            if (blog is not null)
            {
                dbContext.Blogs.Remove(viewModel);
                await dbContext.SaveChangesAsync();
            }
            return RedirectToAction("List");
        }

    }
}
