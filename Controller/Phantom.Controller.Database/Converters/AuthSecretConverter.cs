using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Phantom.Utils.Rpc;

namespace Phantom.Controller.Database.Converters;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
sealed class AuthSecretConverter() : ValueConverter<AuthSecret, byte[]>(
	static units => units.Bytes.ToArray(),
	static value => new AuthSecret(ImmutableArray.Create(value))
);
