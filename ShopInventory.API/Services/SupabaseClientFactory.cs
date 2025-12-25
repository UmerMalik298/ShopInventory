using ShopInventory.API.Interfaces;

namespace ShopInventory.API.Services
{
    public class SupabaseClientFactory : ISupabaseClientFactory
    {
        private readonly Supabase.Client _client;

        public SupabaseClientFactory(IConfiguration config)
        {
            var supabaseUrl = config["Supabase:Url"];
            var supabaseKey = config["Supabase:Key"];

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
                throw new ArgumentException("Supabase URL or Key is not configured properly");

            _client = new Supabase.Client(supabaseUrl, supabaseKey);
            _client.InitializeAsync().Wait();
        }

        public Supabase.Client GetClient() => _client;
    }
}
