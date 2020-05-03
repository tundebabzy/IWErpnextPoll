using Sage.Peachtree.API;
using Serilog;
using System;
using System.Collections.Generic;

namespace IWErpnextPoll
{
    class CreateSalesOrderHandler : AbstractDocumentHandler
    {
        public CreateSalesOrderHandler(Company c, ILogger logger) : base(c, logger) { }


        public override object Handle(object request)
        {
            SalesOrder salesOrder = CreateNewSalesOrder(request as SalesOrderDocument);
            this.SetNext(salesOrder != null ? new LogSalesOrderHandler(Company, Logger) : null);
            return base.Handle(request);
        }

        private SalesOrder CreateNewSalesOrder(SalesOrderDocument document)
        {
            SalesOrder salesOrder = Company.Factories.SalesOrderFactory.Create();
            if (salesOrder != null)
            {
                try
                {
                    /** TODO: customer ID is a more reliable way to get Sage CUstomers */
                    salesOrder.CustomerReference = CustomerReferences[document.Customer];
                    salesOrder.CustomerPurchaseOrderNumber = document.PoNo;
                    salesOrder.CustomerNote = document.NotesOrSpecialInstructions;
                    salesOrder.Date = document.TransactionDate;
                    salesOrder.DiscountAmount = document.DiscountAmount;
                    salesOrder.DiscountDate = document.TransactionDate;
                    salesOrder.ReferenceNumber = document.Name;
                    salesOrder.ShipByDate = document.DeliveryDate;
                    salesOrder.ShipVia = document.ShippingMethod;
                    salesOrder.TermsDescription = document.PaymentTermsTemplate;

                    foreach (var line in document.Items)
                    {
                        AddLine(salesOrder, line);
                    }

                    salesOrder.Save();
                    Logger.Information("Sales Order - {0} was saved successfully", document.Name);
                }
                catch (KeyNotFoundException e)
                {
                    // push to a handler to create this missing customer
                    Logger.Debug(e, e.Message);
                    Logger.Debug("@{Document}", document);
                    Logger.Debug("@{E}", e);
                    salesOrder = null;
                    SetNext(new CreateCustomerHandler(Company, Logger));
                }
                catch (Sage.Peachtree.API.Exceptions.RecordInUseException e)
                {
                    // abort. The unsaved data will eventually be re-queued
                    salesOrder = null;
                    Logger.Debug(e, e.Message);
                    Logger.Debug("@{Document} will be sent back to the queue", document);
                    Logger.Debug("@{E}", e);
                }
                catch (Sage.Peachtree.API.Exceptions.ValidationException e)
                {
                    // abort. This could be a sales order that has already been saved.
                    Logger.Debug(e, e.Message);
                    Logger.Debug("@{Document} will be sent back to the queue", document);
                    Logger.Debug("@{Document}", document);
                    Logger.Debug("@{E}", e);
                    salesOrder = null;
                }
                catch (Exception e)
                {
                    salesOrder = null;
                    Logger.Debug(e, e.Message);
                    Logger.Debug("@{E}", e);
                }
            }
            return salesOrder;
        }

        /**
         * Adds a `SalesOrderLine` to a `SalesOrder` object and populates it.
         * Note:
         * If the given `SalesOrderItem.ForFreight` property is 1, the amount is
         * is added to `SalesOrder.Freight` and no `SalesOrderLine` is added.
         * If the given `SalesOrderItem.ForHandling` property is 1, no `SalesOrderLine`
         * is added to the `SalesOrderLine`.
         */
        private void AddLine(SalesOrder salesOrder, SalesOrderItem line)
        {
            if (line.ForFreight == 1)
            {
                salesOrder.FreightAmount = line.Amount;
            }
            else if (line.ForHandling != 1)
            {
                SalesOrderLine _ = salesOrder.AddLine();
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