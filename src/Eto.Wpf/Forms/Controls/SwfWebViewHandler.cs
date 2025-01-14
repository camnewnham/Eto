using Eto.CustomControls;
namespace Eto.Wpf.Forms.Controls
{
	public class SwfWebViewHandler : WpfFrameworkElement<swf.Integration.WindowsFormsHost, WebView, WebView.ICallback>, WebView.IHandler
	{
		public swf.WebBrowser Browser { get; private set; }

		[ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		[Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
		internal interface IServiceProvider
		{
			[return: MarshalAs(UnmanagedType.IUnknown)]
			object QueryService(ref Guid guidService, ref Guid riid);
		}

		HashSet<string> delayedEvents = new HashSet<string>();

		SHDocVw.WebBrowser_V1 WebBrowserV1
		{
			get { return (SHDocVw.WebBrowser_V1)Browser.ActiveXInstance; }
		}

		protected override sw.Size DefaultSize => new sw.Size(100, 100);

		public void AttachEvent(SHDocVw.WebBrowser_V1 control, string handler)
		{
			switch (handler)
			{
				case WebView.OpenNewWindowEvent:
					control.NewWindow += WebBrowserV1_NewWindow;
					break;
			}
		}

		static readonly string[] ValidInputTags = { "input", "textarea" };

		public SwfWebViewHandler()
		{
			Browser = new swf.WebBrowser
			{
				IsWebBrowserContextMenuEnabled = false,
				WebBrowserShortcutsEnabled = false,
				AllowWebBrowserDrop = false,
				ScriptErrorsSuppressed = true
			};
			Browser.HandleCreated += (sender, e) => HookDocumentEvents();

			Browser.ObjectForScripting = new ScriptingObject(this);

			Control = new swf.Integration.WindowsFormsHost
			{
				Child = Browser
			};

			Browser.PreviewKeyDown += (sender, e) =>
			{
				switch (e.KeyCode)
				{
					case swf.Keys.Down:
					case swf.Keys.Up:
					case swf.Keys.Left:
					case swf.Keys.Right:
					case swf.Keys.PageDown:
					case swf.Keys.PageUp:
						// enable scrolling via keyboard
						e.IsInputKey = true;
						return;
				}

				var doc = Browser.Document;
				if (!Browser.WebBrowserShortcutsEnabled && doc != null)
				{
					// implement shortcut keys for copy/paste
					switch (e.KeyData)
					{
						case (swf.Keys.C | swf.Keys.Control):
							doc.ExecCommand("Copy", false, null);
							break;
						case (swf.Keys.V | swf.Keys.Control):
							if (doc.ActiveElement != null && ValidInputTags.Contains(doc.ActiveElement.TagName.ToLowerInvariant()))
								doc.ExecCommand("Paste", false, null);
							break;
						case (swf.Keys.X | swf.Keys.Control):
							if (doc.ActiveElement != null && ValidInputTags.Contains(doc.ActiveElement.TagName.ToLowerInvariant()))
								doc.ExecCommand("Cut", false, null);
							break;
						case (swf.Keys.A | swf.Keys.Control):
							doc.ExecCommand("SelectAll", false, null);
							break;
					}
				}
			};
		}

		void WebBrowserV1_NewWindow(string URL, int Flags, string TargetFrameName, ref object PostData, string Headers, ref bool Processed)
		{
			var e = new WebViewNewWindowEventArgs(new Uri(URL), TargetFrameName);
			Callback.OnOpenNewWindow(Widget, e);
			Processed = e.Cancel;
		}

		public override void AttachEvent(string handler)
		{
			switch (handler)
			{
				case WebView.NavigatedEvent:
					this.Browser.Navigated += (sender, e) =>
					{
						Callback.OnNavigated(Widget, new WebViewLoadedEventArgs(e.Url));
					};
					break;
				case WebView.DocumentLoadedEvent:
					this.Browser.DocumentCompleted += (sender, e) =>
					{
						var args = new WebViewLoadedEventArgs(e.Url);
						Callback.OnDocumentLoaded(Widget, args);
					};
					break;
				case WebView.DocumentLoadingEvent:
					this.Browser.Navigating += (sender, e) =>
					{
						var args = new WebViewLoadingEventArgs(e.Url, false);
						Callback.OnDocumentLoading(Widget, args);
						e.Cancel = args.Cancel;
					};
					break;
				case WebView.OpenNewWindowEvent:
					HookDocumentEvents(handler);
					break;
				case WebView.DocumentTitleChangedEvent:
					this.Browser.DocumentTitleChanged += delegate
					{
						Callback.OnDocumentTitleChanged(Widget, new WebViewTitleEventArgs(Browser.DocumentTitle));
					};
					break;
				case WebView.MessageReceivedEvent:
					this.Browser.DocumentCompleted += OnDocumentLoadedInjectScript;
					break;
				default:
					base.AttachEvent(handler);
					break;
			}

		}

		void HookDocumentEvents(string newEvent = null)
		{
			if (newEvent != null)
				delayedEvents.Add(newEvent);
			if (Browser.ActiveXInstance != null)
			{
				foreach (var handler in delayedEvents)
					AttachEvent(WebBrowserV1, handler);
				delayedEvents.Clear();
			}
		}

		public Uri Url
		{
			get { return this.Browser.Url; }
			set { this.Browser.Url = value; }
		}

		public string DocumentTitle
		{
			get
			{
				return this.Browser.DocumentTitle;
			}
		}

		/// <summary>
		/// Object exposed to JavaScript via window.external
		/// </summary>
		[ComVisible(true)]
		public class ScriptingObject
		{
			private readonly SwfWebViewHandler Owner;
			public ScriptingObject(SwfWebViewHandler owner)
			{
				Owner = owner;
			}

			/// <summary>
			/// Called from JS via window.eto.postMessage (window.external.postMessage)
			/// </summary>
			public void postMessage(string message)
			{
				Owner.Callback.OnMessageReceived(Owner.Widget, new WebViewMessageEventArgs(message));

			}
		}

		/// <summary>
		/// Called on document load.
		/// Wraps calls from window.external.postMessage to window.eto.postMessage
		/// </summary>
		private void OnDocumentLoadedInjectScript(object sender, EventArgs args)
		{
			ExecuteScript(@"window.eto = { postMessage: function(message) { window.external.postMessage(message); } };");
		}

		public string ExecuteScript(string script)
		{
			var fullScript = string.Format("var _fn = function() {{ {0} }}; _fn();", script);
			var result = Browser.Document.InvokeScript("eval", new object[] { fullScript });
			return Convert.ToString(result);
		}

		public Task<string> ExecuteScriptAsync(string script) => Task.FromResult(ExecuteScript(script));

		public void Stop()
		{
			this.Browser.Stop();
		}

		public void Reload()
		{
			this.Browser.Refresh();
		}

		public void GoBack()
		{
			this.Browser.GoBack();
		}

		public bool CanGoBack
		{
			get
			{
				return this.Browser.CanGoBack;
			}
		}

		public void GoForward()
		{
			this.Browser.GoForward();
		}

		public bool CanGoForward
		{
			get
			{
				return this.Browser.CanGoForward;
			}
		}

		HttpServer server;

		public void LoadHtml(string html, Uri baseUri)
		{
			if (baseUri != null)
			{
				if (server == null)
					server = new HttpServer();
				server.SetHtml(html, baseUri != null ? baseUri.LocalPath : null);
				Browser.Navigate(server.Url);
			}
			else
				this.Browser.DocumentText = html;

		}

		public void ShowPrintDialog()
		{
			this.Browser.ShowPrintDialog();
		}

		public bool BrowserContextMenuEnabled
		{
			get { return Browser.IsWebBrowserContextMenuEnabled; }
			set { Browser.IsWebBrowserContextMenuEnabled = value; }
		}

		public override Eto.Drawing.Color BackgroundColor
		{
			get { return Eto.Drawing.Colors.Transparent; }
			set { }
		}
	}
}
