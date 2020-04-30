using Sage.Peachtree.API;
using Serilog;
using System;
using System.Collections.Generic;

namespace IWErpnextPoll
{
    internal class CreateSalesInvoiceHandler : AbstractDocumentHandler
    {
        public CreateSalesInvoiceHandler(Company company, ILogger logger) : base(company, logger) { }

        public override object Handle(object request)
        {
            SalesInvoice salesInvoice = CreateNewSalesInvoice(request as SalesInvoiceDocument);
            this.SetNext(salesInvoice != null ? new LogSalesInvoiceHandler(Company, Logger) : null);
            return base.Handle(request);
        }

        private SalesInvoice CreateNewSalesInvoice(SalesInvoiceDocument document)
        {
            SalesInvoice salesInvoice = Company.Factories.SalesInvoiceFactory.Create();
            if (salesInvoice != null)
            {
                try
                {
                    /** TODO: customer ID is a more reliable way to get Sage CUstomers */
                    salesInvoice.CustomerReference = CustomerReferences[document.Customer];
                    salesInvoice.CustomerPurchaseOrderNumber = document.PoNo;
                    salesInvoice.CustomerNote = document.NotesOrSpecialInstructions;
                    salesInvoice.Date = document.PostingDate;
                    salesInvoice.DateDue = document.DueDate;
                    salesInvoice.DiscountAmount = document.DiscountAmount;
                    salesInvoice.ReferenceNumber = document.Name;
                    salesInvoice.ShipVia = document.ShippingMethod;
                    salesInvoice.TermsDescription = document.PaymentTermsTemplate;

                    foreach (var line in document.Items)
                    {
                        AddLine(salesInvoice, line);
                    }

                    salesInvoice.Save();
                    Logger.Information("Sales Invoice - {0} was saved successfully", document.Name);
                }
                catch (KeyNotFoundException e)
                {
                    // push to a handler to create this missing customer
                    Logger.Debug(e, e.Message);
                    Logger.Debug("@{Document}", document);
                    Logger.Debug("@{E}", e);
                    salesInvoice = null;
                }
                catch (Sage.Peachtree.API.Exceptions.RecordInUseException e)
                {
                    // abort. The unsaved data will eventually be re-queued
                    salesInvoice = null;
                    Logger.Debug(e, e.Message);
                    Logger.Debug("@{Document} will be sent back to the queue", document);
                    Logger.Debug("@{E}", e);
                }
                catch (Sage.Peachtree.API.Exceptions.ValidationException e)
                {
                    // abort. This could be a Sales Invoice that has already been saved.
                    Logger.Debug(e, e.Message);
                    Logger.Debug("@{Document} will be sent back to the queue", document);
                    Logger.Debug("@{Document}", document);
                    Logger.Debug("@{E}", e);
                    salesInvoice = null;
                }
                catch (Exception e)
                {
                    salesInvoice = null;
                    Logger.Debug(e, e.Message);
                    Logger.Debug("@{E}", e);
                }
            }
            return salesInvoice;
        }

        private void AddLine(SalesInvoice salesInvoice, SalesInvoiceItem line)
        {
            if (line.ForFreight == 1)
            {
                salesInvoice.FreightAmount = line.Amount;
            }
            else if (line.ForHandling != 1)
            {
                SalesInvoiceSalesLine _ = salesInvoice.AddSalesLine();
                EntityReference itemReference = ItemReferences[line.ItemCode];
                ServiceItem item = Company.Factories.ServiceItemFactory.Load(itemReference as EntityReference<ServiceItem>);
                _.AccountReference = item.SalesAccountReference;
                _.Quantity = line.Qty;
                _.UnitPrice = line.Rate;
                _.Amount = line.Amount;
                _.Description = line.Description;
                _.InventoryItemReference = itemReference;
            }
        }
    }
}