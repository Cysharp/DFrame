using System.Threading.Tasks;

namespace DFrame
{
    public interface IDistributedCollection<T>
    {
        string Key { get; }
        Task<int> GetCountAsync(); // TODO: CountAsync???
        Task<T[]> ToArrayAsync();
    }
}
