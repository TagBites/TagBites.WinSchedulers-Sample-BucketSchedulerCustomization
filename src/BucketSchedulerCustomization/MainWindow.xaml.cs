using System.Windows;

namespace BucketSchedulerCustomization
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }


        private void Initialize()
        {
            SC_Scheduler.DataSource = new SchedulerDataSource();
            SC_Scheduler.ColumnScroller.HeaderSize = 150;
            SC_Scheduler.RowScroller.HeaderSize = 100;
            SC_Scheduler.IsColumnSummaryVisible = true;

            SC_Scheduler.ColumnHeaderClick += (s, e) => { MessageBox.Show("Column was clicked"); };
            SC_Scheduler.RowHeaderClick += (s, e) => { MessageBox.Show("Row was clicked"); };
        }
    }
}
