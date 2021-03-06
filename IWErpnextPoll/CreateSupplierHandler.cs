﻿using Sage.Peachtree.API;
using Serilog;
using System;

namespace IWErpnextPoll
{
    internal class CreateSupplierHandler : AbstractDocumentHandler, IResourceAddress
    {
        public CreateSupplierHandler(Company company, ILogger logger) : base(company, logger) { }

        public override object Handle(object request)
        {
            Logger.Information("Version {@Version}", Constants.Version);
            var supplierName = (request as PurchaseOrderDocument)?.Supplier;
            var supplierDocument = GetSupplierDetails(supplierName);
            var supplier = CreateNewSupplier(supplierDocument);
            this.SetNext(supplier != null ? new LogSupplierCreatedHandler(Company, Logger) : null);
            return base.Handle(supplierDocument);
        }

        private Vendor CreateNewSupplier(SupplierDocument supplierDocument)
        {
            var supplier = Company.Factories.VendorFactory.Create();
            if (supplierDocument != null && supplier != null)
            {
                try
                {
                    supplier.ID = supplierDocument.VendorId;
                    supplier.Email = supplierDocument.VendorEmail;
                    supplier.Name = supplierDocument.SupplierName;
                    supplier.WebSiteURL = supplierDocument.Website;
                    supplier.IsInactive = supplierDocument.Disabled == 1;

                    // AddAddresses(supplier, supplierDocument);
                    // AddContacts(supplier, supplierDocument);

                    supplier.Save();
                    Logger.Information("Supplier - {SupplierName} saved successfully", supplierDocument.SupplierName);
                }
                catch (Sage.Peachtree.API.Exceptions.ValidationException e)
                {
                    Logger.Debug("Validation failed.");
                    Logger.Debug(e.Message);
                    Logger.Debug("{@Name} will be sent back to the queue", supplierDocument.Name);
                    supplier = null;
                }
                catch (Sage.Peachtree.API.Exceptions.RecordInUseException)
                {
                    supplier = null;
                    Logger.Debug("Record is in use. {@Name} will be sent back to the queue", supplierDocument.Name);
                }
                catch (Exception e)
                {
                    supplier = null;
                    Logger.Debug(e, e.Message);
                    Logger.Debug("{@E}", e);
                }
            }

            if (supplier == null)
            {
                Logger.Debug("Supplier data was null when trying to create Sage customer");
            }

            return supplier;
        }

        private SupplierDocument GetSupplierDetails(string supplierName)
        {
            var receiver = new SupplierCommand(supplierName, $"{GetResourceServerAddress()}?cn={supplierName}");
            var document = receiver.Execute();

            return document.Data.Message;
        }

        public string GetResourceServerAddress()
        {
            return
                $"{Constants.ServerUrl}/api/method/electro_erpnext.utilities.supplier.get_supplier_details";
        }
    }
}