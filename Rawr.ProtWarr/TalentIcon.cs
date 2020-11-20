﻿using System;
using System.Drawing;
using System.Windows.Forms;

namespace Rawr.ProtWarr
{
    public partial class TalentIcon : UserControl
    {
        private Bitmap _icon;
        private ToolTip _tt;
        private string _name;
        private Label _treeLabel;

        public TalentIcon()
        {
            InitializeComponent();
            panel1.Resize += new EventHandler(panel1_Resize);
            panel1.MouseHover += new EventHandler(panel1_MouseHover);

        }

        void panel1_MouseHover(object sender, EventArgs e)
        {
            if (_tt == null)
            {
                _tt = new ToolTip();
            }
            _tt.Show(_name, panel1);
        }

        public TalentIcon(TalentItem ti, Character.CharacterClass charClass, Label treeLabel) : this()
        {

            Talent = ti;
            _name = ti.Name;
            _treeLabel = treeLabel;
            CharClass = charClass;
            getIcon(ti, charClass);
            panel1.Width = _icon.Width;
            panel1.Height = _icon.Height;

        }

        void panel1_Resize(object sender, EventArgs e)
        {
            this.Width = panel1.Width + 2;
            this.Height = panel1.Height + 20;
        }

        public Character.CharacterClass CharClass
        {
            get;
            set;
        }

        public TalentItem Talent
        {
            get;
            set;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (Talent != null) // && CharClass != null)
            {
                panel1.BackgroundImage = _icon;
                label2.Text = "(" + Talent.PointsInvested + "/" + Talent.Rank + ")";
            }
        }

        private void getIcon(TalentItem ti, Character.CharacterClass charclass)
        {
            if (_icon == null)
            {
                WebRequestWrapper wrw = new WebRequestWrapper();
                string filePath = wrw.DownloadTalentIcon(charclass, ti.Tree.Replace(" ", ""), ti.Name.Replace(" ", ""));
                if (!String.IsNullOrEmpty(filePath))
                {
                    _icon = new Bitmap(filePath);
                }
            }


        }

        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (Talent.PointsInvested < Talent.Rank)
                {
                    Talent.PointsInvested++;
                    this.Refresh();
                    refreshTree(true);
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (Talent.PointsInvested > 0)
                {
                    Talent.PointsInvested--;
                    this.Refresh();
                    refreshTree(false);
                }
            }
        }

        private void refreshTree(bool increase)
        {
            int points = Int32.Parse(_treeLabel.Text.Substring(_treeLabel.Text.IndexOf('(') + 1,
                                     _treeLabel.Text.IndexOf(')') - 1 - _treeLabel.Text.IndexOf('(')));
            string treeName = Talent.Tree;
            points = (increase ? points + 1 : points - 1);
            _treeLabel.Text = treeName + " (" + points.ToString() + ")";
            _treeLabel.Refresh();
        }
    }
}
