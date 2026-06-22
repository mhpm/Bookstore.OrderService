using MediatR;
using OrderService.Data.Repositories;
using System.Linq;

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

    /// <summary>
    /// DEPENDENCY INVERSION PRINCIPLE (DIP):
    /// El Handler de consulta consume IOrderRepository, lo que desacopla la capa de aplicación de EF Core.
    /// 
    /// CQRS PATTERN (QUERY):
    /// Representa una Consulta de lectura (Query). No altera ningún estado y recupera los datos
    /// utilizando el método del repositorio que implementa AsNoTracking para optimizar el rendimiento.
    /// </summary>
    public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<OrderDto>>
    {
        private readonly IOrderRepository _repository;

        public GetOrdersQueryHandler(IOrderRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
        {
            var orders = await _repository.GetAllWithItemsAsync(cancellationToken);

            return orders.Select(order => new OrderDto(
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
            )).ToList();
        }
    }
}

