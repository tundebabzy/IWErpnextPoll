﻿using RestSharp;
using Sage.Peachtree.API;
using Serilog;
using System;
using Sage.Peachtree.API.Exceptions;

namespace IWErpnextPoll
{
    internal class CreateCustomerHandler : AbstractDocumentHandler, IResourceAddress
    {
        protected string CustomerName { get; set; }
        public CreateCustomerHandler(Company company, ILogger logger, EmployeeInformation employeeInformation) : base(company, logger, employeeInformation) { }

        public override object Handle(object request)
        {
            var customerName = (request as SalesOrderDocument)?.Customer;
            var customerDocument = customerName != null ? GetCustomerDetails(customerName) : null;
            var customer = customerDocument != null ? CreateNewCustomer(customerDocument) : null;
            this.SetNext(customer != null ? new LogCustomerCreatedHandler(Company, Logger, EmployeeInformation) : null);
            return base.Handle(customerDocument);
        }

        private CustomerDocument GetCustomerDetails(string customerName)
        {
            var receiver = new CustomerCommand(customerName, $"{GetResourceServerAddress()}?cn={customerName}");
            var document = receiver.Execute();

            return document.Data.Message;
        }

        private Customer CreateNewCustomer(CustomerDocument customerDocument)
        {
            var customer = Company.Factories.CustomerFactory.Create();
            if (customer == null)
            {
                Logger.Information("Customer data was null when trying to create Sage customer");
                return null;
            }

            if (customerDocument == null || customerDocument.Addresses.Count == 0)
            {
                Logger.Information("Customer has no address so the customer cannot be created");
                return customer;
            }
            try
            {
                customer.ID = customerDocument.OldCustomerId;    // add a field - ID to Customer doctype
                customer.Name = customerDocument.CustomerName;
                AddContact(customer, customerDocument);
                AddSalesRep(customer, customerDocument);

                customer.Save();
                Logger.Information("Customer - {Customer} saved successfully", customerDocument.CustomerName);
            }
            catch (ValidationException e)
            {
                Logger.Debug("Validation failed.");
                Logger.Debug(e.Message);
                Logger.Debug("{@Name} will be sent back to the queue", customerDocument.Name);
                customer = null;
            }
            catch (RecordInUseException)
            {
                customer = null;
                Logger.Debug("Record is in use. {@Name} will be sent back to the queue", customerDocument.Name);
            }
            catch (Exception e)
            {
                customer = null;
                Logger.Debug(e, e.Message);
                Logger.Debug("{@E}", e);
            }

            return customer;
        }

        private void AddContact(Customer customer, CustomerDocument customerDocument)
        {
            if (customerDocument.Addresses.Count > 0)
            {
                customer.Email = customerDocument.CustomerEmail;
                customer.ShipVia = customerDocument.ShipVia;
                customer.WebSiteURL = customerDocument.CompanyWebsite;
                customer.CustomerSince = DateTime.Now;
                customer.IsInactive = customerDocument.Disabled == 1;
                AddAddresses(customer, customerDocument);
                AddContacts(customer, customerDocument);
            }
            else
            {
                Logger.Information("Customer {@Name} did not have addresses so will not create a contact", customer.Name);
                ContactList contactsList = customer.Contacts;
                contactsList.Clear();
            }
        }

        private void AddAddresses(Customer customer, CustomerDocument customerDocument)
        {
            AddressDocument billingAddress = customerDocument.Addresses.Find(x => x.AddressType == "Billing");
            AddressDocument shippingAddress = customerDocument.Addresses.Find(x => x.AddressType == "Shipping");
            if (billingAddress != null)
            {
                customer.BillToContact.Address.Address1 = billingAddress.AddressLine1;
                customer.BillToContact.Address.Address2 = billingAddress.AddressLine2;
                customer.BillToContact.Address.City = billingAddress.City;
                customer.BillToContact.Address.State = billingAddress.State;
                customer.BillToContact.Address.Zip = billingAddress.Pincode;
                customer.BillToContact.Address.Country = billingAddress.Country;
            }
            if (shippingAddress != null)
            {
                customer.ShipToContact.Address.Address1 = shippingAddress.AddressLine1;
                customer.BillToContact.Address.Address2 = shippingAddress.AddressLine2;
                customer.BillToContact.Address.City = shippingAddress.City;
                customer.BillToContact.Address.State = shippingAddress.State;
                customer.BillToContact.Address.Zip = shippingAddress.Pincode;
                customer.BillToContact.Address.Country = shippingAddress.Country;
            }
        }

        private void AddSalesRep(Customer customer, CustomerDocument customerDocument)
        {
            EmployeeList employees = Company.Factories.EmployeeFactory.List();
            employees.Load();
            foreach (var item in employees)
            {
                if (item.Name != customerDocument.SalesRep) continue;
                customer.SalesRepresentativeReference = item.Key;
                break;
            }
        }

        private void AddContacts(Customer customer, CustomerDocument customerDocument)
        {
            foreach (var c in customerDocument.Contacts)
            {
                if (!string.IsNullOrEmpty(customer.BillToContact.CompanyName) && !string.IsNullOrEmpty(customer.ShipToContact.CompanyName))
                {
                    break;
                }
                else if (customer.BillToContact.Address.Address1 != null && (string.IsNullOrEmpty(customer.BillToContact.CompanyName)
                                                                             && (customer.BillToContact.Address.Address1 != null || customer.BillToContact.Address.Address1.Length != 0)))
                {
                    customer.BillToContact.CompanyName = customerDocument.CustomerName;
                    customer.BillToContact.Email = c.EmailId;
                    customer.BillToContact.FirstName = c.FirstName;
                    customer.BillToContact.Gender = c.Gender == "Male" ? Gender.Male : c.Gender == "Female" ? Gender.Female : Gender.NotSpecified;
                    customer.BillToContact.LastName = c.LastName;
                    customer.BillToContact.Title = c.Salutation;
                } else if (customer.ShipToContact.Address.Address1 != null && (string.IsNullOrEmpty(customer.ShipToContact.CompanyName)
                                                                               && (customer.ShipToContact.Address.Address1 != null || customer.ShipToContact.Address.Address1.Length != 0)))
                {
                    customer.ShipToContact.CompanyName = customerDocument.CustomerName;
                    customer.ShipToContact.Email = c.EmailId;
                    customer.ShipToContact.FirstName = c.FirstName;
                    customer.ShipToContact.Gender = c.Gender == "Male" ? Gender.Male : c.Gender == "Female" ? Gender.Female : Gender.NotSpecified;
                    customer.ShipToContact.LastName = c.LastName;
                    customer.ShipToContact.Title = c.Salutation;
                }
            }
        }

        public string GetResourceServerAddress()
        {
            return $"{Constants.ServerUrl}/api/method/electro_erpnext.utilities.customer.get_customer_details";
        }
    }
}