using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class InvoiceManagementWindow : Window
{
    private readonly IInvoiceService _service = new InvoiceService();
    private List<Invoice> _all = new();

    public InvoiceManagementWindow() { InitializeComponent(); LoadData(); }

    private void LoadData() { _all = _service.GetAll(); ApplyFilter(); }

    private void ApplyFilter()
    {
        var kw     = txtSearch.Text.Trim().ToLower();
        var status = (cmbStatus.SelectedItem as ComboBoxItem)?.Content?.ToString();
        dgInvoices.ItemsSource = _all
            .Where(i => (string.IsNullOrEmpty(kw)     || i.StudentId.ToString().Contains(kw))
                     && (status == "All" || string.IsNullOrEmpty(status) || i.Status == status))
            .ToList();
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e) => ApplyFilter();
    private void BtnReset_Click(object sender, RoutedEventArgs e)  { txtSearch.Text = ""; cmbStatus.SelectedIndex = 0; ApplyFilter(); }
    private void DgInvoices_SelectionChanged(object sender, SelectionChangedEventArgs e) { }
    private void BtnAdd_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Add invoice — TODO");
    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgInvoices.SelectedItem is not Invoice inv) { MessageBox.Show("Please select an invoice."); return; }
        MessageBox.Show($"Edit invoice #{inv.InvoiceId} — TODO");
    }
    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgInvoices.SelectedItem is not Invoice inv) { MessageBox.Show("Please select an invoice."); return; }
        var confirm = MessageBox.Show($"Delete invoice #{inv.InvoiceId}?", "Confirm", MessageBoxButton.YesNo);
        if (confirm == MessageBoxResult.Yes) { _service.Delete(inv.InvoiceId); LoadData(); }
    }
}
