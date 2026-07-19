using System.Windows;
using System.Windows.Controls;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  TuitionDiscountDetailWindow — Add/Edit a discount code.
//  CONTENTS:
//    1. Construction   — add vs edit (LoadDiscount prefills)
//    2. Save / Cancel  — validate then create/update
//    3. Combo helpers  — get/select combo text
// ============================================================
public partial class TuitionDiscountDetailWindow : Window
{
    private readonly ITuitionDiscountService _service = new TuitionDiscountService();
    private readonly int? _discountId;

    public TuitionDiscountDetailWindow()
    {
        InitializeComponent();
        Title = "Add Tuition Discount";
    }

    public TuitionDiscountDetailWindow(int discountId) : this()
    {
        _discountId = discountId;
        Title = "Edit Tuition Discount";
        LoadDiscount(discountId);
    }

    private void LoadDiscount(int discountId)
    {
        var discount = _service.GetById(discountId);
        if (discount == null)
        {
            MessageBox.Show("Không tìm thấy discount.", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
            DialogResult = false;
            Close();
            return;
        }

        txtCode.Text = discount.Code;
        txtName.Text = discount.Name;
        SelectCombo(cmbDiscountType, discount.DiscountType);
        txtDiscountValue.Text = discount.DiscountValue.ToString("0.##");
        SelectCombo(cmbConditionType, discount.ConditionType);
        txtDeadlineDays.Text = discount.PaymentDeadlineDays?.ToString() ?? "";
        dpStartDate.SelectedDate = discount.StartDate?.ToDateTime(TimeOnly.MinValue);
        dpEndDate.SelectedDate = discount.EndDate?.ToDateTime(TimeOnly.MinValue);
        chkActive.IsChecked = discount.IsActive;
        txtNote.Text = discount.Note ?? "";
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var entity = BuildEntityFromForm();
            if (_discountId.HasValue)
            {
                entity.DiscountId = _discountId.Value;
                _service.Update(entity);
                MessageBox.Show("Cập nhật discount thành công.", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                _service.Save(entity);
                MessageBox.Show("Thêm discount thành công.", "Thành công",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            DialogResult = true;
            Close();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(ex.Message, "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể lưu discount: {ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private TuitionDiscount BuildEntityFromForm()
    {
        if (!decimal.TryParse(txtDiscountValue.Text.Trim(), out var value))
            throw new InvalidOperationException("Discount value phải là số hợp lệ.");

        int? deadlineDays = null;
        if (!string.IsNullOrWhiteSpace(txtDeadlineDays.Text))
        {
            if (!int.TryParse(txtDeadlineDays.Text.Trim(), out var days))
                throw new InvalidOperationException("Deadline days phải là số nguyên.");
            deadlineDays = days;
        }

        return new TuitionDiscount
        {
            Code = txtCode.Text,
            Name = txtName.Text,
            DiscountType = GetComboText(cmbDiscountType),
            DiscountValue = value,
            ConditionType = GetComboText(cmbConditionType),
            PaymentDeadlineDays = deadlineDays,
            StartDate = ToDateOnly(dpStartDate.SelectedDate),
            EndDate = ToDateOnly(dpEndDate.SelectedDate),
            IsActive = chkActive.IsChecked == true,
            Note = txtNote.Text
        };
    }

    private static DateOnly? ToDateOnly(DateTime? value)
        => value.HasValue ? DateOnly.FromDateTime(value.Value) : null;

    private static string GetComboText(ComboBox combo)
        => (combo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";

    private static void SelectCombo(ComboBox combo, string value)
    {
        foreach (var item in combo.Items.OfType<ComboBoxItem>())
        {
            if (item.Content?.ToString() == value)
            {
                combo.SelectedItem = item;
                return;
            }
        }
    }
}
