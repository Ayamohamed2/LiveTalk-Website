using System.Linq.Expressions;

namespace Villa_API_Project.DataAccess.Reposatory.IReposatory
{
    public interface IReposatory<T> where T:class
    {
        List<T> GetALL(Expression<Func<T, bool>>? filter = null, string? Includes = null, int pageSize = 0, int pageNumber = 1);
        T GetByFilter(Expression<Func<T, bool>> filter, string? Includes = null);
        T GetByFilterAsnoTraking(Expression<Func<T, bool>> filter, string? Includes = null);
        void Create(T model);
        void Update(T model);
        void Delete(T model);

        void RemoveRange(IEnumerable<T> entity);

    }
}
