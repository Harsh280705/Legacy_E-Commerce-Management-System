namespace Ecommerce.Data
{
    /// <summary>Database-connectivity probe living in the Data layer.</summary>
    public class DatabaseHealthCheck : Core.Interfaces.IDatabaseHealthCheck
    {
        private readonly EcommerceDbContext _db;
        public DatabaseHealthCheck(EcommerceDbContext db) { _db = db; }
        public bool IsHealthy()
        {
            try
            {
                _db.Database.Connection.Open();
                _db.Database.Connection.Close();
                return true;
            }
            catch { return false; }
        }
    }
}
