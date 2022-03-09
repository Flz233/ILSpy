using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ICSharpCode.ILSpyX;

namespace ICSharpCode.ILSpy.Options
{
	public class DecompilationOptionsFactory
	{
		public static DecompilationOptions Create()
		{
			return Create(MainWindow.Instance.CurrentLanguageVersion, DecompilerSettingsPanel.CurrentDecompilerSettings, DisplaySettingsPanel.CurrentDisplaySettings);
		}

		public static DecompilationOptions Create(LanguageVersion version)
		{
			return Create(version, DecompilerSettingsPanel.CurrentDecompilerSettings, DisplaySettingsPanel.CurrentDisplaySettings);
		}

		public static DecompilationOptions Create(LanguageVersion version, Decompiler.DecompilerSettings settings, Options.DisplaySettings displaySettings)
		{
			if (!Enum.TryParse(version?.Version, out Decompiler.CSharp.LanguageVersion languageVersion))
				languageVersion = Decompiler.CSharp.LanguageVersion.Latest;
			var newSettings = settings.Clone();
			newSettings.SetLanguageVersion(languageVersion);
			newSettings.ExpandMemberDefinitions = displaySettings.ExpandMemberDefinitions;
			newSettings.ExpandUsingDeclarations = displaySettings.ExpandUsingDeclarations;
			newSettings.FoldBraces = displaySettings.FoldBraces;
			newSettings.ShowDebugInfo = displaySettings.ShowDebugInfo;
			newSettings.CSharpFormattingOptions.IndentationString = GetIndentationString(displaySettings);
			return new(newSettings);
		}

		private static string GetIndentationString(DisplaySettings displaySettings)
		{
			if (displaySettings.IndentationUseTabs)
			{
				int numberOfTabs = displaySettings.IndentationSize / displaySettings.IndentationTabSize;
				int numberOfSpaces = displaySettings.IndentationSize % displaySettings.IndentationTabSize;
				return new string('\t', numberOfTabs) + new string(' ', numberOfSpaces);
			}
			return new string(' ', displaySettings.IndentationSize);
		}
	}
}
