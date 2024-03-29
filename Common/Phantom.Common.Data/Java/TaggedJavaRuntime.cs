﻿using MemoryPack;

namespace Phantom.Common.Data.Java;

[MemoryPackable(GenerateType.VersionTolerant)]
public sealed partial record TaggedJavaRuntime(
	[property: MemoryPackOrder(0)] Guid Guid,
	[property: MemoryPackOrder(1)] JavaRuntime Runtime
);
