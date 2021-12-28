using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LORICA4
{
    public partial class Landuse_determinator : Form
    {
        public Landuse_determinator()
        {
            InitializeComponent();
        }

        private void landuse_ready_button_Click(object sender, EventArgs e)
        {
            this.Visible = false;
        }
    }
}
