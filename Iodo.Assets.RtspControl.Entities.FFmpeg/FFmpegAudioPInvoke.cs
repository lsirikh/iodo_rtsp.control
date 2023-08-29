using System;
using System.Runtime.InteropServices;

namespace Iodo.Assets.RtspControl.Entities.FFmpeg;

internal class FFmpegAudioPInvoke
{
	private const string LibraryName = "libffmpeghelper.dll";

	[DllImport("libffmpeghelper.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "create_audio_decoder")]
	public static extern int CreateAudioDecoder(FFmpegAudioCodecId audioCodecId, int bitsPerCodedSample, out IntPtr handle);

	[DllImport("libffmpeghelper.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "set_audio_decoder_extradata")]
	public static extern int SetAudioDecoderExtraData(IntPtr handle, IntPtr extradata, int extradataLength);

	[DllImport("libffmpeghelper.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "remove_audio_decoder")]
	public static extern void RemoveAudioDecoder(IntPtr handle);

	[DllImport("libffmpeghelper.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "decode_audio_frame")]
	public static extern int DecodeFrame(IntPtr handle, IntPtr rawBuffer, int rawBufferLength, out int sampleRate, out int bitsPerSample, out int channels);

	[DllImport("libffmpeghelper.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_decoded_audio_frame")]
	public static extern int GetDecodedFrame(IntPtr handle, out IntPtr outBuffer, out int outDataSize);

	[DllImport("libffmpeghelper.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "create_audio_resampler")]
	public static extern int CreateAudioResampler(IntPtr decoderHandle, int outSampleRate, int outBitsPerSample, int outChannels, out IntPtr handle);

	[DllImport("libffmpeghelper.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "resample_decoded_audio_frame")]
	public static extern int ResampleDecodedFrame(IntPtr decoderHandle, IntPtr resamplerHandle, out IntPtr outBuffer, out int outDataSize);

	[DllImport("libffmpeghelper.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "remove_audio_resampler")]
	public static extern void RemoveAudioResampler(IntPtr handle);
}
