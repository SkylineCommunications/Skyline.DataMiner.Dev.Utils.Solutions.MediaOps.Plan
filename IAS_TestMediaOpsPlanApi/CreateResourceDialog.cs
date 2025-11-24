namespace IAS_TestMediaOpsPlanApi
{
    using Skyline.DataMiner.Automation;
    using Skyline.DataMiner.Utils.InteractiveAutomationScript;

    internal class CreateResourceDialog : Dialog
    {
        public CreateResourceDialog(IEngine engine) : base(engine)
        {
            Title = "Create New Resource";
            AddWidget(ResourceNameTextBox, 0, 0);
            AddWidget(Button, 0, 1, HorizontalAlignment.Right);

            AddWidget(ResultsTextBox, 1, 0, 1, 2);
        }

        public TextBox ResourceNameTextBox { get; private set; } = new TextBox { PlaceHolder = "Resource Name", Width = 150 };

        public Button Button { get; private set; } = new Button("Create Resource") { Style = ButtonStyle.CallToAction };

        public TextBox ResultsTextBox { get; private set; } = new TextBox { IsMultiline = true, Width = 250, Height = 400 };
    }
}
