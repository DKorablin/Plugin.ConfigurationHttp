using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;

namespace Plugin.ConfigurationHttp.Controllers.Message
{
	[DataContract]
	public class SettingsResponse
	{
		[DataMember]
		public String Name { get; private set; }

		[DataMember(EmitDefaultValue=false)]
		public String DisplayName { get; private set; }

		[DataMember]
		public String Type { get; private set; }

		[DataMember(EmitDefaultValue = false)]
		public Boolean CanWrite { get; private set; }

		[DataMember]
		public Object Value { get; private set; }

		[DataMember]
		public String Description { get; private set; }

		[DataMember]
		public Object DefaultValue { get; private set; }

		[DataMember(EmitDefaultValue = false)]
		public Boolean IsReadOnly { get; private set; }

		[DataMember(EmitDefaultValue = false)]
		public Boolean IsPassword { get; private set; }

		[DataMember(EmitDefaultValue=false)]
		public String[] Editors { get; private set; }

		internal SettingsResponse(PropertyInfo item, Object target)
		{
			this.Name = item.Name;
			this.Type = item.PropertyType.FullName;
			this.CanWrite = item.CanWrite;
			this.Value = item.CanRead ? item.GetValue(target, null) : null;

			DescriptionAttribute dAttr = item.GetCustomAttribute<DescriptionAttribute>();
			if(dAttr != null)
				this.Description = dAttr.Description;

			DefaultValueAttribute vAttr = item.GetCustomAttribute<DefaultValueAttribute>();
			if(vAttr != null)
				this.DefaultValue = vAttr.Value;

			ReadOnlyAttribute rAttr = item.GetCustomAttribute<ReadOnlyAttribute>();
			if(rAttr != null)
				this.IsReadOnly = rAttr.IsReadOnly;

			DisplayNameAttribute nAttr = item.GetCustomAttribute<DisplayNameAttribute>();
			if(nAttr != null)
				this.DisplayName = nAttr.DisplayName;

			PasswordPropertyTextAttribute pAttr = item.GetCustomAttribute<PasswordPropertyTextAttribute>();
			if(pAttr != null)
				this.IsPassword = pAttr.Password;

			EditorAttribute[] editorAttr = item.GetCustomAttributes<EditorAttribute>();
			if(editorAttr != null)
				this.Editors = Array.ConvertAll(editorAttr, e => e.EditorTypeName);
		}
	}
}