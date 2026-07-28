using System.Windows.Controls;

namespace WpfApp;

/// <summary>
/// Helpers for ComboBoxes whose items are written straight into XAML as
/// &lt;ComboBoxItem&gt;Male&lt;/ComboBoxItem&gt; rather than bound to a list.
/// </summary>
public static class ComboBoxHelper
{
    /// <summary>
    /// Selects the item whose text equals <paramref name="value"/>, or leaves the
    /// ComboBox alone when there is no match.
    ///
    /// A hand-written ComboBoxItem holds its text in Content, not in a property the
    /// database knows about, so a stored value like "Female" cannot simply be
    /// assigned to SelectedItem — the matching item object has to be found first.
    /// A value with no matching item (an old option since removed from the XAML)
    /// leaves the box empty rather than throwing.
    /// </summary>
    public static void Select(ComboBox comboBox, string? value)
    {
        if (value == null) return;

        comboBox.SelectedItem = comboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => item.Content?.ToString() == value);
    }
}
