using RestSharp;
using System.Threading.Tasks;

namespace IWErpnextPoll
{
    interface ICommand
    {
        Task<IRestResponse<object>> Execute();
    }
}
