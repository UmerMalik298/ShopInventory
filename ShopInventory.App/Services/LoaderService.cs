namespace ShopInventory.App.Services
{
    public class LoaderService
    {
        private int _activeCount = 0;
        public bool IsLoading => _activeCount > 0;
        public event Action? OnChanged;

        public void Show()
        {
            _activeCount++;
            OnChanged?.Invoke();
        }

        public void Hide()
        {
            _activeCount = Math.Max(0, _activeCount - 1);
            OnChanged?.Invoke();
        }

        // Use this in try/finally so loader always hides even on error
        public async Task RunAsync(Func<Task> action)
        {
            Show();
            try { await action(); }
            finally { Hide(); }
        }
    }
}