using Rawr.CustomControls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Rawr
{
    public partial class CalculationDisplay : UserControl
    {
        public CalculationDisplay()
        {
            InitializeComponent();
            BuildControls();
            Calculations.ModelChanged += new EventHandler(Calculations_ModelChanged);
        }

        void Calculations_ModelChanged(object sender, EventArgs e)
        {
            BuildControls();
        }

        Dictionary<string, GroupBox> GroupBoxes = new Dictionary<string, GroupBox>();
        Dictionary<string, ExtendedToolTipLabel> LabelLabels = new Dictionary<string, ExtendedToolTipLabel>();
        Dictionary<string, ExtendedToolTipLabel> ValueLabels = new Dictionary<string, ExtendedToolTipLabel>();
        private void BuildControls()
        {
            this.Controls.Clear();
            GroupBoxes.Clear();
            LabelLabels.Clear();
            ValueLabels.Clear();

            string[] displayLabelConfigurationStrings = null;
            if (Calculations.Instance != null)
            {
                displayLabelConfigurationStrings = Calculations.CharacterDisplayCalculationLabels;
            }
            else
            {
                displayLabelConfigurationStrings = new string[0];
            }
            this.SuspendLayout();
            foreach (string displayLabelConfigurationString in displayLabelConfigurationStrings)
            {
                string[] displayLabelConfigurationSplit = displayLabelConfigurationString.Split(':');
                string group = displayLabelConfigurationSplit[0];
                if (!GroupBoxes.ContainsKey(group))
                {
                    GroupBox groupBox = new GroupBox();
                    groupBox.Text = group;
                    groupBox.Tag = group;
                    groupBox.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
                    groupBox.Dock = DockStyle.Top;
                    GroupBoxes.Add(group, groupBox);
                    this.Controls.Add(groupBox);
                    groupBox.BringToFront();
                }

                string name = displayLabelConfigurationSplit[1];
                string[] nameSplit = name.Split('*');
                name = nameSplit[0];

                ExtendedToolTipLabel labelLabel = new ExtendedToolTipLabel();
                labelLabel.Left = 4;
                labelLabel.Text = name + (nameSplit.Length > 1 ? ": *" : ":");
                labelLabel.AutoSize = true;
                GroupBoxes[group].Controls.Add(labelLabel);
                LabelLabels.Add(name, labelLabel);
                if (nameSplit.Length > 1)
                {
                    labelLabel.ToolTipText = nameSplit[1];
                }

                ExtendedToolTipLabel labelValue = new ExtendedToolTipLabel();
                labelValue.Left = this.Width / 2;
                labelValue.AutoSize = true;
                GroupBoxes[group].Controls.Add(labelValue);
                ValueLabels.Add(name, labelValue);
            }

            int groupY = 3;
            foreach (GroupBox groupBox in GroupBoxes.Values)
            {
                int labelY = 19;
                foreach (ExtendedToolTipLabel label in groupBox.Controls)
                {
                    label.Top = labelY;
                    if (ValueLabels.ContainsValue(label))
                    {
                        labelY += 18;
                    }
                }
                groupBox.Bounds = new Rectangle(6, groupY, 202, labelY);
                groupY += labelY + 6;
            }
            this.ResumeLayout();
        }

        public void SetCalculations(CharacterCalculationsBase characterCalculations)
        {
            foreach (KeyValuePair<string, string> kvp in characterCalculations.GetCharacterDisplayCalculationValues())
            {
                string[] valueSplit = kvp.Value.Split('*');
                string value = valueSplit[0];
                if (valueSplit.Length > 1)
                {
                    ValueLabels[kvp.Key].Text = value + " *";
                    ValueLabels[kvp.Key].ToolTipText = valueSplit[1];
                }
                else
                    ValueLabels[kvp.Key].Text = value;
            }
        }
    }
}
