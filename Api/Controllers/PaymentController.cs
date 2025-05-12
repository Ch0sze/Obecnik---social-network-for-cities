using Microsoft.AspNetCore.Mvc;
using Application.Infastructure.Database;
using Application.Api.Models;
using Application.Infastructure.Database.Models;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Api.Extensions;
using Application.Configuration;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Microsoft.Extensions.Options;

namespace Application.Api.Controllers
{
    [Route("payment")]
    public class PaymentController : Controller
    {
        private readonly DatabaseContext _db;
        private readonly StripeSettings _stripeSettings;
        
        public PaymentController(DatabaseContext db, IOptions<StripeSettings> stripeOptions)
        {
            _db = db;
            _stripeSettings = stripeOptions.Value;
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
        }


        [HttpPost("create-checkout-session")]
        public async Task<IActionResult> CreateCheckoutSession(AdminPaygateViewModel model)
        {
            Console.WriteLine($"EMAIL: {model.OfficialEmail}, POP: {model.TotalPopulation}, AMOUNT: {model.TotalAmount}");
            var userId = User.GetId();
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Recalculate population and amount for security
            var approvedRequests = await _db.AdminRequests
                .Where(r => r.OfficialEmail == model.OfficialEmail && r.Status == AdminRequestStatus.Approved)
                .ToListAsync();

            var totalPopulation = approvedRequests.Sum(r => r.Population);
            var pricePerPerson = 1m;
            var totalAmount = totalPopulation * pricePerPerson;

            if (totalAmount <= 0)
            {
                return RedirectToAction("Error", "Home", new { message = "Platba není možná – nulová částka." });
            }

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = "czk",
                            UnitAmount = (long)(totalAmount * 100 * 12), // 12-krát původní cena kvůli přechodu na roční
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Roční administrátorská práva",
                                Description = $"Za {totalPopulation} obyvatel"
                            },
                        },
                        Quantity = 1,
                    }
                },
                Mode = "payment",
                SuccessUrl = Url.Action("Success", "Payment", null, Request.Scheme),
                CancelUrl = Url.Action("Cancel", "Payment", null, Request.Scheme),
                CustomerEmail = user.Email
            };

            var service = new SessionService();
            Session session = await service.CreateAsync(options);

            return Redirect(session.Url);
        }

        [HttpGet("success")]
        public async Task<IActionResult> Success()
        {
            var userId = User.GetId();
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return RedirectToAction("Login", "Account");

            user.Role = "Admin";
            user.AdminRoleExpiresAt = DateTime.UtcNow.AddYears(1);

            await _db.SaveChangesAsync();

            return View("PaymentSuccess");
        }

        [HttpGet("cancel")]
        public IActionResult Cancel()
        {
            return View("PaymentCanceled");
        }
    }
}
