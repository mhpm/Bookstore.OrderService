using Xunit;
using FluentAssertions;
using Moq;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Features.Orders.Commands;
using Shared;

namespace OrderService.Tests
{
    public class OrderHandlerTests
    {
        private OrderDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<OrderDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new OrderDbContext(options);
        }

        [Fact]
        public async Task CreateOrderCommandHandler_Should_Save_Order_And_Publish_Event()
        {
            var context = GetInMemoryDbContext();
            
            // SIMULACIÓN (MOCK): Simulamos IPublishEndpoint usando la librería Moq
            var mockPublishEndpoint = new Mock<IPublishEndpoint>();

            var handler = new CreateOrderCommandHandler(context, mockPublishEndpoint.Object);

            var orderItemsInput = new List<OrderItemInput>
            {
                new(BookId: 1, Quantity: 2, UnitPrice: 15.00m),
                new(BookId: 2, Quantity: 1, UnitPrice: 20.00m)
            };

            var command = new CreateOrderCommand("customer@deloitte.com", orderItemsInput);
            var orderId = await handler.Handle(command, CancellationToken.None);

            // Assert: 1. Validar que la orden se guardó en la BD
            orderId.Should().BeGreaterThan(0);
            
            var savedOrder = await context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            savedOrder.Should().NotBeNull();
            savedOrder!.CustomerEmail.Should().Be("customer@deloitte.com");
            savedOrder.TotalAmount.Should().Be(50.00m); // 30.00m + 20.00m
            savedOrder.Items.Count.Should().Be(2);

            // Assert: 2. LA MAGIA DEL MOCK: Verificar que el comando sí "gritó" el evento a RabbitMQ
            // Comprobamos que el método Publish fue llamado exactamente una vez con el tipo OrderCreatedEvent
            mockPublishEndpoint.Verify(
                x => x.Publish(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }
    }
}
