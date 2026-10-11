using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using Ecommerce.Core.Interfaces;

namespace Ecommerce.Data.Repositories
{
    public class EfRepository<T> : IRepository<T> where T : class
    {
        private readonly EcommerceDbContext _ctx;
        private readonly DbSet<T> _set;
        public EfRepository(EcommerceDbContext ctx) { _ctx = ctx; _set = ctx.Set<T>(); }
        public IQueryable<T> Query() { return _set; }
        public T GetById(object id) { return _set.Find(id); }
        public void Add(T entity) { _set.Add(entity); }
        public void Update(T entity) { _ctx.Entry(entity).State = EntityState.Modified; }
        public void Delete(T entity) { _set.Remove(entity); }
        public int Count(Expression<Func<T, bool>> predicate) { return _set.Count(predicate); }
    }

    public class EfUnitOfWork : IUnitOfWork
    {
        private readonly EcommerceDbContext _ctx;
        public EfUnitOfWork(EcommerceDbContext ctx) { _ctx = ctx; }
        public IRepository<T> Repository<T>() where T : class { return new EfRepository<T>(_ctx); }
        public int SaveChanges() { return _ctx.SaveChanges(); }
        public void Dispose() { _ctx.Dispose(); }
    }
}
