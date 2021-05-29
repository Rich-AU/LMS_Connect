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
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace LMS_Connect
{
	public partial class MainPage : ContentPage
	{
		private ObservableCollection<string> Itemplayers = new ObservableCollection<string>();
		List<Player> Players = new List<Player>();
		LMSInfo lmsInfo = new LMSInfo();
		public MainPage()
		{
			InitializeComponent();


			if (Preferences.ContainsKey("players")) {
				txtLMSName.Text = Preferences.Get("LMSName", "");
				txtLMSIP.Text = Preferences.Get("LMSIP", "");
				txtPort.Text = Preferences.Get("port", "9000");
				Players = JsonConvert.DeserializeObject<List<Player>>(Preferences.Get("players", ""));
				lstPlayers.Header = "Players: please tap to select the one for streaming.";
				foreach (Player player in Players)
				{
					Itemplayers.Add(player.name);
				}
				lstPlayers.IsVisible = true;

				this.BindingContext = Itemplayers;
				if (Preferences.ContainsKey("c_player"))
				{
					lstPlayers.Header = "Players: please tap to select the one for streaming. (Current Player:" + Preferences.Get("c_player", "") + ")";
				}
			}
			else
			{
				lblMsg.Text = "Message: Discovering LMS...";
				lblMsg.TextColor = Color.Black;
				lmsInfo.AutoDiscover();
				txtLMSName.Text = lmsInfo.name;
				txtLMSIP.Text = lmsInfo.ip;
				txtPort.Text = lmsInfo.port.ToString();
				this.BindingContext = Itemplayers;
				if (lmsInfo.ip == "")
				{
					lblMsg.Text = "Erro: LMS not found.";
					lblMsg.TextColor = Color.Red;
				}
				else
				{
					Preferences.Set("LMSName", lmsInfo.name);
					_ = GetPlayers();
				}

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

		private async Task GetPlayers()
		{
			lblMsg.Text = "Message: Geeting Players Info...";
			lblMsg.TextColor = Color.Black;
			if (txtLMSIP.Text.Trim().Length == 0 || txtPort.Text.Trim().Length == 0)
			{ lblMsg.Text = "Error:please fill in all LMS server info"; lblMsg.TextColor = Color.Red; }
			else
			{
				Preferences.Set("LMSIP", txtLMSIP.Text.Trim());
				Preferences.Set("port", txtPort.Text.Trim());
				HttpClient client = new HttpClient();
				Uri uri = new Uri(string.Format("http://" + txtLMSIP.Text.Trim() + ":" + txtPort.Text.Trim() + "/jsonrpc.js", string.Empty));

				string json = "{\"id\":1,\"method\":\"slim.request\",\"params\":[\"\",[\"players\",\"0\",\"10\"]]}";
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
						Itemplayers.Clear();
						foreach (JToken result in results)
						{
							// JToken.ToObject is a helper method that uses JsonSerializer internally
							Player player = result.ToObject<Player>();
							Players.Add(player);
							Itemplayers.Add(player.name);
				
						}


						Preferences.Set("players", JsonConvert.SerializeObject(Players));
						
						Preferences.Set("c_player", Players[0].name);
						Preferences.Set("c_id", Players[0].playerid);
						lstPlayers.Header = "Players: please tap to select the one for streaming. (Current Player:" + Players[0].name + ")";
						lstPlayers.IsVisible = true;
						lblMsg.Text = "Message: Done, you can choose anther play or exit now.";
					}
					else
					{
						lblMsg.Text = "Error: No player found";
						lblMsg.TextColor = Color.Red;
					}


				}
				catch (Exception ex)
				{
					lblMsg.Text = "Exception:" + ex.Message;
					lblMsg.TextColor = Color.Red;
				}
			}
		}

		private async void Button_Clicked_1(object sender, EventArgs e)
		{
			await GetPlayers();
		}

		private void swhAutoScan_OnChanged(object sender, ToggledEventArgs e)
		{
			if(swhAutoScan.On )
			{
				if (!Preferences.ContainsKey("LMSName")) //xamarin fire this event when startup, this is prevent this happen every startup.
				{
					txtLMSIP.IsEnabled = false;
					txtPort.IsEnabled = false;
					btnGetPlayers.IsVisible = false;
					lblMsg.Text = "Message: Discovering LMS...";
					lblMsg.TextColor = Color.Black;
					lmsInfo.reset();
					lmsInfo.AutoDiscover();
					txtLMSName.Text = lmsInfo.name;
					txtLMSIP.Text = lmsInfo.ip;
					txtPort.Text = lmsInfo.port.ToString();
					if (lmsInfo.ip == "")
					{
						lblMsg.Text = "Erro: LMS not found.";
						lblMsg.TextColor = Color.Red;
					}
					else
					{
						Preferences.Set("LMSName", lmsInfo.name);
						_ = GetPlayers();
					}
				}


			}
			else
			{
				Preferences.Remove("LMSName");
				txtLMSIP.IsEnabled = true;
				txtPort.IsEnabled = true;
				btnGetPlayers.IsVisible = true;
			}
		}
	}



	public class Player
	{
		public string name { get; set; }
		public string playerid { get; set; }
	} 

	public class LMSInfo
	{
		public string ip { get; set; } = "";
		public string name { get; set; } = "";
		public int port { get; set; } = 0;

		public void reset()
		{
			name = "";
			ip = "";
			port = 0;
		}

		public void AutoDiscover()
		{
			int PORT = 3483;
			UdpClient udpClient = new UdpClient();
			udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, PORT));
			var from = new IPEndPoint(0, 0);
			byte[] data = { 0x65, 0x49, 0x50, 0x41, 0x44, 0x00, 0x4e, 0x41, 0x4d, 0x45, 0x00, 0x4a, 0x53, 0x4f, 0x4e, 0x00 };
			//65 49 50 41 44  00 4e 41 4d 45 00 4a 53 4f 4e 00
			var startTime = DateTime.UtcNow;
			udpClient.Send(data, data.Length, "255.255.255.255", PORT);
			while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(10))
			{
				var recvBuffer = udpClient.Receive(ref from);
				if (recvBuffer[0] == 0x45)
				{
					ip = from.Address.ToString();
					byte JSONSeparator = 0x04;
					int JSONIndex = Array.IndexOf(recvBuffer, JSONSeparator);
					name = System.Text.Encoding.UTF8.GetString(recvBuffer, 6, JSONIndex - 10);
					port = int.Parse(System.Text.Encoding.UTF8.GetString(recvBuffer, JSONIndex + 1, recvBuffer.Length - JSONIndex - 1));
					break;

				}
			}

		}
	}

}
