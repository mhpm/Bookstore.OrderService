using Xunit;
using FluentAssertions;
using Moq;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Data.Repositories;
using OrderService.Features.Orders.Commands;
using OrderService.Features.Orders.Queries;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OrderService.Tests
{
    /// <summary>
    /// CLASE DE PRUEBAS UNITARIAS Y DE INTEGRACIÓN:
    /// Valida que el flujo de creación de órdenes y de consulta funcione correctamente
    /// a través de las abstracciones y encapsulaciones SOLID introducidas.
    /// </summary>
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
            // Arrange
            var context = GetInMemoryDbContext();
            
            // DEPENDENCY INVERSION PRINCIPLE (DIP):
            // Instanciamos el repositorio real y el mock del endpoint de eventos.
            var repository = new OrderRepository(context);
            var mockPublishEndpoint = new Mock<IPublishEndpoint>();

            var handler = new CreateOrderCommandHandler(repository, mockPublishEndpoint.Object);

            var orderItemsInput = new List<OrderItemInput>
            {
                new(BookId: 1, Quantity: 2, UnitPrice: 15.00m),
                new(BookId: 2, Quantity: 1, UnitPrice: 20.00m)
            };

            var command = new CreateOrderCommand("customer@deloitte.com", orderItemsInput);

            // Act
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

            // Assert: 2. LA MAGIA DEL MOCK: Verificar que el comando publicó el evento de integración
            mockPublishEndpoint.Verify(
                x => x.Publish(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()),
                Times.Once
            );
        }

        [Fact]
        public async Task GetOrdersQueryHandler_Should_Return_All_Orders_Untracked()
        {
            // Arrange
            var context = GetInMemoryDbContext();
            var order = new Order { CustomerEmail = "query@test.com" };
            order.AddItem(bookId: 10, quantity: 3, unitPrice: 12.00m);
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            var repository = new OrderRepository(context);
            var handler = new GetOrdersQueryHandler(repository);
            var query = new GetOrdersQuery();

            // Act
            var result = await handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Count.Should().Be(1);
            result.First().CustomerEmail.Should().Be("query@test.com");
            result.First().TotalAmount.Should().Be(36.00m);
            result.First().Items.Count.Should().Be(1);
            result.First().Items.First().BookId.Should().Be(10);
        }

        // LISKOV SUBSTITUTION PRINCIPLE (LSP) / DEPENDENCY INVERSION (DIP):
        // Demostramos el uso de un fake o mock para simular IOrderRepository.
        // Aquí usamos Moq para simular la persistencia y asegurar que el handler interactúa
        // de forma correcta con el repositorio abstracto.
        [Fact]
        public async Task CreateOrder_Should_Call_Repository_Add_And_SaveChanges()
        {
            // Arrange
            var mockRepository = new Mock<IOrderRepository>();
            var mockPublishEndpoint = new Mock<IPublishEndpoint>();
            
            var handler = new CreateOrderCommandHandler(mockRepository.Object, mockPublishEndpoint.Object);
            var command = new CreateOrderCommand("unit-test@solid.com", new List<OrderItemInput> { new(1, 1, 10.00m) });

            // Act
            await handler.Handle(command, CancellationToken.None);

            // Assert
            mockRepository.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
            mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
            mockPublishEndpoint.Verify(p => p.Publish(It.IsAny<OrderCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
