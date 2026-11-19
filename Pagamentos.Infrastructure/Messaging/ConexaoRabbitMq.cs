using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pagamentos.Infrastructure.Messaging
{
    public sealed class ConexaoRabbitMq : IAsyncDisposable
    {
        private readonly ConnectionFactory _fabrica;
        private readonly SemaphoreSlim _trava = new(1, 1);
        private IConnection? _conexao;

        public ConexaoRabbitMq(IOptions<OpcoesRabbitMq> opcoes)
        {
            var o = opcoes.Value;
            _fabrica = new ConnectionFactory
            {
                HostName = o.Host,
                Port = o.Porta,
                UserName = o.Usuario,
                Password = o.Senha,
                VirtualHost = o.VirtualHost,
                AutomaticRecoveryEnabled = true
            };
        }

        public async Task<IChannel> CriarCanalAsync(CancellationToken ct = default)
        {
            var conexao = await ObterConexaoAsync(ct);
            return await conexao.CreateChannelAsync(cancellationToken: ct);
        }

        private async Task<IConnection> ObterConexaoAsync(CancellationToken ct)
        {
            if (_conexao is { IsOpen: true })
                return _conexao;

            await _trava.WaitAsync(ct);
            try
            {
                if (_conexao is not { IsOpen: true })
                    _conexao = await _fabrica.CreateConnectionAsync(ct);

                return _conexao;
            }
            finally
            {
                _trava.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_conexao is not null)
                await _conexao.DisposeAsync();

            _trava.Dispose();
        }
    }
}
