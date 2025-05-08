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
        public async Task<ActionResult> GetUsers()
        {
            var users = await UserManager.Users.ToListAsync();
            var userRoles = new List<object>();
            foreach (var user in users)
            {
                var roles = await UserManager.GetRolesAsync(user.Id);
                userRoles.Add(new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.IsActive,
                    Roles = roles
                });
            }
            return Json(userRoles, JsonRequestBehavior.AllowGet);
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
                return Json(new { success = false, errors = result.Errors });
            }
            return Json(new { success = false, errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        [HttpPut]
        public async Task<ActionResult> Update(string id, [Bind(Include = "Email,IsActive,Roles")] UpdateUserViewModel model)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            user.Email = model.Email;
            user.UserName = model.Email;
            user.IsActive = model.IsActive;

            var result = await UserManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return Json(new { success = false, errors = result.Errors });
            }

            var currentRoles = await UserManager.GetRolesAsync(id);
            var rolesToRemove = currentRoles.Except(model.Roles ?? new List<string>()).ToList();
            var rolesToAdd = (model.Roles ?? new List<string>()).Except(currentRoles).ToList();

            foreach (var role in rolesToRemove)
            {
                await UserManager.RemoveFromRoleAsync(id, role);
            }
            foreach (var role in rolesToAdd)
            {
                await UserManager.AddToRoleAsync(id, role);
            }

            return Json(new { success = true });
        }

        [HttpDelete]
        public async Task<ActionResult> Delete(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                return HttpNotFound();
            }

            var result = await UserManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return Json(new { success = true });
            }
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