using Eto.Drawing;
using Eto.Forms;
using System;

namespace WebViewMessageTest
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			Title = "My Eto Form";
			MinimumSize = new Size(200, 200);

			var webview = new Eto.Forms.WebView()
			{
				Url = new Uri("https://www.w3schools.com/tags/att_a_target.asp"),
				Width = 900,
				Height = 600,
			};
			webview.MessageReceived += OnMessageReceived;

			Content = new StackLayout
			{
				Padding = 10,
				Items =
				{
					"Hello World!",
					// add more controls here
					webview
				}
			};

			// create a few commands that can be used for the menu and toolbar
			var postMessage = new Command { MenuText = "Run Script", ToolBarText = "Click Me!" };
			postMessage.Executed += (sender, e) =>
			{
				webview.ExecuteScript("window.eto.postMessage('Hello from Eto!');");
			};

			var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
			quitCommand.Executed += (sender, e) => Application.Instance.Quit();

			var aboutCommand = new Command { MenuText = "About..." };
			aboutCommand.Executed += (sender, e) => new AboutDialog().ShowDialog(this);

			// create menu
			Menu = new MenuBar
			{
				Items =
				{
					// File submenu
					new SubMenuItem { Text = "&File", Items = { postMessage } },
					// new SubMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
					// new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
				},
				ApplicationItems =
				{
					// application (OS X) or file menu (others)
					new ButtonMenuItem { Text = "&Preferences..." },
				},
				QuitItem = quitCommand,
				AboutItem = aboutCommand
			};

			// create toolbar			
			ToolBar = new ToolBar { Items = { postMessage } };
		}

		private void OnMessageReceived(object sender, WebViewMessageEventArgs e)
		{
			Eto.Forms.Application.Instance.Invoke(() =>
			{
				new Eto.Forms.Dialog()
				{
					Title = "Message Received",
					Content = new Label()
					{
						Text = e.Message
					}
				}.ShowModalAsync(this);
			});
		}
	}
}
