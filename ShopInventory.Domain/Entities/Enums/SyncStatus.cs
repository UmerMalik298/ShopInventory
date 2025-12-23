using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Domain.Entities.Enums
{
    public enum SyncStatus
    {
        Pending = 1,
        Synced = 2,
        Failed = 3
    }
}
