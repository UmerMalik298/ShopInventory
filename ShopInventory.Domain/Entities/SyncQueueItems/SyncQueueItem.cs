using ShopInventory.Domain.Entities.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopInventory.Domain.Entities.SyncQueueItems
{
    public class SyncQueueItem : BaseEntity
    {
        public string EventType { get; set; } = "";     
        public string PayloadJson { get; set; } = "";  

        public int SyncStatusId { get; set; } 
        public int AttemptCount { get; set; } = 0;

        public DateTime? SyncedAt { get; set; }
        public string? LastError { get; set; }
    }
}
