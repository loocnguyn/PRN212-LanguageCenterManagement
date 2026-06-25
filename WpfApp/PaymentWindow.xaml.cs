using System.Windows;
using Services;

namespace WpfApp;

public partial class PaymentWindow : Window
{
    private readonly IPaymentService _payService   = new PaymentService();
    private readonly IInvoiceService _invService   = new InvoiceService();

    public PaymentWindow() { InitializeComponent(); }

    private void BtnLoadInvoice_Click(object sender, RoutedEventArgs e)
    {
        // TODO: load invoice by id, fill lblTotal/lblPaid/lblRemaining
        MessageBox.Show("Load invoice details — TODO");
    }

    private void BtnPay_Click(object sender, RoutedEventArgs e)
    {
        // TODO: validate input, create Payment entity, call _payService.Save(), refresh invoice
        MessageBox.Show("Record payment — TODO");
    }
}
