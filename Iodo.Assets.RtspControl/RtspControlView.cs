#define DEBUG
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Iodo.Assets.RtspControl.Entities;
using Iodo.Assets.RtspControl.Entities.Decoded;
using Iodo.Assets.RtspControl.Models;

namespace Iodo.Assets.RtspControl;

public partial class RtspControlView : UserControl, IComponentConnector
{
	public static readonly DependencyProperty VideoSourceProperty = DependencyProperty.Register("VideoSource", typeof(Iodo.Assets.RtspControl.Models.IVideoSource), typeof(RtspControlView), new FrameworkPropertyMetadata(OnVideoSourceChanged));

	public static readonly DependencyProperty FillColorProperty = DependencyProperty.Register("FillColor", typeof(System.Windows.Media.Color), typeof(RtspControlView), new FrameworkPropertyMetadata(DefaultFillColor, OnFillColorPropertyChanged));

	private static readonly System.Windows.Media.Color DefaultFillColor = Colors.Black;

	private static readonly TimeSpan ResizeHandleTimeout = TimeSpan.FromMilliseconds(500.0);

	private System.Windows.Media.Color _fillColor = DefaultFillColor;

	private WriteableBitmap _writeableBitmap;

	private int _width;

	private int _height;

	private Int32Rect _dirtyRect;

	private TransformParameters _transformParameters;

	private readonly Action<IDecodedVideoFrame> _invalidateAction;

	private Task _handleSizeChangedTask = Task.CompletedTask;

	private CancellationTokenSource _resizeCancellationTokenSource = new CancellationTokenSource();

	public Iodo.Assets.RtspControl.Models.IVideoSource VideoSource
	{
		get
		{
			return (Iodo.Assets.RtspControl.Models.IVideoSource)GetValue(VideoSourceProperty);
		}
		set
		{
			SetValue(VideoSourceProperty, value);
		}
	}

	public System.Windows.Media.Color FillColor
	{
		get
		{
			return (System.Windows.Media.Color)GetValue(FillColorProperty);
		}
		set
		{
			SetValue(FillColorProperty, value);
		}
	}

	public RtspControlView()
	{
		InitializeComponent();
		_invalidateAction = Invalidate;
	}

	protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint)
	{
		int newWidth = (int)constraint.Width;
		int newHeight = (int)constraint.Height;
		if (_width != newWidth || _height != newHeight)
		{
			_resizeCancellationTokenSource.Cancel();
			_resizeCancellationTokenSource = new CancellationTokenSource();
			_handleSizeChangedTask = _handleSizeChangedTask.ContinueWith((Task prev) => HandleSizeChangedAsync(newWidth, newHeight, _resizeCancellationTokenSource.Token));
		}
		return base.MeasureOverride(constraint);
	}

	private async Task HandleSizeChangedAsync(int width, int height, CancellationToken token)
	{
		try
		{
			await Task.Delay(ResizeHandleTimeout, token).ConfigureAwait(continueOnCapturedContext: false);
			Application.Current.Dispatcher.Invoke(delegate
			{
				ReinitializeBitmap(width, height);
			}, DispatcherPriority.Send, token);
		}
		catch (OperationCanceledException)
		{
		}
	}

	private void ReinitializeBitmap(int width, int height)
	{
		_width = width;
		_height = height;
		_dirtyRect = new Int32Rect(0, 0, width, height);
		_transformParameters = new TransformParameters(RectangleF.Empty, new System.Drawing.Size(_width, _height), ScalingPolicy.Stretch, Iodo.Assets.RtspControl.Entities.PixelFormat.Bgra32, ScalingQuality.FastBilinear);
		_writeableBitmap = new WriteableBitmap(width, height, ScreenInfo.DpiX, ScreenInfo.DpiY, PixelFormats.Pbgra32, null);
		RenderOptions.SetBitmapScalingMode(_writeableBitmap, BitmapScalingMode.NearestNeighbor);
		_writeableBitmap.Lock();
		try
		{
			UpdateBackgroundColor(_writeableBitmap.BackBuffer, _writeableBitmap.BackBufferStride);
			_writeableBitmap.AddDirtyRect(_dirtyRect);
		}
		finally
		{
			_writeableBitmap.Unlock();
		}
		ImageVideo.Source = _writeableBitmap;
	}

	private static void OnVideoSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		RtspControlView @object = (RtspControlView)d;
		if (e.OldValue is Iodo.Assets.RtspControl.Models.IVideoSource videoSource)
		{
			videoSource.FrameReceived -= @object.OnFrameReceived;
		}
		if (e.NewValue is Iodo.Assets.RtspControl.Models.IVideoSource videoSource2)
		{
			videoSource2.FrameReceived += @object.OnFrameReceived;
		}
	}

	private void OnFrameReceived(object sender, IDecodedVideoFrame decodedFrame)
	{
		Application.Current.Dispatcher.Invoke(_invalidateAction, DispatcherPriority.Send, decodedFrame);
	}

	private void Invalidate(IDecodedVideoFrame decodedVideoFrame)
	{
		if (_width == 0 || _height == 0)
		{
			return;
		}
		_writeableBitmap.Lock();
		try
		{
			decodedVideoFrame.TransformTo(_writeableBitmap.BackBuffer, _writeableBitmap.BackBufferStride, _transformParameters);
			_writeableBitmap.AddDirtyRect(_dirtyRect);
		}
		finally
		{
			_writeableBitmap.Unlock();
		}
	}

	private static void OnFillColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		RtspControlView rtspControlView = (RtspControlView)d;
		rtspControlView._fillColor = (System.Windows.Media.Color)e.NewValue;
	}

	private unsafe void UpdateBackgroundColor(IntPtr backBufferPtr, int backBufferStride)
	{
		byte* ptr = (byte*)(void*)backBufferPtr;
		int num = (_fillColor.A << 24) | (_fillColor.R << 16) | (_fillColor.G << 8) | _fillColor.B;
		Debug.Assert(ptr != null, "pixels != null");
		for (int i = 0; i < _height; i++)
		{
			for (int j = 0; j < _width; j++)
			{
				*(int*)(ptr + (nint)j * (nint)4) = num;
			}
			ptr += backBufferStride;
		}
	}
}
