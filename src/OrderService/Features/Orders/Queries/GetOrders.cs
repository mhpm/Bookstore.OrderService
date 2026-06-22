using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;

namespace OrderService.Features.Orders.Queries
{
    public record OrderItemDto(int Id, int BookId, int Quantity, decimal UnitPrice);

    public record OrderDto(
        int Id,
        string CustomerEmail,
        DateTime OrderDate,
        decimal TotalAmount,
        List<OrderItemDto> Items
    );

    public record GetOrdersQuery() : IRequest<List<OrderDto>>;

    public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<OrderDto>>
    {
        private readonly OrderDbContext _context;

        public GetOrdersQueryHandler(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<List<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .Select(order => new OrderDto(
                    order.Id,
                    order.CustomerEmail,
                    order.OrderDate,
                    order.TotalAmount,
                    order.Items.Select(item => new OrderItemDto(
                        item.Id,
                        item.BookId,
                        item.Quantity,
                        item.UnitPrice
                    )).ToList()
                ))
                .ToListAsync(cancellationToken);
        }
    }
}
