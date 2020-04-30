using Sage.Peachtree.API;
using Serilog;

namespace IWErpnextPoll
{
    class LogPurchaseOrderHandler : AbstractDocumentHandler
    {
        public LogPurchaseOrderHandler(Company c, ILogger logger) : base(c, logger)
        {
        }

        public override object Handle(object request)
        {
            LogPurchaseOrder(request as PurchaseOrderDocument);
            return base.Handle(request);
        }

        private void LogPurchaseOrder(PurchaseOrderDocument document)
        {
            string url = "https://portal.electrocomptr.com/api/resource/Sage 50 Export Log";
            Resource resource = new Resource(url);
            resource.LogPurchaseOrder(document);
        }
    }
}
