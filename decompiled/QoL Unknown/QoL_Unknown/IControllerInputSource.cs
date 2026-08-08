namespace QoL_Unknown;

internal interface IControllerInputSource
{
	string SourceName { get; }

	bool TryRead(out RawControllerState rawState, out string deviceName);
}
