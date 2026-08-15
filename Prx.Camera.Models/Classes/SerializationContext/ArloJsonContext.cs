using System.Text.Json.Serialization;
using Prx.Camera.Models.Records.ArloPayloads;

namespace Prx.Camera.Models.Classes.SerializationContext;

[JsonSerializable(typeof(ArloHandshakeRequest))]
[JsonSerializable(typeof(ArloHandshakeRequest[]))]
[JsonSerializable(typeof(ArloMotionEvent))]
[JsonSerializable(typeof(ArloMotionEvent[]))]
[JsonSerializable(typeof(ArloPing))]
[JsonSerializable(typeof(ArloPing[]))]
[JsonSerializable(typeof(ArloPong))]
[JsonSerializable(typeof(ArloPong[]))]
[JsonSerializable(typeof(ArloRequestSnapshotEvent))]
[JsonSerializable(typeof(ArloRequestSnapshotEvent[]))]
[JsonSerializable(typeof(ArloStartStreamEvent))]
[JsonSerializable(typeof(ArloStartStreamEvent[]))]
[JsonSerializable(typeof(ArloStatusEvent))]
[JsonSerializable(typeof(ArloStatusEvent[]))]
[JsonSerializable(typeof(ArloEventEnvelope))]
[JsonSerializable(typeof(ArloRegistrationAck))]
public sealed partial class ArloJsonContext : JsonSerializerContext;