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
            var url = $"{Constants.ServerUrl}/api/resource/Sage 50 Export Log";
            var resource = new Resource(url);
            resource.LogSalesOrder(document);
        }
    }
}
