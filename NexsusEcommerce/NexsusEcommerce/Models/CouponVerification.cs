using System;
using System.Collections.Generic;

namespace NexsusEcommerce.Models;

public partial class CouponVerification
{
    public int VerificationId { get; set; }

    public int UserId { get; set; }

    public int CouponId { get; set; }

    public string CouponCode { get; set; } = null!;

    public DateTime? VerificationDate { get; set; }

    public bool IsCouponUsed { get; set; }

    public virtual Coupon Coupon { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
