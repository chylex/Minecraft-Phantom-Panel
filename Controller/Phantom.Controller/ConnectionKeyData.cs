using NetMQ;
using Phantom.Common.Data;

namespace Phantom.Controller;

readonly record struct ConnectionKeyData(NetMQCertificate Certificate, AuthToken AuthToken);
