using System;
using System.Collections.Generic;

namespace OrderService.Data
{
    /// <summary>
    /// SINGLE RESPONSIBILITY PRINCIPLE (SRP) & ENCAPSULATION:
    /// La clase Order (Entidad de Dominio / Raíz de Agregado) es responsable de mantener su propio estado
    /// y hacer cumplir sus reglas de integridad (invariantes).
    /// El cálculo del TotalAmount y la instanciación de OrderItems ocurren dentro del propio objeto Order,
    /// evitando que código externo altere el total de manera inconsistente.
    /// </summary>
    public class Order
    {
        public int Id { get; set; }
        public required string CustomerEmail { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        
        // Modificador private set para impedir alteraciones arbitrarias desde fuera del objeto.
        public decimal TotalAmount { get; private set; }
        
        // Una orden contiene muchos ítems de libros
        public List<OrderItem> Items { get; set; } = [];

        /// <summary>
        /// Agrega un ítem de libro a la orden y recalcula el monto total.
        /// </summary>
        public void AddItem(int bookId, int quantity, decimal unitPrice)
        {
            if (quantity <= 0)
            {
                throw new ArgumentException("La cantidad debe ser mayor a cero.", nameof(quantity));
            }
            if (unitPrice < 0)
            {
                throw new ArgumentException("El precio unitario no puede ser negativo.", nameof(unitPrice));
            }

            var orderItem = new OrderItem
            {
                BookId = bookId,
                Quantity = quantity,
                UnitPrice = unitPrice,
                Order = this
            };

            Items.Add(orderItem);
            TotalAmount += quantity * unitPrice;
        }
    }
}

