namespace ShopInventory.Application.Interfaces
{
    public interface ISyncService
    {
        Task SyncAsync();
        Task<bool> IsOnlineAsync();
        Task<SyncResult> GetLastSyncStatusAsync();
    }

    public class SyncResult
    {
        public bool Success { get; set; }
        public DateTime? LastSyncedAt { get; set; }
        public int TotalSynced { get; set; }
        public int TotalFailed { get; set; }
        public string? ErrorMessage { get; set; }
    }
}