using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Pro.Controllers
{

    public class AccountController : Controller
    {
        private ProDBContext _context;
        private PasswordHasher hasher;
        public AccountController(ProDBContext context)
        {
            _context = context;
            hasher = new PasswordHasher();
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                ProUser user = await _context.Users.FirstOrDefaultAsync(u => u.Login == model.Login);
                if (user == null)
                {
                    user = new ProUser { Login = model.Login, Password = hasher.Hash(model.Password) };
                    Role userRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "user");
                    if (userRole != null)
                        user.Role = userRole;
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                    await Authenticate(user);
                    return RedirectToAction("Index", "Home");
                }
                else
                    ModelState.AddModelError("", "Некорректные логин и(или) пароль");
            }
            return View(model);
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                ProUser user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Login == model.Login);
                if (user != null && hasher.Check(user.Password, model.Password))
                {
                    await Authenticate(user);
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Некорректные логин и(или) пароль");
            }
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
        private async Task Authenticate(ProUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.Login),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role?.Name)
            };

            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }
    }
}


