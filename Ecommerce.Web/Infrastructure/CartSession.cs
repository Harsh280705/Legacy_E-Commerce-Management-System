using System.Web;

namespace Ecommerce.Web.Infrastructure
{
    /// <summary>Session-backed cart identity (legacy ASP.NET session adapter).</summary>
    public static class CartSession
    {
        private const string Key = "LE_CartId";
        private const string IdemKey = "LE_CheckoutKey";
        public static string GetCartId(HttpSessionStateBase session)
        {
            if (session[Key] == null) session[Key] = System.Guid.NewGuid().ToString("N");
            return (string)session[Key];
        }
        public static string GetCartId(System.Web.SessionState.HttpSessionState session)
        {
            if (session[Key] == null) session[Key] = System.Guid.NewGuid().ToString("N");
            return (string)session[Key];
        }
        public static string NewIdempotencyKey(HttpSessionStateBase session)
        {
            var k = System.Guid.NewGuid().ToString("N");
            session[IdemKey] = k;
            return k;
        }
        public static string PeekIdempotencyKey(HttpSessionStateBase session)
        {
            return session[IdemKey] as string;
        }
    }
}
