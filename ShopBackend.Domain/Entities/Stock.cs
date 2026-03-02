using System;


namespace ShopBackend.Domain.Entities
{
    public class Stock
    {
        public int Id { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // = 0, falls man Produkte erstmal nur anlegt, die aber noch gar nicht im Lager verfügbar sind.
        public int Quantity { get; set; } = 0;
        public int ReservedQuantity { get; set; } = 0;
    }
}
