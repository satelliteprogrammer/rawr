using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Rawr
{
    public class ItemEnchantContextualMenu : ContextMenuStrip
    {
        private static ItemEnchantContextualMenu _instance = null;
        public static ItemEnchantContextualMenu Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ItemEnchantContextualMenu();
                }
                return _instance;
            }
        }

        private Character _character;
        public Character Character
        {
            get { return _character; }
            set { _character = value; }
        }

        private Item _item;
        private ToolStripMenuItem _menuItemName;
        public ItemEnchantContextualMenu()
        {
            _menuItemName = new ToolStripMenuItem();
            _menuItemName.Enabled = false;

            ToolStripMenuItem _menuItemAny = new ToolStripMenuItem("Any");
            _menuItemAny.Click += new EventHandler(_menuItem_Click);

            this.Items.Add(_menuItemName);
            this.Items.Add(new ToolStripSeparator());
            this.Items.Add(_menuItemAny);
        }

        void _menuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menu = sender as ToolStripMenuItem;
            Enchant enchant = (Enchant)menu.Tag;
            Character.ToggleAvailableItemEnchantRestriction(_item, enchant);
        }

        public void Show(Item item)
        {
            _item = item;
            _menuItemName.Text = item.Name;
            Character.ItemAvailability availability = Character.GetItemAvailability(_item);
            string gemmedId = string.Empty;
            bool allEnabled = false;
            switch (availability)
            {
                case Character.ItemAvailability.Available:
                    gemmedId = item.GemmedId;
                    allEnabled = true;
                    break;
                case Character.ItemAvailability.AvailableWithEnchantRestrictions:
                    gemmedId = item.GemmedId;
                    break;
                case Character.ItemAvailability.RegemmingAllowed:
                    allEnabled = true;
                    break;
                case Character.ItemAvailability.RegemmingAllowedWithEnchantRestrictions:
                    gemmedId = item.Id.ToString() + ".*.*.*";
                    break;
                case Character.ItemAvailability.NotAvailabe:
                    gemmedId = item.Id.ToString() + ".*.*.*";
                    break;
            }

            ((ToolStripMenuItem)Items[2]).Checked = allEnabled;
            List<Enchant> list = Enchant.FindEnchants(_item.Slot);
            for (int i = 0; i < list.Count; i++)
            {
                if (Items.Count <= i + 3)
                {
                    ToolStripMenuItem _menuItem = new ToolStripMenuItem();
                    _menuItem.Click += new EventHandler(_menuItem_Click);
                    this.Items.Add(_menuItem);
                }
                Items[i + 3].Tag = list[i];
                Items[i + 3].Text = list[i].ToString();
                Items[i + 3].Visible = true;
                ((ToolStripMenuItem)Items[i + 3]).Checked = (!allEnabled && Character.AvailableItems.Contains(gemmedId + "." + list[i].Id));
            }
            for (int i = list.Count + 3; i < Items.Count; i++)
            {
                Items[i].Visible = false;
            }

            this.Show(Control.MousePosition);
        }
    }
}
