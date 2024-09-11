using System.Collections.Generic;

public class DashboardViewModel
{
    public decimal TotalSales { get; set; }
    public int TotalOrders { get; set; }
    public IEnumerable<CategorySales> SalesByCategory { get; set; }
    public IEnumerable<CityUserCount> UserCountByCity { get; set; } // Added property for user count by city
}

public class CategorySales
{
    public string Category { get; set; }
    public decimal TotalSales { get; set; }
}

public class CityUserCount
{
    public string City { get; set; }
    public int UserCount { get; set; }
}
