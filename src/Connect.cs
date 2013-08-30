// vim: set ts=4 sw=4 tw=99 et:
//
// VSModelines
// Copyright (C) 2013 David Anderson
// Copyright (C) 2007-2008 Daniel Remenak

// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.Collections.Generic;
using System.Text;

namespace VSModelines
{
    using System;
    using Extensibility;
    using System.Runtime.InteropServices;
    using EnvDTE;

    [GuidAttribute("F37E635A-3B49-441C-A5B3-3111B3CF2A26"), ProgId("VSModelines.Connect")]
    public class Connect : Extensibility.IDTExtensibility2
    {
        private EnvDTE.WindowEvents events_;
        private _DTE appobj_;
        private AddIn addin_;
        private _dispWindowEvents_WindowActivatedEventHandler handler_;

        // Settings that should probably be configurable somehow.
        private const int kMaxLines = 5;
        private const int kDefaultTabSize = 4;
        private const int kDefaultIndentSize = 4;
        private const bool kDefaultHardTabs = true;

        class Settings
        {
            public int? ts;
            public int? sw;
            public bool? et;
        }

        public Connect()
        {
        }

        public void OnConnection(object app, Extensibility.ext_ConnectMode mode, object addin, ref System.Array custom)
        {
            appobj_ = (_DTE)app;
            addin_ = (AddIn)addin;

            EnvDTE.Events events = appobj_.Events;
            events_ = (EnvDTE.WindowEvents)events.get_WindowEvents();
            handler_ = new _dispWindowEvents_WindowActivatedEventHandler(this.OnWindowActivated);
            events_.WindowActivated += handler_;
        }

        public void OnDisconnection(Extensibility.ext_DisconnectMode mode, ref System.Array custom)
        {
            events_.WindowActivated -= handler_;
        }

        public void OnAddInsUpdate(ref System.Array custom)
        {
        }

        public void OnStartupComplete(ref System.Array custom)
        {
        }

        public void OnBeginShutdown(ref System.Array custom)
        {
        }

        public void OnWindowActivated(EnvDTE.Window gotFocus, EnvDTE.Window lostFocus)
        {
            if (gotFocus.Document == null || gotFocus.Document.Type != "Text")
                return;

            TextDocument td = gotFocus.Document.Object() as TextDocument;
            if (td == null)
                return;

            Settings settings = new Settings();

            ReadModelines(td, settings);

            if (settings.et == null)
            {
                // If the language is C++, use the default tab setting (hard tabs), otherwise,
				// VS seems to like soft tabs for other languages.
                if (gotFocus.Document.Language == CodeModelLanguageConstants.vsCMLanguageVC)
                    settings.et = kDefaultHardTabs;
                else
                    settings.et = false;
            }

            if (settings.ts == null)
                settings.ts = kDefaultTabSize;

			// In Vim and Visual Studio, the following settings are roughly analogous:
            //    ts and TabSize
            //    sw and IndentSize
            //    noet/et and InsertTabs
            //
            // In both editors, |ts| controls how many spaces a hard tab is displayed as.
            //
            // In Vim, pressing the tab key will insert |sts| spaces if |et| is set, or a single
            // hard tab if |noet| is set. In Visual Studio, pressing the tab key will perform
            // an indentation.
            //
            // When either editor indents, the behavior is controlled InsertTabs/noet/et. If hard
            // tabs are disabled, then sw/IndentSize spaces are inserted. If hard tabs are
            // enabled, then N hard tabs are inserted, where:
            //    N = (IndentLevel * IndentSize) / TabSize
            // Spaces are inserted for the remaining width not satisfied by hard tabs.
            //
            // Since Vim does not perform indentation when pressing tab, the functionality will
            // differ if the effective tab width is different from |sw|. This also means "sts"
            // can be ignored, since there is no analog.
            //
            // [1] http://social.msdn.microsoft.com/Forums/vstudio/en-US/34cd09e5-c7b5-41cb-bee9-47bcce194260/cannot-get-the-tab-size-to-be-different-than-the-indent-size

            SetDTEProperty("TextEditor", gotFocus.Document.Language, "TabSize", settings.ts);
            SetDTEProperty("TextEditor", gotFocus.Document.Language, "IndentSize", settings.sw);
			SetDTEProperty("TextEditor", gotFocus.Document.Language, "InsertTabs", !settings.et);
        }

        private void ReadModelines(TextDocument td, Settings settings)
        {
            int totalLines = td.EndPoint.Line - td.StartPoint.Line + 1;
            int readLines = totalLines > kMaxLines ? kMaxLines : totalLines;

            EditPoint ep = td.StartPoint.CreateEditPoint();
            string text = ep.GetLines(1, readLines);

            string[] lines = text.Split(new[] { '\r', '\n' });
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                DetectVimModeline(settings, line);
            }
        }

        private bool DetectVimModeline(Settings settings, string line)
        {
            int index = line.IndexOf("vim: set");
            if (index == -1)
                return false;

            // Strings are trimmed before we get here.
            if (!line.EndsWith(":"))
                return false;

            int? maybe = GetKeyValue(line, "ts=");
            if (maybe != null)
                settings.ts = maybe;

            maybe = GetKeyValue(line, "sw=");
            if (maybe != null)
                settings.sw = maybe;

            if (line.IndexOf(" noet") != -1)
                settings.et = false;
            else if (line.IndexOf(" et") != -1)
                settings.et = true;

            return true;
        }

        private int? GetKeyValue(string line, string key)
        {
            int index = line.IndexOf(key);
            if (index == -1)
                return null;

            int nchars = 0;
            for (int i = index + key.Length; i < line.Length; i++)
            {
				if (!Char.IsNumber(line[i]))
					break;
				nchars++;
            }

            int result = 0;
            string chars = line.Substring(index + key.Length, nchars);
            if (!Int32.TryParse(chars, out result))
                return null;

            return result;
        }

        private void SetDTEProperty(string category, string page, string item, object value)
        {
            EnvDTE.Properties props = appobj_.get_Properties(category, page);
            EnvDTE.Property prop = props.Item(item);
            prop.Value = value;
        }
    }
}
