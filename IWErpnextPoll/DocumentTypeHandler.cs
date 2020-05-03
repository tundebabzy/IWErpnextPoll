using Sage.Peachtree.API;
using Serilog;

namespace IWErpnextPoll
{
    class DocumentTypeHandler : AbstractDocumentHandler
    {
        public DocumentTypeHandler(Company c, ILogger logger) : base(c, logger) { }
        public override object Handle(object request)
        {
            if ((request as SalesOrderDocument) != null && (request as SalesOrderDocument).Doctype == "Sales Order")
            {
                SetNext(new CreateSalesOrderHandler(Company, Logger));
            }
            else if ((request as PurchaseOrderDocument) != null && (request as PurchaseOrderDocument).Doctype == "Purchase Order")
            {
                SetNext(new CreatePurchaseOrderHandler(Company, Logger));
            }
            else if ((request as SalesInvoiceDocument) != null && (request as SalesInvoiceDocument).Doctype == "Sales Invoice")
            {
                SetNext(new CreateSalesInvoiceHandler(Company, Logger));
            }
            else
            {
                SetNext(null);
            }
            return base.Handle(request);
        }
    }
}
