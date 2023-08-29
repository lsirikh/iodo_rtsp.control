using System;
using Iodo.Rtsp;

namespace Iodo.Assets.RtspControl.Models;

public interface IRtspControlModel
{
	IVideoSource VideoSource { get; }

	event EventHandler<string> StatusChanged;

	void Start(ConnectionParameters connectionParameters);

	void Stop();
}
