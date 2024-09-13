using System;
using System.Collections.Generic;

namespace NexsusEcommerce.Models
{
    public class DashboardViewModel
    {
        public decimal TotalSales { get; set; }
        public int TotalOrders { get; set; }
        public List<CategorySales> SalesByProduct { get; set; }
        public List<DayWiseSales> DayWiseSales { get; set; } // Day-wise sales data
        public List<Category> Categories { get; set; } // Categories for dropdown
        public int SelectedCategoryId { get; set; } // Selected category ID
        public DateTime? StartDate { get; set; } // Start date for filtering
        public DateTime? EndDate { get; set; } // End date for filtering
        public decimal Baseline { get; set; }
        public List<CategorySales> SalesByCategory { get; internal set; }
        internal List<CityUserCount> UserCountByCity { get; set; }
    }


    public class DayWiseSales
    {
        public DateTime Date { get; set; }
        public decimal TotalSales { get; set; }
    }
    public class CategorySales
    {
        public string Category { get; set; }
        public decimal TotalSales { get; set; }
    }

}
