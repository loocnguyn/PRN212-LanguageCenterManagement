using System.Windows;
using System.Windows.Controls;
using Services;

namespace WpfApp;

public partial class RevenueReportWindow : Window
{
    private readonly IPaymentService _service = new PaymentService();

    public RevenueReportWindow() { InitializeComponent(); }

    private void BtnGenerate_Click(object sender, RoutedEventArgs e)
    {
        // TODO: query payments between dpFrom.SelectedDate and dpTo.SelectedDate
        // calculate totals, bind to dgPayments, update lblTotalRevenue / lblTotalUnpaid
        MessageBox.Show("Generate revenue report — TODO");
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        dpFrom.SelectedDate = null;
        dpTo.SelectedDate   = null;
        dgPayments.ItemsSource = null;
        lblTotalRevenue.Text   = "";
        lblTotalUnpaid.Text    = "";
    }
}
