using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Linq;
using Villa_API_Project.DataAccess.Data;
using Villa_API_Project.DataAccess.Reposatory.IReposatory;

namespace Villa_API_Project.DataAccess.Reposatory
{
    public class Reposatory<T> : IReposatory<T> where T : class
    {

        private Context context;
        internal DbSet<T> Dbset;
        public Reposatory(Context context)
        {
            this.context = context;
            Dbset = context.Set<T>();
        }
        public void Create(T model)
        {
            Dbset.Add(model);
        }

        public void Delete(T model)
        {
            Dbset.Remove(model);
        }

        public List<T> GetALL(Expression<Func<T, bool>>? filter = null, string? Includes = null, int pageSize = 0, int pageNumber = 1)

        {
            IQueryable<T> query = Dbset;
            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (pageSize > 0)
            {
                if (pageSize > 100)
                {
                    pageSize = 100;
                }
          
                query = query.Skip(pageSize * (pageNumber - 1)).Take(pageSize);
            }

            if (!string.IsNullOrEmpty(Includes))
            {
                foreach (string item in Includes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(item);
                }
            }
            return query.ToList();
        }

        public T GetByFilter(Expression<Func<T, bool>> filter, string? Includes = null)
        {
            var query = Dbset.Where(filter);
            if (!string.IsNullOrEmpty(Includes))
            {
                foreach (string item in Includes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(item);
                }
            }
            return query.FirstOrDefault();
        }
        public T GetByFilterAsnoTraking(Expression<Func<T, bool>> filter, string? Includes = null)
        {
            var query = Dbset.AsNoTracking().Where(filter);
            if (!string.IsNullOrEmpty(Includes))
            {
                foreach (string item in Includes.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(item);
                }
            }
            return query.FirstOrDefault();
        }
        public void Update(T model)
        {
            Dbset.Update(model);
            Console.WriteLine(model);
        }

        public void RemoveRange(IEnumerable<T> entity)
        {
            Dbset.RemoveRange(entity);
        }
    }
}
