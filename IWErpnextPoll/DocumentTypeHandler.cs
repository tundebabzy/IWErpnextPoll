using Sage.Peachtree.API;
using Serilog;

namespace IWErpnextPoll
{
    class DocumentTypeHandler : AbstractDocumentHandler
    {
        public DocumentTypeHandler(Company c, ILogger logger, EmployeeInformation employeeInformation) : base(c, logger, employeeInformation){ }
        public override object Handle(object request)
        {
            if ((request as SalesOrderDocument) != null && (request as SalesOrderDocument).Doctype == "Sales Order")
            {
                SetNext(new CreateSalesOrderHandler(Company, Logger, EmployeeInformation));
            }
            else if ((request as PurchaseOrderDocument) != null && (request as PurchaseOrderDocument).Doctype == "Purchase Order")
            {
                SetNext(new CreatePurchaseOrderHandler(Company, Logger));
            }
            else if ((request as SalesInvoiceDocument) != null && (request as SalesInvoiceDocument).Doctype == "Sales Invoice")
            {
                SetNext(new CreateSalesInvoiceHandler(Company, Logger, EmployeeInformation));
            }
            else
            {
                SetNext(null);
            }
            return base.Handle(request);
        }
    }
}
