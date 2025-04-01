using JobFlow.Business.PaymentGateways.SharedModels;
using JobFlow.Business.PaymentGateways;
using JobFlow.Business.Services.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace JobFlow.API.Controllers
{
    [ApiController]
    [Route("api/payments/")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentProcessorFactory _processorFactory;
        private readonly IOrganizationService _organizationService;

        public PaymentController(IPaymentProcessorFactory processorFactory, IOrganizationService organizationService)
        {
            _processorFactory = processorFactory;
            _organizationService = organizationService;
        }

        [HttpPost, Route("{orgId}/checkout")]
        public async Task<IActionResult> Checkout(Guid orgId, [FromBody] PaymentSessionRequest request)
        {
            var org = await _organizationService.GetOrganiztionById(orgId);
            if (org == null) return NotFound("Organization not found.");

            var processor = _processorFactory.GetProcessor(org.Value.PaymentProvider.ToString());
            var url = await processor.CreateCheckoutSessionAsync(request);

            return Ok(new { url });
        }

        [HttpPost, Route("{orgId}/create-connected-account")]
        public async Task<IActionResult> CreateConnectedAccount(Guid orgId)
        {
            var org = await _organizationService.GetOrganiztionById(orgId);
            if (org == null) return NotFound();

            var processor = _processorFactory.GetProcessor(org.Value.PaymentProvider.ToString());

            if (processor is IConnectedAccountProcessor connected)
            {
                var accountId = await connected.CreateConnectedAccountAsync();
                return Ok(new { accountId });
            }

            return BadRequest("This provider does not support connected accounts.");
        }
    }

}
