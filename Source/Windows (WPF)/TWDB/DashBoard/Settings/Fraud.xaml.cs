using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

namespace DashBoard.Settings
{
    /// <summary>
    /// Interaction logic for Fraud.xaml
    /// </summary>
    public partial class Fraud : Window
    {
        public Fraud()
        {
            InitializeComponent();
            // Get the file modified date, and generate appVersion from this date.
            DateTime CompiledDate = File.GetLastWriteTimeUtc(Environment.CurrentDirectory + "\\dashboard.exe");

            FraudTextBlock.Text = "Sign up for free at http://ipqualityscore.com\nto get your Private Key.";
            FraudTextBlock.Foreground = Brushes.LightGray;

            EmailAddressTextBox.Text = Properties.Settings.Default.EmailAddress;
            PrivareKeyTextBox.Text = Properties.Settings.Default.PrivateKey;

            EmailAddressTextBox.Focus();
        }

        private void onOkClick(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(EmailAddressTextBox.Text))
            {
                FraudTextBlock.Text = "PLease enter youe email address.";
                FraudTextBlock.Foreground = Brushes.Red;

                EmailAddressTextBox.Focus();
                SystemSounds.Beep.Play();
            }
            else if (String.IsNullOrEmpty(PrivareKeyTextBox.Text))
            {
                FraudTextBlock.Text = "PLease enter youe private key.";
                FraudTextBlock.Foreground = Brushes.Red;

                PrivareKeyTextBox.Focus();
                SystemSounds.Beep.Play();
            }
            else if (PrivareKeyTextBox.Text.Length != 32)
            {
                FraudTextBlock.Text = "Private key must be 32 characters long.";
                FraudTextBlock.Foreground = Brushes.Red;

                PrivareKeyTextBox.Focus();
                SystemSounds.Beep.Play();
            }
            else
            {
                try
                {
                    string Uri = "https://www.ipqualityscore.com/api/xml/ip";
                    string Email = EmailAddressTextBox.Text;
                    string Key = PrivareKeyTextBox.Text;

                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load($"{Uri}/{Key}/8.8.8.8");
                    XmlNode resultNode = xmlDoc.SelectSingleNode("//result");
                    if (!resultNode["success"].FirstChild.Value.Contains("true"))
                    {
                        FraudTextBlock.Text = "Private key appears to be invalid.\nPlease enter a valid key from ipqualityscore.com";
                        FraudTextBlock.Foreground = Brushes.Red;

                        PrivareKeyTextBox.Focus();
                        SystemSounds.Beep.Play();
                    }
                    else
                    {
                        Properties.Settings.Default.EmailAddress = EmailAddressTextBox.Text;
                        Properties.Settings.Default.PrivateKey = PrivareKeyTextBox.Text;
                        Properties.Settings.Default.FraudDetection = true;
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    FraudTextBlock.Text = ex.Message;
                    FraudTextBlock.Foreground = Brushes.Red;

                    PrivareKeyTextBox.Focus();
                    SystemSounds.Beep.Play();
                }
            }
        }

        private void onCancelClick(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.FraudDetection = false;
            this.Hide();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
