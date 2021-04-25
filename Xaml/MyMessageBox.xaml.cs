namespace SmartLogReader
{
    /// <summary>
    /// Interaction logic for MyMessageBox.xaml
    /// </summary>
    public partial class MyMessageBox : Dialog
    {
        public MyMessageBox(string message)
        {
            InitializeComponent();
            tb.Text = message;
        }
    }
}
