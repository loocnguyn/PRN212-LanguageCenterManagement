using System.Windows;
using System.Windows.Controls;
using Services;

namespace WpfApp;

public partial class StudentInvoiceWindow : Window
{
    private readonly IInvoiceService _service = new InvoiceService();

    public StudentInvoiceWindow() { InitializeComponent(); }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Load student invoices — TODO");

    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtStudentId.Text = ""; dgInvoices.ItemsSource = null; }
}
