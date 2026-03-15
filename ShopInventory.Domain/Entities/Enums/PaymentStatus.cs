using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Domain.Entities.Enums
{
    public enum PaymentStatus
    {
        Unpaid = 0,
        Paid = 1,
        PartiallyPaid = 2
    }
}
