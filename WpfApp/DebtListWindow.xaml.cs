using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class DebtListWindow : Window
{
    private readonly IInvoiceService _service = new InvoiceService();
    private List<Invoice> _all = new();

    public DebtListWindow() { InitializeComponent(); LoadData(); }

    private void LoadData()
    {
        _all = _service.GetAll().Where(i => i.Status == "UNPAID" || i.Status == "PARTIAL").ToList();
        dgDebts.ItemsSource = _all;
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e)
    {
        var kw = txtSearch.Text.Trim();
        dgDebts.ItemsSource = string.IsNullOrEmpty(kw) ? _all
            : _all.Where(i => i.StudentId.ToString().Contains(kw)).ToList();
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtSearch.Text = ""; dgDebts.ItemsSource = _all; }
}
