using System;
using sd = System.Drawing;
using swf = System.Windows.Forms;
using Eto.Drawing;
using Eto.Forms;
using Eto.Platform.Windows.Drawing;

namespace Eto.Platform.Windows
{
	public interface IWindowsControl
	{
		bool InternalVisible { get; }

		swf.DockStyle DockStyle { get; }

		swf.Control ContainerControl { get; }

		Size ParentMinimumSize { get; set; }

		Size GetPreferredSize(Size availableSize);

		bool SetMinimumSize(bool force = false);

		void SetScale(bool xscale, bool yscale);

		bool ShouldCaptureMouse { get; }

		bool XScale { get; }

		bool YScale { get; }
	}

	public static class WindowsControlExtensions
	{
		public static IWindowsControl GetWindowsHandler(this Control control)
		{
			if (control == null)
				return null;

			var handler = control.Handler as IWindowsControl;
			if (handler != null)
				return handler;

			var controlObject = control.ControlObject as Control;
			return controlObject != null ? controlObject.GetWindowsHandler() : null;

		}

		public static Size GetPreferredSize(this Control control)
		{
			var handler = control.GetWindowsHandler();
			return handler != null ? handler.GetPreferredSize(Size.Empty) : Size.Empty;
		}

		public static swf.Control GetContainerControl(this Control control)
		{
			if (control == null)
				return null;

			var handler = control.Handler as IWindowsControl;
			if (handler != null)
				return handler.ContainerControl;

			var controlObject = control.ControlObject as Control;
			if (controlObject != null)
				return controlObject.GetContainerControl();

			return control.ControlObject as swf.Control;
		}

		public static void SetScale(this Control control, bool xscale, bool yscale)
		{
			var handler = control.GetWindowsHandler();

			if (handler != null)
				handler.SetScale(xscale, yscale);
		}
	}

	public abstract class WindowsControl<TControl, TWidget> : WidgetHandler<TControl, TWidget>, IControl, IWindowsControl
		where TControl : swf.Control
		where TWidget : Control
	{
		bool internalVisible = true;
		Font font;
		Cursor cursor;
		string tooltip;
		Size desiredSize = new Size(-1, -1);
		Size parentMinimumSize;

		public bool XScale { get; set; }

		public bool YScale { get; set; }

		public virtual Size? DefaultSize { get { return null; } }

		public virtual Size GetPreferredSize(Size availableSize)
		{
			var size = UserDesiredSize;
			var defSize = DefaultSize;
			if (defSize != null)
			{
				if (size.Width == -1) size.Width = defSize.Value.Width;
				if (size.Height == -1) size.Height = defSize.Value.Height;
			}

			return Size.Max(parentMinimumSize, size);
		}

		public Size UserDesiredSize
		{
			get
			{
				return desiredSize;
			}
		}

		public virtual Size ParentMinimumSize
		{
			get { return parentMinimumSize; }
			set
			{
				parentMinimumSize = value;
				SetMinimumSize();
			}
		}

		public virtual bool ShouldCaptureMouse
		{
			get { return false; }
		}

		public virtual swf.Control ContainerControl
		{
			get { return Control; }
		}

		protected override void Initialize()
		{
			base.Initialize();
			Control.TabIndex = 100;
			XScale = true;
			YScale = true;
			Control.Margin = swf.Padding.Empty;
			Control.Tag = this;
		}

		public virtual swf.DockStyle DockStyle
		{
			get { return swf.DockStyle.Fill; }
		}

		public bool SetMinimumSize(bool force = false)
		{
			if (!force && !Widget.Loaded)
				return false;
			var size = GetPreferredSize(Size.Empty);
			if (XScale) size.Width = 0;
			if (YScale) size.Height = 0;
			return SetMinimumSize(size);
		}

		protected virtual bool SetMinimumSize(Size size)
		{
			var sdsize = size.ToSD();
			if (ContainerControl.MinimumSize != sdsize)
			{
				ContainerControl.MinimumSize = sdsize;
				//Debug.Print(string.Format("Min Size: {0}, Type:{1}", sdsize, Widget));
				return true;
			}
			return false;
		}

		public virtual void SetScale(bool xscale, bool yscale)
		{
			XScale = xscale;
			YScale = yscale;
			SetMinimumSize();
		}

		public void SetScale()
		{
			SetScale(XScale, YScale);
		}

		public override void AttachEvent(string id)
		{
			switch (id)
			{
				case Eto.Forms.Control.KeyDownEvent:
					Control.KeyDown += Control_KeyDown;
					Control.KeyPress += Control_KeyPress;
					break;
				case Eto.Forms.Control.KeyUpEvent:
					Control.KeyUp += Control_KeyUp;
					break;
				case TextControl.TextChangedEvent:
					Control.TextChanged += Control_TextChanged;
					break;
				case Eto.Forms.Control.SizeChangedEvent:
					Control.SizeChanged += Control_SizeChanged;
					break;
				case Eto.Forms.Control.MouseDoubleClickEvent:
					Control.MouseDoubleClick += HandleDoubleClick;
					break;
				case Eto.Forms.Control.MouseEnterEvent:
					Control.MouseEnter += HandleControlMouseEnter;
					break;
				case Eto.Forms.Control.MouseLeaveEvent:
					Control.MouseLeave += HandleControlMouseLeave;
					break;
				case Eto.Forms.Control.MouseDownEvent:
					Control.MouseDown += HandleMouseDown;
					if (ShouldCaptureMouse)
						HandleEvent(Eto.Forms.Control.MouseUpEvent);
					break;
				case Eto.Forms.Control.MouseUpEvent:
					Control.MouseUp += HandleMouseUp;
					if (ShouldCaptureMouse)
						HandleEvent(Eto.Forms.Control.MouseDownEvent);
					break;
				case Eto.Forms.Control.MouseMoveEvent:
					Control.MouseMove += HandleMouseMove;
					break;
				case Eto.Forms.Control.MouseWheelEvent:
					Control.MouseWheel += HandleMouseWheel;
					break;
				case Eto.Forms.Control.GotFocusEvent:
					Control.GotFocus += delegate
					{
						Widget.OnGotFocus(EventArgs.Empty);
					};
					break;
				case Eto.Forms.Control.LostFocusEvent:
					Control.LostFocus += delegate
					{
						Widget.OnLostFocus(EventArgs.Empty);
					};
					break;
				default:
					base.AttachEvent(id);
					break;
			}
		}

		void HandleMouseWheel(object sender, swf.MouseEventArgs e)
		{
			if (!ApplicationHandler.BubbleMouseEvents)
				Widget.OnMouseWheel(e.ToEto(Control));
		}

		void HandleControlMouseLeave(object sender, EventArgs e)
		{
			Widget.OnMouseLeave(new MouseEventArgs(MouseButtons.None, swf.Control.ModifierKeys.ToEto(), swf.Control.MousePosition.ToEto()));
		}

		void HandleControlMouseEnter(object sender, EventArgs e)
		{
			Widget.OnMouseEnter(new MouseEventArgs(MouseButtons.None, swf.Control.ModifierKeys.ToEto(), swf.Control.MousePosition.ToEto()));
		}

		void HandleDoubleClick(object sender, swf.MouseEventArgs e)
		{
			if (!ApplicationHandler.BubbleMouseEvents)
			{
				var ee = e.ToEto(Control);
				Widget.OnMouseDoubleClick(ee);
				if (!ee.Handled)
					Widget.OnMouseDown(ee);
			}
		}

		void HandleMouseUp(Object sender, swf.MouseEventArgs e)
		{
			if (!ApplicationHandler.BubbleMouseEvents)
			{
				if (ShouldCaptureMouse)
					Control.Capture = false;
				Widget.OnMouseUp(e.ToEto(Control));
			}
		}

		void HandleMouseMove(Object sender, swf.MouseEventArgs e)
		{
			if (!ApplicationHandler.BubbleMouseEvents)
				Widget.OnMouseMove(e.ToEto(Control));
		}

		void HandleMouseDown(object sender, swf.MouseEventArgs e)
		{
			if (!ApplicationHandler.BubbleMouseEvents)
			{
				Widget.OnMouseDown(e.ToEto(Control));
				if (ShouldCaptureMouse)
					Control.Capture = true;
			}
		}

		public virtual string Text
		{
			get { return Control.Text; }
			set { Control.Text = value; SetMinimumSize(); }
		}

		public virtual Size Size
		{
			get { return ContainerControl.Size.ToEto(); }
			set
			{
				desiredSize = value;
				ContainerControl.AutoSize = value.Width == -1 || value.Height == -1;
				var minset = SetMinimumSize();
				ContainerControl.Size = value.ToSD();
				if (minset && ContainerControl.IsHandleCreated)
				{
					var parent = Widget.Parent.GetWindowsHandler();
					if (parent != null)
						parent.SetMinimumSize();
				}
			}
		}

		public virtual Size ClientSize
		{
			get { return Control.ClientSize.ToEto(); }
			set
			{
				Control.AutoSize = value.Width == -1 || value.Height == -1;
				Control.ClientSize = value.ToSD();
			}
		}

		public bool Enabled
		{
			get { return Control.Enabled; }
			set { Control.Enabled = value; }
		}

		public Cursor Cursor
		{
			get { return cursor; }
			set
			{
				cursor = value;
				Control.Cursor = cursor != null ? cursor.ControlObject as swf.Cursor : null;
			}
		}

		public string ToolTip
		{
			get { return tooltip; }
			set
			{
				tooltip = value;
				SetToolTip();
			}
		}

		public virtual void Invalidate()
		{
			Control.Invalidate(true);
		}

		public virtual void Invalidate(Rectangle rect)
		{
			Control.Invalidate(rect.ToSD(), true);
		}

		public virtual Color BackgroundColor
		{
			get { return Control.BackColor.ToEto(); }
			set { Control.BackColor = value.ToSD(); }
		}

		public virtual void SuspendLayout()
		{
			Control.SuspendLayout();
		}

		public virtual void ResumeLayout()
		{
			Control.ResumeLayout();
		}

		public void Focus()
		{
			if (Control.IsHandleCreated)
			{
				if (!Control.Visible)
					Control.TabIndex = 0;
				Control.Focus();
			}
			else
				Control.HandleCreated += Control_HandleCreated;
		}

		void Control_HandleCreated(object sender, EventArgs e)
		{
			Control.HandleCreated -= Control_HandleCreated;
			Control.Focus();
		}

		public bool HasFocus
		{
			get { return Control.Focused; }
		}

		bool IWindowsControl.InternalVisible
		{
			get { return internalVisible; }
		}

		public bool Visible
		{
			get { return ContainerControl.Visible; }
			set
			{
				internalVisible = value;
				ContainerControl.Visible = value;
			}
		}

		public virtual void SetParent(Container parent)
		{
		}

		void Control_SizeChanged(object sender, EventArgs e)
		{
			Widget.OnSizeChanged(e);
		}

		public virtual void OnPreLoad(EventArgs e)
		{
		}

		public virtual void OnLoad(EventArgs e)
		{
			SetMinimumSize(true);
		}

		public virtual void OnLoadComplete(EventArgs e)
		{
			SetToolTip();
		}

		public virtual void OnUnLoad(EventArgs e)
		{
		}

		void SetToolTip()
		{
			if (Widget.ParentWindow != null)
			{
				var parent = Widget.ParentWindow.Handler as IWindowHandler;
				if (parent != null)
					parent.ToolTips.SetToolTip(Control, tooltip);
			}
		}

		Keys key;
		bool handled;
		char keyChar;
		bool charPressed;
		public Keys? LastKeyDown { get; set; }

		void Control_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			charPressed = false;
			handled = true;
			key = e.KeyData.ToEto();

			if (key != Keys.None && LastKeyDown != key)
			{
				var kpea = new KeyEventArgs(key, KeyEventType.KeyDown);
				Widget.OnKeyDown(kpea);
				e.SuppressKeyPress = kpea.Handled;
				handled = kpea.Handled;
			}
			else
				handled = false;

			if (!handled && charPressed)
			{
				// this is when something in the event causes messages to be processed for some reason (e.g. show dialog box)
				// we want the char event to come after the dialog is closed, and handled is set to true!
				var kpea = new KeyEventArgs(key, KeyEventType.KeyDown, keyChar);
				Widget.OnKeyDown(kpea);
				e.SuppressKeyPress = kpea.Handled;
			}

			LastKeyDown = null;
		}

		void Control_KeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
			charPressed = true;
			keyChar = e.KeyChar;
			if (!handled)
			{
				var kpea = new KeyEventArgs(key, KeyEventType.KeyDown, keyChar);
				Widget.OnKeyDown(kpea);
				e.Handled = kpea.Handled;
			}
			else
				e.Handled = true;
		}


		void Control_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			key = e.KeyData.ToEto();

			var kpea = new KeyEventArgs(key, KeyEventType.KeyUp);
			Widget.OnKeyUp(kpea);
			e.Handled = kpea.Handled;
		}

		void Control_TextChanged(object sender, EventArgs e)
		{
			var widget = Widget as TextControl;
			if (widget != null)
				widget.OnTextChanged(e);
		}

		public Font Font
		{
			get
			{
				if (font == null)
					font = new Font(Widget.Generator, new FontHandler(Control.Font));
				return font;
			}
			set
			{
				font = value;
				Control.Font = font != null ? font.ControlObject as sd.Font : null;
			}
		}

		public virtual void MapPlatformAction(string systemAction, BaseAction action)
		{
		}

		public virtual PointF PointFromScreen(PointF point)
		{
			return Control.PointToClient(point.ToSDPoint()).ToEto();
		}

		public virtual PointF PointToScreen(PointF point)
		{
			return Control.PointToScreen(point.ToSDPoint()).ToEto();
		}

		public Point Location
		{
			get { return Control.Location.ToEto(); }
		}
	}
}
