using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderService.Features.Orders.Commands;
using OrderService.Features.Orders.Queries;

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

        // Endpoint GET: api/orders
        [HttpGet]
        public async Task<ActionResult<List<OrderDto>>> GetOrders()
        {
            var orders = await _mediator.Send(new GetOrdersQuery());
            return Ok(orders);
        }
    }
}
