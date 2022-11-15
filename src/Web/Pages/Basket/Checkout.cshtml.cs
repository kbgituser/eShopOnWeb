using System.Text;
using System.Text.Json;
using Ardalis.GuardClauses;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Azure.ServiceBus;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Exceptions;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.Infrastructure.Identity;
using Microsoft.eShopWeb.Web.Interfaces;


namespace Microsoft.eShopWeb.Web.Pages.Basket;

[Authorize]
public class CheckoutModel : PageModel
{
    private readonly IBasketService _basketService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOrderService _orderService;
    private string? _username = null;
    private readonly IBasketViewModelService _basketViewModelService;
    private readonly IAppLogger<CheckoutModel> _logger;
    private readonly IConfiguration _configuration;

    public CheckoutModel(IBasketService basketService,
        IBasketViewModelService basketViewModelService,
        SignInManager<ApplicationUser> signInManager,
        IOrderService orderService,
        IAppLogger<CheckoutModel> logger,
        IConfiguration configuration
        )
    {
        _basketService = basketService;
        _signInManager = signInManager;
        _orderService = orderService;
        _basketViewModelService = basketViewModelService;
        _logger = logger;
        _configuration = configuration;
    }

    public BasketViewModel BasketModel { get; set; } = new BasketViewModel();

    public async Task OnGet()
    {
        await SetBasketModelAsync();
    }

    public async Task<IActionResult> OnPost(IEnumerable<BasketItemViewModel> items)
    {
        try
        {
            await SetBasketModelAsync();

            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var updateModel = items.ToDictionary(b => b.Id.ToString(), b => b.Quantity);
            await _basketService.SetQuantities(BasketModel.Id, updateModel);
            await _orderService.CreateOrderAsync(BasketModel.Id, new Address("123 Main St.", "Kent", "OH", "United States", "44240"));

            //changes begins
            //var serviceBusConString = _configuration.GetValue(typeof(string), "SBConnection") as string;
            string serviceBusConnectionString = _configuration.GetConnectionString("SBConnection");
            //const string serviceBusConnectionString = "Endpoint=sb://uploadsb.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=486SYWq9/XGCkGowAW4dQslBABxyHU2qjWedxbzta2U=";
            const string queueName = "sbq";
            IQueueClient queueClient;

            queueClient = new QueueClient(serviceBusConnectionString, queueName);
            string json = JsonSerializer.Serialize(BasketModel.Items.Select(x => new { x.Id, x.Quantity }));
            var message = new Message(Encoding.UTF8.GetBytes(json));
            await queueClient.SendAsync(message);


            var shippingadress = "some shipping address";
            var finalprice = BasketModel.Items.Sum(x => x.UnitPrice * x.Quantity).ToString();
            //var listofitems = string.Join(",", items.Select(x => x.ProductName).ToArray());            

            var listofitems = string.Join(",", BasketModel.Items.Select(x => x.ProductName).ToArray());
            HttpClient _client = new HttpClient();
            var cosmosDBConString = _configuration.GetValue(typeof(string), "CosmosFunctionUrl") as string;
            
            HttpRequestMessage newRequest = new HttpRequestMessage(HttpMethod.Get,
                //"https://funcforcosmos.azurewebsites.net/api/function1/"
                cosmosDBConString
                + "?shippingadress=" + shippingadress
                + "&finalprice=" + finalprice
                + "&listofitems=" + listofitems
                );
            HttpResponseMessage response = await _client.SendAsync(newRequest);
            //changes ends

            await _basketService.DeleteBasketAsync(BasketModel.Id);
        }
        catch (EmptyBasketOnCheckoutException emptyBasketOnCheckoutException)
        {
            //Redirect to Empty Basket page
            _logger.LogWarning(emptyBasketOnCheckoutException.Message);
            return RedirectToPage("/Basket/Index");
        }

        return RedirectToPage("Success");
    }

    private async Task SetBasketModelAsync()
    {
        Guard.Against.Null(User?.Identity?.Name, nameof(User.Identity.Name));
        if (_signInManager.IsSignedIn(HttpContext.User))
        {
            BasketModel = await _basketViewModelService.GetOrCreateBasketForUser(User.Identity.Name);
        }
        else
        {
            GetOrSetBasketCookieAndUserName();
            BasketModel = await _basketViewModelService.GetOrCreateBasketForUser(_username!);
        }
    }

    private void GetOrSetBasketCookieAndUserName()
    {
        if (Request.Cookies.ContainsKey(Constants.BASKET_COOKIENAME))
        {
            _username = Request.Cookies[Constants.BASKET_COOKIENAME];
        }
        if (_username != null) return;

        _username = Guid.NewGuid().ToString();
        var cookieOptions = new CookieOptions();
        cookieOptions.Expires = DateTime.Today.AddYears(10);
        Response.Cookies.Append(Constants.BASKET_COOKIENAME, _username, cookieOptions);
    }
}
