using System.Windows;
using System.Windows.Controls;
using Services;

namespace WpfApp;

public partial class ClassResultWindow : Window
{
    private readonly IGradeService _service = new GradeService();

    public ClassResultWindow() { InitializeComponent(); }

    private void BtnLoad_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show("Load class results — TODO");

    private void BtnReset_Click(object sender, RoutedEventArgs e) { txtClassId.Text = ""; dgResults.ItemsSource = null; }
}
