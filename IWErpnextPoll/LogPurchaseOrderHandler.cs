using Sage.Peachtree.API;
using Serilog;

namespace IWErpnextPoll
{
    class LogPurchaseOrderHandler : AbstractDocumentHandler
    {
        public LogPurchaseOrderHandler(Company c, ILogger logger, EmployeeInformation employeeInformation) : base(c, logger, employeeInformation)
        {
        }

        public override object Handle(object request)
        {
            LogPurchaseOrder(request as PurchaseOrderDocument);
            return base.Handle(request);
        }

        private void LogPurchaseOrder(PurchaseOrderDocument document)
        {
            var url = $"{Constants.ServerUrl}/api/resource/Sage 50 Export Log";
            var resource = new Resource(url);
            resource.LogPurchaseOrder(document);
        }
    }
}
