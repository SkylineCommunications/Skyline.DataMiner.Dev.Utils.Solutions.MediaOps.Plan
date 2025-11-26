namespace IAS_TestMediaOpsPlanApi
{
    using Skyline.DataMiner.Automation;
    using Skyline.DataMiner.Utils.InteractiveAutomationScript;

    internal class HomeDialog : Dialog
    {
        public HomeDialog(IEngine engine) : base(engine)
        {
            int row = -1;
            AddWidget(new Label("What would you like to do?"), ++row, 0, 1, 2);
            AddWidget(new Label("# of runs"), ++row, 0);
            AddWidget(RunCountNumeric, row, 1);
            AddWidget(TestDraftBulkPerformanceButton, ++row, 0, 1, 2);
            AddWidget(TestDraftSinglePerformanceButton, ++row, 0, 1, 2);
            AddWidget(DeleteResources, ++row, 0, 1, 2);
            AddWidget(ResultsTextBox, ++row, 0, 1, 2);
        }

        public Button TestDraftBulkPerformanceButton { get; private set; } = new Button("Create Draft Resources in Bulk");

        public Button TestDraftSinglePerformanceButton { get; private set; } = new Button("Create Draft Resources");

        public Button DeleteResources { get; private set; } = new Button("Delete Green Resources");

        public TextBox ResultsTextBox { get; private set; } = new TextBox() { IsMultiline = true, Width = 400, Height = 800 };

        public Numeric RunCountNumeric { get; private set; } = new Numeric(1) { Minimum = 1, Maximum = 20 };
    }
}
