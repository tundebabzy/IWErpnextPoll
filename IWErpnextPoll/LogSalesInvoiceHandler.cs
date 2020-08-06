using Sage.Peachtree.API;
using Serilog;

namespace IWErpnextPoll
{
    internal class LogSalesInvoiceHandler : AbstractDocumentHandler
    {
        public LogSalesInvoiceHandler(Company c, ILogger logger, EmployeeInformation employeeInformation) : base(c, logger, employeeInformation) { }

        public override object Handle(object request)
        {
            LogSalesInvoice(request as SalesInvoiceDocument);
            return base.Handle(request);
        }

        private void LogSalesInvoice(SalesInvoiceDocument salesInvoiceDocument)
        {
            var url = $"{Constants.ServerUrl}/api/resource/Sage 50 Export Log";
            var resource = new Resource(url);
            resource.LogSalesInvoice(salesInvoiceDocument);
        }
    }
}