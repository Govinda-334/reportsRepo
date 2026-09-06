// Controllers/AuthController.cs
using JWT_authentication.Data;
using JWT_authentication.DTOs;
using JWT_authentication.Models;
using JWT_authentication.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JWT_authentication.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwt;

        public AuthController(AppDbContext context, JwtService jwt)
        {
            _context = context;
            _jwt = jwt;
        }

        [HttpGet]
        public IActionResult Register() => View();

        // POST: /Auth/Register
        [HttpPost]
        public IActionResult Register(RegisterDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var exists = _context.Users.Any(x => x.Username == model.Username);
            if (exists)
            {
                ModelState.AddModelError("Username", "Username already taken.");
                return View(model);
            }

            var user = new Users
            {
                Username = model.Username,
                Email = model.Email,
                Password = model.Password
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["Success"] = "Registered successfully! Please login.";
            return RedirectToAction("Login");
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login() => View();

        // POST: /Auth/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = _context.Users
                .FirstOrDefault(x => x.Username == model.Username
                                  && x.Password == model.Password);

            if (user == null)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }

    
            var token = _jwt.GenerateToken(user.Username);
            Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTimeOffset.UtcNow.AddHours(1)
            });

           
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Dashboard");
        }

     
        [Authorize]
        [HttpGet]
        public IActionResult Dashboard()
        {
            ViewBag.Username = User.Identity?.Name;
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Response.Cookies.Delete("jwt");
            return RedirectToAction("Login");
        }
    }
}