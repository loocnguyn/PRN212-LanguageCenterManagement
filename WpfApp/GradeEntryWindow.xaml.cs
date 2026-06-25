using System.Windows;
using System.Windows.Controls;
using Services;

namespace WpfApp;

public partial class GradeEntryWindow : Window
{
    private readonly IGradeService _service = new GradeService();

    public GradeEntryWindow() { InitializeComponent(); }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Load students for grade entry — TODO");

    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtClassId.Text = ""; dgGrades.ItemsSource = null; }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Save grades — TODO");
}
