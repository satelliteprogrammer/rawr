using System;
using System.Collections.Generic;
using System.Text;

namespace Rawr.Tankadin
{
    public partial class CalculationOptionsPanelTankadin : CalculationOptionsPanelBase
    {

        private bool _loadingCalculationOptions;
        private Dictionary<int, string> armorBosses = new Dictionary<int, string>();

        public CalculationOptionsPanelTankadin()
        {
            InitializeComponent();
            armorBosses.Add(3800, "Shade of Aran");
            armorBosses.Add(4700, "Roar");
            armorBosses.Add(5500, "Netherspite");
            armorBosses.Add(6100, "Julianne, Curator");
            armorBosses.Add(6200, "Karathress, Vashj, Solarian, Kael'thas, Winterchill, Anetheron, Kaz'rogal, Azgalor, Archimonde, Teron, Shahraz");
            armorBosses.Add(6700, "Maiden, Illhoof");
            armorBosses.Add(7300, "Strawman");
            armorBosses.Add(7500, "Attumen");
            armorBosses.Add(7600, "Romulo, Nightbane, Malchezaar, Doomwalker");
            armorBosses.Add(7700, "Hydross, Lurker, Leotheras, Tidewalker, Al'ar, Naj'entus, Supremus, Akama, Gurtogg");
            armorBosses.Add(8200, "Midnight");
            armorBosses.Add(8800, "Void Reaver");
        }

        protected override void LoadCalculationOptions()
        {
            _loadingCalculationOptions = true;
            if (Character.CalculationOptions == null)
                Character.CalculationOptions = new CalculationOptionsTankadin();

            CalculationOptionsTankadin calcOpts = Character.CalculationOptions as CalculationOptionsTankadin;
            cmbTargetLevel.SelectedIndex = calcOpts.TargetLevel - 70;
            nubAtkSpeed.Value = (decimal)calcOpts.AttackSpeed;
            nubAttackers.Value = (decimal)calcOpts.NumberAttackers;
            trackBarBossAttackValue.Value = calcOpts.AverageHit;
            trackBarMitigationScale.Value = calcOpts.MitigationScale;
            trackBarTargetArmor.Value = calcOpts.TargetArmor;
            trackBarThreatScale.Value = calcOpts.ThreatScale;
            labelBossAttackValue.Text = calcOpts.AverageHit.ToString();
            labelMitigationScale.Text = calcOpts.MitigationScale.ToString();
            labelTargetArmor.Text = calcOpts.TargetArmor.ToString();
            labelThreatScale.Text = calcOpts.ThreatScale.ToString();
            checkBoxExorcism.Checked = calcOpts.Exorcism;
            checkBoxRetAura.Checked = calcOpts.RetAura;
            checkBoxJotC.Checked = calcOpts.JotC;
            _loadingCalculationOptions = false;

        }

        private void cmbTargetLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!_loadingCalculationOptions)
            {
                CalculationOptionsTankadin calcOpts = Character.CalculationOptions as CalculationOptionsTankadin;
                calcOpts.TargetLevel = int.Parse(cmbTargetLevel.SelectedItem.ToString());
                Character.OnItemsChanged();
            }
        }

        private void nubAttackers_ValueChanged(object sender, EventArgs e)
        {
            if (!_loadingCalculationOptions)
            {
                CalculationOptionsTankadin calcOpts = Character.CalculationOptions as CalculationOptionsTankadin;
                calcOpts.NumberAttackers = (int)nubAttackers.Value;
                Character.OnItemsChanged();
            }
        }

        private void nubAtkSpeed_ValueChanged(object sender, EventArgs e)
        {
            if (!_loadingCalculationOptions)
            {
                CalculationOptionsTankadin calcOpts = Character.CalculationOptions as CalculationOptionsTankadin;
                calcOpts.AttackSpeed = (float)nubAtkSpeed.Value;
                Character.OnItemsChanged();
            }
        }

        private void trackBarTargetArmor_Scroll(object sender, EventArgs e)
        {
            if (!_loadingCalculationOptions)
            {
                CalculationOptionsTankadin calcOpts = Character.CalculationOptions as CalculationOptionsTankadin;
                calcOpts.TargetArmor = trackBarTargetArmor.Value;
                labelTargetArmor.Text = trackBarTargetArmor.Value.ToString();
                Character.OnItemsChanged();
            }
        }

        private void trackBarThreatScale_Scroll(object sender, EventArgs e)
        {
            if (!_loadingCalculationOptions)
            {
                CalculationOptionsTankadin calcOpts = Character.CalculationOptions as CalculationOptionsTankadin;
                calcOpts.ThreatScale = trackBarThreatScale.Value;
                labelThreatScale.Text = trackBarThreatScale.Value.ToString();
                Character.OnItemsChanged();
            }
        }

        private void trackBarMitigationScale_Scroll(object sender, EventArgs e)
        {
            if (!_loadingCalculationOptions)
            {
                CalculationOptionsTankadin calcOpts = Character.CalculationOptions as CalculationOptionsTankadin;
                calcOpts.MitigationScale = trackBarMitigationScale.Value;
                labelMitigationScale.Text = trackBarMitigationScale.Value.ToString();
                Character.OnItemsChanged();
            }
        }

        private void trackBarBossAttackValue_Scroll(object sender, EventArgs e)
        {
            if (!_loadingCalculationOptions)
            {
                CalculationOptionsTankadin calcOpts = Character.CalculationOptions as CalculationOptionsTankadin;
                calcOpts.AverageHit = trackBarBossAttackValue.Value;
                labelBossAttackValue.Text = trackBarBossAttackValue.Value.ToString();
                Character.OnItemsChanged();
            }
        }

        private void checkBoxExorcism_CheckedChanged(object sender, EventArgs e)
        {
            if (!_loadingCalculationOptions)
            {
                CalculationOptionsTankadin calcOpts = Character.CalculationOptions as CalculationOptionsTankadin;
                calcOpts.Exorcism = checkBoxExorcism.Checked;
                Character.OnItemsChanged();
            }
        }

        private void checkBoxRetAura_CheckedChanged(object sender, EventArgs e)
        {
            if (!_loadingCalculationOptions)
            {
                CalculationOptionsTankadin calcOpts = Character.CalculationOptions as CalculationOptionsTankadin;
                calcOpts.RetAura = checkBoxRetAura.Checked;
                Character.OnItemsChanged();
            }
        }

        private void checkBoxJotC_CheckedChanged(object sender, EventArgs e)
        {
            if (!_loadingCalculationOptions)
            {
                CalculationOptionsTankadin calcOpts = Character.CalculationOptions as CalculationOptionsTankadin;
                calcOpts.JotC = checkBoxJotC.Checked;
                Character.OnItemsChanged();
            }
        }
    }

    [Serializable]
    public class CalculationOptionsTankadin : ICalculationOptionBase
    {
        public string GetXml()
        {
            System.Xml.Serialization.XmlSerializer serializer =
                new System.Xml.Serialization.XmlSerializer(typeof(CalculationOptionsTankadin));
            StringBuilder xml = new StringBuilder();
            System.IO.StringWriter writer = new System.IO.StringWriter(xml);
            serializer.Serialize(writer, this);
            return xml.ToString();
        }

        public bool EnforceMetagemRequirements = false;
        public int TargetLevel = 73;
        public int AverageHit = 20000;
        public float AttackSpeed = 2;
        public int NumberAttackers = 1;
        public int TargetArmor = 6600;
        public int ThreatScale = 100;
        public int MitigationScale = 4000;
        public bool Exorcism = false;
        public bool RetAura = false;
        public bool JotC = true;
    }

}
