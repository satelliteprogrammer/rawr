﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Rawr.Retribution
{
    public partial class CalculationOptionsPanelRetribution : CalculationOptionsPanelBase
    {
        private Dictionary<int, string> armorBosses = new Dictionary<int, string>();

        public CalculationOptionsPanelRetribution()
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
            if (Character.CalculationOptions == null)
                Character.CalculationOptions = new CalculationOptionsRetribution();

            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;

            rbSoC.Checked = calcOpts.Seal == 0;
            rbSoB.Checked = calcOpts.Seal == 1;

            cbTargetLevel.SelectedItem = calcOpts.TargetLevel.ToString();
            tbFightLength.Value = calcOpts.FightLength;
            lblFightLengthNum.Text = tbFightLength.Value.ToString();

            checkBoxConsecration.Checked = calcOpts.ConsecRank > 0;
            cbConsRank.SelectedItem = "Rank " + calcOpts.ConsecRank.ToString();
            checkBoxExorcism.Checked = calcOpts.Exorcism;

            checkBoxMeta.Checked = calcOpts.EnforceMetagemRequirements;

            rbAldor.Checked = calcOpts.ShattrathFaction == "Aldor";
            rbScryer.Checked = calcOpts.ShattrathFaction == "Scryer";

            tbExposeWeakness.Value = calcOpts.ExposeWeaknessAPValue;
            lblExposeWeaknessNum.Text = tbExposeWeakness.Value.ToString();

            tbBloodlust.Value = calcOpts.Bloodlust;
            lblBloodlustNum.Text = tbBloodlust.Value.ToString();

            tbDrumsOfBattle.Value = calcOpts.DrumsOfBattle;
            lblDrumsOfBattleNum.Text = tbDrumsOfBattle.Value.ToString();

            tbDrumsOfWar.Value = calcOpts.DrumsOfWar;
            lblDrumsOfWarNum.Text = tbDrumsOfWar.Value.ToString();

            tbFerociousInspiration.Value = calcOpts.FerociousInspiration;
            lblFerociousInspirationNum.Text = tbFerociousInspiration.Value.ToString();

            nudTargetArmor.Value = calcOpts.BossArmor;
        }


        private void rbSoC_CheckedChanged(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            calcOpts.Seal = rbSoC.Checked ? 0 : 1;
            Character.OnItemsChanged();
        }

        private void rbSoB_CheckedChanged(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            calcOpts.Seal = rbSoB.Checked ? 1 : 0;
            Character.OnItemsChanged();
        }

        private void checkBoxConsecration_CheckedChanged(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            if (checkBoxConsecration.Checked)
            {
                cbConsRank.Enabled = true;
                cbConsRank.SelectedItem = "Rank 1";
                calcOpts.ConsecRank = 1;
            }
            else
            {
                cbConsRank.Enabled = false;
                calcOpts.ConsecRank = 0;
            }
            Character.OnItemsChanged();
        }

        private void cbConsRank_SelectedIndexChanged(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            calcOpts.ConsecRank = int.Parse(cbConsRank.SelectedItem.ToString().Substring(5, 1));
            Character.OnItemsChanged();
        }

        private void cbTargetLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            calcOpts.TargetLevel = int.Parse(cbTargetLevel.SelectedItem.ToString());
            Character.OnItemsChanged();
        }

        private void tbFightLength_Scroll(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            calcOpts.FightLength = tbFightLength.Value;
            lblFightLengthNum.Text = tbFightLength.Value.ToString();
            Character.OnItemsChanged();
        }

        private void checkBoxExorcism_CheckedChanged(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            calcOpts.Exorcism = checkBoxExorcism.Checked;
            Character.OnItemsChanged();
        }

        private void btnTalents_Click(object sender, EventArgs e)
        {
            Talents talents = new Talents(this);
            talents.Show();
        }

        private void btnGraph_Click(object sender, EventArgs e)
        {
            CalculationsRetribution retCalc = new CalculationsRetribution();
            CharacterCalculationsRetribution baseCalc = retCalc.GetCharacterCalculations(Character) as CharacterCalculationsRetribution;
            Bitmap _prerenderedGraph = global::Rawr.Retribution.Properties.Resources.GraphBase;
            Graphics g = Graphics.FromImage(_prerenderedGraph);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            float graphHeight = 700f, graphStart = 100f;
            Color[] colors = new Color[] {
                Color.FromArgb(127,202,180,96), // Strength
                Color.FromArgb(127,101,225,240), // Agility
                Color.FromArgb(127,0,4,3), // Attack Power
                Color.FromArgb(127,123,238,199), // Crit Rating
                Color.FromArgb(127,45,112,63), // Hit Rating
                Color.FromArgb(127,121,72,210), //Expertise Rating
                Color.FromArgb(127,217,100,54), // Haste Rating
                Color.FromArgb(127,210,72,195), // Armor Penetration
                Color.FromArgb(127,206,189,191), // Spell Damage
            };
            Stats[] statsList = new Stats[] {
                new Stats() { Strength = 10 },
                new Stats() { Agility = 10 },
                new Stats() { AttackPower = 20 },
                new Stats() { CritRating = 10 },
                new Stats() { HitRating = 10 },
                new Stats() { ExpertiseRating = 10 },
                new Stats() { HasteRating = 10 },
                new Stats() { ArmorPenetration = 66.667f },
                new Stats() { SpellDamageRating = 11.17f },
            };

            for (int index = 0; index < statsList.Length; index++)
            {
                Stats newStats = new Stats();
                Point[] points = new Point[100];
                for (int count = 0; count < 100; count++)
                {
                    newStats = newStats + statsList[index];

                    CharacterCalculationsRetribution currentCalc = retCalc.GetCharacterCalculations(Character, new Item() { Stats = newStats }) as CharacterCalculationsRetribution;
                    float overallPoints = currentCalc.DPSPoints - baseCalc.DPSPoints;

                    if ((graphHeight - overallPoints) > 16)
                        points[count] = new Point(Convert.ToInt32(graphStart + count * 5), (Convert.ToInt32(graphHeight - overallPoints)));
                    else
                        points[count] = points[count - 1];

                }
                Brush statBrush = new SolidBrush(colors[index]);
                g.DrawLines(new Pen(statBrush, 3), points);
            }

            #region Graph Ticks
            float graphWidth = 500f;// this.Width - 150f;
            float graphEnd = graphStart + graphWidth;
            //float graphStartY = 16f;
            float maxScale = 100f;
            float[] ticks = new float[] {(float)Math.Round(graphStart + graphWidth * 0.5f),
                            (float)Math.Round(graphStart + graphWidth * 0.75f),
                            (float)Math.Round(graphStart + graphWidth * 0.25f),
                            (float)Math.Round(graphStart + graphWidth * 0.125f),
                            (float)Math.Round(graphStart + graphWidth * 0.375f),
                            (float)Math.Round(graphStart + graphWidth * 0.625f),
                            (float)Math.Round(graphStart + graphWidth * 0.875f)};
            Pen black200 = new Pen(Color.FromArgb(200, 0, 0, 0));
            Pen black150 = new Pen(Color.FromArgb(150, 0, 0, 0));
            Pen black75 = new Pen(Color.FromArgb(75, 0, 0, 0));
            Pen black50 = new Pen(Color.FromArgb(50, 0, 0, 0));
            Pen black25 = new Pen(Color.FromArgb(25, 0, 0, 0));
            StringFormat formatTick = new StringFormat();
            formatTick.LineAlignment = StringAlignment.Far;
            formatTick.Alignment = StringAlignment.Center;
            Brush black200brush = new SolidBrush(Color.FromArgb(200, 0, 0, 0));
            Brush black150brush = new SolidBrush(Color.FromArgb(150, 0, 0, 0));
            Brush black75brush = new SolidBrush(Color.FromArgb(75, 0, 0, 0));
            Brush black50brush = new SolidBrush(Color.FromArgb(50, 0, 0, 0));
            Brush black25brush = new SolidBrush(Color.FromArgb(25, 0, 0, 0));

            g.DrawLine(black200, graphStart - 4, 20, graphEnd + 4, 20);
            g.DrawLine(black200, graphStart, 16, graphStart, _prerenderedGraph.Height - 16);
            g.DrawLine(black200, graphEnd, 16, graphEnd, 19);
            g.DrawLine(black200, ticks[0], 16, ticks[0], 19);
            g.DrawLine(black150, ticks[1], 16, ticks[1], 19);
            g.DrawLine(black150, ticks[2], 16, ticks[2], 19);
            g.DrawLine(black75, ticks[3], 16, ticks[3], 19);
            g.DrawLine(black75, ticks[4], 16, ticks[4], 19);
            g.DrawLine(black75, ticks[5], 16, ticks[5], 19);
            g.DrawLine(black75, ticks[6], 16, ticks[6], 19);
            g.DrawLine(black75, graphEnd, 21, graphEnd, _prerenderedGraph.Height - 4);
            g.DrawLine(black75, ticks[0], 21, ticks[0], _prerenderedGraph.Height - 4);
            g.DrawLine(black50, ticks[1], 21, ticks[1], _prerenderedGraph.Height - 4);
            g.DrawLine(black50, ticks[2], 21, ticks[2], _prerenderedGraph.Height - 4);
            g.DrawLine(black25, ticks[3], 21, ticks[3], _prerenderedGraph.Height - 4);
            g.DrawLine(black25, ticks[4], 21, ticks[4], _prerenderedGraph.Height - 4);
            g.DrawLine(black25, ticks[5], 21, ticks[5], _prerenderedGraph.Height - 4);
            g.DrawLine(black25, ticks[6], 21, ticks[6], _prerenderedGraph.Height - 4);
            g.DrawLine(black200, graphStart - 4, _prerenderedGraph.Height - 20, graphEnd + 4, _prerenderedGraph.Height - 20);

            Font tickFont = new Font("Calibri", 11);
            g.DrawString((0f).ToString(), tickFont, black200brush, graphStart, 16, formatTick);
            g.DrawString((maxScale).ToString(), tickFont, black200brush, graphEnd, 16, formatTick);
            g.DrawString((maxScale * 0.5f).ToString(), tickFont, black200brush, ticks[0], 16, formatTick);
            g.DrawString((maxScale * 0.75f).ToString(), tickFont, black150brush, ticks[1], 16, formatTick);
            g.DrawString((maxScale * 0.25f).ToString(), tickFont, black150brush, ticks[2], 16, formatTick);
            g.DrawString((maxScale * 0.125f).ToString(), tickFont, black75brush, ticks[3], 16, formatTick);
            g.DrawString((maxScale * 0.375f).ToString(), tickFont, black75brush, ticks[4], 16, formatTick);
            g.DrawString((maxScale * 0.625f).ToString(), tickFont, black75brush, ticks[5], 16, formatTick);
            g.DrawString((maxScale * 0.875f).ToString(), tickFont, black75brush, ticks[6], 16, formatTick);

            g.DrawString((0f).ToString(), tickFont, black200brush, graphStart, _prerenderedGraph.Height - 16, formatTick);
            g.DrawString((maxScale).ToString(), tickFont, black200brush, graphEnd, _prerenderedGraph.Height - 16, formatTick);
            g.DrawString((maxScale * 0.5f).ToString(), tickFont, black200brush, ticks[0], _prerenderedGraph.Height - 16, formatTick);
            g.DrawString((maxScale * 0.75f).ToString(), tickFont, black150brush, ticks[1], _prerenderedGraph.Height - 16, formatTick);
            g.DrawString((maxScale * 0.25f).ToString(), tickFont, black150brush, ticks[2], _prerenderedGraph.Height - 16, formatTick);
            g.DrawString((maxScale * 0.125f).ToString(), tickFont, black75brush, ticks[3], _prerenderedGraph.Height - 16, formatTick);
            g.DrawString((maxScale * 0.375f).ToString(), tickFont, black75brush, ticks[4], _prerenderedGraph.Height - 16, formatTick);
            g.DrawString((maxScale * 0.625f).ToString(), tickFont, black75brush, ticks[5], _prerenderedGraph.Height - 16, formatTick);
            g.DrawString((maxScale * 0.875f).ToString(), tickFont, black75brush, ticks[6], _prerenderedGraph.Height - 16, formatTick);
            #endregion

            Graph graph = new Graph(_prerenderedGraph);
            graph.Show();
        }

        private void checkBoxMeta_CheckedChanged(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            Character.EnforceMetagemRequirements = checkBoxMeta.Checked;
            Character.OnItemsChanged();
        }

        private void rbAldor_CheckedChanged(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            calcOpts.ShattrathFaction = rbAldor.Checked ? "Aldor" : "Scryer";
            Character.OnItemsChanged();
        }

        private void rbScryer_CheckedChanged(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            calcOpts.ShattrathFaction = rbAldor.Checked ? "Aldor" : "Scryer";
            Character.OnItemsChanged();
        }

        private void tbExposeWeakness_ValueChanged(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            calcOpts.ExposeWeaknessAPValue = tbExposeWeakness.Value;
            lblExposeWeaknessNum.Text = tbExposeWeakness.Value.ToString();
            Character.OnItemsChanged();
        }

        private void tbBloodlust_ValueChanged(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            calcOpts.Bloodlust = tbBloodlust.Value;
            lblBloodlustNum.Text = tbBloodlust.Value.ToString();
            Character.OnItemsChanged();
        }

        private void tbDrumsOfBattle_ValueChanged(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            calcOpts.DrumsOfBattle = tbDrumsOfBattle.Value;
            lblDrumsOfBattleNum.Text = tbDrumsOfBattle.Value.ToString();
            Character.OnItemsChanged();
        }

        private void tbDrumsOfWar_ValueChanged(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            calcOpts.DrumsOfWar = tbDrumsOfWar.Value;
            lblDrumsOfWarNum.Text = tbDrumsOfWar.Value.ToString();
            Character.OnItemsChanged();
        }

        private void tbFerociousInspiration_ValueChanged(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            calcOpts.FerociousInspiration = tbFerociousInspiration.Value;
            lblFerociousInspirationNum.Text = tbFerociousInspiration.Value.ToString();
            Character.OnItemsChanged();
        }

        private void nudTargetArmor_ValueChanged(object sender, EventArgs e)
        {
            CalculationOptionsRetribution calcOpts = Character.CalculationOptions as CalculationOptionsRetribution;
            calcOpts.BossArmor = (int)nudTargetArmor.Value;
            Character.OnItemsChanged();
        }
    }

    [Serializable]
    public class CalculationOptionsRetribution : ICalculationOptionBase
    {
        public string GetXml()
        {
            System.Xml.Serialization.XmlSerializer serializer =
                new System.Xml.Serialization.XmlSerializer(typeof(CalculationOptionsRetribution));
            StringBuilder xml = new StringBuilder();
            System.IO.StringWriter writer = new System.IO.StringWriter(xml);
            serializer.Serialize(writer, this);
            return xml.ToString();
        }

        public int TargetLevel = 73;
        public int BossArmor = 7700;
        public int FightLength = 10;
        public bool Exorcism = false;
        public int ConsecRank = 0;
        public int Seal = 0;
        public bool EnforceMetagemRequirements = false;
        public string ShattrathFaction = "Aldor";
        public int Bloodlust = 1;
        public int DrumsOfBattle = 1;
        public int DrumsOfWar = 1;
        public int ExposeWeaknessAPValue = 200;
        public int FerociousInspiration = 2;

        public int TwoHandedSpec = 0;
        public int Conviction = 0;
        public int Crusade = 0;
        public int DivineStrength = 0;
        public int Fanaticism = 0;
        public int ImprovedSanctityAura = 0;
        public int Precision = 0;
        public int SanctityAura = 0;
        public int SanctifiedSeals = 0;
        public int Vengeance = 0;
        public bool TalentsSaved = false;
    }
}
