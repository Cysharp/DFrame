using System.Threading.Tasks;

namespace DFrame
{
    public interface IDistributedCollection<T>
    {
        string Key { get; }
        Task<int> GetCountAsync();
        Task<T[]> ToArrayAsync();
    }
}
