using Sage.Peachtree.API;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Timers;

namespace IWErpnextPoll
{
    public partial class ErpnextService : ServiceBase
    {
        private const string CompanyName = "Electro-Comp Tape & Reel Services, LLC";
        private const string ApplicationId = "41Dzi73wlWhIwx8yTdVQ6dJRbGU9nJtcu47x2rY4MOS7u9tAALysow==VK7xyIAjtHr7JDWfDXJYhVLelt+UlNwmGpSMCjISTVFfEo6e0pRzQR7cnDraS9HTpJnP34yIDrgC4nA7QU1rMCPWNQ4eOs+7+dm5AV/mymbQWUEx+tNEQhDka3PJi6nFXkZEaDa+I6bhbQnAAg+65ZD6/+IJ02CjaAGCAzaRiQebwojCJiKuSKoiDk/xkDTuD975uiTLcGZ3ZrzByxfoPdaUkAPQRTaEFmiARO/eBtNg7nSXUvVaEF0NddSjTyf6jbwycx6NnzmRWk/qBJPW0g==";
        private bool _canRequest = true;
        private Timer _timer;
        private ILogger Logger { get; set; }
        private static PeachtreeSession Session { get; set; }
        public static Company Company { get; set; }
        public ConcurrentQueue<object> Queue = new ConcurrentQueue<object>();
        private EmployeeInformation EmployeeInformation { get; } = new EmployeeInformation();
        private void OpenCompany(CompanyIdentifier companyId)
        {
            // Request authorization from Sage 50 for our third-party application.
            try
            {
                var authorizationResult = Session.RequestAccess(companyId);

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

        private static void CloseCompany()
        {
            Company?.Close();
        }

        private void OpenSession(string appKeyId)
        {
            try
            {
                if (Session != null)
                {
                    CloseSession();
                }

                // create new session                                
                Session = new PeachtreeSession();

                // start the new session
                Session.Begin(appKeyId);
            }
            catch (Sage.Peachtree.API.Exceptions.ApplicationIdentifierExpiredException e)
            {
                Logger.Debug(e, "Your application identifier has expired.");
            }
            catch (Sage.Peachtree.API.Exceptions.ApplicationIdentifierRejectedException e)
            {
                Logger.Debug(e, "Your application identifier was rejected.");
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

        // Closes current Sage 50 Session
        //
        private static void CloseSession()
        {
            if (Session != null && Session.SessionActive)
            {
                Session.End();
            }
        }

        public enum ServiceState
        {
            // ReSharper disable once InconsistentNaming
            SERVICE_STOPPED = 0x00000001,
            // ReSharper disable once InconsistentNaming
            SERVICE_START_PENDING = 0x00000002,
            // ReSharper disable once InconsistentNaming
            SERVICE_STOP_PENDING = 0x00000003,
            // ReSharper disable once InconsistentNaming
            SERVICE_RUNNING = 0x00000004,
            // ReSharper disable once InconsistentNaming
            SERVICE_CONTINUE_PENDING = 0x00000005,
            // ReSharper disable once InconsistentNaming
            SERVICE_PAUSE_PENDING = 0x00000006,
            // ReSharper disable once InconsistentNaming
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
            const string path = @"%PROGRAMDATA%\IWERPNextPoll\Logs\log-.txt";
            var logFilePath = Environment.ExpandEnvironmentVariables(path);
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
            this.SetServiceStatus(ServiceState.SERVICE_RUNNING);
        }

        protected override void OnPause()
        {
            _canRequest = false;
            Logger.Information("Service paused");
            Logger.Debug("State = {0}", _canRequest);
            this.SetServiceStatus(ServiceState.SERVICE_PAUSED);
        }

        protected override void OnStart(string[] args)
        {
            _canRequest = true;
            Logger.Information("Service started");
            Logger.Debug("State = {0}", _canRequest);
            OpenSession(ApplicationId);
            StartTimer();
            SetServiceStatus(ServiceState.SERVICE_RUNNING);
        }

        private void ClearQueue()
        {
            Logger.Information("Version {@Version}", Constants.Version);
            if (Queue.IsEmpty || Company == null || Company.IsClosed) return;
            var handler = new DocumentTypeHandler(Company, Logger, EmployeeInformation);
            while (Queue.TryDequeue(out var document) && Session.SessionActive)
            {
                handler.Handle(document);
            }
        }


        private void StartTimer()
        {
            _timer = new Timer
            {
                Interval = Constants.TimerInterval
            };
            _timer.Elapsed += OnTimer;
            _timer.Start();
            Logger.Information("Timer started");
            Logger.Information("Timer interval is {0} minutes", _timer.Interval / 60000);
        }

        /**
         * Update the service state to Running.
         * 
         */
        private void SetServiceStatus(ServiceState serviceState)
        {
            var serviceStatus = new ServiceStatus
            {
                dwCurrentState = serviceState
            };
            SetServiceStatus(ServiceHandle, ref serviceStatus);
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
            if (!_canRequest || (DateTime.Now.Hour >= 6 && DateTime.Now.Hour <= 17))
            {
                Logger.Debug("Service cannot request: {0}, {1}", _canRequest, DateTime.Now.Hour);
                return;
            }
            
            if (Company == null || Company.IsClosed)
            {
                DiscoverAndOpenCompany();
            } else
            {
                Logger.Debug("Company is null: {0}; Company is closed: {1}", Company == null, Company?.IsClosed);
            }
            
            if (Session != null && Session.SessionActive && Company != null)
            {
                if (!Company.IsClosed)
                {
                    GetDocumentsThenProcessQueue();
                } else
                {
                    Logger.Debug("Session is null: {0}, Session is active: {1}, Company is null: {2}", Session == null, Session?.SessionActive, Company == null);
                }
            }
            else
            {
                Logger.Debug("Session is initialized: {0}", Session != null);
                Logger.Debug("Session is active: {0}", Session != null && Session.SessionActive);
                Logger.Debug("Company is initialized: {0}", Company != null);
            }
        }

        private void GetDocumentsThenProcessQueue()
        {
            GetDocuments();
            ClearQueue();
        }

        private CompanyIdentifier DiscoverCompany()
        {
            bool Predicate(CompanyIdentifier c) { return c.CompanyName == CompanyName; }
            var companies = Session.CompanyList();
            var company = companies.Find(Predicate);
            return company;
        }

        private void DiscoverAndOpenCompany()
        {
            var company = DiscoverCompany();
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
            var purchaseOrderCommand = new PurchaseOrderCommand(serverUrl: $"{Constants.ServerUrl}/api/method/electro_erpnext.utilities.purchase_order.get_purchase_orders_for_sage");
            var salesOrderCommand = new SalesOrderCommand(serverUrl: $"{Constants.ServerUrl}/api/method/electro_erpnext.utilities.sales_order.get_sales_orders_for_sage");
            var salesInvoiceCommand = new SalesInvoiceCommand(serverUrl: $"{Constants.ServerUrl}/api/method/electro_erpnext.utilities.sales_invoice.get_sales_invoices_for_sage");

            var salesOrders = salesOrderCommand.Execute();
            var purchaseOrders = purchaseOrderCommand.Execute();
            var salesInvoices = salesInvoiceCommand.Execute();
            SendToQueue(salesOrders.Data);
            SendToQueue(purchaseOrders.Data);
            SendToQueue(salesInvoices.Data);
        }

        /**
         * Push documents to internal queue
         */
        private void SendToQueue(SalesOrderResponse response)
        {
            if (response?.Message == null) return;
            foreach (var item in response.Message)
            {
                this.Queue.Enqueue(item);
            }

        }

        private void SendToQueue(PurchaseOrderResponse response)
        {
            if (response?.Message == null) return;
            foreach (var item in response.Message)
            {
                this.Queue.Enqueue(item);
            }

        }

        private void SendToQueue(SalesInvoiceResponse response)
        {
            if (response?.Message == null) return;
            foreach (var item in response.Message)
            {
                this.Queue.Enqueue(item);
            }
        }

        protected override void OnStop()
        {
            _canRequest = false;
            CloseCompany();
            CloseSession();
            StopTimer();
            SetServiceStatus(ServiceState.SERVICE_STOPPED);
            Logger.Debug("Timer stopped");
        }

        private void StopTimer()
        {
            _timer.Stop();
            _timer.Close();
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(IntPtr handle, ref ServiceStatus serviceStatus);
    }
}
