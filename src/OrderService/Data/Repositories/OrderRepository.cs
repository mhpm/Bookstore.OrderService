using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace OrderService.Data.Repositories
{
    /// <summary>
    /// SINGLE RESPONSIBILITY PRINCIPLE (SRP):
    /// La clase tiene la única responsabilidad de actuar como adaptador de persistencia para las Órdenes,
    /// encapsulando el uso del DbContext de EF Core y aislando al resto de la aplicación de los detalles de SQL Server/Postgres.
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _context;

        public OrderRepository(OrderDbContext context)
        {
            _context = context;
        }

        // --- Lecturas (CQRS - Optimización NoTracking) ---

        public async Task<List<Order>> GetAllWithItemsAsync(CancellationToken cancellationToken = default)
        {
            // Usamos AsNoTracking() porque es una consulta de solo lectura, mejorando enormemente
            // el rendimiento al no almacenar los objetos en el Change Tracker de EF Core.
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(cancellationToken);
        }

        // --- Escrituras (CQRS - Tracking Activo) ---

        public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            await _context.Orders.AddAsync(order, cancellationToken);
        }

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }
    }
}
