using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Telerik.Windows.Controls;

namespace HMS_Client
{
    /// <summary>
    /// Interaction logic for DialogEmail.xaml
    /// </summary>
    public partial class DialogEmail : RadWindow
    {
        private HelideckReportVM helideckReportVM;

        private string reportFolder;

        public DialogEmail(HelideckReportVM helideckReportVM, string reportFile)
        {
            InitializeComponent();

            DataContext = helideckReportVM;
            this.helideckReportVM = helideckReportVM;

            reportFolder = Path.Combine(Environment.CurrentDirectory, Constants.HelideckReportFolder);

            helideckReportVM.EmailStatus = EmailStatus.PREVIEW;

            if (!string.IsNullOrEmpty(helideckReportVM.flightNumber))
            {
                // Med flight number
                helideckReportVM.emailSubject = string.Format("{0}, {1}, {2}, {3}",
                    helideckReportVM.vesselNameString,
                    Constants.HelideckReportName,
                    helideckReportVM.dateString_NOROG,
                    helideckReportVM.flightNumber);
            }
            else
            {
                // Uten flight number
                helideckReportVM.emailSubject = string.Format("{0}, {1}, {2}",
                    helideckReportVM.vesselNameString,
                    Constants.HelideckReportName,
                    helideckReportVM.dateString_NOROG);
            }

            helideckReportVM.helideckReportFile = reportFile;

            // Sjekke om screen capture file skal sendes
            if (helideckReportVM.sendHMSScreenCapture)
            {
                // Vise attachment 2 feltet
                dpEmailAttachment2.Visibility = Visibility.Visible;

                // Sjekke om screen capture file er tilgjenglig
                if (!string.IsNullOrEmpty(helideckReportVM.screenCaptureFile))
                {
                    // Sjekke dato på screen capture -> Varsel dersom eldre enn 10 minutter
                    DateTime fileCreationTime = File.GetCreationTime(string.Format(@"{0}\{1}", reportFolder, helideckReportVM.screenCaptureFile));

                    if (fileCreationTime.AddMinutes(10) < DateTime.Now)
                    {
                        helideckReportVM.warningMessage = "Warning: HMS screen capture file is more than 10 minutes old.";
                    }
                    else
                    {
                        helideckReportVM.warningMessage = string.Empty;
                    }
                }
                else
                {
                    helideckReportVM.warningMessage = string.Empty;
                }
            }
            else
            {
                dpEmailAttachment2.Visibility = Visibility.Collapsed;
                helideckReportVM.warningMessage = string.Empty;
            }
        }

        private void SendEmail()
        {
            helideckReportVM.EmailStatus = EmailStatus.SENDING;

            // Sjekke at vi har nødvendige data tilgjengelig for å sende data
            if (!string.IsNullOrEmpty(helideckReportVM.emailServer.data3) &&
                !string.IsNullOrEmpty(helideckReportVM.emailFrom) &&
                !string.IsNullOrEmpty(helideckReportVM.emailTo) &&
                !(helideckReportVM.sendHMSScreenCapture && string.IsNullOrEmpty(helideckReportVM.screenCaptureFile)))
            {
                Thread thread = new Thread(() => SendEmail_Thread());
                thread.Start();

                void SendEmail_Thread()
                {
                    try
                    {
                        // Opprette epost ny melding
                        MailMessage email = new MailMessage();

                        // From
                        email.From = new MailAddress(helideckReportVM.emailFrom);

                        // To
                        email.To.Add(helideckReportVM.emailTo);

                        // Subject
                        email.Subject = helideckReportVM.emailSubject;

                        // Body
                        email.Body = "";

                        // Legge til en CC mottaker
                        if (!string.IsNullOrEmpty(helideckReportVM.emailCC))
                            email.CC.Add(new MailAddress(helideckReportVM.emailCC));

                        // Attachment: Helideck Report
                        if (!string.IsNullOrEmpty(helideckReportVM.helideckReportFile))
                        {
                            Attachment newAttachment = new Attachment(string.Format(@"{0}\{1}", reportFolder, helideckReportVM.helideckReportFile));
                            email.Attachments.Add(newAttachment);
                        }

                        // Attachment: HMS Screen Capture
                        if (helideckReportVM.sendHMSScreenCapture &&
                            !string.IsNullOrEmpty(helideckReportVM.screenCaptureFile))
                        {
                            Attachment newAttachment = new Attachment(string.Format(@"{0}\{1}", reportFolder, helideckReportVM.screenCaptureFile));
                            email.Attachments.Add(newAttachment);
                        }

                        // Opprette epost klient
                        SmtpClient client = new SmtpClient(helideckReportVM.emailServer.data3, (int)helideckReportVM.emailPort.data);
                        //client.Timeout = 100;

                        // Username / password
                        if (!string.IsNullOrEmpty(helideckReportVM.emailUsername.data3) &&
                            !string.IsNullOrEmpty(helideckReportVM.emailPassword.data3))
                            client.Credentials = new NetworkCredential(helideckReportVM.emailUsername.data3, helideckReportVM.emailPassword.data3);
                        else
                            client.Credentials = CredentialCache.DefaultNetworkCredentials;

                        // Secure Connection
                        if (helideckReportVM.emailSecureConnection.data == 1)
                            client.EnableSsl = true;
                        else
                            client.EnableSsl = false;

                        // Sende epost
                        if (client != null)
                            client.Send(email);

                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Normal,
                            new Action(() =>
                            {
                                // OK melding
                                helideckReportVM.EmailStatus = EmailStatus.SUCCESS;
                            }));

                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Normal,
                            new Action(() =>
                            {
                                // Lukke email sending dialog
                                Close();

                                // Sette email sending status
                                helideckReportVM.EmailStatus = EmailStatus.FAILED;

                                DialogHandler.Warning("Error sending email", ex.Message);
                            }));
                    }
                }
            }
            else
            {
                // Lukke email sending dialog
                Close();

                // Sette email sending status
                helideckReportVM.EmailStatus = EmailStatus.FAILED;

                //Meldinger om manglende info
                if (string.IsNullOrEmpty(helideckReportVM.emailServer.data3))
                    DialogHandler.Warning("Email parameter missing", "Email server not set.");
                else
                if (string.IsNullOrEmpty(helideckReportVM.emailFrom))
                    DialogHandler.Warning("Email parameter missing", "Email sender (from) not set.");
                else
                if (string.IsNullOrEmpty(helideckReportVM.emailTo))
                    DialogHandler.Warning("Email parameter missing", "Email recipient (to) not set.");
                else
                if (helideckReportVM.sendHMSScreenCapture && string.IsNullOrEmpty(helideckReportVM.screenCaptureFile))
                    DialogHandler.Warning("No screen capture created", "Go to the HMS main page and press the screen capture button. If you do not want to include a screen capture, uncheck the 'Include HMS screen capture' option.");
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            SendEmail();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
