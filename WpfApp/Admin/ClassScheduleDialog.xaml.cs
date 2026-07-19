using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

public partial class ClassScheduleDialog : Window
{
    public ClassSchedule? Result { get; private set; }
    private readonly int? _editId;
    private readonly IClassService _classService = new ClassService();
    private readonly IClassScheduleService _scheduleService = new ClassScheduleService();
    private readonly ISlotService _slotService = new SlotService();

    private static readonly List<KeyValuePair<string, byte>> Days = new()
    {
        new("Monday", 1), new("Tuesday", 2), new("Wednesday", 3),
        new("Thursday", 4), new("Friday", 5), new("Saturday", 6), new("Sunday", 7)
    };

    public ClassScheduleDialog()
    {
        InitializeComponent();
        LoadDropdowns();
    }

    public ClassScheduleDialog(ClassSchedule schedule)
    {
        InitializeComponent();
        _editId = schedule.ScheduleId;
        LoadDropdowns();
        cboClass.SelectedValue = schedule.ClassId;
        cboDayOfWeek.SelectedValue = schedule.DayOfWeek;
        // Preselect the slot whose start time matches the existing schedule.
        var slots = (List<Slot>)cboSlot.ItemsSource;
        var match = slots.FirstOrDefault(s => s.StartTime == schedule.StartTime && s.EndTime == schedule.EndTime)
            ?? slots.FirstOrDefault(s => s.StartTime == schedule.StartTime);
        if (match != null) cboSlot.SelectedValue = match.SlotId;
    }

    private void LoadDropdowns()
    {
        cboClass.ItemsSource = _classService.GetAll();
        cboDayOfWeek.ItemsSource = Days;
        cboSlot.ItemsSource = _slotService.GetAll();
        tbHint.Text = "The slot's start/end time is applied to this schedule.";
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (cboClass.SelectedValue == null) { MessageBox.Show("Please select a Class.", "Validation"); return; }
        if (cboDayOfWeek.SelectedValue == null) { MessageBox.Show("Please select a Day of Week.", "Validation"); return; }
        if (cboSlot.SelectedItem is not Slot slot) { MessageBox.Show("Please select a Slot.", "Validation"); return; }

        var schedule = new ClassSchedule
        {
            ScheduleId = _editId ?? 0,
            ClassId = (int)cboClass.SelectedValue,
            DayOfWeek = (byte)cboDayOfWeek.SelectedValue,
            StartTime = slot.StartTime,
            EndTime = slot.EndTime
        };

        var conflicts = _scheduleService.CheckConflicts(schedule);
        if (conflicts.Count > 0)
        {
            var msg = "Conflict detected:\n\n" + string.Join("\n", conflicts);
            MessageBox.Show(msg, "Conflict Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = schedule;
        DialogResult = true;
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
