﻿using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Globalization;

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

		private static Boolean IsNullableTimeSpan(Type t)
			=> t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>) && t.GetGenericArguments()[0] == typeof(TimeSpan);

		internal SettingsResponse(PropertyInfo item, Object target)
		{
			this.Name = item.Name;
			this.Type = item.PropertyType.FullName;
			this.CanWrite = item.CanWrite;

			Object rawValue = item.CanRead ? item.GetValue(target, null) : null;

			// Force TimeSpan (and nullable TimeSpan) to serialize as constant (invariant) string for the JS UI
			if(item.PropertyType == typeof(TimeSpan) && rawValue != null)
				this.Value = TypeDescriptor.GetConverter(item.PropertyType).ConvertToInvariantString(rawValue);
			else if(IsNullableTimeSpan(item.PropertyType) && rawValue != null)
				this.Value = ((TimeSpan)rawValue).ToString("c", CultureInfo.InvariantCulture);
			else
				this.Value = rawValue;

			DescriptionAttribute dAttr = item.GetCustomAttribute<DescriptionAttribute>();
			if(dAttr != null)
				this.Description = dAttr.Description;

			DefaultValueAttribute vAttr = item.GetCustomAttribute<DefaultValueAttribute>();
			if(vAttr != null)
			{
				Object def = vAttr.Value;
				this.DefaultValue = def is TimeSpan tsDef
					? TypeDescriptor.GetConverter(item.PropertyType).ConvertToInvariantString(tsDef)
					: def;
			}

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