using RestSharp;

namespace IWErpnextPoll
{
    class PurchaseOrderCommand
    {
        private readonly Resource _receiver;

        public PurchaseOrderCommand(string serverURL = "https://portal.electrocomptr.com")
        {
            _receiver = new Resource(serverURL);
        }

        public IRestResponse<PurchaseOrderResponse> Execute()
        {
            IRestResponse<PurchaseOrderResponse> documents = _receiver.GetPurchaseOrderList();
            return documents;
        }
    }
}
