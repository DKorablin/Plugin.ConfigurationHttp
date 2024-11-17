using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Plugin.ConfigurationHttp.UI
{
	internal class ColumnEditorTyped<T> : UITypeEditor
	{
		private ColumnEditorControl _control;

		public override Object EditValue(ITypeDescriptorContext context, IServiceProvider provider, Object value)
		{
			if(this._control == null)
				this._control = new ColumnEditorControl();

			String[] valueStrings = value.ToString().Split(',');
			T[] valueTypes = new T[valueStrings.Length];
			for(Int32 loop = 0; loop < valueStrings.Length; loop++)
				valueTypes[loop] = (T)Enum.Parse(typeof(T), valueStrings[loop]);

			this._control.SetStatus(valueTypes);
			((IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService))).DropDownControl(this._control);
			return this._control.Result; //return base.EditValue(context, provider, value);
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
			=> UITypeEditorEditStyle.DropDown; //return base.GetEditStyle(context);

		private class ColumnEditorControl : UserControl
		{
			private readonly CheckedListBox cblColumns = new CheckedListBox();
			public T Result
			{
				get
				{
					List<String> result = new List<String>();
					for(Int32 loop = 0; loop < this.cblColumns.Items.Count; loop++)
						if(this.cblColumns.GetItemChecked(loop))
							result.Add((String)this.cblColumns.Items[loop]);

					if(result.Count == 0)
						return default;
					else
					{
						String resultString = String.Join(",", result.ToArray());
						return (T)Enum.Parse(typeof(T), resultString);
					}
				}
			}

			public ColumnEditorControl()
			{
				base.SuspendLayout();
				base.BackColor = SystemColors.Control;
				this.cblColumns.FormattingEnabled = true;
				this.cblColumns.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
				this.cblColumns.BorderStyle = BorderStyle.None;
				base.Size = new Size(this.cblColumns.Width, this.cblColumns.Height);

				foreach(String name in Enum.GetNames(typeof(T)))
					this.cblColumns.Items.Add(name);

				base.Controls.AddRange(new Control[] { this.cblColumns });
				this.cblColumns.Focus();
				base.ResumeLayout();
			}
			public void SetStatus(T[] codes)
			{
				for(Int32 loop = 0; loop < cblColumns.Items.Count; loop++)
					cblColumns.SetItemChecked(loop, false);

				if(codes != null && codes.Length > 0)
				{
					String[] strCodes = Array.ConvertAll(codes, p => p.ToString());
					for(Int32 loop = 0; loop < this.cblColumns.Items.Count; loop++)
						foreach(String code in strCodes)
							if((String)cblColumns.Items[loop] == code)
								cblColumns.SetItemChecked(loop, true);
				}
			}
		}
	}
}