// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace ICSharpCode.ILSpyX
{
	/// <summary>
	/// Manages ILSpy settings.
	/// </summary>
	public class ILSpySettings
	{
		readonly string fileName;
		readonly XElement root;

		public string FileName => fileName;

		ILSpySettings(string fileName)
		{
			this.root = new XElement("ILSpy");
			this.fileName = fileName;
		}

		ILSpySettings(XElement root, string fileName)
		{
			this.root = root;
			this.fileName = fileName;
		}

		public XElement this[XName section] {
			get {
				return root.Element(section) ?? new XElement(section);
			}
		}

		/// <summary>
		/// Loads the settings file from disk.
		/// </summary>
		/// <returns>
		/// An instance used to access the loaded settings.
		/// </returns>
		public static ILSpySettings Load(string configFile)
		{
			using (new MutexProtector(ConfigFileMutex))
			{
				try
				{
					XDocument doc = LoadWithoutCheckingCharacters(configFile);
					return new ILSpySettings(doc.Root, configFile);
				}
				catch (IOException)
				{
					return new ILSpySettings(configFile);
				}
				catch (XmlException)
				{
					return new ILSpySettings(configFile);
				}
			}
		}

		static XDocument LoadWithoutCheckingCharacters(string fileName)
		{
			return XDocument.Load(fileName, LoadOptions.None);
		}

		/// <summary>
		/// Saves a setting section.
		/// </summary>
		public static void SaveSettings(XElement section, string fileName)
		{
			Update(
				delegate (XElement root) {
					XElement existingElement = root.Element(section.Name);
					if (existingElement != null)
						existingElement.ReplaceWith(section);
					else
						root.Add(section);
				}, fileName);
		}

		/// <summary>
		/// Updates the saved settings.
		/// We always reload the file on updates to ensure we aren't overwriting unrelated changes performed
		/// by another ILSpy instance.
		/// </summary>
		public static void Update(Action<XElement> action, string configFile)
		{
			using (new MutexProtector(ConfigFileMutex))
			{
				XDocument doc;
				try
				{
					doc = LoadWithoutCheckingCharacters(configFile);
				}
				catch (IOException)
				{
					// ensure the directory exists
					Directory.CreateDirectory(Path.GetDirectoryName(configFile));
					doc = new XDocument(new XElement("ILSpy"));
				}
				catch (XmlException)
				{
					doc = new XDocument(new XElement("ILSpy"));
				}
				doc.Root.SetAttributeValue("version", RevisionClass.Major + "." + RevisionClass.Minor + "." + RevisionClass.Build + "." + RevisionClass.Revision);
				action(doc.Root);
				doc.Save(configFile, SaveOptions.None);
			}
		}

		const string ConfigFileMutex = "01A91708-49D1-410D-B8EB-4DE2662B3971";

		/// <summary>
		/// Helper class for serializing access to the config file when multiple ILSpy instances are running.
		/// </summary>
		sealed class MutexProtector : IDisposable
		{
			readonly Mutex mutex;

			public MutexProtector(string name)
			{
				bool createdNew;
				this.mutex = new Mutex(true, name, out createdNew);
				if (!createdNew)
				{
					try
					{
						mutex.WaitOne();
					}
					catch (AbandonedMutexException)
					{
					}
				}
			}

			public void Dispose()
			{
				mutex.ReleaseMutex();
				mutex.Dispose();
			}
		}
	}
}
