using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Content;
using System.Net.Http;
using Xamarin.Essentials;

namespace LMS_Connect.Droid
{
    [Activity(Label = "LMS_Connect", Name = "com.companyname.lms_connect.MainActivity", Icon = "@mipmap/play", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            TabLayoutResource = Resource.Layout.Tabbar;
            ToolbarResource = Resource.Layout.Toolbar;

            base.OnCreate(savedInstanceState);

            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());

            if (Intent.ActionSend.Equals(Intent.Action) && Intent.Type != null && "text/plain".Equals(Intent.Type))
            {
                handleIntent();
            }
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        public virtual void handleIntent()
		{
            handleSendUrl();

        }
        public void handleSendUrl(bool IsQueue =false)
        {
            if (!Preferences.ContainsKey("protocol") || !Preferences.ContainsKey("LMSServer") || !Preferences.ContainsKey("port") || !Preferences.ContainsKey("c_id"))
            {
                FinishAndRemoveTask();
                FinishAffinity();
            }
            var DefinedProtocol = Preferences.Get("protocol", "http");
            var DefinedLMSServer = Preferences.Get("LMSServer", "");
            var DefinedPort = Preferences.Get("port", "9000");
            var DefinedPlayerid = Preferences.Get("c_id", "");



            var url = Intent.GetStringExtra(Intent.ExtraText);
            var ServiceProvider = "";
            StringComparison comp = StringComparison.OrdinalIgnoreCase;
            if (url.Contains("tidal.com", comp))
            { ServiceProvider = "Tidal"; } 
            else if(url.Contains("Qobuz.com", comp)) ServiceProvider = "Qobuz";
            if (ServiceProvider == "")
			{
                FinishAndRemoveTask();
                FinishAffinity();
            }
			else
			{
                var DomainIndex = url.IndexOf(".com/")+5;
                var info = url.Substring(DomainIndex).Split("/");
                string LMSAction = IsQueue ? "add" : "play";
                if (info.Length>=2)
				{
                    url = "Service Provider:" + ServiceProvider + " Type:" + info[0] + " ID:" + info[1];


                    //string playerid = "b8:27:eb:8f:24:4b";
                    string cmdPara="";

                    if (ServiceProvider == "Tidal" )                       
					{

                        if (info[0].Equals("track", StringComparison.OrdinalIgnoreCase))
                        {
                                cmdPara = "\"playlist\",\""+LMSAction+"\",\"wimp://" + info[1] + ".flac\"";
                            }
                        else if (info[0].Equals("album", StringComparison.OrdinalIgnoreCase))
                        {
                            cmdPara = "\"playlist\",\"" + LMSAction + "\",\"wimp://album:" + info[1] + ".tdl\"";
                        }
                        else if (info[0].Equals("playlist", StringComparison.OrdinalIgnoreCase))
                        {
                            cmdPara = "\"playlist\",\"" + LMSAction + "\",\"wimp://playlist:" + info[1] + ".tdl\"";
                        }

                    }
                    else
					{
                        if (info[0].Equals("track", StringComparison.OrdinalIgnoreCase))
                        {
                            cmdPara = "\"playlist\",\"" + LMSAction + "\",\"qobuz://" + info[1] + ".flac\"";
                        }
                        else if (info[0].Equals("album", StringComparison.OrdinalIgnoreCase))
                        {
                            cmdPara = "\"playlist\",\"" + LMSAction + "\",\"qobuz://album:" + info[1] + ".qbz\"";
                        }
                        else if (info[0].Equals("playlist", StringComparison.OrdinalIgnoreCase))
                        {
                            cmdPara = "\"playlist\",\"" + LMSAction + "\",\"qobuz://playlist:" + info[1] + ".qbz\"";
                        }
                    }
                    if (cmdPara !="")SendLMSRequest(DefinedProtocol, DefinedLMSServer, DefinedPort, DefinedPlayerid, cmdPara);

                }
				else
				{
                    url = "Service Provider:" + ServiceProvider + " format error:" + url.Substring(DomainIndex).Split("/");

                }

               


                   FinishAndRemoveTask();
                   FinishAffinity();

                //var urlTextView = new TextView(this) { Gravity = GravityFlags.Center };
                //urlTextView.Text = url;
                //view.AddView(urlTextView);

                //var description = new EditText(this) { Gravity = GravityFlags.Top };
                //view.AddView(description);

                //new AlertDialog.Builder(this)
                //    .SetTitle("Save a URL Link")
                //    .SetMessage("Type a description for your link")
                //    .SetView(view)
                //    .SetPositiveButton("Add", (dialog, whichButton) =>
                //    {
                //        var desc = description.Text;
                //    //Save off the url and description here

                //    //Remove dialog and navigate back to app or browser that shared the link
                //    FinishAndRemoveTask();
                //        FinishAffinity();
                //    })
                //    .Show();
            }

        }

        private async void SendLMSRequest(string Protocol, string LMSServer, string port, string PlayerID, string CmdPara)
		{
            HttpClient client = new HttpClient();
            Uri uri = new Uri(string.Format(Protocol+"://"+ LMSServer+":"+ port + "/jsonrpc.js", string.Empty));

            string LMSRequest = "{\"id\":1,\"method\":\"slim.request\",\"params\":[\""+ PlayerID + "\",["+CmdPara+ "]]}";
            StringContent content = new StringContent(LMSRequest);
			try
			{
				HttpResponseMessage response = await client.PostAsync(uri, content);
			}
            catch (Exception ex)
            {  }
        }
    }

    [Activity(Label = "LMS_Connect", Name = "com.companyname.lms_connect.QueueActivity")]
    public class QueueActivity: MainActivity
	{
        public override void handleIntent()
        {
            handleSendUrl(true);

        }
    }

}