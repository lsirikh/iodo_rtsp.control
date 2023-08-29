using System;
using System.Runtime.InteropServices;

namespace Iodo.Assets.RtspControl.Entities.FFmpeg;

internal static class FFmpegVideoPInvoke
{
	private const string LibraryName = "libffmpeghelper.dll";

	[DllImport("libffmpeghelper.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "create_video_decoder")]
	public static extern int CreateVideoDecoder(FFmpegVideoCodecId videoCodecId, out IntPtr handle);

	[DllImport("libffmpeghelper.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "remove_video_decoder")]
	public static extern void RemoveVideoDecoder(IntPtr handle);

	[DllImport("libffmpeghelper.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "set_video_decoder_extradata")]
	public static extern int SetVideoDecoderExtraData(IntPtr handle, IntPtr extradata, int extradataLength);

	[DllImport("libffmpeghelper.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "decode_video_frame")]
	public static extern int DecodeFrame(IntPtr handle, IntPtr rawBuffer, int rawBufferLength, out int frameWidth, out int frameHeight, out FFmpegPixelFormat framePixelFormat);

	[DllImport("libffmpeghelper.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "scale_decoded_video_frame")]
	public static extern int ScaleDecodedVideoFrame(IntPtr handle, IntPtr scalerHandle, IntPtr scaledBuffer, int scaledBufferStride);

	[DllImport("libffmpeghelper.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "create_video_scaler")]
	public static extern int CreateVideoScaler(int sourceLeft, int sourceTop, int sourceWidth, int sourceHeight, FFmpegPixelFormat sourcePixelFormat, int scaledWidth, int scaledHeight, FFmpegPixelFormat scaledPixelFormat, FFmpegScalingQuality qualityFlags, out IntPtr handle);

	[DllImport("libffmpeghelper.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "remove_video_scaler")]
	public static extern void RemoveVideoScaler(IntPtr handle);
}
