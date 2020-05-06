﻿using System.Drawing;
using System.IO;

namespace TombLib.Scripting.TextEditors.Configuration.Colors
{
	public sealed class ClassicScriptColors : ConfigurationBase
	{
		public override string DefaultPath { get; }

		public string Sections { get; set; } = ColorTranslator.ToHtml(Color.SteelBlue);
		public string Values { get; set; } = ColorTranslator.ToHtml(Color.LightSalmon);
		public string References { get; set; } = ColorTranslator.ToHtml(Color.Orchid);
		public string StandardCommands { get; set; } = ColorTranslator.ToHtml(Color.MediumAquamarine);
		public string NewCommands { get; set; } = ColorTranslator.ToHtml(Color.SpringGreen);
		public string Comments { get; set; } = ColorTranslator.ToHtml(Color.Green);
		public string UnknownCommands { get; set; } = ColorTranslator.ToHtml(Color.Red);

        public ClassicScriptColors()
        {
            DefaultPath = Path.Combine(DefaultPaths.GetTextEditorColorConfigsPath(), "ClassicScript.xml");
        }
	}
}