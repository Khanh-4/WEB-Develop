namespace WebBanHang_Bai2.Services;

/// <summary>Danh sách yêu thích — lưu trong Session.</summary>
public class WishlistService
{
    private const string Key = "TechStore.Wishlist";
    private readonly IHttpContextAccessor _http;

    public WishlistService(IHttpContextAccessor http) => _http = http;

    private ISession Session => _http.HttpContext!.Session;

    public HashSet<int> GetIds() =>
        Session.GetObject<HashSet<int>>(Key) ?? new HashSet<int>();

    public bool Contains(int productId) => GetIds().Contains(productId);

    public bool Toggle(int productId)
    {
        var ids = GetIds();
        var added = ids.Add(productId);
        if (!added) ids.Remove(productId);
        Session.SetObject(Key, ids);
        return added;
    }

    public void Remove(int productId)
    {
        var ids = GetIds();
        ids.Remove(productId);
        Session.SetObject(Key, ids);
    }

    public int Count => GetIds().Count;
}
