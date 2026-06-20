namespace OrderService.Data
{
    public class Order
    {
        public int Id { get; set; }
        public required string CustomerEmail { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        
        // Una orden contiene muchos ítems de libros
        public List<OrderItem> Items { get; set; } = [];
    }
}
