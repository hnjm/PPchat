using System.Threading.Tasks;

namespace PPchat_lib
{
    public interface IPrinter
    {
        Task Print(string message);
    }
}
