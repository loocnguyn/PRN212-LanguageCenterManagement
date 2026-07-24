using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using BusinessObjects;
using Services;

namespace WpfApp;

// ============================================================
//  StudentPickerDialog — pick students to enrol into ONE class.
//  CONTENTS:
//    1. Construction & load  — enrollable students, discount options
//    2. Search & bulk apply  — filtering, "apply to all ticked"
//    3. Footer               — running count and total
//    4. Enroll               — batch call, per-student reporting
//    5. StudentPick          — one row's state
//    6. DiscountOption       — one entry of the per-row dropdown
//
//  Replaces typing a raw student ID: the ID was something the user had to look up
//  elsewhere, and a wrong one only failed after the button was pressed.
//
//  Discounts are per row, not per batch. Most of the centre's discounts describe an
//  individual circumstance (sibling, returning student, scholarship) rather than a
//  promotion everyone shares, so a single discount for the whole selection could not
//  express the common case. The "apply to all" control is a shortcut on top of that,
//  not a replacement for it.
//
//  No pager here, unlike the list windows: selections must stay visible to be
//  trustworthy. Paging would hide ticked students on other pages while still counting
//  them in "Enroll (N)". Searching narrows the list instead.
// ============================================================
public partial class StudentPickerDialog : Window
{
    private readonly IEnrollmentService _enrollmentService = new EnrollmentService();
    private readonly ITuitionDiscountService _discountService = new TuitionDiscountService();

    private readonly int _classId;
    private readonly Class _class;

    private List<StudentPick> _all = new();
    private List<DiscountOption> _discountOptions = new();

    /// <summary>True once at least one student was actually enrolled, so the caller knows to reload.</summary>
    public bool EnrolledAnyone { get; private set; }

    public StudentPickerDialog(Class cls)
    {
        InitializeComponent();
        _class = cls;
        _classId = cls.ClassId;
        LoadDiscounts();
        LoadStudents();
    }

    // ---- 1. Load -----------------------------------------------
    private void LoadDiscounts()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        _discountOptions = new List<DiscountOption> { new(null, "No discount", null, null) };
        _discountOptions.AddRange(_discountService.GetActive(today)
            .Select(d => new DiscountOption(
                d.DiscountId,
                $"{d.Code} — {FormatDiscount(d)}",
                d.ConditionType,
                d.PaymentDeadlineDays)));

        cboBulkDiscount.ItemsSource = _discountOptions;
        cboBulkDiscount.SelectedIndex = 0;
    }

    private void LoadStudents()
    {
        tbTitle.Text = $"Add students to {_class.Name}";
        tbSubtitle.Text = $"{_class.SnapCourseCode} — {_class.SnapTuitionFee:N0} đ tuition";

        _all = _enrollmentService.GetEnrollableStudents(_classId)
            .Select(es => new StudentPick(es, _discountOptions,
                                          d => _enrollmentService.PreviewFinalAmount(_classId, d)))
            .ToList();

        // Any row changing selection or discount moves the footer figures.
        foreach (var pick in _all) pick.PropertyChanged += (_, _) => RefreshFooter();

        UpdateSeatsBadge();
        ApplyFilter();
        RefreshFooter();
    }

    private void UpdateSeatsBadge()
    {
        var active = _enrollmentService.GetByClassId(_classId).Count(e => e.Status != "DROPPED");
        var left = Math.Max(0, _class.MaxStudents - active);
        tbSeats.Text = $"Seats left: {left}";
    }

    // ---- 2. Search & bulk apply --------------------------------
    private void TxtSearch_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => ApplyFilter();

    /// <summary>
    /// Filters the same StudentPick instances rather than rebuilding them, so ticks and
    /// per-row discounts survive a search that hides and then re-shows a row.
    /// </summary>
    private void ApplyFilter()
    {
        var kw = txtSearch.Text.Trim().ToLower();

        var shown = string.IsNullOrEmpty(kw)
            ? _all
            : _all.Where(p => p.SearchBlob.Contains(kw)).ToList();

        lstStudents.ItemsSource = shown;

        emptyState.Visibility = shown.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        if (shown.Count != 0) return;

        var searching = !string.IsNullOrEmpty(kw);
        tbEmpty.Text = searching ? "No match" : "Nobody left to enrol";
        tbEmptyHint.Text = searching
            ? "Try a different name, phone or email."
            : "Every active student is already enrolled in this class.";
    }

    private void BtnApplyAll_Click(object sender, RoutedEventArgs e)
    {
        if (cboBulkDiscount.SelectedItem is not DiscountOption option) return;

        var ticked = _all.Where(p => p.IsSelected).ToList();
        if (ticked.Count == 0)
        {
            MessageBox.Show("Tick at least one student first.", "Nothing selected",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        foreach (var pick in ticked) pick.DiscountId = option.DiscountId;
    }

    // ---- 3. Footer ---------------------------------------------
    private void RefreshFooter()
    {
        var picked = _all.Where(p => p.IsSelected).ToList();

        tbSelected.Text = $"Selected: {picked.Count}";
        tbTotal.Text = picked.Count == 0
            ? ""
            : $"Total: {picked.Sum(p => p.FinalAmount):N0} đ";

        btnEnroll.Content = picked.Count == 0 ? "Enroll" : $"Enroll ({picked.Count})";
        btnEnroll.IsEnabled = picked.Count > 0;
    }

    // ---- 4. Enroll ---------------------------------------------
    private void BtnEnroll_Click(object sender, RoutedEventArgs e)
    {
        var requests = _all.Where(p => p.IsSelected)
            .Select(p => new EnrollRequest(p.StudentId, p.DiscountId))
            .ToList();

        if (requests.Count == 0)
        {
            MessageBox.Show("Tick at least one student to enrol.", "Nothing selected",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        List<EnrollOutcome> outcomes;
        try
        {
            outcomes = _enrollmentService.EnrollMany(_classId, requests);
        }
        catch (Exception ex)
        {
            // EnrollMany absorbs business refusals into its report, so reaching here means
            // something genuinely broke (connection, mapping) — do not dress it up as a
            // per-student refusal.
            MessageBox.Show($"Enrollment could not be completed:\n{ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var ok = outcomes.Where(o => o.Success).ToList();
        var failed = outcomes.Where(o => !o.Success).ToList();

        if (ok.Count > 0) EnrolledAnyone = true;

        if (failed.Count == 0)
        {
            DialogResult = true;
            return;
        }

        // Partial success: reloading drops the students who did get in, leaving exactly the
        // problem cases on screen. Stay open so they can be dealt with.
        LoadStudents();

        MessageBox.Show(
            $"Enrolled {ok.Count} of {outcomes.Count} students.\n\n"
            + $"{failed.Count} could not be enrolled:\n"
            + string.Join("\n", failed.Select(f => $"  • {f.StudentName}: {f.Error}")),
            "Partly done", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        // Enrollments already committed cannot be undone by cancelling, so report them.
        DialogResult = EnrolledAnyone;
    }

    private static string FormatDiscount(TuitionDiscount d)
        => d.DiscountType == "PERCENT" ? $"{d.DiscountValue:0.##}%" : $"{d.DiscountValue:N0} đ";

    // ---- 5. StudentPick ----------------------------------------
    /// <summary>One row: who they are, whether they are ticked, and which discount they get.</summary>
    private sealed class StudentPick : INotifyPropertyChanged
    {
        private readonly Func<int?, decimal> _preview;

        public StudentPick(EnrollableStudent source,
                           IReadOnlyList<DiscountOption> options,
                           Func<int?, decimal> preview)
        {
            var s = source.Student;
            StudentId = s.StudentId;
            FullName = s.FullName;
            PreviouslyDropped = source.PreviouslyDropped;
            DiscountOptions = options;
            _preview = preview;

            var contact = string.Join(" · ",
                new[] { s.Phone, s.Email }.Where(v => !string.IsNullOrWhiteSpace(v)));
            Detail = string.IsNullOrEmpty(contact) ? $"#{s.StudentId}" : $"#{s.StudentId} · {contact}";
            SearchBlob = $"{s.FullName} {s.Phone} {s.Email} {s.StudentId}".ToLower();
        }

        public int StudentId { get; }
        public string FullName { get; }
        public string Detail { get; }
        public string SearchBlob { get; }
        public bool PreviouslyDropped { get; }
        public IReadOnlyList<DiscountOption> DiscountOptions { get; }

        public Visibility DroppedVisibility => PreviouslyDropped ? Visibility.Visible : Visibility.Collapsed;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                Raise();
                Raise(nameof(PriceText));
            }
        }

        private int? _discountId;
        public int? DiscountId
        {
            get => _discountId;
            set
            {
                if (_discountId == value) return;
                _discountId = value;
                Raise();
                Raise(nameof(PriceText));
                Raise(nameof(DiscountHint));
            }
        }

        /// <summary>What this student would be billed — quoted by the service, not recomputed here.</summary>
        public decimal FinalAmount => _preview(_discountId);

        public string PriceText => IsSelected ? $"{FinalAmount:N0} đ" : "—";

        /// <summary>
        /// EARLY_PAYMENT discounts expire if the invoice is not paid in time, unlike the rest
        /// which are locked in at enrollment. Without this the two look interchangeable.
        /// </summary>
        public string DiscountHint
        {
            get
            {
                var option = DiscountOptions.FirstOrDefault(o => o.DiscountId == _discountId);
                return option?.ConditionType == "EARLY_PAYMENT"
                    ? $"pay within {option.PaymentDeadlineDays ?? 7} days"
                    : "";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void Raise([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    // ---- 6. DiscountOption -------------------------------------
    private sealed record DiscountOption(
        int? DiscountId, string DisplayText, string? ConditionType, int? PaymentDeadlineDays);
}
