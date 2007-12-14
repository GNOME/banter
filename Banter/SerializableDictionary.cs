//***********************************************************************
// *  $RCSfile$ - SerializableDictionary.cs
// *
// *  Copyright (C) 2007 Kevin Kubasik <kevin@kubasik.net>
// *
// *  This program is free software; you can redistribute it and/or
// *  modify it under the terms of the GNU General Public
// *  License as published by the Free Software Foundation; either
// *  version 2 of the License, or (at your option) any later version.
// *
// *  This program is distributed in the hope that it will be useful,
// *  but WITHOUT ANY WARRANTY; without even the implied warranty of
// *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// *  General Public License for more details.
// *
// *  You should have received a copy of the GNU General Public
// *  License along with this program; if not, write to the Free
// *  Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
// *
// **********************************************************************

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Banter{

	[XmlRoot("dictionary")]
	public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
	{
	    #region IXmlSerializable Members
	    public System.Xml.Schema.XmlSchema GetSchema()
	    {
	        return null;
	    }


	    public void ReadXml(System.Xml.XmlReader reader)
	    {
	        XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
	        XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));


	        bool wasEmpty = reader.IsEmptyElement;
	        reader.Read();


	        if (wasEmpty)
	            return;


	        while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
	        {
	            reader.ReadStartElement("item");

	            reader.ReadStartElement("key");
	            TKey key = (TKey)keySerializer.Deserialize(reader);
	            reader.ReadEndElement();



	            reader.ReadStartElement("value");
	            TValue value = (TValue)valueSerializer.Deserialize(reader);
	            reader.ReadEndElement();

	            this.Add(key, value);

	            reader.ReadEndElement();
	            reader.MoveToContent();

	        }

	        reader.ReadEndElement();

	    }



	    public void WriteXml(System.Xml.XmlWriter writer)
	    {

	        XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
	        XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));


	        foreach (TKey key in this.Keys)
	        {

	            writer.WriteStartElement("item");

	            writer.WriteStartElement("key");
	            keySerializer.Serialize(writer, key);
	            writer.WriteEndElement();

	            writer.WriteStartElement("value");
	            TValue value = this[key];
	            valueSerializer.Serialize(writer, value);
	            writer.WriteEndElement();


	            writer.WriteEndElement();

	        }

	    }

	    #endregion
	}
}
