﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Manga_Scan_Helper.BackEnd.Translation.HTTPTranslators
{
	class HTTPBingTranslator : HTTPTranslator
	{
		private const string _URL = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0&from=ja&to=en";
		

		public override TranslationType Type{
			get { return TranslationType.Bing_API; }
		}

		protected override string GetUri(string text)
		{
			throw new NotImplementedException();
		}

		protected override string ProcessResponse(string response)
		{
			throw new NotImplementedException();
		}

		public override string Translate(string text)
		{
			string result = "";
			object [] body = new object [] { new { Text = text} };
			string requestBody = JsonConvert.SerializeObject(body);

			
			using (HttpClient client = new HttpClient())
			using (HttpRequestMessage request = new HttpRequestMessage()) {
				request.Method = HttpMethod.Post;
				request.RequestUri = new Uri(_URL);
				request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
				request.Headers.Add("Ocp-Apim-Subscription-Key", _A);

				HttpResponseMessage response = client.SendAsync(request).Result;

				if (response.StatusCode == HttpStatusCode.OK) {
					result = response.Content.ReadAsStringAsync().Result;
					string find = "\"text\":";
					if (result.Contains(find)){
						result = result.Substring(result.IndexOf(find) + find.Length);
						result = result.Substring(result.IndexOf("\"") + 1);
						result = result.Substring(0, result.IndexOf("\",\""));
						if (result.Contains("\\u"))
							result = DecodeEncodedUnicodeCharacters(result);
					}
					else {
						throw new Exception("Bad response format");
					}
				}
				else {
					throw new Exception("HTTP bad response (" + response.StatusCode.ToString() + ")");
				}
			}
			
			return result;
		}
	}
}
