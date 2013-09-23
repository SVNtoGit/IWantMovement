#region Revision info
/*
 * $Author: millz $
 * $Date: 2013-06-28 14:41:37 +0200 (Fri, 28 Jun 2013) $
 * $ID: $
 * $Revision: 46 $
 * $URL: http://subversion.assembla.com/svn/iwantmovement/trunk/IWantMovement/Settings/GUI.cs $
 * $LastChangedBy: millz $
 * $ChangesMade: $
 */
#endregion

using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace IWantMovement.Settings
{
    public partial class GUI : Form
    {
        public GUI()
        {
            InitializeComponent();
        }

        private void GUI_Load(object sender, EventArgs e)
        {
            pgSettings.SelectedObject = IWMSettings.Instance;
            IWMSettings.Instance.Load();
        }

        private void GUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            IWMSettings.Instance.Save();
            //Managers.Movement.ValidatedSettings = false;
        }

        private void pgSettings_Click(object sender, EventArgs e)
        {

        }

        private void donateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Thanks! All donations are greatly appreciated.");
            Process.Start("http://bit.ly/YEb4SU");
        }

        private void reportAnIssueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.thebuddyforum.com/honorbuddy-forum/plugins/movement/116958-plugin-i-want-movement-use-lazyraider-crs-afk-bot-bases.html");
        }


    }
}
