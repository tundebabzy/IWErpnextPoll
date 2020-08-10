using Sage.Peachtree.API;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using Sage.Peachtree.API.Collections.Generic;
using Sage.Peachtree.API.Validations;

namespace IWErpnextPoll
{
    class CreateSalesOrderHandler : AbstractDocumentHandler
    {
        public CreateSalesOrderHandler(Company c, ILogger logger, EmployeeInformation employeeInformation=null) : base(c, logger, employeeInformation) { }


        public override object Handle(object request)
        {
            var salesOrder = CreateNewSalesOrder(request as SalesOrderDocument);
            if (GetNext() == null)
            {
                this.SetNext(salesOrder != null ? new LogSalesOrderHandler(Company, Logger, EmployeeInformation) : null);
            }
            return base.Handle(request);
        }

        private SalesOrder CreateNewSalesOrder(SalesOrderDocument document)
        {
            var customerDocument = GetCustomerFromErpNext(document.CustomerName);
            var salesOrder = Company.Factories.SalesOrderFactory.Create();
            var customerEntityReference = GetCustomerEntityReference(customerDocument?.OldCustomerId);
            if (customerEntityReference == null)
            {
                Logger.Debug("Customer {@name} in {@Document} was not found in Sage.", document.Customer, document.Name);
                salesOrder = null;
                SetNext(new CreateCustomerHandler(Company, Logger, EmployeeInformation));
                Logger.Debug("Customer {@name} has been queued for creation in Sage", document.Customer);
            }
            else if (salesOrder != null)
            {
                try
                {
                    salesOrder.CustomerReference = customerEntityReference;
                    salesOrder.CustomerPurchaseOrderNumber = document.PoNo;
                    salesOrder.CustomerNote = document.NotesOrSpecialInstructions;
                    salesOrder.Date = document.TransactionDate;
                    salesOrder.DiscountAmount = document.DiscountAmount;
                    salesOrder.DiscountDate = document.TransactionDate;
                    salesOrder.ReferenceNumber = document.Name;
                    salesOrder.ShipByDate = document.DeliveryDate;
                    salesOrder.ShipVia = document.ShippingMethod;
                    salesOrder.TermsDescription = document.PaymentTermsTemplate;
                    salesOrder.CustomerPurchaseOrderNumber = document.PoNo;
                    AddSalesRep(salesOrder, document);
                    AddShipAddress(salesOrder);

                    foreach (var line in document.Items)
                    {
                        AddLine(salesOrder, line);
                    }

                    salesOrder.Save();
                    Logger.Information("Sales Order - {0} was saved successfully", document.Name);
                }
                catch (Sage.Peachtree.API.Exceptions.RecordInUseException)
                {
                    // abort. The unsaved data will eventually be re-queued
                    salesOrder = null;
                    Logger.Debug("Record is in use. {@Name} will be sent back to the queue", document.Name);
                }
                catch (Sage.Peachtree.API.Exceptions.ValidationException e)
                {
                    Logger.Debug("Validation failed.");
                    Logger.Debug(e.Message);
                    if (e.ProblemList.OfType<DuplicateValueProblem>().Any() && e.Message.Contains("duplicate reference number"))
                    {
                        Logger.Debug("{@Name} is already in Sage so will notify ERPNext", document.Name);
                    }
                    else
                    {
                        Logger.Debug("{@Name} will be sent back to the queue", document.Name);
                        salesOrder = null;   
                    }
                }
                catch (Exception e)
                {
                    salesOrder = null;
                    Logger.Debug(e, e.Message);
                    Logger.Debug("{@E}", e);
                }
            }
            return salesOrder;
        }

        private void AddShipAddress(SalesOrder salesOrder)
        {
            var customer = Company.Factories.CustomerFactory.Load(salesOrder.CustomerReference);
            var contact = customer.ShipToContact;
            salesOrder.ShipToAddress.Name = customer.Name;
            salesOrder.ShipToAddress.Address.Zip = contact.Address.Zip;
            salesOrder.ShipToAddress.Address.Address1 = contact.Address.Address1;
            salesOrder.ShipToAddress.Address.Address2 = contact.Address.Address2;
            salesOrder.ShipToAddress.Address.City = contact.Address.City;
            salesOrder.ShipToAddress.Address.State = contact.Address.State;
            salesOrder.ShipToAddress.Address.Country = contact.Address.Country;
        }

        private void AddSalesRep(SalesOrder salesOrder, SalesOrderDocument document)
        {
            if (document.SalesRep == null) return;
            var salesRep = EmployeeInformation.Data[document.SalesRep];
            salesOrder.SalesRepresentativeReference = salesRep;
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
                var _ = salesOrder.AddLine();
                var itemReference = ItemReferences[line.ItemCode];
                var item = Company.Factories.ServiceItemFactory.Load(itemReference as EntityReference<ServiceItem>);
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