using RestSharp;
using Sage.Peachtree.API;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Timers;

namespace IWErpnextPoll
{
    public partial class ErpnextService : ServiceBase
    {
        private const string COMPANY_NAME = "Electro-Comp Tape & Reel Services, LLC";
        private const string APPLICATION_ID = "41Dzi73wlWhIwx8yTdVQ6dJRbGU9nJtcu47x2rY4MOS7u9tAALysow==VK7xyIAjtHr7JDWfDXJYhVLelt+UlNwmGpSMCjISTVFfEo6e0pRzQR7cnDraS9HTpJnP34yIDrgC4nA7QU1rMCPWNQ4eOs+7+dm5AV/mymbQWUEx+tNEQhDka3PJi6nFXkZEaDa+I6bhbQnAAg+65ZD6/+IJ02CjaAGCAzaRiQebwojCJiKuSKoiDk/xkDTuD975uiTLcGZ3ZrzByxfoPdaUkAPQRTaEFmiARO/eBtNg7nSXUvVaEF0NddSjTyf6jbwycx6NnzmRWk/qBJPW0g==";
        private bool _canRequest = true;
        private ILogger Logger { get; set; }
        public static PeachtreeSession Session { get; set; }
        public static Company Company { get; set; }
        public ConcurrentQueue<object> queue = new ConcurrentQueue<object>();
        private EmployeeInformation EmployeeInformation { get; } = new EmployeeInformation();
        private void OpenCompany(CompanyIdentifier companyId)
        {
            // Request authorization from Sage 50 for our third-party application.
            try
            {
                AuthorizationResult authorizationResult = Session.RequestAccess(companyId);

                // Check to see we were granted access to Sage 50 company, if so, go ahead and open the company.
                if (authorizationResult == AuthorizationResult.Granted)
                {
                    Company = Session.Open(companyId);
                    Logger.Information("Authorization granted");
                    InitSalesRepresentativeList();
                }
                else // otherwise, display a message to user that there was insufficient access.
                {
                    Logger.Error("Authorization request was not successful - {0}. Will retry.", authorizationResult);
                }
            }
            catch (Sage.Peachtree.API.Exceptions.LicenseNotAvailableException e)
            {
                Logger.Debug(e, e.Message);
            }
            catch (Sage.Peachtree.API.Exceptions.RecordInUseException e)
            {
                Logger.Debug(e, e.Message);
            }
            catch (Sage.Peachtree.API.Exceptions.AuthorizationException e)
            {
                Logger.Debug(e, e.Message);
            }
            catch (Sage.Peachtree.API.Exceptions.PeachtreeException e)
            {
                Logger.Debug(e, e.Message);
            }
            catch (Exception e)
            {
                Logger.Debug(e, e.Message);
            }
        }

        private void InitSalesRepresentativeList()
        {
            EmployeeInformation.Logger = Logger;
            EmployeeInformation.Load(Company);
        }

        private void CloseCompany()
        {
            Company?.Close();
        }
        public void OpenSession(string appKeyID)
        {
            if (Session != null)
            {
                CloseSession();
            }

            // create new session                                
            Session = new PeachtreeSession();

            // start the new session
            Session.Begin(appKeyID);
        }

        // Closes current Sage 50 Session
        //
        public void CloseSession()
        {
            if (Session != null && Session.SessionActive)
            {
                Session.End();
            }
        }

        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };

        public ErpnextService()
        {
            SetupLogger();
            Logger.Information("initializing object");
            InitializeComponent();
            ServiceName = "IWErpnextPollingService";
        }

        private void SetupLogger()
        {
            String path = @"%PROGRAMDATA%\IWERPNextPoll\Logs\log-.txt";
            String logFilePath = Environment.ExpandEnvironmentVariables(path);
            Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }

        protected override void OnContinue()
        {
            _canRequest = true;
            Logger.Information("Service continued");
            Logger.Debug("State = {0}", _canRequest);
        }

        protected override void OnPause()
        {
            _canRequest = false;
            Logger.Information("Service paused");
            Logger.Debug("State = {0}", _canRequest);
        }

        protected override void OnStart(string[] args)
        {
            _canRequest = true;
            Logger.Information("Service started");
            Logger.Debug("State = {0}", _canRequest);
            OpenSession(APPLICATION_ID);
            this.StartTimer();
            this.SetServiceStatus();
        }

        private void ClearQueue()
        {
            if (!queue.IsEmpty && Company != null && !Company.IsClosed)
            {
                DocumentTypeHandler handler = new DocumentTypeHandler(Company, Logger, EmployeeInformation);
                while (queue.TryDequeue(out object document) && Session.SessionActive)
                {
                    handler.Handle(document);
                }
            }
        }


        private void StartTimer()
        {
            Timer timer = new Timer
            {
                Interval = 120000
            };
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();
            Logger.Information("Timer started");
            Logger.Information("Timer interval is {0} minutes", timer.Interval / 60000);
        }

        /**
         * Update the service state to Running.
         * 
         */
        private void SetServiceStatus()
        {
            ServiceStatus serviceStatus = new ServiceStatus
            {
                dwCurrentState = ServiceState.SERVICE_RUNNING
            };
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        /**
         * Starts the process of getting documents from ERPNext and pushing
         * them into Sage 50.
         * When there is no active session or the service cannot connect to
         * the company, `OnTimer` will fail silently.
         */
        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            Logger.Information("Timer callback called");
            if (_canRequest)
            {
                if (Company == null || Company.IsClosed)
                {
                    DiscoverAndOpenCompany();
                }
                if (Session != null && Session.SessionActive && Company != null)
                {
                    if (!Company.IsClosed)
                    {
                        GetDocumentsThenProcessQueue();
                    }
                }
                else
                {
                    Logger.Debug("Session is initialized: {0}", Session != null);
                    Logger.Debug("Session is active: {0}", Session.SessionActive);
                    Logger.Debug("Company is initialized: {0}", Company != null);
                }
            }
        }

        private void GetDocumentsThenProcessQueue()
        {
            GetDocuments();
            ClearQueue();
        }

        private CompanyIdentifier DiscoverCompany()
        {
            bool predicate(CompanyIdentifier c) { return c.CompanyName == COMPANY_NAME; }
            CompanyIdentifierList companies = Session.CompanyList();
            CompanyIdentifier company = companies.Find(predicate);
            return company;
        }

        private void DiscoverAndOpenCompany()
        {
            CompanyIdentifier company = DiscoverCompany();
            if (company != null)
            {
                OpenCompany(company);
            }
        }

        /**
         * Pull documents from ERPNext and queue them for processing.
         * The documents pulled are Sales Orders and Purchase Orders, in that order
         */
        private void GetDocuments()
        {
            PurchaseOrderCommand purchaseOrderCommand = new PurchaseOrderCommand(serverURL: "https://portal.electrocomptr.com/api/method/electro_erpnext.utilities.purchase_order.get_purchase_orders_for_sage");
            SalesOrderCommand salesOrderCommand = new SalesOrderCommand(serverURL: "https://portal.electrocomptr.com/api/method/electro_erpnext.utilities.sales_order.get_sales_orders_for_sage");
            SalesInvoiceCommand salesInvoiceCommand = new SalesInvoiceCommand(serverURL: "https://portal.electrocomptr.com/api/method/electro_erpnext.utilities.sales_invoice.get_sales_invoices_for_sage");

            IRestResponse<SalesOrderResponse> salesOrders = salesOrderCommand.Execute();
            IRestResponse<PurchaseOrderResponse> purchaseOrders = purchaseOrderCommand.Execute();
            IRestResponse<SalesInvoiceResponse> salesInvoices = salesInvoiceCommand.Execute();
            this.SendToQueue(salesOrders.Data);
            this.SendToQueue(purchaseOrders.Data);
            this.SendToQueue(salesInvoices.Data);
        }

        /**
         * Push documents to internal queue
         */
        private void SendToQueue(SalesOrderResponse response)
        {
            if (response != null && response.Message != null)
            {
                foreach (var item in response.Message)
                {
                    this.queue.Enqueue(item);
                }
            }

        }

        private void SendToQueue(PurchaseOrderResponse response)
        {
            if (response != null && response.Message != null)
            {
                foreach (var item in response.Message)
                {
                    this.queue.Enqueue(item);
                }
            }

        }

        private void SendToQueue(SalesInvoiceResponse response)
        {
            if (response != null && response.Message != null)
            {
                foreach (var item in response.Message)
                {
                    this.queue.Enqueue(item);
                }
            }
        }

        protected override void OnStop()
        {
            _canRequest = false;
            CloseCompany();
            CloseSession();

            Logger.Information("Service stopped");
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);
    }
}
