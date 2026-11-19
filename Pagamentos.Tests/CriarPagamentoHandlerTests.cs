using Pagamentos.Application.Abstraction;
using Pagamentos.Application.UseCases;
using Pagamentos.Application.Messages;
using Pagamentos.Domain.Entity;
using Pagamentos.Domain.Enumerator;
using Pagamentos.Domain.Exceptions;

namespace Pagamentos.Tests;

public class CriarPagamentoHandlerTests
{
    private readonly RepositorioFalso _repositorio = new();
    private readonly PublicadorFalso _publicador = new();

    private CriarPagamentoHandler CriarHandler() => new(_repositorio, _publicador);

    [Fact]
    public async Task CriarPagamentoGravaPendenteEPublicaNaFila()
    {
        var resposta = await CriarHandler().ExecutarAsync(new CriarPagamentoRequisicao(
            "Maria", "maria@x.com", 100m, MeioPagamento.Pix));

        Assert.Equal(StatusPagamento.Pendente, resposta.Status);

        var (fila, mensagem) = Assert.Single(_publicador.Publicadas);
        Assert.Equal(Filas.PagamentoSolicitado, fila);
        Assert.Equal(resposta.Id, ((PagamentoSolicitadoMensagem)mensagem).PagamentoId);
    }

    [Fact]
    public async Task CartaoGuardaApenasOsUltimos4Digitos()
    {
        var resposta = await CriarHandler().ExecutarAsync(new CriarPagamentoRequisicao(
            "Maria", "maria@x.com", 100m, MeioPagamento.CartaoDebito, NumeroCartao: "4242 4242 4242 4242"));

        var pagamento = await _repositorio.ObterPorIdAsync(resposta.Id);
        Assert.Equal("4242", pagamento!.CartaoUltimos4);
    }

    [Fact]
    public async Task NumeroDeCartaoInvalidoERejeitado()
    {
        await Assert.ThrowsAsync<ExcecaoDeDominio>(() => CriarHandler().ExecutarAsync(
            new CriarPagamentoRequisicao("Maria", "maria@x.com", 100m, MeioPagamento.CartaoDebito, NumeroCartao: "123")));
    }

    private sealed class RepositorioFalso : IRepositorioPagamento
    {
        private readonly Dictionary<Guid, Pagamento> _dados = new();

        public Task AdicionarAsync(Pagamento pagamento, CancellationToken ct = default)
        {
            _dados[pagamento.Id] = pagamento;
            return Task.CompletedTask;
        }

        public Task<Pagamento?> ObterPorIdAsync(Guid id, CancellationToken ct = default)
            => Task.FromResult(_dados.GetValueOrDefault(id));

        public Task AtualizarAsync(Pagamento pagamento, CancellationToken ct = default)
        {
            _dados[pagamento.Id] = pagamento;
            return Task.CompletedTask;
        }
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
}