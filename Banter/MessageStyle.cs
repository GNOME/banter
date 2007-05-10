//***********************************************************************
// *  $RCSfile$ - MessageStyle.cs
// *
// *  Copyright (C) 2007 Novell, Inc.
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
using System.IO;
using System.Reflection;
using System.Xml;

namespace Banter
{
	public enum MessageStyleBackgroundType
	{
		Normal,
		Center,
		Tile
	}
	
	// <summary>
	// This represents an Adium Message Style
	// </summary>
	public class MessageStyle
	{
		public const string SERVICE_KEYWORD = "%service%";
		public const string USER_ICON_PATH_KEYWORD = "%userIconPath%";
		public const string MESSAGE_KEYWORD = "%message%";
		public const string SENDER_KEYWORD = "%sender%";
		public const string TIME_KEYWORD = "%time%";
		public const string TIME_OPENED_KEYWORD = "%timeOpened{%B %e %Y}%";
		
//		int styleVersion;
		string stylePath;
		
		// Templates
		string headerHtml;
		string footerHtml;
		string templateHtml;
//		string templateHtmlPath;
		string contentInHtml;
		string nextContentInHtml;
		string contextInHtml;
		string nextContextInHtml;
		string contentOutHtml;
		string nextContentOutHtml;
		string contextOutHtml;
		string nextContextOutHtml;
		string statusHtml;
		
		// Style settings
//		bool allowsCustomBackground;
//		bool transparentDefaultBackground;
//		bool allowsUserIcons;
//		bool usingCustomTemplateHtml;
		
		// Behavior
//		bool useCustomNameFormat;
		bool showUserIcons;
//		bool showHeader;
//		bool combineConsecutive;
//		bool allowTextBackgrounds;
//		bool showIncomingFonts;
//		bool showIncomingColors;
//		MessageStyleBackgroundType customBackgroundType;
//		string customBackgroundPath;
//		string customBackgroundColor;
//		//image userIconMask;
		
		// Icon path caches
		//Dictionary statusIconPathCache;
		
		string name;
		string description;
		string identifier;
		string defaultBackgroundColor;
		string defaultFontFamily;
		int defaultFontSize;
		bool disableCustomBackground;
		string displayNameForNoVariant;
		
		List<string> variants;
		
		static string defaultTemplateHtml = String.Empty;
		
		static MessageStyle ()
		{
			// Load in the Template HTML
			Assembly asm = Assembly.GetExecutingAssembly ();
//			Stream resource = asm.GetManifestResourceStream ("MessageViewBase.html");
			Stream resource = asm.GetManifestResourceStream ("Template.html");
			if (resource != null) {
				defaultTemplateHtml = LoadHtmlFromStream (resource, false);
			}
		}

		// <summary>
		// Loads and validates the MessageStyle.  Throws an exception if
		// anything is found bad.
		// </summary>
		private MessageStyle(string stylePath)
		{
			this.stylePath = stylePath;
			
			LoadStyle ();
			
//			Logger.Debug ("Loaded MessageStyle: {0}", ToString ());
		}
		
		private void LoadStyle ()
		{
			string infoPlistPath = System.IO.Path.Combine (stylePath, "Contents/Info.plist");
			XmlDocument doc = new XmlDocument ();
			doc.Load (infoPlistPath);
			
			variants = LoadVariants ();

			CreateTemplateHtmlFiles ();

			name = GetPlistStringValue (doc, "CFBundleName");
			description = GetPlistStringValue (doc, "CFBundleGetInfoString");
			identifier = GetPlistStringValue (doc, "CFBundleIdentifier");
			defaultBackgroundColor = GetPlistStringValue (doc, "DefaultBackgroundColor");
			defaultFontFamily = GetPlistStringValue (doc, "DefaultFontFamily");
			defaultFontSize = GetPlistIntValue (doc, "DefaultFontSize");
			disableCustomBackground = GetPlistBoolValue (doc, "DisableCustomBackground");
			showUserIcons = GetPlistBoolValue (doc, "ShowUserIcons");
			displayNameForNoVariant = GetPlistStringValue (doc, "DisplayNameForNoVariant");
			
			headerHtml = LoadHtmlFromTemplate ("Contents/Resources/Header.html", true);
			footerHtml = LoadHtmlFromTemplate ("Contents/Resources/Footer.html", true);
			templateHtml = LoadHtmlFromTemplate ("Contents/Resources/Template.html", false);
			contentInHtml = LoadHtmlFromTemplate ("Contents/Resources/Incoming/Content.html", true);
			nextContentInHtml = LoadHtmlFromTemplate ("Contents/Resources/Incoming/NextContent.html", true);
			contextInHtml = LoadHtmlFromTemplate ("Contents/Resources/Incoming/Context.html", true);
			nextContextInHtml = LoadHtmlFromTemplate ("Contents/Resources/Incoming/NextContext.html", true);
			contentOutHtml = LoadHtmlFromTemplate ("Contents/Resources/Outgoing/Content.html", true);
			nextContentOutHtml = LoadHtmlFromTemplate ("Contents/Resources/Outgoing/NextContent.html", true);
			contextOutHtml = LoadHtmlFromTemplate ("Contents/Resources/Outgoing/Context.html", true);
			nextContextOutHtml = LoadHtmlFromTemplate ("Contents/Resources/Outgoing/NextContext.html", true);
			statusHtml = LoadHtmlFromTemplate ("Contents/Resources/Status.html", true);
		}
		
		// <summary>
		// Create a Template.html for the default and all variants.
		// </summary>
		void CreateTemplateHtmlFiles ()
		{
			// Create the Template.html file
			string html = TemplateHtml;
			
			string defaultTemplatePath = GetTemplateHtmlPath ();
			// Replace the <base> tag
			html = Utilities.ReplaceString (
						html, 
						"<base href=\"%@\">",
						string.Format ("<base href=\"file://{0}\">", defaultTemplatePath));
						
			// Create a default Template.html file.
			string defaultHtml = Utilities.ReplaceString (
						html,
							"@import url( \"%@\" );",
							"@import url( \"main.css\" );");
			SaveTemplateFile (defaultHtml, defaultTemplatePath);
			
			// Loop through the variants and create template files for each one.
			foreach (string variant in variants) {
				// Loop through the variants and create each file here
				string variantCssFile = string.Format (
						"@import url( \"Variants/{0}.css\" );",
						variant);
				// Replace the css path
				string variantHtml = Utilities.ReplaceString (
							html,
							"@import url( \"%@\" );",
							variantCssFile);
				SaveTemplateFile (variantHtml, GetTemplateHtmlPath (variant));
			}
		}
		
		static void SaveTemplateFile (string html, string filePath)
		{
			try {
				if (File.Exists (filePath))
					File.Delete (filePath);
				
				StreamWriter sw = new StreamWriter (File.OpenWrite (filePath));
				sw.Write (html);
				sw.Close ();
			} catch {}
		}
		
		List<string> LoadVariants ()
		{
			variants = new List<string> ();
			
			string variantDirectoryPath = Path.Combine (stylePath, "Contents/Resources/Variants");
			DirectoryInfo di = new DirectoryInfo (variantDirectoryPath);
			foreach (FileInfo fi in di.GetFiles ("*.css")) {
				// Strip off the ".css" portion
				string name = fi.Name;
				int dotCssPos = name.IndexOf (".css");
				if (dotCssPos <= 0)
					continue;
				name = name.Substring (0, dotCssPos);
				variants.Add (name);
			}
			
			return variants;
		}
		
		string GetPlistStringValue (XmlDocument doc, string keyName)
		{
			XmlNode node = GetPlistValueNode (doc, keyName, "string");
			if (node != null)
				return node.InnerText;
			
			return null;
		}
		
		int GetPlistIntValue (XmlDocument doc, string keyName)
		{
			int val = 0;
			XmlNode node = GetPlistValueNode (doc, keyName, "integer");
			if (node != null)
				val = Int32.Parse (node.InnerText);
			
			return val;
		}
		
		bool GetPlistBoolValue (XmlDocument doc, string keyName)
		{
			bool val = false;
			XmlNode node = GetPlistValueNode (doc, keyName, "true");
			if (node != null)
				val = true;
			
			return val;
		}
		
		XmlNode GetPlistValueNode (XmlDocument doc, string keyName, string valueType)
		{
			XmlNode node = null;
			
			string xPathExpression = string.Format (
					"//{0}[preceding-sibling::key[.='{1}']]",
					valueType,
					keyName);
			
			node = doc.SelectSingleNode (xPathExpression);
			
			return node;
		}
		
		string LoadHtmlFromTemplate (string templatePath, bool suppressNewlines)
		{
			string filePath = Path.Combine (stylePath, templatePath);
			return LoadHtmlFromFile (filePath, suppressNewlines);
		}
		
		static string LoadHtmlFromFile (string filePath, bool suppressNewlines)
		{
			if (!File.Exists (filePath))
				return null;
			
			string buffer = String.Empty;
			try {
				FileStream fileStream = File.OpenRead (filePath);
				buffer = LoadHtmlFromStream (fileStream, suppressNewlines);
				fileStream.Close ();
			} catch {}
			
			if (buffer != null && buffer != String.Empty)
				return buffer;
			
			return null;
		}
		
		static string LoadHtmlFromStream (Stream stream, bool suppressNewlines)
		{
			string buffer = String.Empty;
			try {
				StreamReader sr = new StreamReader (stream);
				string s = sr.ReadLine ();
				while (s != null) {
					buffer += s;
					if (!suppressNewlines)
						buffer += "\n";
					s = sr.ReadLine ();
				}
				sr.Close ();
			} catch {
				// Ignore any errors
			}
			
			if (buffer != String.Empty)
				return buffer;
			
			return null;
		}
		
		
		
#region Public Methods
		public static MessageStyle CreateFromPath (string stylePath)
		{
			MessageStyle style = null;
			
			if (!Directory.Exists (stylePath))
				throw new System.IO.FileNotFoundException (
						"The Adium Message Style directory does not exist",
						stylePath);
			
			style = new MessageStyle (stylePath);
			
			return style;
		}
		
		public override string ToString ()
		{
			string variantsStr = String.Empty;
			foreach (string variant in variants) {
				if (variantsStr.Length > 0)
					variantsStr += ", ";
				variantsStr += variant;
			}
			string val = string.Format (
					"{0}\n" +
					"\tName: {1}\n" +
					"\tDescription: {2}\n" +
					"\tIdentifier: {3}\n" +
					"\tDefault Background Color: {4}\n" +
					"\tDefault Font Family: {5}\n" +
					"\tDefault Font Size: {6}\n" +
					"\tDisable Custom Background: {7}\n" +
					"\tShow User Icons: {8}\n" +
					"\tDisplay Name For No Variant: {9}\n" +
					"\tVariants: {10}\n",
					stylePath,
					name == null ? "<Unknown>" : name,
					description == null ? "<Unknown>" : description,
					identifier == null ? "<Unknown>" : identifier,
					defaultBackgroundColor == null ? "<Unknown>" : defaultBackgroundColor,
					defaultFontFamily == null ? "<Unknown>" : defaultFontFamily,
					defaultFontSize,
					disableCustomBackground,
					showUserIcons,
					displayNameForNoVariant == null ? "<Unknown>" : displayNameForNoVariant,
					variantsStr);
			
			return val;
		}
		
		// <summary>
		// Returns the path to the default Template.html (one that does not us
		// any variant.  Note: does not validate that the file exists.
		// </summary>
		public string GetTemplateHtmlPath ()
		{
			return Path.Combine (stylePath,
					"Contents/Resources/Template.html");
		}
		
		// <summary>
		// Returns the path to a variant Template-<Variant>.html.  Note: does
		// not validate that the file exists.
		// </summary>
		public string GetTemplateHtmlPath (string variantName)
		{
			if (variantName == null || variantName.Trim () == String.Empty)
				return GetTemplateHtmlPath ();
			
			return Path.Combine (
					stylePath,
					string.Format (
						"Contents/Resources/Template{0}.html",
						variantName));
		}

#endregion
		
#region Public Properties
		public string Name
		{
			get { return name; }
		}
		
		public string StylePath
		{
			get { return stylePath; }
		}
		
		public string HeaderHtml
		{
			get { return headerHtml; }
		}
		
		public string FooterHtml
		{
			get { return footerHtml; }
		}
		
		public string TemplateHtml
		{
			get {
				if (templateHtml == null || templateHtml == String.Empty)
					return defaultTemplateHtml;
				
				return templateHtml;
			}
		}
		
//		public string TemplateHtmlPath
//		{
//			get { return Path.Combine (stylePath, "Contents/Resources/Template.html"); }
//		}
		
		public string ContentInHtml
		{
			get { return contentInHtml; }
		}
		
		public string NextContentInHtml
		{
			get { return nextContentInHtml; }
		}
		
		public string ContextInHtml
		{
			get { return contextInHtml; }
		}
		
		public string NextContextInHtml
		{
			get { return nextContextInHtml; }
		}
		
		public string ContentOutHtml
		{
			get { return contentOutHtml; }
		}
		
		public string NextContentOutHtml
		{
			get { return nextContentOutHtml; }
		}
		
		public string ContextOutHtml
		{
			get { return contextOutHtml; }
		}
		
		public string NextContextOutHtml
		{
			get { return nextContextOutHtml; }
		}
		
		public string StatusHtml
		{
			get { return statusHtml; }
		}
		
		public List<string> Variants
		{
			get { return variants; }
		}
#endregion
	}
}
