using System;
using System.Net.Http;
using System.Text;
using Xamarin.Forms;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Xamarin.Essentials;
using System.Collections.ObjectModel;

namespace LMS_Connect
{
	public partial class MainPage : ContentPage
	{
		private ObservableCollection<string> Itemplayers = new ObservableCollection<string>();
		List<Player> Players = new List<Player>();
		public MainPage()
		{
			InitializeComponent();
			 txtProtocol.Text = Preferences.Get("protocol", "http");
			txtLMSServer.Text = Preferences.Get("LMSServer", "");
			txtPort.Text = Preferences.Get("port", "9000");
			if (Preferences.ContainsKey("players")) {
				Players = JsonConvert.DeserializeObject<List<Player>>(Preferences.Get("players", ""));
				lstPlayers.Header = "Players: please tap to select the one for streaming.";
				foreach (Player player in Players)
				{
					Itemplayers.Add(player.name);
				}
				lstPlayers.IsVisible = true;
			}
			this.BindingContext = Itemplayers;
			if (Preferences.ContainsKey("c_player"))
			{
				lstPlayers.Header = "Players: please tap to select the one for streaming. (Current Player:" + Preferences.Get("c_player", "") + ")";
			}

		}
		void OnItemTapped(object sender, ItemTappedEventArgs e)
		{
			if (e == null) return; // has been set to null, do not 'process' tapped event
			Preferences.Set("c_player", e.Item.ToString());
			Player currentPlayer = Players.Find(x => x.name == e.Item.ToString());
			Preferences.Set("c_id", currentPlayer.playerid);
			Debug.WriteLine(currentPlayer.playerid);
			((ListView)sender).SelectedItem = null; // de-select the row
			lstPlayers.Header = "Players: please tap to select the one for streaming. (Current Player:"+ e.Item+")";
		}



		private async void Button_Clicked_1(object sender, EventArgs e)
		{
			lblMsg.Text = "Message:";
			if (txtProtocol.Text.Trim().Length==0 || txtLMSServer.Text.Trim().Length == 0 || txtPort.Text.Trim().Length == 0) 
			{ lblMsg.Text = "Error:please fill in all LMS server info";lblMsg.TextColor = Color.Red; }
			else
			{
				Preferences.Set("protocol", txtProtocol.Text.Trim());
				Preferences.Set("LMSServer", txtLMSServer.Text.Trim());
				Preferences.Set("port", txtPort.Text.Trim());

				HttpClient client = new HttpClient();
				Uri uri = new Uri(string.Format(txtProtocol.Text.Trim()+"://"+ txtLMSServer.Text.Trim()+":"+ txtPort.Text.Trim()+"/jsonrpc.js", string.Empty));

				string json = "{\"id\":1,\"method\":\"slim.request\",\"params\":[\"\",[\"players\",\"0\",\"5\"]]}";
				StringContent content = new StringContent(json);

				HttpResponseMessage response = null;
				try
				{
					response = await client.PostAsync(uri, content);
					var contents = await response.Content.ReadAsStringAsync();
					Debug.WriteLine(response);
					JObject ObjPlayers = JObject.Parse(contents);
					dynamic res = Newtonsoft.Json.JsonConvert.DeserializeObject(contents);

					// get JSON result objects into a list
					IList<JToken> results = ObjPlayers["result"]["players_loop"].Children().ToList();

					// serialize JSON results into .NET objects
					if (results.Count > 0)
					{
						Players.Clear();
						lstPlayers.Header = "Players: please tap to select the one for streaming.";
						Itemplayers.Clear();
						foreach (JToken result in results)
						{
							// JToken.ToObject is a helper method that uses JsonSerializer internally
							Player player = result.ToObject<Player>();
							Players.Add(player);
							Itemplayers.Add(player.name);
						}

						lstPlayers.IsVisible = true;
						Preferences.Set("players", JsonConvert.SerializeObject(Players));
					}
					else
					{
						lblMsg.Text = "Error: No player found"; 
						lblMsg.TextColor = Color.Red;
					}


				}
				catch (Exception ex)
				{
					lblMsg.Text = "Exception:" +ex.Message;
					lblMsg.TextColor = Color.Red;
				}
			}
		}
			
	}

	public class Player
	{
		public string name { get; set; }
		public string playerid { get; set; }
	}

}
