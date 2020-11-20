using System;
using System.Windows.Forms;

namespace Rawr
{
    public partial class FormFillSockets : Form, IFormItemSelectionProvider
    {
        private FormItemSelection _formItemSelection;
        public FormItemSelection FormItemSelection
        {
            get
            {
                if (_formItemSelection == null || _formItemSelection.IsDisposed)
                {
                    _formItemSelection = new FormItemSelection();
                    _formItemSelection.Character = FormMain.Instance.FormItemSelection.Character;
                    _formItemSelection.Items = ItemCache.RelevantItems;
                }
                return _formItemSelection;
            }
        }

        public bool FillEmptySockets
        {
            get { return radioButtonEmpty.Checked; }
        }

        public Item GemRed
        {
            get { return gemButtonRed.SelectedItem; }
        }

        public Item GemBlue
        {
            get { return gemButtonBlue.SelectedItem; }
        }

        public Item GemYellow
        {
            get { return gemButtonYellow.SelectedItem; }
        }

        public Item GemMeta
        {
            get { return gemButtonMeta.SelectedItem; }
        }

        public FormFillSockets()
        {
            InitializeComponent();
            //gemButtonRed.Items = gemButtonBlue.Items = gemButtonYellow.Items = gemButtonMeta.Items = ItemCache.GetItemsArray();
        }

        private void FormFillSockets_Load(object sender, EventArgs e)
        {
        }

        private void gemButtonYellow_Click(object sender, EventArgs e)
        {
        }
    }
}