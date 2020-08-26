using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Config;

namespace CodeImp.DoomBuilder.UDBScript
{
	public partial class ScriptOptionsControl : UserControl
	{
		public DataGridView ParametersView { get { return parametersview; } }

		public ScriptOptionsControl()
		{
			InitializeComponent();
		}

		private void parametersview_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
		{
			ScriptOption so = (ScriptOption)parametersview.Rows[e.RowIndex].Tag;

			// Enumerable?
			if (so.typehandler.IsEnumerable)
			{
				// Fill combo with enums
				enumscombo.SelectedItem = null;
				enumscombo.Text = "";
				enumscombo.Items.Clear();
				enumscombo.Items.AddRange(so.typehandler.GetEnumList().ToArray());
				//enumscombo.Tag = frow;

				// Lock combo to enums?
				if (so.typehandler.IsLimitedToEnums)
					enumscombo.DropDownStyle = ComboBoxStyle.DropDownList;
				else
					enumscombo.DropDownStyle = ComboBoxStyle.DropDown;

				// Position combobox
				Rectangle cellrect = parametersview.GetCellDisplayRectangle(1, e.RowIndex, false);
				enumscombo.Location = new Point(cellrect.Left, cellrect.Top);
				enumscombo.Width = cellrect.Width;
				int internalheight = cellrect.Height - (enumscombo.Height - enumscombo.ClientRectangle.Height) - 6;
				//General.SendMessage(enumscombo.Handle, General.CB_SETITEMHEIGHT, new IntPtr(-1), new IntPtr(internalheight));

				// Select the value of this field (for DropDownList style combo)
				foreach (EnumItem i in enumscombo.Items)
				{
					// Matches?
					if (string.Compare(i.Title, so.typehandler.GetStringValue(), StringComparison.OrdinalIgnoreCase) == 0)
					{
						// Select this item
						enumscombo.SelectedItem = i;
						break; //mxd
					}
				}

				// Put the display text in the text (for DropDown style combo)
				enumscombo.Text = so.typehandler.GetStringValue();

				// Show combo
				enumscombo.Show();
			}
		}
	}
}
