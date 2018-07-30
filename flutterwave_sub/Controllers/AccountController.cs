using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using flutterwave_sub.Models;
using System.Collections.Generic;

namespace flutterwave_sub.Controllers
{
    [Authorize(Roles = "tenat,manager,admin")]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private ApplicationRoleManager _roleManager;

        private ApplicationDbContext db = new ApplicationDbContext();

        static HttpClient client = new HttpClient();

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager, ApplicationRoleManager roleManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
            RoleManager = _roleManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationRoleManager RoleManager
        {
            get
            {
                return _roleManager ?? HttpContext.GetOwinContext().Get<ApplicationRoleManager>();
            }
            private set
            {
                _roleManager = value;
            }
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

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("admin"))
                    return RedirectToAction("AllServices");
                if (User.IsInRole("manager"))
                    return RedirectToAction("manager");
                if (User.IsInRole("tenat"))
                    return RedirectToAction("tenat");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, change to shouldLockout: true
            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid login attempt.");
                    return View(model);
            }
        }

        #region Tenats
        //
        // GET: /Account/Register
        [AllowAnonymous]
        public async Task<ActionResult> Register()
        {
            await PopulateManagerIdAsync();
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.Number,
                };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (!await RoleManager.RoleExistsAsync("tenat"))
                    {
                        addError();
                        await PopulateManagerIdAsync();
                        return View(model);
                    }
                    else
                    {
                        user = await UserManager.FindByEmailAsync(model.Email);
                        var addToRole = await UserManager.AddToRoleAsync(user.Id, "tenat");
                        if (!addToRole.Succeeded)
                        {
                            addError();
                            await PopulateManagerIdAsync();
                            return View(model);
                        }
                        else
                        {
                            var vendor = new Vendor
                            {
                                ManagerId = model.ManagerId,
                                ApplicationUserId = user.Id,
                            };
                            try
                            {
                                db.Vendors.Add(vendor);
                                await db.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                throw;
                            }
                            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        }
                    }

                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    return RedirectToAction("tenat", new { mid = model.ManagerId });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            addError();
            await PopulateManagerIdAsync();
            return View(model);
        }

        //[Authorize(Roles = "tenats")]
        //public async Task<ActionResult> Tenat()
        //{
        //    var userId = User.Identity.GetUserId();
        //    var tenatServises = await db.Subs.FirstOrDefault(x => x.ApplicationUserId == userId)
        //        .Services.AsQueryable().ToListAsync();
        //    return View(tenatServises);
        //}

        [Authorize(Roles = "tenat")]
        public async Task<ActionResult> Tenat(int? mid)
        {
            if (!mid.HasValue)
            {
                var userid = User.Identity.GetUserId();
                mid = db.Vendors.Where(x => x.ApplicationUserId == userid).FirstOrDefault().ManagerId;
            }
            var manager = await db.Managers.FirstOrDefaultAsync(x => x.Id == mid.Value);
            ViewBag.Manager = manager;

            var services = await db.Managers.FirstOrDefault(x => x.Id == mid.Value)
                .Services.AsQueryable().ToListAsync();

            var userId = User.Identity.GetUserId();
            var tenatServises = await db.Vendors.FirstOrDefault(x => x.ApplicationUserId == userId)
                .Services.AsQueryable().ToListAsync();

            var serviceModel = new List<ServiceViewModel>();

            services.ForEach(x =>
            {
                var sm = new ServiceViewModel
                {
                    Id = x.Id,
                    name = x.Name,
                    PlanId = x.PlanId,
                };
                if (tenatServises.Any(y => y.Id == x.Id))
                    sm.active = true;
                else
                    sm.active = false;

                serviceModel.Add(sm);
            });

            return View(serviceModel);
        }

        #endregion

        #region Managers

        [AllowAnonymous]
        public ActionResult RegisterM()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RegisterM(ManagerRegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.Number,
                };
                Manager manager;
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (!await RoleManager.RoleExistsAsync("manager"))
                    {
                        addError();
                        return View(model);
                    }
                    else
                    {
                        user = await UserManager.FindByEmailAsync(model.Email);
                        var addToRole = await UserManager.AddToRoleAsync(user.Id, "manager");
                        if (!addToRole.Succeeded)
                        {
                            addError();
                            return View(model);
                        }
                        else
                        {
                            manager = new Manager
                            {
                                AccountName = model.AccountName,
                                ApplicationUserId = user.Id,
                            };
                            db.Managers.Add(manager);
                            try
                            {
                                await db.SaveChangesAsync();
                                manager = await db.Managers.FirstOrDefaultAsync(x => x.ApplicationUserId == user.Id);
                            }
                            catch (Exception ex)
                            {

                                throw;
                            }
                            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        }
                    }

                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                    // string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                    // var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                    // await UserManager.SendEmailAsync(user.Id, "Confirm your account", "Please confirm your account by clicking <a href=\"" + callbackUrl + "\">here</a>");

                    return RedirectToAction("manager", new { mid = manager.Id });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            addError();
            return View(model);
        }

        //[Authorize(Roles = "manager")]
        //public async Task<ActionResult> Manager()
        //{
        //    var userId = User.Identity.GetUserId();
        //    var manager = await db.Managers.FirstOrDefaultAsync(x => x.ApplicationUserId == userId);
        //    if (manager == null)
        //    {
        //        addError();
        //        return RedirectToAction("index");
        //    }
        //    else
        //        return View(manager);
        //}

        [Authorize(Roles = "manager")]
        public async Task<ActionResult> Manager(int? mid)
        {
            if (!mid.HasValue)
            {
                var userId = User.Identity.GetUserId();
                var query = "select Id from managers where ApplicationUserId = @p0";
                mid = await db.Database.SqlQuery<int>(query, userId).FirstOrDefaultAsync();
            }

            ViewBag.ManagerId = mid.Value;

            var services = await db.Services.Where(x => x.ManagerId == mid.Value).ToListAsync();

            return View(services);
        }

        [Authorize(Roles = "manager,admin")]
        public async Task<ActionResult> Service(int? sid)
        {
            if (!sid.HasValue)
            {
                var userId = User.Identity.GetUserId();
                var query = "select Id from managers where ApplicationUserId = @p0";
                sid = await db.Database.SqlQuery<int>(query, userId).FirstOrDefaultAsync();
            }

            var service = await db.Services.AsQueryable().FirstOrDefaultAsync(x => x.Id == sid.Value);

            return View(service);
        }

        [Authorize(Roles = "manager")]
        public ActionResult Addservice()
        {
            PopulatePlanInterval();
            return View(new ServiceAddModel());
        }

        [Authorize(Roles = "manager"), HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Addservice(ServiceAddModel model)
        {
            if (!ModelState.IsValid)
            {
                addError();
                return View(model);
            }
            model.seckey = Credentials.API_Secret_Key;
            var data = await CreatePaymentPlanAsync(model);
            //db.Services.Add(model);
            db.SaveChanges();
            addSuccess();
            return RedirectToAction("Manager");
        }

        #endregion

        #region API Endpoint

        static async Task<string> CreatePaymentPlanAsync(ServiceAddModel model)
        {
            var baseUrl = "https://ravesandboxapi.flutterwave.com";
            var url = "v2/gpx/paymentplans/create";
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await client.PostAsJsonAsync(
                url, model);
            response.EnsureSuccessStatusCode();

            // return URI of the created resource.
            return await response.Content.ReadAsStringAsync();
        }

        #endregion

        #region Admin

        [Authorize(Roles = "admin")]
        public async Task<ActionResult> AllServices()
        {
            var services = await db.Services.AsQueryable().ToListAsync();
            return View("Manager", services);
        }

        #endregion

        void addError()
        {
            TempData["error"] = "error occured, contact the admin";
        }

        void addSuccess()
        {
            TempData["success"] = "action successful";
        }

        async Task PopulateManagerIdAsync()
        {
            var manager = await db.Managers.Select(x => new { ID = x.Id, Name = x.AccountName }).ToListAsync();
            ViewBag.ManagerId = new SelectList(manager, "ID", "Name");
        }

        void PopulatePlanInterval()
        {
            List<SelectListItem> interval = new List<SelectListItem>();
            interval.Add(new SelectListItem { Text = "daily", Value = "daily" });
            interval.Add(new SelectListItem { Text = "weekly", Value = "weekly" });
            interval.Add(new SelectListItem { Text = "monthly", Value = "monthly" });
            interval.Add(new SelectListItem { Text = "quarterly", Value = "quarterly" });
            interval.Add(new SelectListItem { Text = "yearly", Value = "yearly" });
            ViewBag.interval = new SelectList(interval, "Value", "Text");
        }

        #region Not Needed
        //
        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }
        #endregion

        //
        // GET: /Account/ConfirmEmail
        [AllowAnonymous]
        public async Task<ActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return View("Error");
            }
            var result = await UserManager.ConfirmEmailAsync(userId, code);
            return View(result.Succeeded ? "ConfirmEmail" : "Error");
        }

        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByNameAsync(model.Email);
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                // Send an email with this link
                // string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                // var callbackUrl = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);		
                // await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
                // return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        //
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }
        #endregion
    }
}