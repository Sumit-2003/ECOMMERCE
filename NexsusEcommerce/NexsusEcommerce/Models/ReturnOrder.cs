using System;
using System.Collections.Generic;

namespace NexsusEcommerce.Models;

public partial class ReturnOrder
{
    public int ReturnOrderId { get; set; }

    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int UserId { get; set; }

    public int Quantity { get; set; }

    public string ProductName { get; set; } = null!;

    public string Action { get; set; } = null!;

    public string? Reason { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string? Comfirmation { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
