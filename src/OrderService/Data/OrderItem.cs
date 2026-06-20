namespace OrderService.Data
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        
        // Relación con la orden principal (clave foránea)
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;
    }
}
