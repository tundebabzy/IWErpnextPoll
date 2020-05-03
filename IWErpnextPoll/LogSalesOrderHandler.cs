using Sage.Peachtree.API;
using Serilog;

namespace IWErpnextPoll
{
    class LogSalesOrderHandler : AbstractDocumentHandler
    {
        public LogSalesOrderHandler(Company c, ILogger logger) : base(c, logger) { }

        public override object Handle(object request)
        {
            LogSalesOrder(request as SalesOrderDocument);
            return base.Handle(request);
        }

        private void LogSalesOrder(SalesOrderDocument document)
        {
            string url = "https://portal.electrocomptr.com/api/resource/Sage 50 Export Log";
            Resource resource = new Resource(url);
            resource.LogSalesOrder(document);
        }
    }
}
