using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Domain.Entities.Enums
{
    public enum StockMoveType
    {
        StockIn = 1,
        Sale = 2,
        Return = 3,
        Adjust = 4
    }
}
