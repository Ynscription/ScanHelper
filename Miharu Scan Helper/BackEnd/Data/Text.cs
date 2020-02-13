﻿
using Miharu.BackEnd.Translation;
using Miharu.Properties;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Xml.Linq;

namespace Miharu.BackEnd.Data
{
	[JsonObject(MemberSerialization.OptOut)]
	public class Text
    {
		
		public event TxtEventHandler TextChanged;

		[JsonIgnore]
		private readonly static string TEMP_IMG = @"./tmp.png";
		[JsonIgnore]
		private readonly static string TEMP_TXT = @"./tmp.hocr";

		[JsonIgnore]
		private bool _parseInvalidated = true;


		public void Invalidate () {
			_parseInvalidated = true;
		}

		private bool _vertical;
		public bool Vertical {
			get => _vertical;
			set {
				_vertical = value;
				TextChanged?.Invoke(this, new TxtChangedEventArgs(TextChangeType.Vertical, null, null));
			}
		}
		[JsonIgnoreAttribute]
		public Bitmap Source { get; private set; }
		public Rect Rectangle { get; private set; }

		private string _parsedText = null;
		public string ParsedText {
			get {
				if (_parsedText == null || _parseInvalidated) {
					_parsedText = ParseText();
					_parseInvalidated = false;
					TextChanged?.Invoke(this, new TxtChangedEventArgs(TextChangeType.Parse, null, _parsedText));
				}
				return _parsedText;
			}
			set {
				_parsedText = value;
				TextChanged?.Invoke(this, new TxtChangedEventArgs(TextChangeType.Parse, null, _parsedText));
			}
		}

		[JsonProperty]
		private volatile Dictionary<TranslationType, string> _translations;
		public string GetTranslation (TranslationType type) {
			string res = null;
			if (!_translations.TryGetValue(type, out res))
				return null;
			return res;
		}
		public void SetTranslation (TranslationType type, string value) {
			Monitor.Enter(_translations);
			try {
				_translations[type] = value;
			}
			finally {
				Monitor.Exit(_translations);
			}
			TextChanged?.Invoke(this, new TxtChangedEventArgs(TextChangeType.TranslationSource, type, value));
		}

	
		

		private string _translatedText;
		public string TranslatedText {
			get => _translatedText;
			set {
				_translatedText = value;
				TextChanged?.Invoke(this, new TxtChangedEventArgs(TextChangeType.Translation, null, value));
			}
		}

		public Text (Bitmap src, Rect rect) {
			if (rect .Width == 0 || rect.Height == 0)
				throw new ArgumentOutOfRangeException("rect", rect, "Can't create text entry with 0 width or 0 height rectangle.");
			Source = src;
			Rectangle = rect;
			TranslatedText = "";
			Vertical = src.Height >= src.Width;
			_translations = new Dictionary<TranslationType, string>();
		}

		
		
		//There are legacy parameters, so loading old saves still works
		[JsonConstructor]
		public Text (Rect rectangle, bool vertical, bool parseInvalidated,
					string parsedText, Dictionary<TranslationType, string> translations, 
					string googleTranslatedText, string bingTranslatedText,
					string translatedText) {
			Rectangle = rectangle;
			Vertical = vertical;
			_parseInvalidated = parseInvalidated;
			ParsedText = parsedText;
			if (translations != null)
				_translations = translations;
			else {
				_translations = new Dictionary<TranslationType, string>();
				_translations[TranslationType.Google_API] = googleTranslatedText;
				_translations[TranslationType.Bing_API] = bingTranslatedText;
			}
			TranslatedText = translatedText;			
		}
		
		[JsonIgnore]
		private static readonly string _START_BODY = "<body>";
		[JsonIgnore]
		private static readonly string _END_BODY = "</body>";
		private string ReadHOCR (string hocrPath) {
			string res = "";

			using (StreamReader reader = new StreamReader (hocrPath)) {
				string body = reader.ReadToEnd();
				reader.Close();
				int bodyStartIndex = body.IndexOf(_START_BODY);
				int bodyEndIndex = body.IndexOf(_END_BODY);
				body = body.Substring(bodyStartIndex, bodyEndIndex - bodyStartIndex + _END_BODY.Length);
				XElement XMLBody = XElement.Parse(body);
				IEnumerable<XElement> paragraphs = XMLBody.Descendants("p");
				foreach (XElement p in paragraphs) {
					foreach (XElement line in p.Elements("span")) {
						foreach (XElement word in line.Elements("span"))
							res += word.Value;
					}
					if (p != paragraphs.Last())
						res += Environment.NewLine;
				}
			}

			return res;
		}
		

		private string ParseText () {
			if (Source == null)
				return "";

			Source.Save(TEMP_IMG, ImageFormat.Png);

			using (Process pProcess = new Process()) {
				pProcess.StartInfo.FileName = (string)Settings.Default["TesseractPath"];
				string lang = Vertical ? "jpn_vert" : "jpn";
				int psm = Vertical ? 5 : 6;
				pProcess.StartInfo.Arguments = TEMP_IMG + " tmp -l " + lang + " --psm " + psm + " hocr"; //argument
				pProcess.StartInfo.UseShellExecute = false;
				//pProcess.StartInfo.RedirectStandardOutput = true;
				pProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
				pProcess.StartInfo.CreateNoWindow = true; //not diplay a windows
				pProcess.Start();
				pProcess.WaitForExit();
			}
			
			return ReadHOCR (TEMP_TXT);
		}

		
		public void Load (Bitmap src) {
			Source = src;
		}




	}
}
