using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OrderService.Data.Repositories
{
    /// <summary>
    /// INTERFACE SEGREGATION PRINCIPLE (ISP):
    /// Esta interfaz contiene un conjunto altamente cohesivo de métodos de acceso a datos de órdenes.
    /// No se mezclan operaciones de libros, stock o pasarelas de pago, lo que mantiene el contrato limpio y enfocado.
    /// 
    /// DEPENDENCY INVERSION PRINCIPLE (DIP):
    /// Los componentes superiores de la aplicación (los handlers de MediatR) consumen esta abstracción
    /// en lugar de instanciar o acoplarse con la tecnología de acceso a datos directa (DbContext/EF Core).
    /// </summary>
    public interface IOrderRepository
    {
        // --- CQRS Read Operations (Consultas / Queries) ---
        // Optimizado para lecturas sin rastreo de cambios en la base de datos (AsNoTracking).
        
        Task<List<Order>> GetAllWithItemsAsync(CancellationToken cancellationToken = default);

        // --- CQRS Write Operations (Comandos / Commands) ---
        // Operaciones destinadas a la persistencia y cambios de estado con rastreo activo (Tracking).

        Task AddAsync(Order order, CancellationToken cancellationToken = default);
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
