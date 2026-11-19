using Pagamentos.Application.Abstraction;
using Pagamentos.Application.Messages;
using Pagamentos.Application.UseCases;
using Pagamentos.Domain.Entity;
using Pagamentos.Domain.Enumerator;
using Pagamentos.Infrastructure.Gateways;
using Pagamentos.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace Pagamentos.Tests
{
    public class FluxoPagamentoTests
    {
        private readonly RepositorioPagamentoEmMemoria _repositorio = new();
        private readonly PublicadorFalso _publicador = new();
        private readonly NotificadorFalso _notificador = new();

        private readonly IGatewayPagamento[] _gateways =
        [
            new GatewayPixSimulado(), new GatewayDebitoSimulado(), new GatewayCreditoSimulado()
        ];

        private CriarPagamentoHandler Criar() => new(_repositorio, _publicador);
        private ProcessarPagamentoHandler Processar() => new(_repositorio, _gateways, _publicador);
        private NotificarResultadoHandler Notificar() => new(_repositorio, _notificador);

        [Theory]
        [InlineData(MeioPagamento.Pix, null, 1, null)]
        [InlineData(MeioPagamento.CartaoDebito, null, 1, "4242424242424242")]
        [InlineData(MeioPagamento.CartaoCredito, TipoCartaoCredito.Padrao, 3, "4242424242424242")]
        [InlineData(MeioPagamento.CartaoCredito, TipoCartaoCredito.Premium, 10, "4242424242424242")]
        public async Task FluxoCompletoAprovaENotificaOCliente(
            MeioPagamento meio, TipoCartaoCredito? tipo, int parcelas, string? cartao)
        {
            var criado = await Criar().ExecutarAsync(new CriarPagamentoRequisicao(
                "Maria", "maria@x.com", 500m, meio, tipo, parcelas, cartao));

            await Processar().ExecutarAsync(new PagamentoSolicitadoMensagem(criado.Id));

            var processado = (PagamentoProcessadoMensagem)_publicador.Publicadas.Last().Mensagem;
            Assert.True(processado.Aprovado);

            await Notificar().ExecutarAsync(processado);

            Assert.Equal(StatusPagamento.Aprovado, (await _repositorio.ObterPorIdAsync(criado.Id))!.Status);
            Assert.Equal(1, _notificador.Aprovados);
            Assert.Equal(0, _notificador.Recusados);
        }

        [Fact]
        public async Task CartaoDeTeste0002ERecusadoEOClienteEAvisado()
        {
            var criado = await Criar().ExecutarAsync(new CriarPagamentoRequisicao(
                "Maria", "maria@x.com", 300m, MeioPagamento.CartaoCredito, TipoCartaoCredito.Padrao, 1, "4000000000000002"));

            await Processar().ExecutarAsync(new PagamentoSolicitadoMensagem(criado.Id));
            var processado = (PagamentoProcessadoMensagem)_publicador.Publicadas.Last().Mensagem;
            await Notificar().ExecutarAsync(processado);

            Assert.False(processado.Aprovado);
            Assert.Equal(StatusPagamento.Recusado, (await _repositorio.ObterPorIdAsync(criado.Id))!.Status);
            Assert.Equal(1, _notificador.Recusados);
        }

        [Fact]
        public async Task ProcessarAMesmaMensagemDuasVezesEIdempotente()
        {
            var criado = await Criar().ExecutarAsync(new CriarPagamentoRequisicao(
                "Maria", "maria@x.com", 100m, MeioPagamento.Pix));

            await Processar().ExecutarAsync(new PagamentoSolicitadoMensagem(criado.Id));
            await Processar().ExecutarAsync(new PagamentoSolicitadoMensagem(criado.Id));

            Assert.Single(_publicador.Publicadas, p => p.Fila == Filas.PagamentoProcessado);
        }

        [Fact]
        public async Task CreditoPadraoAcimaDoLimiteERecusado()
        {
            var criado = await Criar().ExecutarAsync(new CriarPagamentoRequisicao(
                "Maria", "maria@x.com", 6000m, MeioPagamento.CartaoCredito, TipoCartaoCredito.Padrao, 1, "4242424242424242"));

            await Processar().ExecutarAsync(new PagamentoSolicitadoMensagem(criado.Id));

            Assert.Equal(StatusPagamento.Recusado, (await _repositorio.ObterPorIdAsync(criado.Id))!.Status);
        }

        private sealed class PublicadorFalso : IPublicarMensagens
        {
            public List<(string Fila, object Mensagem)> Publicadas { get; } = [];

            public Task PublicarAsync<T>(string fila, T mensagem, CancellationToken ct = default)
            {
                Publicadas.Add((fila, mensagem!));
                return Task.CompletedTask;
            }
        }

        private sealed class NotificadorFalso : INotificadorCliente
        {
            public int Aprovados { get; private set; }
            public int Recusados { get; private set; }

            public Task NotificarPagamentoAprovadoAsync(Pagamento pagamento, CancellationToken ct = default)
            {
                Aprovados++;
                return Task.CompletedTask;
            }

            public Task NotificarPagamentoRecusadoAsync(Pagamento pagamento, CancellationToken ct = default)
            {
                Recusados++;
                return Task.CompletedTask;
            }
        }
    }
}
