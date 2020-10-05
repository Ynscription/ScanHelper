﻿
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;

namespace Miharu.BackEnd.Data {
	[JsonObject(MemberSerialization.OptOut)]
	public class Page
    {
		[JsonIgnoreAttribute]
		private volatile bool _ready = false;
		[JsonIgnoreAttribute]
		public bool Ready {
			get => _ready;
			private set => _ready = value;
		}
		[JsonIgnoreAttribute]
		public EventWaitHandle PageWaitHandle {
			get;
			private set;
		}


		public event EventHandler PageChanged;

		[JsonIgnore]
		public string Name {
			get {
				int lastSlash = Path.LastIndexOf('\\') + 1;
				return Path.Substring(lastSlash);
			}
		}

		public string Path {
			get; private set;
		}
		public void MakePathRelative (Uri relativeTo) {
			Uri pageUri = new Uri(Path);
			Path = Uri.UnescapeDataString(relativeTo.MakeRelativeUri(pageUri).ToString().Replace("/", "\\"));
		}
		public void MakePathAbsolute (string absoluteFrom) {
			string res = System.IO.Path.Combine(absoluteFrom, Path);
			if (File.Exists(res))
				Path = res;
			if (!File.Exists(Path))
				throw new IOException("Couldn't find file: " + Path);
		}
		

		[JsonIgnoreAttribute]
		public Bitmap Source {
			get; private set;
		}


		[JsonIgnoreAttribute]
		private double ScreenDPIX {
			get; set;
		}
		[JsonIgnoreAttribute]
		private double ScreenDPIY {
			get; set;
		}


		public List<Text> TextEntries {
			get; private set;
		}

		

		public Page (string src) {
			Path = src;
			TextEntries = new List<Text>();
			PageWaitHandle = new ManualResetEvent(false);
			Graphics g = Graphics.FromHwnd(IntPtr.Zero);
			ScreenDPIX = g.DpiX;
			ScreenDPIY = g.DpiY;
		}

		[JsonConstructor]
		public Page (string path, List<Text> textEntries) {
			Path = path;
			TextEntries = textEntries;
			PageWaitHandle = new ManualResetEvent(false);
			Graphics g = Graphics.FromHwnd(IntPtr.Zero);
			ScreenDPIX = g.DpiX;
			ScreenDPIY = g.DpiY;
		}

		

		public Text AddTextEntry (DPIAwareRectangle rect) {
			
			Text txt = new Text(CropImage(rect), rect);
			TextEntries.Add(txt);
			PageChanged?.Invoke(this, new EventArgs());
			return txt;
		}

		public void MoveTextEntry (int index, int newIndex) {
			Text tmp = TextEntries [index];
			TextEntries.RemoveAt(index);
			TextEntries.Insert(newIndex, tmp);
			PageChanged?.Invoke(this, new EventArgs());
		}

		public void RemoveTextEntry (int index) {
			TextEntries.RemoveAt(index);
			PageChanged?.Invoke(this, new EventArgs());
		}

		private Bitmap CropImage (DPIAwareRectangle DPIrect) {
			if (DPIrect.Width == 0 || DPIrect.Height == 0)
				return null;


			double xdpiRatio = DPIrect.DpiX / ScreenDPIX;
			double ydpiRatio = DPIrect.DpiY / ScreenDPIY;
			Rectangle rect = new Rectangle((int)(DPIrect.X*xdpiRatio), (int)(DPIrect.Y*ydpiRatio), (int)(DPIrect.Width*xdpiRatio), (int)(DPIrect.Height*ydpiRatio));
			
			//Rectangle rect = new Rectangle((int)(DPIrect.X), (int)(DPIrect.Y), (int)(DPIrect.Width), (int)(DPIrect.Height));
			
						Bitmap cropped = new Bitmap((int) rect.Width, (int) rect.Height);
			Graphics g = Graphics.FromImage(cropped);
			g.DrawImage(Source, new Rectangle(0, 0, (int) rect.Width, (int) rect.Height),
						rect,
						GraphicsUnit.Pixel);
			return cropped;
		}

		

		public void Load () {
			Source = new Bitmap(Path);
			foreach (Text t in TextEntries)
				t.Load(CropImage(t.DpiAwareRectangle));
			Ready = true;
			PageWaitHandle.Set();
		}

		public void ExportScript (StreamWriter writer) {
			foreach (Text t in TextEntries)
				writer.WriteLine(t.TranslatedText + Environment.NewLine);
		}

		public void ExportCustomScript (StreamWriter writer, ExportData ed) {
			foreach (Text t in TextEntries) {
				if (ed.HasFlag(ExportData.Japanese))
					writer.WriteLine (t.ParsedText + Environment.NewLine);
				if (ed.HasFlag(ExportData.Notes)) {
					foreach (Note n in t.NotesEnumerator)
						writer.WriteLine("Note: " + n.Content);
					writer.WriteLine();
				}
				if (ed.HasFlag(ExportData.Translation))
					writer.WriteLine(t.TranslatedText + Environment.NewLine);
			}
		}

		public override string ToString() {
			return Name;
		}


	}
}
