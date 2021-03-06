﻿using System;
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
using flutterwave_sub.JsonModel;
using flutterwave_sub.Encryption;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

        RavePaymentDataEncryption rEn = new RavePaymentDataEncryption();

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
            if (result == SignInStatus.Success)
                return RedirectToAction("manager");

            ModelState.AddModelError("", "Invalid login attempt.");
            return View(model);
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
                                dateTime = DateTime.Now,
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
            var userId = User.Identity.GetUserId();
            if (!mid.HasValue)
            {
                mid = db.Vendors.Where(x => x.ApplicationUserId == userId).FirstOrDefault().ManagerId;
            }
            var manager = await db.Managers.FirstOrDefaultAsync(x => x.Id == mid.Value);
            ViewBag.Manager = manager;

            var sQuery = "Select * from Services where ManagerId = @p0 order by Id desc";

            var vsQuery = "Select * from Services where Id in ( Select ServiceId from VendorService Where VendorId In" +
                "( Select Id from Vendors Where ApplicationUserId = @p0 ))";

            var services = await db.Services.SqlQuery(sQuery, mid.Value).ToListAsync();

            var tenatServises = await db.Services.SqlQuery(vsQuery, userId).ToListAsync();

            var serviceModel = new List<ServiceViewModel>();

            services.ForEach(x =>
            {
                var sm = new ServiceViewModel
                {
                    Id = x.Id,
                    name = x.Name,
                    PlanId = x.PlanId,
                    interval = x.Interval,
                    amount = x.Amount,
                };
                if (tenatServises.Any(y => y.Id == x.Id))
                    sm.active = true;
                else
                    sm.active = false;

                serviceModel.Add(sm);
            });

            return View(serviceModel);
        }

        [Authorize(Roles = "tenat")]
        public async Task<ActionResult> Subscribe(int? sId)
        {
            if (!sId.HasValue)
                return RedirectToAction("tenat");
            var service = await db.Services.FindAsync(sId.Value);
            if (service == null)
            {
                addError();
                return RedirectToAction("tenat");
            }

            Session.Clear();

            Session.Add("Service", service);
            ViewBag.Service = service;
            Session.Add("ServiceId", service.Id);
            return View(new CardDetails());
        }

        [Authorize(Roles = "tenat"), HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Subscribe(CardDetails model)
        {
            if (!ModelState.IsValid)
            {
                addError();
                return View(model);
            }
            var user = await UserManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                addError();
                return RedirectToAction("tenat");
            }


            var details = await generateCardPayDetailsAsync(user, model);
            Session.Add("CardDetails", details);
            var stringDetails = JsonConvert.SerializeObject(details);

            var key = rEn.GetEncryptionKey(Credentials.API_Secret_Key);
            var cipher = rEn.EncryptData(key, stringDetails);

            var stringReponse = await PostWithEncryptionAsync(cipher, EndPoints.charge);

            if (stringReponse.Contains("success") && stringReponse.Contains("AUTH_SUGGESTION"))
            {
                if (stringReponse.Contains("PIN"))
                {
                    return View("Subscribe_pin");
                }

                if (stringReponse.Contains("NOAUTH_INTERNATIONAL"))
                {
                    Session.Add("type", "NOAUTH_INTERNATIONAL");
                    return View("Subscribe_auth_inter");
                }
                if (stringReponse.Contains("AVS_VBVSECURECODE"))
                {
                    Session.Add("type", "AVS_VBVSECURECODE");
                    return View("Subscribe_vbv");
                }
            }

            if (stringReponse.Contains("success") && stringReponse.Contains("V-COMP"))
            {
                addSubToSession(stringReponse);

                var data = (JObject)Session["data"];
                var amount = Convert.ToDecimal(data["amount"].ToString());
                var charged_amount = Convert.ToDecimal(data["charged_amount"].ToString());
                if (charged_amount < amount)
                {
                    addSuccess();
                    return View(model);
                }
                var chargeResponseCode = data["chargeResponseCode"].ToString();
                var authModelUsed = data["authModelUsed"].ToString();
                var chargeResponseMessage = data["chargeResponseMessage"].ToString();

                if (chargeResponseCode == "02")
                {
                    if (authModelUsed.Contains("OTP") || authModelUsed.Contains("PIN"))
                    {
                        addSuccess(chargeResponseMessage);
                        return View("Subscribe_otp");
                    }
                    if (authModelUsed.Contains("VBVSECURECODE"))
                    {
                        Session.Add("type", "AVS_VBVSECURECODE");
                        addSuccess(chargeResponseMessage);
                        return View("Subscribe_vbv");
                    }
                }

            }


            var res = JObject.Parse(stringReponse);
            var message = res["message"].ToString();
            addError(message);

            ViewBag.Service = (Service)Session["Service"];

            return View(model);
        }

        [Authorize(Roles = "tenat"), HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Subscribe_auth_inter(BillingDetails model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Check your Input");
                return View(model);
            }
            var x = (CardPayDetails_NotComplete)Session["CardDetails"];

            var cardDetails_Billings = new CardPayDetails_Billing
            {
                amount = x.amount,
                cardno = x.cardno,
                charge_type = x.charge_type,
                country = "NG",
                billingaddress = model.billingaddress,
                billingcity = model.billingcity,
                billingcountry = model.billingcountry,
                billingstate = model.billingstate,
                billingzip = model.billingzip,
                currency = "NGN",
                cvv = x.cvv,
                device_fingerprint = x.device_fingerprint,
                email = x.email,
                expirymonth = x.expirymonth,
                expiryyear = x.expiryyear,
                firstname = x.firstname,
                IP = x.IP,
                suggested_auth = (string)Session["type"],
                lastname = x.lastname,
                payment_plan = x.payment_plan,
                PBFPubKey = x.PBFPubKey,
                phonenumber = x.phonenumber,
                txRef = x.txRef
            };

            var stringDetails = JsonConvert.SerializeObject(cardDetails_Billings);

            var key = rEn.GetEncryptionKey(Credentials.API_Secret_Key);
            var cipher = rEn.EncryptData(key, stringDetails);

            var stringResponse = await PostWithEncryptionAsync(cipher, EndPoints.charge);

            if (stringResponse.Contains("success") && stringResponse.Contains("V-COMP"))
            {
                var res = JObject.Parse(stringResponse);
                var data = (JObject)res["data"];
                var authurl = data["authurl"].ToString();
                Session.Add("authurl", authurl);

                return View();
            }
            if (stringResponse.Contains("error"))
            {
                var res = JObject.Parse(stringResponse);
                var message = res["message"].ToString();
                addError(message);
                return RedirectToAction("tenat");
            }

            return View(model);
        }

        [Authorize(Roles = "tenat"), HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Subscribe_vbv(BillingDetails model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Check your Input");
                return View(model);
            }
            var x = (CardPayDetails_NotComplete)Session["CardDetails"];

            var authModelUsed = Session["type"].ToString();

            var cardDetails_Billings = new CardPayDetails_Billing
            {
                amount = x.amount,
                cardno = x.cardno,
                charge_type = x.charge_type,
                country = "NG",
                billingaddress = model.billingaddress,
                billingcity = model.billingcity,
                billingcountry = model.billingcountry,
                billingstate = model.billingstate,
                billingzip = model.billingzip,
                currency = "NGN",
                cvv = x.cvv,
                device_fingerprint = x.device_fingerprint,
                email = x.email,
                expirymonth = x.expirymonth,
                expiryyear = x.expiryyear,
                firstname = x.firstname,
                IP = x.IP,
                lastname = x.lastname,
                payment_plan = x.payment_plan,
                PBFPubKey = x.PBFPubKey,
                phonenumber = x.phonenumber,
                txRef = x.txRef
            };

            if (authModelUsed.Contains("VBV"))
                cardDetails_Billings.suggested_auth = "AVS_VBVSECURECODE";
            else
                cardDetails_Billings.suggested_auth = "NOAUTH_INTERNATIONAL";

            var stringDetails = JsonConvert.SerializeObject(cardDetails_Billings);

            var key = rEn.GetEncryptionKey(Credentials.API_Secret_Key);
            var cipher = rEn.EncryptData(key, stringDetails);

            var stringResponse = await PostWithEncryptionAsync(cipher, EndPoints.charge);

            if (stringResponse.Contains("success") && stringResponse.Contains("V-COMP"))
            {
                var res = JObject.Parse(stringResponse);
                var data = (JObject)res["data"];
                var authurl = data["authurl"].ToString();
                Session.Add("authurl", authurl);

                return View();
            }

            return View(model);
        }

        [Authorize(Roles = "tenat"), HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> submit_vbv(string otp)
        {
            var subs = (Subs)Session["Sub"];

            var verifyD = new
            {
                txref = subs.txRef,
                SECKEY = Credentials.API_Secret_Key
            };
            var verifyResponse = await CreatePostAsync(verifyD, EndPoints.verifyCharge);

            if (verifyResponse.Contains("Fetched"))
            {
                //var res = JObject.Parse(verifyResponse);
                //var data = (JObject)res["data"];
                //var card = (JObject)data["card"];
                //var card_tokens = (JArray)card["card_tokens"];
                ////var a = card_tokens
                subs.status = "success";
                subs.dateTime = DateTime.Now;

                var query = "Insert Into VendorService(VendorId, ServiceId) Values (@p0,@p1)";

                try
                {
                    db.Entry(subs).State = EntityState.Added;
                    var result = await db.Database.ExecuteSqlCommandAsync(query, subs.VendorId, subs.ServiceId);

                    addSuccess("Successfully subscribed");
                    return RedirectToAction("tenat");
                }
                catch (Exception ex)
                {

                    throw;
                }
            }

            return RedirectToAction("tenat");
        }

        [Authorize(Roles = "tenat"), HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Subscribe_otp(string otp)
        {
            var subs = (Subs)Session["Sub"];
            var validate = new { PBFPubKey = Credentials.API_Public_Key, otp, transaction_reference = subs.flwRef };
            var stringResponse = await CreatePostAsync(validate, EndPoints.validateCharge);

            if (stringResponse.Contains("Complete"))
            {
                var verifyD = new
                {
                    txref = subs.txRef,
                    SECKEY = Credentials.API_Secret_Key
                };
                var verifyResponse = await CreatePostAsync(verifyD, EndPoints.verifyCharge);

                if (verifyResponse.Contains("Fetched"))
                {
                    //var res = JObject.Parse(verifyResponse);
                    //var data = (JObject)res["data"];
                    //var card = (JObject)data["card"];
                    //var card_tokens = (JArray)card["card_tokens"];
                    ////var a = card_tokens
                    subs.status = "success";
                    subs.dateTime = DateTime.Now;

                    var query = "Insert Into VendorService(VendorId, ServiceId) Values (@p0,@p1)";

                    try
                    {
                        db.Entry(subs).State = EntityState.Added;
                        var result = await db.Database.ExecuteSqlCommandAsync(query, subs.VendorId, subs.ServiceId);

                        addSuccess("Successfully subscribed");
                        return RedirectToAction("tenat");
                    }
                    catch (Exception ex)
                    {

                        throw;
                    }
                }
            }

            return View();
        }

        [Authorize(Roles = "tenat"), HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Subscribe_pin(string pin)
        {
            CardPayDetails_NotComplete x = (CardPayDetails_NotComplete)Session["CardDetails"];
            var cardDetails = new CardPayDetails_Pin
            {
                amount = x.amount,
                cardno = x.cardno,
                charge_type = x.charge_type,
                cvv = x.cvv,
                device_fingerprint = x.device_fingerprint,
                email = x.email,
                expirymonth = x.expirymonth,
                expiryyear = x.expiryyear,
                firstname = x.firstname,
                IP = x.IP,
                lastname = x.lastname,
                payment_plan = x.payment_plan,
                PBFPubKey = x.PBFPubKey,
                phonenumber = x.phonenumber,
                pin = pin,
                txRef = x.txRef,
                currency = "NGN",
                country = "NG",
                suggested_auth = "PIN"
            };

            var stringDetails = JsonConvert.SerializeObject(cardDetails);

            var key = rEn.GetEncryptionKey(Credentials.API_Secret_Key);
            var cipher = rEn.EncryptData(key, stringDetails);

            var stringResponse = await PostWithEncryptionAsync(cipher, EndPoints.charge);

            if (stringResponse.Contains("success"))
            {
                addSubToSession(stringResponse);

                var data = (JObject)Session["data"];
                var chargeResponseMessage = data["chargeResponseMessage"].ToString();
                var chargeResponseCode = data["chargeResponseCode"].ToString();

                if (stringResponse.Contains("OTP"))
                {
                    addSuccess(chargeResponseMessage);
                    return View("Subscribe_otp");
                }
                if (chargeResponseCode == "00")
                {
                    var subs = (Subs)Session["Sub"];

                    var verifyD = new
                    {
                        txref = subs.txRef,
                        SECKEY = Credentials.API_Secret_Key
                    };
                    var verifyResponse = await CreatePostAsync(verifyD, EndPoints.verifyCharge);

                    if (verifyResponse.Contains("Fetched"))
                    {
                        subs.status = "success";
                        subs.dateTime = DateTime.Now;

                        var query = "Insert Into VendorService(VendorId, ServiceId) Values (@p0,@p1)";

                        try
                        {
                            db.Entry(subs).State = EntityState.Added;
                            var result = await db.Database.ExecuteSqlCommandAsync(query, subs.VendorId, subs.ServiceId);

                            addSuccess("Successfully subscribed");
                            return RedirectToAction("tenat");
                        }
                        catch (Exception ex)
                        {

                            throw;
                        }
                    }
                }
            }
            if (stringResponse.Contains("error"))
            {
                var res = JObject.Parse(stringResponse);
                var message = res["message"].ToString();
                addError(message);
                return RedirectToAction("tenat");
            }

            addError();

            return View("Subscribe_pin");
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
                                dateTime = DateTime.Now,
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

            var services = await db.Services.Where(x => x.ManagerId == mid.Value)
                .OrderByDescending(x => x.Id).ToListAsync();

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

            var service = await db.Services.Include(x => x.Vendors
            .Select(y => y.ApplicationUser)).AsQueryable()
                .FirstOrDefaultAsync(x => x.Id == sid.Value);

            return View(service);
        }

        [Authorize(Roles = "manager")]
        public ActionResult Addservice(int? mid)
        {
            if (!mid.HasValue)
                return RedirectToAction("manager");
            PopulatePlanInterval();
            return View(new ServiceAddModel { ManagerId = mid.Value });
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
            try
            {
                var modelX = new
                {
                    model.name,
                    model.interval,
                    model.seckey,
                    model.amount
                };

                var dataString = await CreatePostAsync(modelX, EndPoints.paymentPlan);
                var data = JsonConvert.DeserializeObject<PaymentPlanObject>(dataString);
                var plan = new Service
                {
                    PlanId = data.data.id,
                    Plan_token = data.data.plan_token,
                    Amount = Convert.ToDecimal(data.data.amount),
                    ManagerId = model.ManagerId,
                    Interval = data.data.interval,
                    Name = data.data.name,
                    dateTime = DateTime.Now,
                };

                db.Services.Add(plan);

                db.SaveChanges();
                addSuccess();
            }
            catch (Exception ex)
            {

                throw;
            }

            return RedirectToAction("Manager");
        }

        #endregion

        #region API Endpoint

        static async Task<string> CreatePostAsync(dynamic model, string url)
        {
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = await client.PostAsJsonAsync(url, (object)model);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        static async Task<String> PostWithEncryptionAsync(string cipher, string url)
        {
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage response = await client.PostAsJsonAsync(url,
                new { PBFPubKey = Credentials.API_Public_Key, client = cipher, alg = "3DES-24" });
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

        void addSubToSession(string stringResponse)
        {
            var res = JObject.Parse(stringResponse);
            var data = (JObject)res["data"];

            var orderRef = data["orderRef"].ToString();
            var flwRef = data["flwRef"].ToString();
            var chargeResponseCode = Convert.ToInt32(data["chargeResponseCode"].ToString());
            var authModelUsed = data["authModelUsed"].ToString();
            var txRef = data["txRef"].ToString();
            var userid = User.Identity.GetUserId();
            var query = "select Id from vendors where applicationuserid = @p0";
            var vId = db.Database.SqlQuery<int>(query, userid).FirstOrDefault();

            var serviceId = (int)Session["ServiceId"];

            var sub = new Subs
            {
                VendorId = vId,
                ServiceId = serviceId,
                txRef = txRef,
                flwRef = flwRef,
                orderRef = orderRef,
            };

            Session.Add("Sub", sub);
            Session.Add("data", data);
        }

        void addError()
        {
            TempData["error"] = "error occured, contact the admin";
        }

        void addError(string message)
        {
            TempData["error"] = message;
        }

        void addSuccess()
        {
            TempData["success"] = "action successful";
        }

        void addSuccess(string message)
        {
            TempData["success"] = message;
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

        async Task<CardPayDetails_NotComplete> generateCardPayDetailsAsync(ApplicationUser user, CardDetails details)
        {
            Fingerprinter.Generate(Request.ServerVariables);

            var _details = new CardPayDetails_NotComplete
            {
                email = user.Email,
                firstname = user.FirstName,
                lastname = user.LastName,
                PBFPubKey = Credentials.API_Public_Key,
                cardno = details.cardno.Replace(" ", "").Trim(),
                cvv = details.cvv,
                expirymonth = details.expirymonth,
                expiryyear = details.expiryyear,
                phonenumber = user.PhoneNumber,
                currency = "NGN",
                country = "NG"
            };

            var sQuery = "Select * from Services where Id = @p0";

            var ServiceId = (int)Session["ServiceId"];

            var service = await db.Services.SqlQuery(sQuery, ServiceId).FirstOrDefaultAsync();

            if (service == null)
                throw new NullReferenceException("Service");

            _details.amount = service.Amount.ToString();
            _details.payment_plan = service.PlanId.ToString();
            _details.charge_type = "recurring-" + service.Interval;
            _details.IP = Fingerprinter.IP;
            _details.device_fingerprint = Fingerprinter.FingerPrint.ToUpper();
            _details.txRef = "FWXX-" + Guid.NewGuid().ToString().ToUpper().Replace("-", "");

            return _details;
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