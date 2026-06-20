using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Features.Orders.Commands;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public OrdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // Endpoint POST: api/orders
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderCommand command)
        {
            if (command == null || command.Items == null || !command.Items.Any())
            {
                return BadRequest("La orden debe contener al menos un artículo.");
            }

            var orderId = await _mediator.Send(command);

            return CreatedAtAction(nameof(CreateOrder), new { id = orderId }, orderId);
        }
    }
}
