using System.Windows;
using BusinessObjects;

namespace WpfApp;

public partial class SlotDialog : Window
{
    public Slot Result { get; private set; } = null!;

    private readonly Slot? _existing;

    public SlotDialog(Slot? existing = null)
    {
        InitializeComponent();
        _existing = existing;

        if (existing != null)
        {
            tbTitle.Text = "Edit slot";
            txtSlotNo.Text = existing.SlotNo.ToString();
            txtStart.Text = existing.StartTime.ToString("HH\\:mm");
            txtEnd.Text = existing.EndTime.ToString("HH\\:mm");
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(txtSlotNo.Text.Trim(), out var slotNo) || slotNo <= 0)
        {
            MessageBox.Show("Slot number must be a positive integer.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!TimeOnly.TryParse(txtStart.Text.Trim(), out var start))
        {
            MessageBox.Show("Start time must be in HH:mm format (e.g. 07:00).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!TimeOnly.TryParse(txtEnd.Text.Trim(), out var end))
        {
            MessageBox.Show("End time must be in HH:mm format (e.g. 09:15).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (end <= start)
        {
            MessageBox.Show("End time must be after start time.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = _existing ?? new Slot();
        Result.SlotNo = slotNo;
        Result.StartTime = start;
        Result.EndTime = end;

        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
