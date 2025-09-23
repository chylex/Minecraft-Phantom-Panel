using Phantom.Utils.Rpc;
using Phantom.Utils.Rpc.Runtime.Tls;

namespace Phantom.Controller;

readonly record struct ConnectionKeyData(RpcServerCertificate Certificate, AuthToken AuthToken);
