using System;
using System.Collections.Generic;
using System.Text;

namespace Pagamentos.Infrastructure.Messaging
{
    public sealed class OpcoesRabbitMq
    {
        public const string NomeSecao = "RabbitMq";
        public string Host { get; set; } = "localhost";
        public int Porta { get; set; } = 5672;
        public string Usuario { get; set; } = "guest";
        public string Senha { get; set; } = "guest";
        public string VirtualHost { get; set; } = "/";
    }
}
