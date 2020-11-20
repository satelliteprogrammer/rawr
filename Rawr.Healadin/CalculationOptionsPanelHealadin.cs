﻿using System;
using System.Text;

namespace Rawr.Healadin
{
    public partial class CalculationOptionsPanelHealadin : CalculationOptionsPanelBase
    {
        public CalculationOptionsPanelHealadin()
        {
            InitializeComponent();
        }

        private bool loading;

        protected override void LoadCalculationOptions()
        {
            loading = true;
            if (Character.CalculationOptions == null)
                Character.CalculationOptions = new CalculationOptionsHealadin();

            CalculationOptionsHealadin calcOpts = Character.CalculationOptions as CalculationOptionsHealadin;
            cmbLength.Value = (decimal)calcOpts.Length;
            cmbManaAmt.Text = calcOpts.ManaAmt.ToString();
            cmbManaTime.Value = (decimal)calcOpts.ManaTime;
            cmbSpriest.Value = (decimal)calcOpts.Spriest;
            cmbSpiritual.Value = (decimal)calcOpts.Spiritual;
            chkBoL.Checked = calcOpts.BoL;

            trkActivity.Value = (int)calcOpts.Activity;
            lblActivity.Text = trkActivity.Value + "%";

            trkRatio.Value = (int)(calcOpts.Ratio * 100);
            labHL1.Text = (100 - trkRatio.Value).ToString() + "%";
            labHL2.Text = trkRatio.Value.ToString() + "%";
            if (calcOpts.Rank1 >= 4 && calcOpts.Rank1 <= 11) nubHL1.Value = (decimal)calcOpts.Rank1;
            if (calcOpts.Rank2 >= 4 && calcOpts.Rank2 <= 11) nubHL2.Value = (decimal)calcOpts.Rank2;

            loading = false;
        }

        private void cmbLength_ValueChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                CalculationOptionsHealadin calcOpts = Character.CalculationOptions as CalculationOptionsHealadin;
                calcOpts.Length = (float)cmbLength.Value;
                Character.OnItemsChanged();
            }
        }

        private void cmbManaAmt_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                CalculationOptionsHealadin calcOpts = Character.CalculationOptions as CalculationOptionsHealadin;
                try
                {
                    calcOpts.ManaAmt = float.Parse(cmbManaAmt.Text);
                }
                catch { }
                Character.OnItemsChanged();
            }
        }

        private void cmbManaTime_ValueChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                CalculationOptionsHealadin calcOpts = Character.CalculationOptions as CalculationOptionsHealadin;
                calcOpts.ManaTime = (float)cmbManaTime.Value;
                Character.OnItemsChanged();
            }
        }

        private void cmbManaAmt_TextUpdate(object sender, EventArgs e)
        {
            if (!loading)
            {
                CalculationOptionsHealadin calcOpts = Character.CalculationOptions as CalculationOptionsHealadin;
                try
                {
                    calcOpts.ManaAmt = float.Parse(cmbManaAmt.Text);
                }
                catch { }
                Character.OnItemsChanged();
            }
        }

        private void trkActivity_Scroll(object sender, EventArgs e)
        {
            if (!loading)
            {
                CalculationOptionsHealadin calcOpts = Character.CalculationOptions as CalculationOptionsHealadin;
                lblActivity.Text = trkActivity.Value + "%";
                calcOpts.Activity = trkActivity.Value;
                Character.OnItemsChanged();
            }
        }

        private void cmbSpriest_ValueChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                CalculationOptionsHealadin calcOpts = Character.CalculationOptions as CalculationOptionsHealadin;
                calcOpts.Spriest = (float)cmbSpriest.Value;
                Character.OnItemsChanged();
            }
        }

        private void cmbSpiritual_ValueChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                CalculationOptionsHealadin calcOpts = Character.CalculationOptions as CalculationOptionsHealadin;
                calcOpts.Spiritual = (float)cmbSpiritual.Value;
                Character.OnItemsChanged();
            }
        }

        private void chkBoL_CheckedChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                CalculationOptionsHealadin calcOpts = Character.CalculationOptions as CalculationOptionsHealadin;
                calcOpts.BoL = chkBoL.Checked;
                Character.OnItemsChanged();
            }

        }

        private void trkRatio_Scroll(object sender, EventArgs e)
        {
            if (!loading)
            {
                CalculationOptionsHealadin calcOpts = Character.CalculationOptions as CalculationOptionsHealadin;
                calcOpts.Ratio = trkRatio.Value / 100f;
                labHL1.Text = (100 - trkRatio.Value).ToString() + "%";
                labHL2.Text = trkRatio.Value.ToString() + "%";
                Character.OnItemsChanged();
            }
        }

        private void nubHL1_ValueChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                CalculationOptionsHealadin calcOpts = Character.CalculationOptions as CalculationOptionsHealadin;
                calcOpts.Rank1 = (int)nubHL1.Value;
                Character.OnItemsChanged();
            }
        }

        private void nubHL2_ValueChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                CalculationOptionsHealadin calcOpts = Character.CalculationOptions as CalculationOptionsHealadin;
                calcOpts.Rank2 = (int)nubHL2.Value;
                Character.OnItemsChanged();
            }
        }

    }

    [Serializable]
    public class CalculationOptionsHealadin : ICalculationOptionBase
    {
        public string GetXml()
        {
            System.Xml.Serialization.XmlSerializer serializer =
                new System.Xml.Serialization.XmlSerializer(typeof(CalculationOptionsHealadin));
            StringBuilder xml = new StringBuilder();
            System.IO.StringWriter writer = new System.IO.StringWriter(xml);
            serializer.Serialize(writer, this);
            return xml.ToString();
        }

        public bool EnforceMetagemRequirements = false;
        public float Length = 5;
        public float ManaAmt = 2400;
        public float ManaTime = 2.5f;
        public float Activity = 80;
        public float Spriest = 0;
        public float Spiritual = 0;
        public bool BoL = true;
        public float Ratio = .25f;
        public int Rank1 = 11;
        public int Rank2 = 9;
    }
}
