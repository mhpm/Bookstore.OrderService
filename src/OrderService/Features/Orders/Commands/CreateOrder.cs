using MediatR;
using MassTransit;
using Shared;
using OrderService.Data;
using OrderService.Data.Repositories;
using System.Linq;

namespace OrderService.Features.Orders.Commands
{
    public record OrderItemInput(int BookId, int Quantity, decimal UnitPrice);

    public record CreateOrderCommand(
        string CustomerEmail, 
        List<OrderItemInput> Items
    ) : IRequest<int>;

    /// <summary>
    /// DEPENDENCY INVERSION PRINCIPLE (DIP):
    /// El Handler ya no depende de la base de datos física (OrderDbContext), sino de la abstracción IOrderRepository.
    /// 
    /// SINGLE RESPONSIBILITY PRINCIPLE (SRP):
    /// El Handler orquesta el proceso: inicializa la entidad de orden, agrega ítems delegando la lógica
    /// de negocio (cálculo de totales e invariantes) al dominio, guarda los cambios usando el repositorio y
    /// publica el evento de integración para notificar a otros servicios.
    /// 
    /// CQRS PATTERN (COMMAND):
    /// Este Handler procesa un Comando de escritura. Crea nuevos registros, modifica el estado del sistema
    /// y utiliza transacciones de escritura (guardado con tracking).
    /// </summary>
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, int>
    {
        private readonly IOrderRepository _repository;
        private readonly IPublishEndpoint _publishEndpoint;

        public CreateOrderCommandHandler(IOrderRepository repository, IPublishEndpoint publishEndpoint)
        {
            _repository = repository;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<int> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            var order = new Order
            {
                CustomerEmail = request.CustomerEmail,
                OrderDate = DateTime.UtcNow
            };

            foreach (var item in request.Items)
            {
                // Encapsulación & OCP:
                // Delegamos la creación del ítem y el incremento del total al método del dominio Order.AddItem.
                // Si cambian las reglas de validación o la fórmula de cálculo del total, solo se altera la entidad Order.
                order.AddItem(item.BookId, item.Quantity, item.UnitPrice);
            }

            await _repository.AddAsync(order, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            var eventMessage = new OrderCreatedEvent
            {
                OrderId = order.Id,
                CustomerEmail = order.CustomerEmail,
                Items = order.Items.Select(oi => new OrderItemEventDto
                {
                    BookId = oi.BookId,
                    Quantity = oi.Quantity
                }).ToList()
            };

            await _publishEndpoint.Publish(eventMessage, cancellationToken);

            return order.Id;
        }
    }
}

