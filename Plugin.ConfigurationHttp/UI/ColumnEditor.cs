using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Plugin.ConfigurationHttp.UI
{
	internal class ColumnEditor<T> : UITypeEditor
	{
		private ColumnEditorControl _control;

		public override Object EditValue(ITypeDescriptorContext context, IServiceProvider provider, Object value)
		{
			if(this._control == null)
				this._control = new ColumnEditorControl(typeof(T));
			this._control.SetStatus((Int32)value);
			((IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService))).DropDownControl(this._control);
			return this._control.Result;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
			=> UITypeEditorEditStyle.DropDown;

		private sealed class ColumnEditorControl : UserControl
		{
			private readonly CheckedListBox cblColumns = new CheckedListBox();
			public Int32 Result
			{
				get
				{
					Boolean[] columns = new Boolean[this.cblColumns.Items.Count];
					for(Int32 loop = 0; loop < columns.Length; loop++)
						columns[loop] = this.cblColumns.GetItemChecked(loop);

					return Array.Exists<Boolean>(columns, item => item != columns[0])
						? (Int32)Utils.BitToInt(columns)[0]
						: 0;
				}
			}

			public ColumnEditorControl(Type enumType)
			{
				base.SuspendLayout();
				base.BackColor = SystemColors.Control;
				this.cblColumns.FormattingEnabled = true;
				this.cblColumns.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
				this.cblColumns.BorderStyle = BorderStyle.None;
				base.Size = new Size(this.cblColumns.Width, this.cblColumns.Height);

				foreach(Object value in Enum.GetValues(enumType))
					this.cblColumns.Items.Add(value.ToString());

				base.Controls.AddRange(new Control[] { this.cblColumns });
				this.cblColumns.Focus();
				base.ResumeLayout();
			}

			public void SetStatus(Int32 flags)
			{
				for(Int32 loop = 0; loop < this.cblColumns.Items.Count; loop++)
					if(flags == 0)
						cblColumns.SetItemChecked(loop, true);
					else
						cblColumns.SetItemChecked(loop, (flags >> loop & 0x01) == 1);
			}
		}
	}
}