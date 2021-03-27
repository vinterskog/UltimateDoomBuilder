#region ================== Copyright (c) 2021 Boris Iwanski

/*
 * This program is free software: you can redistribute it and/or modify
 *
 * it under the terms of the GNU General Public License as published by
 * 
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 *    but WITHOUT ANY WARRANTY; without even the implied warranty of
 * 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * 
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.If not, see<http://www.gnu.org/licenses/>.
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Map;

#endregion

namespace CodeImp.DoomBuilder.UDBScript.Wrapper
{
	internal abstract class MapElementWrapper
	{
		#region ================== Variables

		private MapElement element;

		#endregion

		#region ================== Properties

		public ExpandoObject fields
		{
			get
			{
				if (element.IsDisposed)
					throw BuilderPlug.Me.ScriptRunner.CreateRuntimeException(element.GetType() + " is disposed, the fields property can not be accessed.");

				dynamic eo = new ExpandoObject();
				IDictionary<string, object> o = eo as IDictionary<string, object>;

				foreach (KeyValuePair<string, UniValue> f in element.Fields)
					o.Add(f.Key, f.Value.Value);

				// Create event that gets called when a property is changed. This sets the flag
				((INotifyPropertyChanged)eo).PropertyChanged += new PropertyChangedEventHandler((sender, ea) => {
					PropertyChangedEventArgs pcea = ea as PropertyChangedEventArgs;
					IDictionary<string, object> so = sender as IDictionary<string, object>;

					// Only allow known flags to be set
					//if (!General.Map.Config.ThingFlags.Keys.Contains(pcea.PropertyName))
					//	throw BuilderPlug.Me.ScriptRunner.CreateRuntimeException("UDMF field name '" + pcea.PropertyName + "' is not valid.");

					// New value must be bool
					//if (!(so[pcea.PropertyName] is bool))
					//	throw BuilderPlug.Me.ScriptRunner.CreateRuntimeException("Flag values must be bool.");

					string pname = pcea.PropertyName;
					object newvalue = null;

					// Types of old and new value must match
					if (element.Fields.ContainsKey(pname))
					{
						Type oldtype = element.Fields[pname].Value.GetType();
						Type newtype = so[pname].GetType();
						object oldvalue = element.Fields[pname].Value;

						if (so[pname] is double && (oldvalue is int) || (oldvalue is double))
						{
							if (oldvalue is int)
							{
								newvalue = Convert.ToInt32((double)so[pname]);
							}
							else if(oldvalue is double)
							{
								newvalue = (double)so[pname];
							}
						}
						else if(so[pname] is string && oldvalue is string)
						{
							newvalue = (string)so[pname];
						}
						else if(so[pname] is UniValue)
						{
							newvalue = ((UniValue)so[pname]).GetValue();
						}
						else
						//if (!oldvalue.GetType().Equals(so[pname].GetType()))
							throw BuilderPlug.Me.ScriptRunner.CreateRuntimeException("UDMF field '" + pcea.PropertyName + "' is of incompatible type for value " + so[pname]);
					}
					else // Property name doesn't exist yet
					{
						//General.map.Config.
					}

					//if (old.Value.GetType() != so[pcea.PropertyName].GetType())
					//	throw BuilderPlug.Me.ScriptRunner.CreateRuntimeException("UDMF field '" + pcea.PropertyName + "' is of type ");

					if(newvalue == null)
						throw BuilderPlug.Me.ScriptRunner.CreateRuntimeException("Something went wront while trying to assing a value to an UDMF field");

					element.Fields.BeforeFieldsChange();
					UniFields.SetFloat(element.Fields, pcea.PropertyName, (double)so[pcea.PropertyName]);

					AfterFieldsUpdate();
				});


				return eo;
			}
		}

		#endregion

		#region ================== Constructors

		internal MapElementWrapper(MapElement element)
		{
			this.element = element;
		}

		#endregion

		#region ================== Methods

		internal abstract void AfterFieldsUpdate();

		internal static object GetVectorFromObject(object data, bool allow3d)
		{
			if (data is Vector2D)
				return (Vector2D)data;
			else if (data.GetType().IsArray)
			{
				object[] vals = (object[])data;

				// Make sure all values in the array are doubles
				foreach (object v in vals)
					if (!(v is double))
						throw new CantConvertToVectorException("Values in array must be numbers.");

				if (vals.Length == 2)
					return new Vector2D((double)vals[0], (double)vals[1]);
				if (vals.Length == 3)
					return new Vector3D((double)vals[0], (double)vals[1], (double)vals[2]);
				else
				{
					if (allow3d)
						throw new CantConvertToVectorException("Array must contain 2 or 3 values.");
					else
						throw new CantConvertToVectorException("Array must contain 2 values.");
				}
			}
			else
			{
				if(allow3d)
					throw new CantConvertToVectorException("Data must be a Vector2D, Vector3D, or an array of numbers.");
				else
					throw new CantConvertToVectorException("Data must be a Vector2D, or an array of numbers.");
			}
		}

		#endregion
	}
}
