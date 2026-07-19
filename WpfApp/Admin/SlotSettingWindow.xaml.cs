using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class SlotSettingWindow : Window
{
    private readonly ISlotService _service = new SlotService();

    public SlotSettingWindow()
    {
        InitializeComponent();
        LoadData();
    }

    private void LoadData() => dgSlots.ItemsSource = _service.GetAll();

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SlotDialog { Owner = this };
        if (dlg.ShowDialog() != true) return;
        try
        {
            _service.Save(dlg.Result);
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error adding slot (slot number may already exist):\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e) => EditSelected();

    private void DgSlots_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgSlots.SelectedItem is Slot) EditSelected();
    }

    private void EditSelected()
    {
        if (dgSlots.SelectedItem is not Slot slot)
        {
            MessageBox.Show("Please select a slot to edit.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dlg = new SlotDialog(slot) { Owner = this };
        if (dlg.ShowDialog() != true) return;
        try
        {
            _service.Update(dlg.Result);
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating slot:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgSlots.SelectedItem is not Slot slot)
        {
            MessageBox.Show("Please select a slot to delete.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var confirm = MessageBox.Show($"Delete Slot {slot.SlotNo}?", "Confirm",
            MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;
        try
        {
            _service.Delete(slot.SlotId);
            LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting slot:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
