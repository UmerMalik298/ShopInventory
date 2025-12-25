namespace ShopInventory.API.Interfaces
{
    public interface ISupabaseClientFactory
    {
        Supabase.Client GetClient();
    }
}
