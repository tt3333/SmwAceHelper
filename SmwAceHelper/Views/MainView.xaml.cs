using System.Windows;
using System.Windows.Threading;

namespace SmwAceHelper.Views
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        public MainView()
        {
            InitializeComponent();
        }

        public void UpdateWindowSize()
        {
            MinHeight = 0;
            MaxHeight = double.PositiveInfinity;
            SizeToContent = SizeToContent.Height;

            Dispatcher.InvokeAsync(() =>
            {
                MinHeight = ActualHeight;
                MaxHeight = ActualHeight;
            }, DispatcherPriority.ApplicationIdle);
        }
    }
}
