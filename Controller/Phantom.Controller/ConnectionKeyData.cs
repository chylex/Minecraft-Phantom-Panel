using NetMQ;
using Phantom.Common.Data.Agent;

namespace Phantom.Controller;

readonly record struct ConnectionKeyData(NetMQCertificate Certificate, AuthToken AuthToken);
