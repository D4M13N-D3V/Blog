using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Blog.Models;
using System.IO;
using Blog.Utilities;
using System.Configuration;
using System.Collections.Generic;

namespace Blog.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private ApplicationDbContext db = new ApplicationDbContext();

        public AccountController()
        {
        }

        public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager )
        {
            UserManager = userManager;
            SignInManager = signInManager;
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

        public ActionResult Profile(string id)
        {
            var user = UserManager.FindById(id);
            if (user != null)
            {
                return View(user);
            }
            else
            {
                return RedirectToAction("Error", "Home", new { errorText = "Invalid profile!" });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditDisplayName(string profileId, string newDisplayName)
        {
            if(newDisplayName!=null && User.IsInRole("Admin") || User.Identity.IsAuthenticated && User.Identity.GetUserId() == profileId)
            {
                var user = UserManager.FindById(profileId);
                user.DisplayName = newDisplayName;

                UserManager.Update(user);
                return RedirectToAction("Profile", new { id = profileId });
            }
            else
            {
                return RedirectToAction("Error", "Home", new { errorText="Invalid infromation passed or invalid permissions." });
            }
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult EditBio(string profileId, string newBio)
        {
            if (newBio != null && User.IsInRole("Admin") || User.Identity.IsAuthenticated && User.Identity.GetUserId() == profileId)
            {
                var user = UserManager.FindById(profileId);
                user.Bio = newBio;
                UserManager.Update(user);
                return RedirectToAction("Profile", new { id = profileId });
            }
            else
            {
                return RedirectToAction("Error", "Home", new { errorText = "Invalid infromation passed or invalid permissions." });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditImage(string profileId, HttpPostedFileBase uploadImage)
        {
            if ( User.IsInRole("Admin") || User.Identity.IsAuthenticated && User.Identity.GetUserId() == profileId)
            {
                var user = UserManager.FindById(profileId);

                if (ImageUploadValidator.IsWebFriendlyImage(uploadImage))
                {
                    var fileName = DateTime.Now.Ticks + ".png";
                    var path = Path.Combine(Server.MapPath("~/Uploads/"), fileName);
                    uploadImage.SaveAs(path);
                    user.AvatarPath = "/Uploads/" + fileName;
                }
                else
                {
                    return RedirectToAction("Error", "Home", new { errorText = "Invalid image provided." });
                }

                UserManager.Update(user);
                return RedirectToAction("Profile", new { id = profileId });
            }
            else
            {
                return RedirectToAction("Error", "Home", new { errorText = "Invalid infromation passed or invalid permissions." });
            }
        }
        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Error", "Home", new { errorText = "You are already logged in!" });
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
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent:  model.RememberMe, rememberBrowser: model.RememberBrowser);
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

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            if (User.Identity.IsAuthenticated) return RedirectToAction("Error", "Home", new { errorText = "You are already logged in!" });
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
                var user = new ApplicationUser { AvatarPath = Utilities.Utilities.GetGravatarLinkFromEmail(model.Email,60),Reputation=0, UserName = model.Email, Email = model.Email, FirstName = model.FirstName, LastName=model.LastName, DisplayName=model.DisplayName };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await SignInManager.SignInAsync(user, isPersistent:false, rememberBrowser:false);
                    
                    // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                    // Send an email with this link
                //    string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                //    var url = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                //    Utilities.Utilities.SendEmail(new EmailInformation
                //    {
                //        Title = ConfigurationManager.AppSettings.Get("ApplicationName")+" - Account Confirmation",
                //        Body = " <!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Strict//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd'><html xmlns='http://www.w3.org/1999/xhtml'><head><meta http-equiv='Content-Type' content='text/html; charset=utf-8'><style type='text/css'>.ExternalClass{width:100%}.ExternalClass, .ExternalClass p, .ExternalClass span, .ExternalClass font, .ExternalClass td, .ExternalClass div{line-height:150%}a{text-decoration:none}@media screen and (max-width: 600px){table.row th.col-lg-1, table.row th.col-lg-2, table.row th.col-lg-3, table.row th.col-lg-4, table.row th.col-lg-5, table.row th.col-lg-6, table.row th.col-lg-7, table.row th.col-lg-8, table.row th.col-lg-9, table.row th.col-lg-10, table.row th.col-lg-11, table.row th.col-lg-12{display:block;width:100% !important}.d-mobile{display:block !important}.d-desktop{display:none !important}.w-lg-25{width:auto !important}.w-lg-25>tbody>tr>td{width:auto !important}.w-lg-50{width:auto !important}.w-lg-50>tbody>tr>td{width:auto !important}.w-lg-75{width:auto !important}.w-lg-75>tbody>tr>td{width:auto !important}.w-lg-100{width:auto !important}.w-lg-100>tbody>tr>td{width:auto !important}.w-lg-auto{width:auto !important}.w-lg-auto>tbody>tr>td{width:auto !important}.w-25{width:25% !important}.w-25>tbody>tr>td{width:25% !important}.w-50{width:50% !important}.w-50>tbody>tr>td{width:50% !important}.w-75{width:75% !important}.w-75>tbody>tr>td{width:75% !important}.w-100{width:100% !important}.w-100>tbody>tr>td{width:100% !important}.w-auto{width:auto !important}.w-auto>tbody>tr>td{width:auto !important}.p-lg-0>tbody>tr>td{padding:0 !important}.pt-lg-0>tbody>tr>td,.py-lg-0>tbody>tr>td{padding-top:0 !important}.pr-lg-0>tbody>tr>td,.px-lg-0>tbody>tr>td{padding-right:0 !important}.pb-lg-0>tbody>tr>td,.py-lg-0>tbody>tr>td{padding-bottom:0 !important}.pl-lg-0>tbody>tr>td,.px-lg-0>tbody>tr>td{padding-left:0 !important}.p-lg-1>tbody>tr>td{padding:0 !important}.pt-lg-1>tbody>tr>td,.py-lg-1>tbody>tr>td{padding-top:0 !important}.pr-lg-1>tbody>tr>td,.px-lg-1>tbody>tr>td{padding-right:0 !important}.pb-lg-1>tbody>tr>td,.py-lg-1>tbody>tr>td{padding-bottom:0 !important}.pl-lg-1>tbody>tr>td,.px-lg-1>tbody>tr>td{padding-left:0 !important}.p-lg-2>tbody>tr>td{padding:0 !important}.pt-lg-2>tbody>tr>td,.py-lg-2>tbody>tr>td{padding-top:0 !important}.pr-lg-2>tbody>tr>td,.px-lg-2>tbody>tr>td{padding-right:0 !important}.pb-lg-2>tbody>tr>td,.py-lg-2>tbody>tr>td{padding-bottom:0 !important}.pl-lg-2>tbody>tr>td,.px-lg-2>tbody>tr>td{padding-left:0 !important}.p-lg-3>tbody>tr>td{padding:0 !important}.pt-lg-3>tbody>tr>td,.py-lg-3>tbody>tr>td{padding-top:0 !important}.pr-lg-3>tbody>tr>td,.px-lg-3>tbody>tr>td{padding-right:0 !important}.pb-lg-3>tbody>tr>td,.py-lg-3>tbody>tr>td{padding-bottom:0 !important}.pl-lg-3>tbody>tr>td,.px-lg-3>tbody>tr>td{padding-left:0 !important}.p-lg-4>tbody>tr>td{padding:0 !important}.pt-lg-4>tbody>tr>td,.py-lg-4>tbody>tr>td{padding-top:0 !important}.pr-lg-4>tbody>tr>td,.px-lg-4>tbody>tr>td{padding-right:0 !important}.pb-lg-4>tbody>tr>td,.py-lg-4>tbody>tr>td{padding-bottom:0 !important}.pl-lg-4>tbody>tr>td,.px-lg-4>tbody>tr>td{padding-left:0 !important}.p-lg-5>tbody>tr>td{padding:0 !important}.pt-lg-5>tbody>tr>td,.py-lg-5>tbody>tr>td{padding-top:0 !important}.pr-lg-5>tbody>tr>td,.px-lg-5>tbody>tr>td{padding-right:0 !important}.pb-lg-5>tbody>tr>td,.py-lg-5>tbody>tr>td{padding-bottom:0 !important}.pl-lg-5>tbody>tr>td,.px-lg-5>tbody>tr>td{padding-left:0 !important}.p-0>tbody>tr>td{padding:0 !important}.pt-0>tbody>tr>td,.py-0>tbody>tr>td{padding-top:0 !important}.pr-0>tbody>tr>td,.px-0>tbody>tr>td{padding-right:0 !important}.pb-0>tbody>tr>td,.py-0>tbody>tr>td{padding-bottom:0 !important}.pl-0>tbody>tr>td,.px-0>tbody>tr>td{padding-left:0 !important}.p-1>tbody>tr>td{padding:4px !important}.pt-1>tbody>tr>td,.py-1>tbody>tr>td{padding-top:4px !important}.pr-1>tbody>tr>td,.px-1>tbody>tr>td{padding-right:4px !important}.pb-1>tbody>tr>td,.py-1>tbody>tr>td{padding-bottom:4px !important}.pl-1>tbody>tr>td,.px-1>tbody>tr>td{padding-left:4px !important}.p-2>tbody>tr>td{padding:8px !important}.pt-2>tbody>tr>td,.py-2>tbody>tr>td{padding-top:8px !important}.pr-2>tbody>tr>td,.px-2>tbody>tr>td{padding-right:8px !important}.pb-2>tbody>tr>td,.py-2>tbody>tr>td{padding-bottom:8px !important}.pl-2>tbody>tr>td,.px-2>tbody>tr>td{padding-left:8px !important}.p-3>tbody>tr>td{padding:16px !important}.pt-3>tbody>tr>td,.py-3>tbody>tr>td{padding-top:16px !important}.pr-3>tbody>tr>td,.px-3>tbody>tr>td{padding-right:16px !important}.pb-3>tbody>tr>td,.py-3>tbody>tr>td{padding-bottom:16px !important}.pl-3>tbody>tr>td,.px-3>tbody>tr>td{padding-left:16px !important}.p-4>tbody>tr>td{padding:24px !important}.pt-4>tbody>tr>td,.py-4>tbody>tr>td{padding-top:24px !important}.pr-4>tbody>tr>td,.px-4>tbody>tr>td{padding-right:24px !important}.pb-4>tbody>tr>td,.py-4>tbody>tr>td{padding-bottom:24px !important}.pl-4>tbody>tr>td,.px-4>tbody>tr>td{padding-left:24px !important}.p-5>tbody>tr>td{padding:48px !important}.pt-5>tbody>tr>td,.py-5>tbody>tr>td{padding-top:48px !important}.pr-5>tbody>tr>td,.px-5>tbody>tr>td{padding-right:48px !important}.pb-5>tbody>tr>td,.py-5>tbody>tr>td{padding-bottom:48px !important}.pl-5>tbody>tr>td,.px-5>tbody>tr>td{padding-left:48px !important}.s-lg-1>tbody>tr>td,.s-lg-2>tbody>tr>td,.s-lg-3>tbody>tr>td,.s-lg-4>tbody>tr>td,.s-lg-5>tbody>tr>td{font-size:0 !important;line-height:0 !important;height:0 !important}.s-0>tbody>tr>td{font-size:0 !important;line-height:0 !important;height:0 !important}.s-1>tbody>tr>td{font-size:4px !important;line-height:4px !important;height:4px !important}.s-2>tbody>tr>td{font-size:8px !important;line-height:8px !important;height:8px !important}.s-3>tbody>tr>td{font-size:16px !important;line-height:16px !important;height:16px !important}.s-4>tbody>tr>td{font-size:24px !important;line-height:24px !important;height:24px !important}.s-5>tbody>tr>td{font-size:48px !important;line-height:48px !important;height:48px !important}}@media yahoo{.d-mobile{display:none !important}.d-desktop{display:block !important}.w-lg-25{width:25% !important}.w-lg-25>tbody>tr>td{width:25% !important}.w-lg-50{width:50% !important}.w-lg-50>tbody>tr>td{width:50% !important}.w-lg-75{width:75% !important}.w-lg-75>tbody>tr>td{width:75% !important}.w-lg-100{width:100% !important}.w-lg-100>tbody>tr>td{width:100% !important}.w-lg-auto{width:auto !important}.w-lg-auto>tbody>tr>td{width:auto !important}.p-lg-0>tbody>tr>td{padding:0 !important}.pt-lg-0>tbody>tr>td,.py-lg-0>tbody>tr>td{padding-top:0 !important}.pr-lg-0>tbody>tr>td,.px-lg-0>tbody>tr>td{padding-right:0 !important}.pb-lg-0>tbody>tr>td,.py-lg-0>tbody>tr>td{padding-bottom:0 !important}.pl-lg-0>tbody>tr>td,.px-lg-0>tbody>tr>td{padding-left:0 !important}.p-lg-1>tbody>tr>td{padding:4px !important}.pt-lg-1>tbody>tr>td,.py-lg-1>tbody>tr>td{padding-top:4px !important}.pr-lg-1>tbody>tr>td,.px-lg-1>tbody>tr>td{padding-right:4px !important}.pb-lg-1>tbody>tr>td,.py-lg-1>tbody>tr>td{padding-bottom:4px !important}.pl-lg-1>tbody>tr>td,.px-lg-1>tbody>tr>td{padding-left:4px !important}.p-lg-2>tbody>tr>td{padding:8px !important}.pt-lg-2>tbody>tr>td,.py-lg-2>tbody>tr>td{padding-top:8px !important}.pr-lg-2>tbody>tr>td,.px-lg-2>tbody>tr>td{padding-right:8px !important}.pb-lg-2>tbody>tr>td,.py-lg-2>tbody>tr>td{padding-bottom:8px !important}.pl-lg-2>tbody>tr>td,.px-lg-2>tbody>tr>td{padding-left:8px !important}.p-lg-3>tbody>tr>td{padding:16px !important}.pt-lg-3>tbody>tr>td,.py-lg-3>tbody>tr>td{padding-top:16px !important}.pr-lg-3>tbody>tr>td,.px-lg-3>tbody>tr>td{padding-right:16px !important}.pb-lg-3>tbody>tr>td,.py-lg-3>tbody>tr>td{padding-bottom:16px !important}.pl-lg-3>tbody>tr>td,.px-lg-3>tbody>tr>td{padding-left:16px !important}.p-lg-4>tbody>tr>td{padding:24px !important}.pt-lg-4>tbody>tr>td,.py-lg-4>tbody>tr>td{padding-top:24px !important}.pr-lg-4>tbody>tr>td,.px-lg-4>tbody>tr>td{padding-right:24px !important}.pb-lg-4>tbody>tr>td,.py-lg-4>tbody>tr>td{padding-bottom:24px !important}.pl-lg-4>tbody>tr>td,.px-lg-4>tbody>tr>td{padding-left:24px !important}.p-lg-5>tbody>tr>td{padding:48px !important}.pt-lg-5>tbody>tr>td,.py-lg-5>tbody>tr>td{padding-top:48px !important}.pr-lg-5>tbody>tr>td,.px-lg-5>tbody>tr>td{padding-right:48px !important}.pb-lg-5>tbody>tr>td,.py-lg-5>tbody>tr>td{padding-bottom:48px !important}.pl-lg-5>tbody>tr>td,.px-lg-5>tbody>tr>td{padding-left:48px !important}.s-lg-0>tbody>tr>td{font-size:0 !important;line-height:0 !important;height:0 !important}.s-lg-1>tbody>tr>td{font-size:4px !important;line-height:4px !important;height:4px !important}.s-lg-2>tbody>tr>td{font-size:8px !important;line-height:8px !important;height:8px !important}.s-lg-3>tbody>tr>td{font-size:16px !important;line-height:16px !important;height:16px !important}.s-lg-4>tbody>tr>td{font-size:24px !important;line-height:24px !important;height:24px !important}.s-lg-5>tbody>tr>td{font-size:48px !important;line-height:48px !important;height:48px !important}}</style></head><body style='outline: 0; width: 100%; min-width: 100%; height: 100%; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; font-family: Helvetica, Arial, sans-serif; line-height: 24px; font-weight: normal; font-size: 16px; -moz-box-sizing: border-box; -webkit-box-sizing: border-box; box-sizing: border-box; margin: 0; padding: 0; border: 0;'><div class='preview' style='display: none; max-height: 0px; overflow: hidden;'> A new article has been posted on the website!                                                                </div><table valign='top' class='bg-dark body' style='outline: 0; width: 100%; min-width: 100%; height: 100%; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; font-family: Helvetica, Arial, sans-serif; line-height: 24px; font-weight: normal; font-size: 16px; -moz-box-sizing: border-box; -webkit-box-sizing: border-box; box-sizing: border-box; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; margin: 0; padding: 0; border: 0;' bgcolor='#343a40'><tbody><tr><td valign='top' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0;' align='left' bgcolor='#343a40'><table class='container' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; width: 100%;'><tbody><tr><td align='center' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0; padding: 0 16px;'><table align='center' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; width: 100%; max-width: 600px; margin: 0 auto;'><tbody><tr><td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0;' align='left'><table class='mx-auto' align='center' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; margin: 0 auto;'><tbody><tr><td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0;' align='left'><table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'><tbody><tr><td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>  </td></tr></tbody></table><img class=' ' src='https://damiensblog.azurewebsites.net/Images/white-text.png' style='height: auto; line-height: 100%; outline: none; text-decoration: none; border: 0 none;'><table class='s-3 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'><tbody><tr><td height='16' style='border-spacing: 0px; border-collapse: collapse; line-height: 16px; font-size: 16px; width: 100%; height: 16px; margin: 0;' align='left'>  </td></tr></tbody></table></td></tr></tbody></table><table class='card ' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: separate !important; border-radius: 4px; width: 100%; overflow: hidden; border: 1px solid #dee2e6;' bgcolor='#ffffff'><tbody><tr><td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; width: 100%; margin: 0;' align='left'><div style='border-top-width: 5px; border-top-color: #007bff; border-top-style: solid;'><table class='card-body' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; width: 100%;'><tbody><tr><td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; width: 100%; margin: 0; padding: 20px;' align='left'><div><h4 class='text-center' style='margin-top: 0; margin-bottom: 0; font-weight: 500; color: inherit; vertical-align: baseline; font-size: 24px; line-height: 28.8px;' align='center'>Confirm your acccount!</h4><h5 class='text-muted text-center' style='margin-top: 0; margin-bottom: 0; font-weight: 500; color: #636c72; vertical-align: baseline; font-size: 20px; line-height: 24px;' align='center'>" + DateTime.Now.Month + "/" + DateTime.Now.Day + "/" + DateTime.Now.Year + " at " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "</h5><h5 class='text-muted text-center' style='margin-top: 0; margin-bottom: 0; font-weight: 500; color: #636c72; vertical-align: baseline; font-size: 20px; line-height: 24px;' align='center'>" + url + "</h5></div></td></tr></tbody></table></div></td></tr></tbody></table><table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'><tbody><tr><td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>  </td></tr></tbody></table><table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'><tbody><tr><td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>  </td></tr></tbody></table><table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'><tbody><tr><td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>  </td></tr></tbody></table></td></tr></tbody></table></td></tr></tbody></table></td></tr></tbody></table></body></html>",
                //        Reciepents = new List<string> { user.Email }
                //});
                    return RedirectToAction("Profile", "Account", new { id=user.Id });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

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
                if (user == null)
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordConfirmation");
                }

                // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
                //Send an email with this link
                 string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var url = Url.Action("ResetPassword", "Account", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                Utilities.Utilities.SendEmail(new EmailInformation
                {
                    Title = ConfigurationManager.AppSettings.Get("ApplicationName") + " - Account Recovery",
                    Body = " <!DOCTYPE html PUBLIC '-//W3C//DTD XHTML 1.0 Strict//EN' 'http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd'><html xmlns='http://www.w3.org/1999/xhtml'><head><meta http-equiv='Content-Type' content='text/html; charset=utf-8'><style type='text/css'>.ExternalClass{width:100%}.ExternalClass, .ExternalClass p, .ExternalClass span, .ExternalClass font, .ExternalClass td, .ExternalClass div{line-height:150%}a{text-decoration:none}@media screen and (max-width: 600px){table.row th.col-lg-1, table.row th.col-lg-2, table.row th.col-lg-3, table.row th.col-lg-4, table.row th.col-lg-5, table.row th.col-lg-6, table.row th.col-lg-7, table.row th.col-lg-8, table.row th.col-lg-9, table.row th.col-lg-10, table.row th.col-lg-11, table.row th.col-lg-12{display:block;width:100% !important}.d-mobile{display:block !important}.d-desktop{display:none !important}.w-lg-25{width:auto !important}.w-lg-25>tbody>tr>td{width:auto !important}.w-lg-50{width:auto !important}.w-lg-50>tbody>tr>td{width:auto !important}.w-lg-75{width:auto !important}.w-lg-75>tbody>tr>td{width:auto !important}.w-lg-100{width:auto !important}.w-lg-100>tbody>tr>td{width:auto !important}.w-lg-auto{width:auto !important}.w-lg-auto>tbody>tr>td{width:auto !important}.w-25{width:25% !important}.w-25>tbody>tr>td{width:25% !important}.w-50{width:50% !important}.w-50>tbody>tr>td{width:50% !important}.w-75{width:75% !important}.w-75>tbody>tr>td{width:75% !important}.w-100{width:100% !important}.w-100>tbody>tr>td{width:100% !important}.w-auto{width:auto !important}.w-auto>tbody>tr>td{width:auto !important}.p-lg-0>tbody>tr>td{padding:0 !important}.pt-lg-0>tbody>tr>td,.py-lg-0>tbody>tr>td{padding-top:0 !important}.pr-lg-0>tbody>tr>td,.px-lg-0>tbody>tr>td{padding-right:0 !important}.pb-lg-0>tbody>tr>td,.py-lg-0>tbody>tr>td{padding-bottom:0 !important}.pl-lg-0>tbody>tr>td,.px-lg-0>tbody>tr>td{padding-left:0 !important}.p-lg-1>tbody>tr>td{padding:0 !important}.pt-lg-1>tbody>tr>td,.py-lg-1>tbody>tr>td{padding-top:0 !important}.pr-lg-1>tbody>tr>td,.px-lg-1>tbody>tr>td{padding-right:0 !important}.pb-lg-1>tbody>tr>td,.py-lg-1>tbody>tr>td{padding-bottom:0 !important}.pl-lg-1>tbody>tr>td,.px-lg-1>tbody>tr>td{padding-left:0 !important}.p-lg-2>tbody>tr>td{padding:0 !important}.pt-lg-2>tbody>tr>td,.py-lg-2>tbody>tr>td{padding-top:0 !important}.pr-lg-2>tbody>tr>td,.px-lg-2>tbody>tr>td{padding-right:0 !important}.pb-lg-2>tbody>tr>td,.py-lg-2>tbody>tr>td{padding-bottom:0 !important}.pl-lg-2>tbody>tr>td,.px-lg-2>tbody>tr>td{padding-left:0 !important}.p-lg-3>tbody>tr>td{padding:0 !important}.pt-lg-3>tbody>tr>td,.py-lg-3>tbody>tr>td{padding-top:0 !important}.pr-lg-3>tbody>tr>td,.px-lg-3>tbody>tr>td{padding-right:0 !important}.pb-lg-3>tbody>tr>td,.py-lg-3>tbody>tr>td{padding-bottom:0 !important}.pl-lg-3>tbody>tr>td,.px-lg-3>tbody>tr>td{padding-left:0 !important}.p-lg-4>tbody>tr>td{padding:0 !important}.pt-lg-4>tbody>tr>td,.py-lg-4>tbody>tr>td{padding-top:0 !important}.pr-lg-4>tbody>tr>td,.px-lg-4>tbody>tr>td{padding-right:0 !important}.pb-lg-4>tbody>tr>td,.py-lg-4>tbody>tr>td{padding-bottom:0 !important}.pl-lg-4>tbody>tr>td,.px-lg-4>tbody>tr>td{padding-left:0 !important}.p-lg-5>tbody>tr>td{padding:0 !important}.pt-lg-5>tbody>tr>td,.py-lg-5>tbody>tr>td{padding-top:0 !important}.pr-lg-5>tbody>tr>td,.px-lg-5>tbody>tr>td{padding-right:0 !important}.pb-lg-5>tbody>tr>td,.py-lg-5>tbody>tr>td{padding-bottom:0 !important}.pl-lg-5>tbody>tr>td,.px-lg-5>tbody>tr>td{padding-left:0 !important}.p-0>tbody>tr>td{padding:0 !important}.pt-0>tbody>tr>td,.py-0>tbody>tr>td{padding-top:0 !important}.pr-0>tbody>tr>td,.px-0>tbody>tr>td{padding-right:0 !important}.pb-0>tbody>tr>td,.py-0>tbody>tr>td{padding-bottom:0 !important}.pl-0>tbody>tr>td,.px-0>tbody>tr>td{padding-left:0 !important}.p-1>tbody>tr>td{padding:4px !important}.pt-1>tbody>tr>td,.py-1>tbody>tr>td{padding-top:4px !important}.pr-1>tbody>tr>td,.px-1>tbody>tr>td{padding-right:4px !important}.pb-1>tbody>tr>td,.py-1>tbody>tr>td{padding-bottom:4px !important}.pl-1>tbody>tr>td,.px-1>tbody>tr>td{padding-left:4px !important}.p-2>tbody>tr>td{padding:8px !important}.pt-2>tbody>tr>td,.py-2>tbody>tr>td{padding-top:8px !important}.pr-2>tbody>tr>td,.px-2>tbody>tr>td{padding-right:8px !important}.pb-2>tbody>tr>td,.py-2>tbody>tr>td{padding-bottom:8px !important}.pl-2>tbody>tr>td,.px-2>tbody>tr>td{padding-left:8px !important}.p-3>tbody>tr>td{padding:16px !important}.pt-3>tbody>tr>td,.py-3>tbody>tr>td{padding-top:16px !important}.pr-3>tbody>tr>td,.px-3>tbody>tr>td{padding-right:16px !important}.pb-3>tbody>tr>td,.py-3>tbody>tr>td{padding-bottom:16px !important}.pl-3>tbody>tr>td,.px-3>tbody>tr>td{padding-left:16px !important}.p-4>tbody>tr>td{padding:24px !important}.pt-4>tbody>tr>td,.py-4>tbody>tr>td{padding-top:24px !important}.pr-4>tbody>tr>td,.px-4>tbody>tr>td{padding-right:24px !important}.pb-4>tbody>tr>td,.py-4>tbody>tr>td{padding-bottom:24px !important}.pl-4>tbody>tr>td,.px-4>tbody>tr>td{padding-left:24px !important}.p-5>tbody>tr>td{padding:48px !important}.pt-5>tbody>tr>td,.py-5>tbody>tr>td{padding-top:48px !important}.pr-5>tbody>tr>td,.px-5>tbody>tr>td{padding-right:48px !important}.pb-5>tbody>tr>td,.py-5>tbody>tr>td{padding-bottom:48px !important}.pl-5>tbody>tr>td,.px-5>tbody>tr>td{padding-left:48px !important}.s-lg-1>tbody>tr>td,.s-lg-2>tbody>tr>td,.s-lg-3>tbody>tr>td,.s-lg-4>tbody>tr>td,.s-lg-5>tbody>tr>td{font-size:0 !important;line-height:0 !important;height:0 !important}.s-0>tbody>tr>td{font-size:0 !important;line-height:0 !important;height:0 !important}.s-1>tbody>tr>td{font-size:4px !important;line-height:4px !important;height:4px !important}.s-2>tbody>tr>td{font-size:8px !important;line-height:8px !important;height:8px !important}.s-3>tbody>tr>td{font-size:16px !important;line-height:16px !important;height:16px !important}.s-4>tbody>tr>td{font-size:24px !important;line-height:24px !important;height:24px !important}.s-5>tbody>tr>td{font-size:48px !important;line-height:48px !important;height:48px !important}}@media yahoo{.d-mobile{display:none !important}.d-desktop{display:block !important}.w-lg-25{width:25% !important}.w-lg-25>tbody>tr>td{width:25% !important}.w-lg-50{width:50% !important}.w-lg-50>tbody>tr>td{width:50% !important}.w-lg-75{width:75% !important}.w-lg-75>tbody>tr>td{width:75% !important}.w-lg-100{width:100% !important}.w-lg-100>tbody>tr>td{width:100% !important}.w-lg-auto{width:auto !important}.w-lg-auto>tbody>tr>td{width:auto !important}.p-lg-0>tbody>tr>td{padding:0 !important}.pt-lg-0>tbody>tr>td,.py-lg-0>tbody>tr>td{padding-top:0 !important}.pr-lg-0>tbody>tr>td,.px-lg-0>tbody>tr>td{padding-right:0 !important}.pb-lg-0>tbody>tr>td,.py-lg-0>tbody>tr>td{padding-bottom:0 !important}.pl-lg-0>tbody>tr>td,.px-lg-0>tbody>tr>td{padding-left:0 !important}.p-lg-1>tbody>tr>td{padding:4px !important}.pt-lg-1>tbody>tr>td,.py-lg-1>tbody>tr>td{padding-top:4px !important}.pr-lg-1>tbody>tr>td,.px-lg-1>tbody>tr>td{padding-right:4px !important}.pb-lg-1>tbody>tr>td,.py-lg-1>tbody>tr>td{padding-bottom:4px !important}.pl-lg-1>tbody>tr>td,.px-lg-1>tbody>tr>td{padding-left:4px !important}.p-lg-2>tbody>tr>td{padding:8px !important}.pt-lg-2>tbody>tr>td,.py-lg-2>tbody>tr>td{padding-top:8px !important}.pr-lg-2>tbody>tr>td,.px-lg-2>tbody>tr>td{padding-right:8px !important}.pb-lg-2>tbody>tr>td,.py-lg-2>tbody>tr>td{padding-bottom:8px !important}.pl-lg-2>tbody>tr>td,.px-lg-2>tbody>tr>td{padding-left:8px !important}.p-lg-3>tbody>tr>td{padding:16px !important}.pt-lg-3>tbody>tr>td,.py-lg-3>tbody>tr>td{padding-top:16px !important}.pr-lg-3>tbody>tr>td,.px-lg-3>tbody>tr>td{padding-right:16px !important}.pb-lg-3>tbody>tr>td,.py-lg-3>tbody>tr>td{padding-bottom:16px !important}.pl-lg-3>tbody>tr>td,.px-lg-3>tbody>tr>td{padding-left:16px !important}.p-lg-4>tbody>tr>td{padding:24px !important}.pt-lg-4>tbody>tr>td,.py-lg-4>tbody>tr>td{padding-top:24px !important}.pr-lg-4>tbody>tr>td,.px-lg-4>tbody>tr>td{padding-right:24px !important}.pb-lg-4>tbody>tr>td,.py-lg-4>tbody>tr>td{padding-bottom:24px !important}.pl-lg-4>tbody>tr>td,.px-lg-4>tbody>tr>td{padding-left:24px !important}.p-lg-5>tbody>tr>td{padding:48px !important}.pt-lg-5>tbody>tr>td,.py-lg-5>tbody>tr>td{padding-top:48px !important}.pr-lg-5>tbody>tr>td,.px-lg-5>tbody>tr>td{padding-right:48px !important}.pb-lg-5>tbody>tr>td,.py-lg-5>tbody>tr>td{padding-bottom:48px !important}.pl-lg-5>tbody>tr>td,.px-lg-5>tbody>tr>td{padding-left:48px !important}.s-lg-0>tbody>tr>td{font-size:0 !important;line-height:0 !important;height:0 !important}.s-lg-1>tbody>tr>td{font-size:4px !important;line-height:4px !important;height:4px !important}.s-lg-2>tbody>tr>td{font-size:8px !important;line-height:8px !important;height:8px !important}.s-lg-3>tbody>tr>td{font-size:16px !important;line-height:16px !important;height:16px !important}.s-lg-4>tbody>tr>td{font-size:24px !important;line-height:24px !important;height:24px !important}.s-lg-5>tbody>tr>td{font-size:48px !important;line-height:48px !important;height:48px !important}}</style></head><body style='outline: 0; width: 100%; min-width: 100%; height: 100%; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; font-family: Helvetica, Arial, sans-serif; line-height: 24px; font-weight: normal; font-size: 16px; -moz-box-sizing: border-box; -webkit-box-sizing: border-box; box-sizing: border-box; margin: 0; padding: 0; border: 0;'><div class='preview' style='display: none; max-height: 0px; overflow: hidden;'> A new article has been posted on the website!                                                                </div><table valign='top' class='bg-dark body' style='outline: 0; width: 100%; min-width: 100%; height: 100%; -webkit-text-size-adjust: 100%; -ms-text-size-adjust: 100%; font-family: Helvetica, Arial, sans-serif; line-height: 24px; font-weight: normal; font-size: 16px; -moz-box-sizing: border-box; -webkit-box-sizing: border-box; box-sizing: border-box; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; margin: 0; padding: 0; border: 0;' bgcolor='#343a40'><tbody><tr><td valign='top' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0;' align='left' bgcolor='#343a40'><table class='container' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; width: 100%;'><tbody><tr><td align='center' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0; padding: 0 16px;'><table align='center' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; width: 100%; max-width: 600px; margin: 0 auto;'><tbody><tr><td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0;' align='left'><table class='mx-auto' align='center' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; margin: 0 auto;'><tbody><tr><td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; margin: 0;' align='left'><table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'><tbody><tr><td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>  </td></tr></tbody></table><img class=' ' src='https://damiensblog.azurewebsites.net/Images/white-text.png' style='height: auto; line-height: 100%; outline: none; text-decoration: none; border: 0 none;'><table class='s-3 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'><tbody><tr><td height='16' style='border-spacing: 0px; border-collapse: collapse; line-height: 16px; font-size: 16px; width: 100%; height: 16px; margin: 0;' align='left'>  </td></tr></tbody></table></td></tr></tbody></table><table class='card ' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: separate !important; border-radius: 4px; width: 100%; overflow: hidden; border: 1px solid #dee2e6;' bgcolor='#ffffff'><tbody><tr><td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; width: 100%; margin: 0;' align='left'><div style='border-top-width: 5px; border-top-color: #007bff; border-top-style: solid;'><table class='card-body' border='0' cellpadding='0' cellspacing='0' style='font-family: Helvetica, Arial, sans-serif; mso-table-lspace: 0pt; mso-table-rspace: 0pt; border-spacing: 0px; border-collapse: collapse; width: 100%;'><tbody><tr><td style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 16px; width: 100%; margin: 0; padding: 20px;' align='left'><div><h4 class='text-center' style='margin-top: 0; margin-bottom: 0; font-weight: 500; color: inherit; vertical-align: baseline; font-size: 24px; line-height: 28.8px;' align='center'>Recover your acccount!</h4><h5 class='text-muted text-center' style='margin-top: 0; margin-bottom: 0; font-weight: 500; color: #636c72; vertical-align: baseline; font-size: 20px; line-height: 24px;' align='center'>" + DateTime.Now.Month + "/" + DateTime.Now.Day + "/" + DateTime.Now.Year + " at " + DateTime.Now.Hour + ":" + DateTime.Now.Minute + "</h5><h5 class='text-muted text-center' style='margin-top: 0; margin-bottom: 0; font-weight: 500; color: #636c72; vertical-align: baseline; font-size: 20px; line-height: 24px;' align='center'>" + url + "</h5></div></td></tr></tbody></table></div></td></tr></tbody></table><table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'><tbody><tr><td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>  </td></tr></tbody></table><table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'><tbody><tr><td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>  </td></tr></tbody></table><table class='s-4 w-100' border='0' cellpadding='0' cellspacing='0' style='width: 100%;'><tbody><tr><td height='24' style='border-spacing: 0px; border-collapse: collapse; line-height: 24px; font-size: 24px; width: 100%; height: 24px; margin: 0;' align='left'>  </td></tr></tbody></table></td></tr></tbody></table></td></tr></tbody></table></td></tr></tbody></table></body></html>",
                    Reciepents = new List<string> { user.Email }
                });
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
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
                var user = new ApplicationUser { AvatarPath = Utilities.Utilities.GetGravatarLinkFromEmail(model.Email, 60), UserName = model.Email, Email = model.Email, Reputation = 0 };
                var result = await UserManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await UserManager.AddLoginAsync(user.Id, info.Login);
                    if (result.Succeeded)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                        return RedirectToAction("Profile", "Account", new { id = user.Id });
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
            if (!User.Identity.IsAuthenticated) return RedirectToAction("Error", "Home", new { errorText = "You are not logged in!" });
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