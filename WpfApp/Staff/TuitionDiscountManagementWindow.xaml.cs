using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  TuitionDiscountManagementWindow — list & manage discount codes.
//  CONTENTS:
//    1. Construction & load  — discounts into the grid
//    2. Add / edit / delete  — TuitionDiscountDetailWindow; delete selected
// ============================================================
public partial class TuitionDiscountManagementWindow : Window
{
    private readonly ITuitionDiscountService _service = new TuitionDiscountService();
    private List<TuitionDiscount> _items = new();

    public TuitionDiscountManagementWindow()
    {
        InitializeComponent();
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            var status = (cmbStatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            _items = _service.Search(txtSearch.Text, status);
            pager.Reset();
            BindPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải danh sách discount: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BindPage()
    {
        dgDiscounts.ItemsSource = pager.Slice(_items);
        emptyState.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Pager_PageChanged(object sender, EventArgs e) => BindPage();

    private void DgDiscounts_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgDiscounts.SelectedItem is TuitionDiscount) BtnEdit_Click(sender, e);
    }

    private void BtnSearch_Click(object sender, RoutedEventArgs e) => LoadData();

    private void BtnClear_Click(object sender, RoutedEventArgs e)
    {
        txtSearch.Clear();
        cmbStatusFilter.SelectedIndex = 0;
        LoadData();
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var window = new TuitionDiscountDetailWindow { Owner = this };
        if (window.ShowDialog() == true)
            LoadData();
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgDiscounts.SelectedItem is not TuitionDiscount discount)
        {
            MessageBox.Show("Vui lòng chọn discount cần sửa.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var window = new TuitionDiscountDetailWindow(discount.DiscountId) { Owner = this };
        if (window.ShowDialog() == true)
            LoadData();
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgDiscounts.SelectedItem is not TuitionDiscount discount)
        {
            MessageBox.Show("Vui lòng chọn discount cần xóa/deactivate.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            "Nếu discount đã được dùng bởi invoice, hệ thống sẽ deactivate thay vì xóa hẳn.\nBạn có muốn tiếp tục?",
            "Xác nhận",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            _service.DeleteOrDeactivate(discount.DiscountId);
            MessageBox.Show("Đã xóa hoặc deactivate discount.", "Thành công",
                MessageBoxButton.OK, MessageBoxImage.Information);
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể xóa/deactivate discount: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
