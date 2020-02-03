﻿using Manga_Scan_Helper.BackEnd.Translation;
using System;

namespace Manga_Scan_Helper.BackEnd
{

	public delegate void TxtEventHandler (object sender, TxtChangedEventArgs e);
	public enum TextChangeType {
		Vertical,
		Parse,
		TranslationSource,
		Translation
	}
	public class TxtChangedEventArgs : EventArgs
	{
		
		public TextChangeType ChangeType {
			get;
		}

		public TranslationType? TranslationType {
			get;
		}

		public string Text {
			get;
		}

		public TxtChangedEventArgs (TextChangeType textChangeType, TranslationType? translationType, string text) {
			ChangeType = textChangeType;
			TranslationType = translationType;
			Text = text;
		}


	}
}
