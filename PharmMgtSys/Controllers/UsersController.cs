using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.Identity.EntityFramework;
using PharmMgtSys.Models;
using System.Data.Entity;
using System.Diagnostics;

namespace PharmMgtSys.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private ApplicationUserManager _userManager;
        private RoleManager<IdentityRole> _roleManager;

        public UsersController()
        {
        }

        public UsersController(ApplicationUserManager userManager, RoleManager<IdentityRole> roleManager)
        {
            UserManager = userManager;
            RoleManager = roleManager;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public RoleManager<IdentityRole> RoleManager
        {
            get
            {
                if (_roleManager == null)
                {
                    var context = HttpContext.GetOwinContext().Get<ApplicationDbContext>();
                    _roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
                }
                return _roleManager;
            }
            private set
            {
                _roleManager = value;
            }
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<ActionResult> GetUsers(int skip = 0, int take = 10)
        {
            var users = await UserManager.Users.OrderBy(u => u.UserName).Skip(skip).Take(take).ToListAsync();
            var totalCount = await UserManager.Users.CountAsync();
            var userRoles = new List<object>();
            foreach (var user in users)
            {
                var roles = await UserManager.GetRolesAsync(user.Id);
                userRoles.Add(new
                {
                    Id = user.Id,
                    user.UserName,
                    user.Email,
                    user.IsActive,
                    Roles = roles
                });
            }
            Debug.WriteLine($"GetUsers: Returning JSON with {userRoles.Count} users, totalCount: {totalCount}");
            return Json(new { data = userRoles, totalCount = totalCount }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public async Task<ActionResult> Create([Bind(Include = "Email,Password,IsActive,Roles")] CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, IsActive = model.IsActive };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (model.Roles != null)
                    {
                        foreach (var role in model.Roles)
                        {
                            await UserManager.AddToRoleAsync(user.Id, role);
                        }
                    }
                    return Json(new { success = true });
                }
                Debug.WriteLine("Create failed: " + string.Join(", ", result.Errors));
                return Json(new { success = false, errors = result.Errors });
            }
            var modelErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            Debug.WriteLine("Create model invalid: " + string.Join(", ", modelErrors));
            return Json(new { success = false, errors = modelErrors });
        }

        [HttpPut]
        public async Task<ActionResult> Update(string id, [Bind(Include = "Email,IsActive,Roles")] UpdateUserViewModel model)
        {
            Debug.WriteLine($"Update called for user ID: {id}, Email: {model?.Email}, IsActive: {model?.IsActive}, Roles: {string.Join(", ", model?.Roles ?? new List<string>())}");
            if (string.IsNullOrEmpty(id))
            {
                Debug.WriteLine("Update failed: ID is null or empty");
                return Json(new { success = false, errors = new[] { "User ID is required." } });
            }

            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByIdAsync(id);
                if (user == null)
                {
                    Debug.WriteLine($"Update failed: User not found for ID: {id}");
                    return Json(new { success = false, errors = new[] { "User not found." } });
                }

                user.Email = model.Email ?? user.Email;
                user.UserName = model.Email ?? user.UserName;
                user.IsActive = model.IsActive;

                var result = await UserManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    Debug.WriteLine("Update failed: " + string.Join(", ", result.Errors));
                    return Json(new { success = false, errors = result.Errors });
                }

                if (model.Roles != null)
                {
                    var currentRoles = await UserManager.GetRolesAsync(id);
                    var rolesToRemove = currentRoles.Except(model.Roles).ToList();
                    var rolesToAdd = model.Roles.Except(currentRoles).ToList();

                    foreach (var role in rolesToRemove)
                    {
                        var removeResult = await UserManager.RemoveFromRoleAsync(id, role);
                        if (!removeResult.Succeeded)
                        {
                            Debug.WriteLine($"Failed to remove role {role}: {string.Join(", ", removeResult.Errors)}");
                            return Json(new { success = false, errors = removeResult.Errors });
                        }
                    }
                    foreach (var role in rolesToAdd)
                    {
                        var addResult = await UserManager.AddToRoleAsync(id, role);
                        if (!addResult.Succeeded)
                        {
                            Debug.WriteLine($"Failed to add role {role}: {string.Join(", ", addResult.Errors)}");
                            return Json(new { success = false, errors = addResult.Errors });
                        }
                    }
                }

                Debug.WriteLine("Update succeeded for user ID: " + id);
                return Json(new { success = true });
            }

            var modelErrors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            Debug.WriteLine("Update model invalid: " + string.Join(", ", modelErrors));
            return Json(new { success = false, errors = modelErrors });
        }

        [HttpDelete]
        public async Task<ActionResult> Delete(string id)
        {
            Debug.WriteLine($"Delete called for user ID: {id}");
            if (string.IsNullOrEmpty(id))
            {
                Debug.WriteLine("Delete failed: ID is null or empty");
                return Json(new { success = false, errors = new[] { "User ID is required." } });
            }

            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                Debug.WriteLine($"Delete failed: User not found for ID: {id}");
                return Json(new { success = false, errors = new[] { "User not found." } });
            }

            var result = await UserManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                Debug.WriteLine("Delete succeeded for user ID: " + id);
                return Json(new { success = true });
            }

            Debug.WriteLine("Delete failed: " + string.Join(", ", result.Errors));
            return Json(new { success = false, errors = result.Errors });
        }

        [HttpGet]
        public ActionResult GetRoles()
        {
            var roles = RoleManager.Roles.Select(r => new { r.Name }).ToList();
            return Json(roles, JsonRequestBehavior.AllowGet);
        }
    }

    public class CreateUserViewModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; }
    }

    public class UpdateUserViewModel
    {
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; }
    }
}