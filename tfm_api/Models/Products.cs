namespace tfm_web.Models
{
    public class Products
    {
        public int Id { set; get; }
        public string ProductName { set; get; }
        public decimal  Price { set; get; }
        public decimal StockQuantity { set; get; }
        public string Description { set; get; }
        public int IsActive { set; get; }
        public DateTime CreatedDate { set; get; }
        public DateTime ExpireDate { set; get; }
    }
}
