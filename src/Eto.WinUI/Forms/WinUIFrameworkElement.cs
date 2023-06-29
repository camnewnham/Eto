using Eto.Drawing;
using Eto.Forms;
namespace Eto.WinUI.Forms
{
	public abstract partial class WinUIFrameworkElement<TControl, TWidget, TCallback> : WidgetHandler<TControl, TWidget, TCallback>, Control.IHandler
		where TControl : class
		where TWidget : Control
		where TCallback : Control.ICallback
	{

		public abstract mux.FrameworkElement ContainerControl { get; }
		public abstract mux.FrameworkElement FocusControl { get; }

		public Color BackgroundColor { get; set; }
		public Size Size
		{
			get => new Size(Width, Height);
			set
			{
				Width = value.Width;
				Height = value.Height;
			}
		}

		public int Width
		{
			get => (int)ContainerControl.Width; 
			set => ContainerControl.Width = value;
		}
		public int Height
		{
			get => (int)ContainerControl.Height;
			set => ContainerControl.Height = value;
		}
		public virtual bool Enabled { get; set; }
		public virtual bool HasFocus => ContainerControl.FocusState != mux.FocusState.Unfocused;
		public virtual bool Visible
		{
			get => ContainerControl.Visibility == mux.Visibility.Visible;
			set => ContainerControl.Visibility = value ? mux.Visibility.Visible : mux.Visibility.Collapsed;
		}
		public IEnumerable<string> SupportedPlatformCommands { get; }
		public Point Location { get; }
		public string ToolTip { get; set; }
		public Cursor Cursor { get; set; }
		public int TabIndex { get; set; }
		public virtual IEnumerable<Control> VisualControls { get; }
		public virtual bool AllowDrop
		{
			get => ContainerControl.AllowDrop;
			set => ContainerControl.AllowDrop = value;
		}

		public virtual void DoDragDrop(DataObject data, DragEffects allowedEffects, Image image, PointF cursorOffset)
		{
			throw new NotImplementedException();
		}

		public virtual void Focus()
		{
			ContainerControl.Focus(mux.FocusState.Programmatic);
		}

		public Window GetNativeParentWindow()
		{
			throw new NotImplementedException();
		}

		public virtual SizeF GetPreferredSize(SizeF availableSize)
		{
			throw new NotImplementedException();
		}

		public virtual void Invalidate(bool invalidateChildren)
		{
		}

		public virtual void Invalidate(Rectangle rect, bool invalidateChildren)
		{
		}

		public virtual void MapPlatformCommand(string systemCommand, Command command)
		{
		}

		public virtual void OnLoad(EventArgs e)
		{
		}

		public virtual void OnLoadComplete(EventArgs e)
		{
		}

		public virtual void OnPreLoad(EventArgs e)
		{
		}

		public virtual void OnUnLoad(EventArgs e)
		{
		}

		public virtual PointF PointFromScreen(PointF point)
		{
			throw new NotImplementedException();
		}

		public virtual PointF PointToScreen(PointF point)
		{
			throw new NotImplementedException();
			//ContainerControl.TransformToVisual()
		}

		public virtual void ResumeLayout()
		{
		}

		public virtual void SetParent(Container oldParent, Container newParent)
		{
		}

		public virtual void SuspendLayout()
		{
		}

		public virtual void UpdateLayout() => ContainerControl.InvalidateMeasure();
	}
}
