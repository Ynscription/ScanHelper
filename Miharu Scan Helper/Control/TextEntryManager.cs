﻿using Miharu.BackEnd.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Miharu.Control
{
	public class TextEntryManager
	{

		public event EventHandler TextChanged;
		public event EventHandler TextIndexChanged;

		private PageManager pageManager;


		private Text _currentText = null;
		private Text CurrentText {
			get => _currentText;
			set {
				_currentText = value;
				TextChanged?.Invoke(this, new EventArgs());
			}
		}

		private int _currentTextIndex = -1;
		public int CurrentTextIndex {
			get => _currentTextIndex;
			set {
				_currentTextIndex = value;
				TextIndexChanged?.Invoke(this, new EventArgs());
			}
		}


		public TextEntryManager(PageManager pageManager)
		{
			this.pageManager = pageManager;
		}

		internal void SelectTextEntry(Text entry, int index)
		{
			CurrentText = entry;
			CurrentTextIndex = index;
		}

		internal void Unload()
		{
			CurrentText = null;
			CurrentTextIndex = 0;
		}
	}
}